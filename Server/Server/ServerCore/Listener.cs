using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
	public class Listener
	{
		Socket _listenSocket;
		Action<Socket, Socket, EndPoint> _sessionFactory; // 

		// TODO : 수정
		Socket _udpSocket;
		int _serverUdpPort = 7778;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="sessionFactory">Connect()가 되었을때 실행될 함수 등록 => 새로운 클라이언트 세션 생성하기</param>
		/// <param name="register">Accpet 이벤트 등록 수</param>
		/// <param name="backlog">최대 대기수</param>
		public void Init(IPEndPoint endPoint, Action<Socket, Socket, EndPoint> sessionFactory, int register = 10, int backlog = 100)
		{
			_sessionFactory += sessionFactory;

			_listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			_listenSocket.Bind(endPoint);
			_listenSocket.Listen(backlog);

			_udpSocket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			IPEndPoint udpBindPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7778);
			_udpSocket.Bind(udpBindPoint);

			for (int i = 0; i < register; i++)
			{
				SocketAsyncEventArgs args = new SocketAsyncEventArgs();
				//args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted); // 클라이언트의 Conenct를 감지하면 실행 => *또다른 스레드를 실행함
				args.Completed += new EventHandler<SocketAsyncEventArgs>((sender, args) => { OnAcceptCompleted(sender, args, _udpSocket); });
				RegisterAccept(args);
			}
		}

		void RegisterAccept(SocketAsyncEventArgs args)
		{
			args.AcceptSocket = null; // 재사용하기 위해 이전 소켓 데이터를 말끔히 지워줌

			try
			{
				// 비동기 Accept()
				bool pending = _listenSocket.AcceptAsync(args);
				if (pending == false) // 대기시간 없이 곧바로 Connect()가 되었을 경우
					OnAcceptCompleted(null, args);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		void OnAcceptCompleted(object sender, SocketAsyncEventArgs args, Socket udpSocket = null)
		{
			try
			{
				if (args.SocketError == SocketError.Success)
				{
                    _sessionFactory?.Invoke(args.AcceptSocket, udpSocket, args.AcceptSocket.RemoteEndPoint); 
				}
				else
					Console.WriteLine(args.SocketError.ToString());
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}

			RegisterAccept(args);
		}
	}
}
