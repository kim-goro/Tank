using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using System.Net.Sockets;

public class ServerSession
{
	public static int SessionId;
	public SessionTCP _sessionTCP { get; set; }
	public SessionUDP _sessionUDP { get; set; }

	public void Send(IPacket packet)
	{
		_sessionTCP.Send(packet.Write());
	}

	public void DirectSend(IPacket packet)
	{
		_sessionUDP.Send(packet.Write());
	}

	public ServerSession(Socket tcpSocket, Socket udpSocket, EndPoint resultEndPoint)
	{
		_sessionTCP = new SessionTCP();
		_sessionTCP.OnRecvPacketOccur += (sender, buffer) => { OnRecvPacket(buffer); };
		_sessionTCP.OnConnectedOccur += (sender, endPoints) => { OnConnected(endPoints); };
		_sessionTCP.OnSendOccur += (sender, numOfBytes) => { OnSend(numOfBytes); };
		_sessionTCP.OnDisconnectedOccur += (sender, endPoints) => { OnDisconnected(endPoints); };
		_sessionTCP.Start(tcpSocket);
		_sessionTCP.OnConnected(resultEndPoint);

		// 클라이언트의 고유 UDP Port를 서버에 전송
		C_ConnectUDPPort newCP = new C_ConnectUDPPort();
		newCP.availablePort = Connector.clientUdpPort;
		Send(newCP);

		_sessionUDP = new SessionUDP();
		_sessionUDP.OnRecvPacketOccur += (sender, buffer) => { OnRecvPacket(buffer); };
		_sessionUDP.Start(udpSocket);
		_sessionUDP.OnConnected(resultEndPoint);
	}

	void OnConnected(EndPoint endPoint)
	{
		Debug.Log($"OnConnected : {endPoint}");

		// 패킷이 도착하면 각 세션 스레드에서 게임오브젝트에 접근하는 것이 아닌, 유니티 메인스레드에서 실행할 수 있도록 Queue담도록 함
		PacketManager.Instance.CustomHandler = (s, m, i) =>
		{
			PacketQueue.Instance.Push(i, m);
		};
	}

	void OnDisconnected(EndPoint endPoint)
	{
		Debug.Log($"OnDisconnected : {endPoint}");
	}

	void OnRecvPacket(ArraySegment<byte> buffer)
	{
		PacketManager.Instance.OnRecvPacket(this, buffer);
	}

	void OnSend(int numOfBytes)
	{
	}
}