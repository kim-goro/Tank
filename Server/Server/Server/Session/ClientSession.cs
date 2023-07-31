using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ServerCore;
using System.Net;
using Server.Game;
using System.Numerics;
using static System.Collections.Specialized.BitVector32;

namespace Server
{
	public partial class ClientSession
	{
		public int SessionId { get; set; } // 고유한 세션값
		public GameRoom Room { get; set; } // 접속한 게임방
		public SessionTCP _sessionTCP { get; set; }
		public SessionUDP _sessionUDP { get; set; } = null;

		object _lock = new object(); // 데이터 송신 목적 Queue 잠금용
		List<ArraySegment<byte>> _reserveQueue = new List<ArraySegment<byte>>(); // *플레이어에게 보낼 데이터들을 flush가 되기전까지 모아둠
		int _reservedSendBytes = 0; // 쌓은 패킷 데이터의 양
		long _lastSendTick = 0; // 다음 flush까지 시간

		bool isClientUdpPortSetted = false;
		int cllientUDPPort = 0;
		Socket _serverUdpSocket;
		EndPoint _udpEndPoint;

		public ClientSession(Socket TCPsocekt, Socket UDPsocekt, EndPoint endPoint)
		{
			// 게임방 입장과 퇴장은 TCP Socket을 기준으로
			_sessionTCP = new SessionTCP();
			_sessionTCP.OnRecvPacketOccur += (sender, buffer) => { OnRecvPacket(buffer); };
			_sessionTCP.OnConnectedOccur += (sender, endPoint) => { OnConnected(endPoint); };
			_sessionTCP.OnSendOccur += (sender, numOfBytes) => { OnSend(numOfBytes); };
			_sessionTCP.OnDisconnectedOccur += (sender, EndPoint) => { OnDisconnected(EndPoint); };
			_sessionTCP.Start(TCPsocekt);
			_sessionTCP.OnConnected(endPoint);

			this._serverUdpSocket = UDPsocekt;
			this._udpEndPoint = endPoint;
			CreateUDP();
		}

		/// <summary>
		/// 클라이언트에서 보내주는 UDP Port를 받아옴
		/// </summary>
		/// <param name="newPort"></param>
		public void BindUDPConnection(int newPort)
		{
			isClientUdpPortSetted = true;
			cllientUDPPort = newPort;
			CreateUDP();
		}

		/// <summary>
		/// UDP Session 생성 시도하기 (udp socket과 port가 모두 준비되었다면)
		/// </summary>
		void CreateUDP()
		{
			if (_serverUdpSocket == null) { return; }
			if (!isClientUdpPortSetted) { return; }

			_sessionUDP = new SessionUDP();
			_sessionUDP.OnRecvPacketOccur += (sender, buffer) => { OnRecvPacket(buffer); };
			_sessionUDP.sendPort = cllientUDPPort;
			_sessionUDP.Start(_serverUdpSocket);
			_sessionUDP.OnConnected(_udpEndPoint);
		}

		#region Network
		/// <summary>
		/// Send()할 버퍼 데이터 예약해두기 (Queue에 담아두기) 
		/// </summary>
		/// <param name="packet"></param>
		public void Send(IPacket packet) 
		{
			lock (_lock)
			{
				_reserveQueue.Add(packet.Write());
				_reservedSendBytes += _reserveQueue[_reserveQueue.Count - 1].Count;
			}
		}

		/// <summary>
		/// 예약하지 않고 바로 데이터를 보낸다 (UDP만 활용)
		/// </summary>
		/// <param name="packet"></param>
		public void DirectSend(IPacket packet)
		{
			if (_sessionUDP == null) { return; }
			_sessionUDP.Send(packet.Write());
		}

		/// <summary>
		/// 이 함수를 호출해주는 쪽이 NetworkTask 스레드가 될 것임
		/// 실제 Network IO 보내는 부분
		/// </summary>
		public void FlushSend()
		{
			List<ArraySegment<byte>> sendList = null;

			lock (_lock)
			{
				// 0.1초가 지났거나, 너무 패킷이 많이 모일 때 (1만 바이트) => flush 시작
				long delta = (System.Environment.TickCount64 - _lastSendTick);
				if (delta < 100 && _reservedSendBytes < 10000)
					return;

				// 패킷 모아 보내기
				_reservedSendBytes = 0;
				_lastSendTick = System.Environment.TickCount64;

				sendList = _reserveQueue;
				_reserveQueue = new List<ArraySegment<byte>>();
			}

			_sessionTCP.Send(sendList);
		}

		/// <summary>
		/// 세션 게임방 입장하기
		/// </summary>
		/// <param name="endPoint"></param>
		void OnConnected(EndPoint endPoint)
		{
			Console.WriteLine($"OnConnected : {endPoint}");

			GameRoom room = GameLogic.Instance.Find(1);
			if (room == null)
				return;

			Room = room;
			GameLogic.Instance.Push(() => { room.Push(room.EnterGameRoom, this); });
		}

		void OnRecvPacket(ArraySegment<byte> buffer)
		{
			PacketManager.Instance.OnRecvPacket(this, buffer);
		}

		/// <summary>
		/// 세션 게임방 퇴장하기
		/// </summary>
		/// <param name="endPoint"></param>
		void OnDisconnected(EndPoint endPoint)
		{
			Console.WriteLine($"OnDisconnected : {endPoint}");
			GameLogic.Instance.Push(() =>
			{
				GameRoom room = GameLogic.Instance.Find(1);
				room.Push(room.LeaveGame, this);
			});

			SessionManager.Instance.Remove(this);
		}

		void OnSend(int numOfBytes)
		{
		}
		#endregion
	}
}
