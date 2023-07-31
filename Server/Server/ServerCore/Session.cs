using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerCore
{
	/// <summary>
	/// TCP 송수신
	/// </summary>
	public class SessionTCP : PacketSession
	{
		protected override void RegisterSend()
		{
			if (_disconnected == 1)
				return;

			while (_sendQueue.Count > 0)
			{
				// 보낼 데이터 뽑아옴
				ArraySegment<byte> buff = _sendQueue.Dequeue(); 
				_pendingList.Add(buff);
			}
			_sendArgs.BufferList = _pendingList;

			try
			{
				bool pending = _socket.SendAsync(_sendArgs);
				if (pending == false)
					OnSendCompleted(null, _sendArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine($"RegisterSend Failed {e}");
			}
		}


		protected override void RegisterRecv()
		{
			if (_disconnected == 1)
				return;

			// Receive()할 데이터 공간 정의
			_recvBuffer.Clean();
			ArraySegment<byte> segment = _recvBuffer.WriteSegment;
			_recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count); // 버퍼의 범위를 표현(배열, 시작인덱스, 실제로 사용할 크기)

			try
			{
				bool pending = _socket.ReceiveAsync(_recvArgs);
				if (pending == false)
					OnRecvCompleted(null, _recvArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine($"RegisterRecv Failed {e}");
			}
		}
	}

	/// <summary>
	/// UDP 송수신
	/// </summary>
	public class SessionUDP : PacketSession
	{
		public int sendPort = 0; // *send()하기전에 우선적으로 설정해주어야 함
		protected override void RegisterSend()
		{
			if (_disconnected == 1)
				return;

			while (_sendQueue.Count > 0)
			{
				ArraySegment<byte> buff = _sendQueue.Dequeue();
				_pendingList.Add(buff);
			}
			_sendArgs.BufferList = _pendingList;
			_sendArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), sendPort);

			try
			{
				bool pending = _socket.SendToAsync(_sendArgs);
				if (pending == false)
					OnSendCompleted(null, _sendArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine($"RegisterSend Failed {e}");
			}
		}


		protected override void RegisterRecv()
		{
			if (_disconnected == 1)
				return;

			_recvBuffer.Clean();
			ArraySegment<byte> segment = _recvBuffer.WriteSegment;
			_recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);
			_recvArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), sendPort);

			try
			{
				bool pending = _socket.ReceiveFromAsync(_recvArgs);
				if (pending == false)
					OnRecvCompleted(null, _recvArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine($"RegisterRecv Failed {e}");
			}
		}
	}

	/// <summary>
	/// 수신된 버퍼데이터를 패킷으로 조립하는 과정
	/// </summary>
	public abstract class PacketSession : Session
	{
		// 패킷의 설계 => [size(2)][packetId(2)][ ... ]    [size(2)][packetId(2)][ ... ]
		public static readonly int HeaderSize = 2;

		/// <summary>
		/// 버퍼 데이터 조립하기
		/// </summary>
		/// <param name="buffer">asyncReceive()으로 부터 전송받은 버퍼데이터</param>
		public sealed override int OnRecv(ArraySegment<byte> buffer)
		{
			int processLen = 0; // 최종적으로 몇 바이트를 처리했는가?

			while (true)
			{
				// 최소한 헤더는 파싱할 수 있는지 확인
				if (buffer.Count < HeaderSize)
					break;

				// 패킷이 완전체로 도착했는지 확인
				ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset); 
				if (buffer.Count < dataSize) // 설계한 패킷 size보다 작은 데이터가 왔을 경우 => 아직 패킷으로 조립하기 이르다
					break;

				// 여기까지 왔으면 패킷 조립 가능
				OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));

				processLen += dataSize;
				buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
			}

			return processLen;
		}

		#region 이벤트 핸들러
		public System.EventHandler<ArraySegment<byte>> OnRecvPacketOccur;

		/// <summary>
		/// 쓰기 유효한 패킷이 들어왔을 때 처리
		/// </summary>
		/// <param name="buffer">온전한 패킷 데이터</param>
		public virtual void OnRecvPacket(ArraySegment<byte> buffer) { OnRecvPacketOccur?.Invoke(this, buffer); }

		public System.EventHandler<EndPoint> OnConnectedOccur;
		public System.EventHandler<int> OnSendOccur;
		public System.EventHandler<EndPoint> OnDisconnectedOccur;
		public override void OnConnected(EndPoint endPoint) { OnConnectedOccur?.Invoke(this, endPoint); }
		public override void OnSend(int numOfBytes) { OnSendOccur?.Invoke(this, numOfBytes); }
		public override void OnDisconnected(EndPoint endPoint) { OnDisconnectedOccur?.Invoke(this, endPoint); }
		#endregion
	}

	public abstract class Session
	{
		protected Socket _socket;
		protected int _disconnected = 0; // 소켓과 연결이 끊어졌는지 여부 ( 0 = 아직 안끊어짐, 1 = 이미 끊어짐);

		protected RecvBuffer _recvBuffer = new RecvBuffer(65535);
		protected object _lock = new object();
		protected Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>(); // 담고 있는 Send()할 데이터
		protected List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>(); // (flush() 중인) 남은 데이터들
		protected SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
		protected SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();
		public abstract void OnConnected(EndPoint endPoint);
		public abstract int OnRecv(ArraySegment<byte> buffer);
		public abstract void OnSend(int numOfBytes);
		public abstract void OnDisconnected(EndPoint endPoint);

		protected void Clear()
		{
			lock (_lock)
			{
				// *send() 일감들을 처리할 때는 순서가 보장되도록 Lock
				_sendQueue.Clear();
				_pendingList.Clear();
			}
		}

		public void Start(Socket socket)
		{
			_socket = socket;

			_recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted); // 소켓의 비동기 이벤트(Receive)이 감지되면 발동
			_sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted); // 소켓의 비동기 이벤트(Send)이 감지되면 발동

			RegisterRecv();
		}

		public void Send(List<ArraySegment<byte>> sendBuffList)
		{
			if (sendBuffList.Count == 0)
				return;

			lock (_lock)
			{
				foreach (ArraySegment<byte> sendBuff in sendBuffList)
					_sendQueue.Enqueue(sendBuff);

				if (_pendingList.Count == 0) // 전송 대기중인 리스트가 없다 
					RegisterSend(); // => 바로 send()
			}
		}

		public void Send(ArraySegment<byte> sendBuff)
		{
			lock (_lock)
			{
				_sendQueue.Enqueue(sendBuff);
				if (_pendingList.Count == 0)
					RegisterSend();
			}
		}

		public void Disconnect()
		{
			// 같은 소켓을 2번 연속으로 끊으면 문제가 발생함 => 한 번만 발생하도록 해야함
			if (Interlocked.Exchange(ref _disconnected, 1) == 1)
				return;

			OnDisconnected(_socket.RemoteEndPoint);
			_socket.Shutdown(SocketShutdown.Both);
			_socket.Close();
			Clear();
		}

		protected abstract void RegisterSend();
		protected void OnSendCompleted(object sender, SocketAsyncEventArgs args)
		{
			lock (_lock)
			{
				if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
				{
					try
					{
						_sendArgs.BufferList = null;
						_pendingList.Clear();

						OnSend(_sendArgs.BytesTransferred);

						if (_sendQueue.Count > 0) // send()를 처리하는 동안에 또다른 스레드가 send()한 경우 다시 send() 로직으로
							RegisterSend();
					}
					catch (Exception e)
					{
						Console.WriteLine($"OnSendCompleted Failed {e}");
					}
				}
				else
				{
					Disconnect();
				}
			}
		}
		protected abstract void RegisterRecv();
		protected void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
		{
			// args.BytesTransferred == 0 이면 접속이 끊어진 상태
			if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
			{
				try
				{
					// Write 커서 이동
					if (_recvBuffer.OnWrite(args.BytesTransferred) == false) // args.BytesTransferred : 수신받은 byte
					{
						// 작성 가능한 메모리를 Over했거나 오류가 난 상태 => 중단
						Disconnect();
						return;
					}

					// 수신 받은 버퍼 데이터를 조립가능한지 확인 => 가능하다면 패킷으로 콘텐츠 처리
					int processLen = OnRecv(_recvBuffer.ReadSegment);
					if (processLen < 0 || _recvBuffer.DataSize < processLen)
					{
						//  설계한 패킷과 맞지 않는 데이터가 들어왔을 때 => 중단
						Disconnect();
						return;
					}

					// Read 커서 이동
					if (_recvBuffer.OnRead(processLen) == false)
					{
						// r커서가 전체 데이터보다 클때 => 중단
						Disconnect();
						return;
					}

					RegisterRecv(); // 다시 Receive()로
				}
				catch (Exception e)
				{
					Console.WriteLine($"OnRecvCompleted Failed {e}");
				}
			}
			else
			{
				Disconnect();
			}
		}
	}
}
