using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;
using Server.DB;

namespace Server.Game
{
	/// <summary>
	/// 게임방
	/// </summary>
	public partial class GameRoom : JobSerializer
	{
		public int RoomId { get; set; } // 방고유 Id
		public Map Map { get; private set; } = new Map();

		Dictionary<int, Tank> _players = new Dictionary<int, Tank>(); // 방에사 연산처리하는 Tank 들
		Dictionary<int, Shell> _missles = new Dictionary<int, Shell>(); // 방에서 연산처리하는 Missle 들

		public void Init()
		{
			Map.LoadMap();
		}

		public void Update()
		{
			// 게임 로직 Queue
			Flush();
		}

		/// <summary>
		/// 클라이언트와 TCP 연결이 성공적이면 플레이어의 Tank 입장
		/// </summary>
		/// <param name="session"></param>
		public void EnterGameRoom(ClientSession session)
		{
			Console.WriteLine($"EnterGameRoom : {session.SessionId}");

			session.Room = this;
			Tank newPlayerTank = ObjectManager.Instance.Add<Tank>();
			newPlayerTank.Init(session, this);
			_players.Add(newPlayerTank.ObjId, newPlayerTank);
			Map.ApplyEnter(newPlayerTank.ObjId, newPlayerTank);

			// map area 빈 공간중, 랜덤 스폰 위치를 가져온다 
            Random _rand = new Random();
			bool isVerifiedSpawn = false;
			int insideBoundary = 10;
			do
			{
				Vector3 newRandomVox = new Vector3(_rand.Next(insideBoundary, Map.maxCellX- insideBoundary), 0, _rand.Next(insideBoundary, Map.maxCellZ- insideBoundary));
				newPlayerTank.curPos = Map.VoxToPos(newRandomVox);
				if (Map.ApplyMove(newPlayerTank.ObjId, newPlayerTank.curPos))
				{
					isVerifiedSpawn = true;
					break;
				}
				Thread.Sleep(0);
			} while (!isVerifiedSpawn);
			newPlayerTank.curPos = new Vector3(newPlayerTank.curPos.X, 0, newPlayerTank.curPos.Z);

			// 기존에 입장해있던 Tank 정보를 알려준다
			List<S_BroadcastEnter> otherTankList = new List<S_BroadcastEnter>();
			foreach(var index in _players.Values)
			{
				S_BroadcastEnter otherPlayerPacket = new S_BroadcastEnter();
				otherPlayerPacket.objId = index.ObjId;
				otherPlayerPacket.sessionId = index.ownerSession.SessionId;
				otherPlayerPacket.isPlayer = newPlayerTank == index ? true : false;
				otherPlayerPacket.hp = index.hp;
				otherPlayerPacket.posX = index.curPos.X;
				otherPlayerPacket.posY = index.curPos.Y;
				otherPlayerPacket.posZ = index.curPos.Z;
				otherPlayerPacket.angX = index.curAng.X;
				otherPlayerPacket.angY = index.curAng.Y;
				otherPlayerPacket.angZ = index.curAng.Z;
				otherTankList.Add(otherPlayerPacket);
				session.Send(otherPlayerPacket);
			}

			// 다른 클라이언트에게 새로 입장한 Tank 정보를 알려준다
			S_BroadcastEnter enterPacket = new S_BroadcastEnter();
			enterPacket.objId = newPlayerTank.ObjId;
			enterPacket.hp = newPlayerTank.hp;
			enterPacket.sessionId = newPlayerTank.ownerSession.SessionId;
			enterPacket.isPlayer = false;
			enterPacket.posX = newPlayerTank.curPos.X;
			enterPacket.posY = newPlayerTank.curPos.Y;
			enterPacket.posZ = newPlayerTank.curPos.Z;
			enterPacket.angX = newPlayerTank.curAng.X;
			enterPacket.angY = newPlayerTank.curAng.Y;
			enterPacket.angZ = newPlayerTank.curAng.Z;
			Push(Broadcast, enterPacket);

			newPlayerTank.Update();
			DbTransaction.Instance.GetOrCreateSessionData(session, this);
		}

		/// <summary>
		/// 플레이어 퇴장
		/// 플레이어의 Tank을 게임룸에서 삭제
		/// </summary>
		/// <param name="session"></param>
		public void LeaveGame(ClientSession session)
		{
			Tank playerTank = ObjectManager.Instance.Find(session);
			if (playerTank == null) { return; }

			session.Room = null;
			playerTank.Room = null;
			_players.Remove(playerTank.ObjId);
			ObjectManager.Instance.Remove(playerTank.ObjId);
			Map.ApplyLeave(playerTank.ObjId);

			S_BroadcastLeaveGame despawnPacket = new S_BroadcastLeaveGame();
			despawnPacket.objId = playerTank.ObjId;
			Push(Broadcast, despawnPacket);
		}

		/// <summary>
		/// TCP로 브로드캐스트
		/// 플레이어 입장, 퇴장, 파괴 등 순서 보장 등이 필요한 경우
		/// </summary>
		/// <param name="packet"></param>
		public void Broadcast(IPacket packet)
		{
			foreach (Tank p in _players.Values) 
			{
				p.ownerSession.Send(packet);
			}
		}

		/// <summary>
		/// UDP로 브로드캐스트
		/// 움직임 등 그다지 보장이 필요하지 않는 경우
		/// </summary>
		/// <param name="packet"></param>
		public void BroadcastDirect(IPacket packet)
		{
			foreach (Tank p in _players.Values)
			{
				p.ownerSession.DirectSend(packet);
			}
		}

		public Tank findPlayerTank(ClientSession session)
		{
			foreach(var index in _players.Values)
			{
				if(index.ownerSession == session)
				{
					return index;
				}
			}
			return null;
		}
	}
}
