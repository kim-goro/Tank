using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Server.DB;
using Server.Game;
using ServerCore;

namespace Server
{
	class Program
	{
		static Listener _listener = new Listener();

		#region Tasks
		static void GameLogicTask()
		{
			while (true)
			{
				GameLogic.Instance.Update();
				Thread.Sleep(0); // 잠시 내 실행권을 타인(커널)에게 양도하는 쉼표개념 => CPU가 너무 낭비되지 않도록 함
			}
		}

		static void DbTask()
		{
			while (true)
			{
				DbTransaction.Instance.Flush();
				Thread.Sleep(0);
			}
		}

		static void NetworkTask()
		{
			while (true)
			{
				List<ClientSession> sessions = SessionManager.Instance.GetSessions();
				foreach (ClientSession session in sessions)
				{
					session.FlushSend();
				}

				Thread.Sleep(0);
			}
		}
		#endregion

		static void Main(string[] args)
		{
			// 게임방 초기화
			GameLogic.Instance.Push(() => { GameLogic.Instance.Add(1); });

			// DNS (Domain Name System)
			string host = Dns.GetHostName();
			IPHostEntry ipHost = Dns.GetHostEntry(host);
			IPAddress ipAddr = ipHost.AddressList[1]; // IPv4
			IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777); // TCP

			// 리스너
			_listener.Init(endPoint, (tcpSocket, udpSocket, resultEndPoint) => { SessionManager.Instance.Generate(tcpSocket, udpSocket, resultEndPoint); });
			Console.WriteLine("Listening...");

			// NetworkTask : 패킷 버퍼 데이터 송신 담당 스레드
			{
				Thread t = new Thread(NetworkTask);
				t.Name = "NetworkTask";
				t.Start();
			}

			// DbTask : DB 입출력 담당 스레드
			DbTransaction.Instance.Init();
			{
				Thread t = new Thread(DbTask);
				t.Name = "DbTask";
				t.Start();
			}

			// GameLogic : 게임 로직을 수행하는 메인 스레드
			Thread.CurrentThread.Name = "GameLogic";
			GameLogicTask();
		}
	}
}