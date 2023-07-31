using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Linq;
using UnityEngine;


public class NetworkManager
{
	public int SessionId = 0; // (접속한 세션의) 고유 Session Id

	ServerSession _session; // 세션

	public void Send(IPacket packet)
	{
		if(_session == null) { return; }
		_session.Send(packet);
	}

	public void Disconnect()
	{
		if (_session == null) { return; }
		C_LeaveGame movePacket = new C_LeaveGame();
		movePacket.objId = SessionId;
		Managers.Network.Send(movePacket);

		_session._sessionTCP.Disconnect();
	}

	public void Init()
	{
		string host = Dns.GetHostName();
		IPHostEntry ipHost = Dns.GetHostEntry(host);
		IPAddress ipAddr = ipHost.AddressList[1];
		IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777); 

		Connector connector = new Connector();
		connector.Connect(endPoint,
			(tcpSocket, udpSocket, resultEndPoint) => { _session = new ServerSession(tcpSocket, udpSocket, resultEndPoint); }, // 서버 세션 만든 후 연결시도
			1);
	}

	public void Update()
	{
		// *ClientPacketManager의 'CustomHandler()'를 큐에 모아놨다가 Unity 메인스레드에서 처리 
		List<PacketMessage> list = PacketQueue.Instance.PopAll();
		foreach (PacketMessage packet in list)
		{
			Action<ServerSession, IPacket> handler = PacketManager.Instance.GetPacketHandler(packet.Id);
			if (handler != null)
				handler.Invoke(_session, packet.Message);
		}
	}

	public static int FreeTcpPort()
	{
		TcpListener l = new TcpListener(IPAddress.Loopback, 0);
		l.Start();
		int port = ((IPEndPoint)l.LocalEndpoint).Port;
		l.Stop();
		return port;
	}
}