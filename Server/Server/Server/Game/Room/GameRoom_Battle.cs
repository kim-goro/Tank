using ServerCore;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static System.Collections.Specialized.BitVector32;
using Server.DB;

namespace Server.Game
{
	public partial class GameRoom : JobSerializer
	{
		/// <summary>
		/// 클라이언트에서 온 Tank 이동 조작 처리하기
		/// </summary>
		/// <param name="session"></param>
		/// <param name="movePacket"></param>
		public void HandleMove(ClientSession session, C_Move movePacket)
		{
			Tank playerTank = findPlayerTank(session);

			if (playerTank == null)
				return;

			playerTank.inputMovementValue.X = Math.Clamp(movePacket.inputForward, -1, 1);
			playerTank.inputMovementValue.Y = Math.Clamp(movePacket.rotateRight, -1, 1);
			playerTank.forwardDir.X = movePacket.forwardX;
			playerTank.forwardDir.Y = movePacket.forwardY;
			playerTank.forwardDir.Z = movePacket.forwardZ;
		}

		/// <summary>
		/// 클라이언트에서 온 Tank 발사 조작 처리하기
		/// </summary>
		/// <param name="session"></param>
		/// <param name="skillPacket"></param>
		public void HandleSkill(ClientSession session, C_Shoot skillPacket)
		{
			Tank playerTank = findPlayerTank(session);
			if (playerTank == null)
				return;

			if (!playerTank.IsShootable())
				return;

			Shell missle = ObjectManager.Instance.Add<Shell>();
			if (missle == null)
				return;

			missle.Init(session, this, playerTank, skillPacket);
			_missles.Add(missle.ObjId, missle);
			Map.ApplyEnter(missle.ObjId, missle);
			missle.Update();

            S_BroadcastSpawnMissle misslePacket = new S_BroadcastSpawnMissle();
			misslePacket.objId = missle.ObjId;
			misslePacket.posX = missle.curPos.X;
			misslePacket.posY = missle.curPos.Y;
			misslePacket.posZ = missle.curPos.Z;
			misslePacket.AngX = missle.curAng.X;
			misslePacket.AngY = missle.curAng.Y;
			misslePacket.ANgZ = missle.curAng.Z;
			Broadcast(misslePacket);
		}

		/// <summary>
		/// 미사일 오브젝트가 폭팔했을 때
		/// </summary>
		/// <param name="missle"></param>
		public void MissleBomb(Shell missle)
		{
			if (missle == null) { return; }

			foreach (var index in _players.Values)
			{
				float distanceBet = Vector3.Distance(index.curPos, missle.curPos);
				if (distanceBet <= missle.RANGE_SHELLBOMB)
				{
					// 범위 안에 Tank 오브젝트가 있다면
					float damageByDistance = missle.DAMAGE_SHELL_MAX * (missle.RANGE_SHELLBOMB - distanceBet) / missle.RANGE_SHELLBOMB;
					bool isDead = false;
					index.OnDamaged(damageByDistance, ref isDead); // 거리에 따라 반비례하여 데미지 주기
					if (isDead && index != missle.ownerTank)
					{
						// 미사일을 발사한 주체 Tank가 다른 Tank를 박살냈을 때 => killCount 갱신 
						DbTransaction.Instance.GetOrCreateSessionData(missle.ownerSession, this, true);
					}
				}
			}

			missle.Room = null;
			_missles.Remove(missle.ObjId);
			ObjectManager.Instance.Remove(missle.ObjId);
			Map.ApplyLeave(missle.ObjId);

			S_BroadcastDespawnMissle despawnPacket = new S_BroadcastDespawnMissle();
			despawnPacket.objId = missle.ObjId;
			Broadcast(despawnPacket);
		}

		/// <summary>
		/// DB 조회 결과(killCount)를 클라이언트에 적용함
		/// </summary>
		/// <param name="session"></param>
		/// <param name="rank"></param>
		public void UpdatePlayerRank(ClientSession session, int rank)
		{
			if (session == null) { return; }

			Tank playerTank = ObjectManager.Instance.Find(session);
			if (playerTank == null) { return; }

			S_BroadcastUpdateNewScore despawnPacket = new S_BroadcastUpdateNewScore();
			despawnPacket.objId = playerTank.ObjId;
			despawnPacket.score = rank;

			Broadcast(despawnPacket);
		}
	}
}
