using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Server.Game;
using System.Data.Common;
using System.Numerics;
using ServerCore;

namespace Server.DB
{
	public class DbTransaction : JobSerializer
	{
		public static DbTransaction Instance { get; } = new DbTransaction();

		// 로컬 sql Server 연결 스트링
		string constring = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=RankingDB;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False";

		public void Init()
		{
			DropAndCreateTable();
		}

		public void GetOrCreateSessionData(ClientSession session, GameRoom room, bool increamnet = false)
		{
			Instance.Push<ClientSession, GameRoom, bool>(UpdateOrInsertSession, session, room, increamnet);
		}

		/// <summary>
		/// Tank를 박살낸 플레이어의 SessionId로 killcount를 업데이트
		/// SessionId가 있다면 UPDATE 없다면 INSERT
		/// </summary>
		/// <param name="session"></param>
		/// <param name="room"></param>
		/// <param name="isIncrement"></param>
		void UpdateOrInsertSession(ClientSession session, GameRoom room, bool isIncrement = false)
		{
			if (session == null || room == null)
				return;

			using (SqlConnection Conn = new SqlConnection(constring))
			{
				try
				{
					Conn.Open();
					try
					{
						SqlDataReader dr1;
						SqlCommand cmd = new SqlCommand($" MERGE INTO accounts USING(SELECT 1 AS DUM) X ON(sessionId = {session.SessionId}) WHEN NOT MATCHED THEN INSERT (sessionId, amountKill) VALUES ({session.SessionId}, {0});", Conn);
						dr1 = cmd.ExecuteReader();
						dr1.Read();
						dr1.Close();

						SqlDataReader dr2;
						cmd = new SqlCommand($"SELECT * FROM accounts WHERE sessionId = {session.SessionId}", Conn);
						dr2 = cmd.ExecuteReader();
						dr2.Read();
						int rseultValue = dr2.GetInt32(2);
						dr2.Close();

						if (isIncrement)
						{
							rseultValue++;
							SqlDataReader dr3;
							cmd = new SqlCommand($" MERGE INTO accounts USING(SELECT 1 AS DUM) X ON(sessionId = {session.SessionId}) WHEN MATCHED THEN UPDATE SET amountKill = {rseultValue} WHEN NOT MATCHED THEN INSERT (sessionId, amountKill) VALUES ({session.SessionId}, {rseultValue});", Conn);
							dr3 = cmd.ExecuteReader();
							dr3.Close();
						}
						room.UpdatePlayerRank(session, rseultValue);
					}
					catch
					{
						Console.WriteLine("UpdateOrInsertSession Error");
					}
					Conn.Close();
				}
				catch (Exception DE)
				{
					Console.WriteLine(DE.Message);
					Console.WriteLine(DE.Source);
					Console.WriteLine(DE.StackTrace);
				}
			}
		}

		/// <summary>
		/// account 테이블 삭제하고 다시 만들기
		/// SessionId를 키값으로 killCounts를 찾을 수 있도록 함
		/// </summary>
		void DropAndCreateTable()
		{
			// Me (GameRoom)
			using (SqlConnection Conn = new SqlConnection(constring))
			{
				try
				{
					Conn.Open();
					try
					{
						SqlDataReader dr;
						SqlCommand cmd = new SqlCommand("DROP TABLE IF EXISTS accounts", Conn);
						dr = cmd.ExecuteReader();
						dr.Close();

						SqlDataReader dr2;
						cmd = new SqlCommand(@"CREATE TABLE accounts(
	accountID INTEGER NOT NULL IDENTITY (1, 1),
	sessionId INTEGER UNIQUE  NOT NULL,
	amountKill INTEGER NOT NULL,
);", Conn);
						dr2 = cmd.ExecuteReader();
						dr2.Close();

					}
					catch
					{
						Console.WriteLine("DB Init Error");
					}
					Conn.Close();
				}
				catch (Exception DE)
				{
					Console.WriteLine(DE.Message);
					Console.WriteLine(DE.Source);
					Console.WriteLine(DE.StackTrace);
				}
			}
		}
	}
}
