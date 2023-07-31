using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
	/// <summary>
	/// Client Session 발급 및 보관/관리
	/// </summary>
	class SessionManager
	{
		static SessionManager _session = new SessionManager();
		public static SessionManager Instance { get { return _session; } }

		int _sessionId = 0;
		Dictionary<int, ClientSession> _sessions = new Dictionary<int, ClientSession>(); // 접속한 클라이언트 세션들
		object _lock = new object();

		public List<ClientSession> GetSessions()
		{
			List<ClientSession> sessions = new List<ClientSession>();

			lock (_lock)
			{
				sessions = _sessions.Values.ToList();
			}

			return sessions;
		}

		/// <summary>
		/// 클라이언트 세션 생성
		/// </summary>
		/// <param name="tcpSocket"></param>
		/// <param name="udpSocket"></param>
		/// <param name="resultEndPoint"></param>
		/// <returns></returns>
		public ClientSession Generate(Socket tcpSocket, Socket udpSocket, EndPoint resultEndPoint)
		{
			lock (_lock)
			{
				int sessionId = ++_sessionId;

				ClientSession session = new ClientSession(tcpSocket, udpSocket, resultEndPoint);
				session.SessionId = sessionId;
				_sessions.Add(sessionId, session);

				return session;
			}
		}

		public ClientSession Find(int id)
		{
			lock (_lock)
			{
				ClientSession session = null;
				_sessions.TryGetValue(id, out session);
				return session;
			}
		}

		public void Remove(ClientSession session)
		{
			lock (_lock)
			{
				_sessions.Remove(session.SessionId);
			}
		}
	}
}
