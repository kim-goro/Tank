using ServerCore;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Server.Game
{
	public class Tank : BasicObject
	{
		readonly float VELOCITY_DRAG = 0.2f; // 직후진 가속도 저항값
		readonly float ANGULAR_DRAG = 0.1f; // 좌우회전 가속도 저항값
		readonly float MOVEMENT_SPEED = 2.5f; // 직후진 속도
		readonly float TURN_SPEED = 60f; // 좌우회전 속도

		public float hp;
		public Vector2 inputMovementValue; // 클라이언트에서 입력한 방향값
		float curVel = 0; // 현재 적용중인 Tank의 직후진 가속도
		float curVelAng = 0; // 현재 적용중인 Tank의 좌우회전 가속도

		readonly float COOLTIME_SHOOT = (float)1000 / 0.3f; // 발사 쿨타임
		float curLeftShootTime = 0f; // 현재 남은 쿨타임

		public Tank()
		{
			ObjectType = ObjectType.Tank;
		}

		public override void Init(ClientSession session, GameRoom room)
		{
			base.Init(session, room);
			hp = 100;
			curPos = new Vector3(0, 0, 0);
			curAng = new Vector3(0, 0, 0);
			curLeftShootTime = 0f;
		}

		public override void Update()
		{
            base.Update();
			if (Room == null) { return; }

			// 업데이트 주기 설정
			int tick = (int)(1000 / 60);
			Room.PushAfter(tick, Update);
			float TimeDelta = (float)60 / (float)1000;
			curLeftShootTime -= tick * 100; // 쿨타임 줄이기

			// 직후진 가속도 설정
			bool isMoveToBackward = Math.Sign(curVel) <= -1;
			curVel = curVel - (VELOCITY_DRAG + TimeDelta) * (isMoveToBackward ? -1 : 1);
			if (isMoveToBackward)
			{
				if (curVel > 0) { curVel = 0; }
			}
			else
			{
				if (curVel < 0) { curVel = 0; }
			}
			Vector3 movementValue = forwardDir * inputMovementValue.X * MOVEMENT_SPEED * TimeDelta;
			curVel = Math.Clamp(curVel + movementValue.Length() * Math.Sign(inputMovementValue.X), -5f, 5f);

			// 좌우회전 가속도 설정
            bool isTurnToLeftside = Math.Sign(curVelAng) <= -1;
			curVelAng = curVelAng - (ANGULAR_DRAG + TimeDelta) * (isTurnToLeftside ? -1 : 1);
			if (isTurnToLeftside)
			{
				if (curVelAng > 0) { curVelAng = 0; }
			}
			else
			{
				if (curVelAng < 0) { curVelAng = 0; }
			}
			float turnValue = inputMovementValue.Y * TURN_SPEED * TimeDelta;
			curVelAng = Math.Clamp(curVelAng + turnValue, -2, 2);

			// 이동할 다음 좌표를 적용할 수 있는지 확인
			Vector3 nextPos = curPos + forwardDir * curVel;
			if (Map.instance.ApplyMove(ObjId, nextPos))
			{
				curPos = nextPos;
			}
			curAng = new Vector3(0, (curAng.Y + curVelAng), 0);

			// 이동 패킷 전송
			S_BroadcastMove movePacket = new S_BroadcastMove();
			movePacket.objId = ObjId;
			movePacket.speed = TimeDelta;
			movePacket.posX = curPos.X; movePacket.posY = curPos.Y; movePacket.posZ = curPos.Z;
			movePacket.AngX = curAng.X; movePacket.AngY = curAng.Y; movePacket.ANgZ = curAng.Z;
			Room.BroadcastDirect(movePacket);
        }

		public virtual void OnDamaged(float damage, ref bool isDead)
		{
			GameRoom room = Room;
			if (room == null)
				return;

			hp = Math.Max(hp - damage, 0);

			S_BroadcastDamage changePacket = new S_BroadcastDamage();
			changePacket.objId = ObjId;
			changePacket.damage = damage;
			room.Push(room.Broadcast, changePacket);

			if (hp <= 0)
			{
				DestroySelf();
			}
		}

		/// <summary>
		/// 발사 가능한가?
		/// </summary>
		/// <returns></returns>
		public bool IsShootable()
		{
			bool isShootable = curLeftShootTime <= 0f;
			if(isShootable)
			{
				curLeftShootTime = COOLTIME_SHOOT;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Tank가 박살나면 다시 게임에 입장시켜라...
		/// </summary>
		public override void DestroySelf()
		{
			base.DestroySelf();

			GameRoom room = Room;
			if (room == null)
				return;

			if (ownerSession == null)
				return;

			room.Push(room.LeaveGame, ownerSession);
			room.Push(room.EnterGameRoom, ownerSession);
		}
	}
}
