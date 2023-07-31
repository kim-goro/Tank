using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static System.Collections.Specialized.BitVector32;

namespace ServerCore
{
	public class Connector
	{
		Action<Socket, Socket, EndPoint> _sessionFactory;

		public static int clientUdpPort = 0; // 클라이언트의 UDP 포트

		/// <summary>
		/// 
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="sessionFactory">접속이 완료되면 실행할 함수</param>
		/// <param name="count">접속할 소켓의 수</param>
		public void Connect(IPEndPoint endPoint, Action<Socket, Socket, EndPoint> sessionFactory, int count = 1)
		{
			for (int i = 0; i < count; i++)
			{
				Socket _tcpSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				Socket _udpSocket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
				clientUdpPort = NetworkManager.FreeTcpPort();
				IPEndPoint udpBindPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), clientUdpPort);
				_udpSocket.Bind(udpBindPoint);
				_sessionFactory = sessionFactory;

				SocketAsyncEventArgs args = new SocketAsyncEventArgs();
				args.Completed += new EventHandler<SocketAsyncEventArgs>((sender, argss) => { OnConnectCompleted(sender, argss, _udpSocket);});
				args.RemoteEndPoint = endPoint; // 연결할 주소
				args.UserToken = _tcpSocket;

				RegisterConnect(args);

				// TEMP (클라이언트가 너무 몰리면 연결실패할 수 있으니까 중간에 쉬는 타임)
				Thread.Sleep(10);
			}
		}

		void RegisterConnect(SocketAsyncEventArgs args)
		{
			Socket socket = args.UserToken as Socket;
			if (socket == null)
				return;

			try
			{
				bool pending = socket.ConnectAsync(args);
				if (pending == false)
					OnConnectCompleted(null, args);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		void OnConnectCompleted(object sender, SocketAsyncEventArgs args, Socket udpSocket = null)
		{
			try
			{
				if (args.SocketError == SocketError.Success)
				{
					_sessionFactory?.Invoke(args.ConnectSocket, udpSocket, args.RemoteEndPoint);
				}
				else
				{
					Console.WriteLine($"OnConnectCompleted Fail: {args.SocketError}");
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}
	}
}
