using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Server.Game
{
	public class Shell : BasicObject
	{
		public Tank ownerTank { get; set; } // 발사한 주체
		readonly float LIFETIME_MAX = 2f; // 발사한 후 자동 삭제할 시간
		public readonly float DAMAGE_SHELL_MAX = 50; // 최대 데미지
		readonly float VELOCITY_DRAG = 0.2f; // 속도 저항값
		readonly float GRAVITY_DRAG = 9.8f; // 중력값
		public readonly float RANGE_SHELLBOMB = 4f; // 폭팔 범위
		float curVel = 0;

		public Shell()
		{
			ObjectType = ObjectType.Missle;
		}

		public virtual void Init(ClientSession session, GameRoom room, Tank ownerTank, C_Shoot packet)
		{
			Init(session, room);
			this.ownerTank = ownerTank;

			curPos = ownerTank.curPos + new Vector3(0, 0.5f, 0);
			curAng = ownerTank.curAng;
			forwardDir = ownerTank.forwardDir + new Vector3(0, 1f, 0);
			curVel = packet.power * 5;
		}

		public override void Update()
		{
			base.Update();
			if (Room == null)
				return;

			// 업데이트 주기 설정
			int tick = (int)(1000 / 60);
			Room.PushAfter(tick, Update);
			float TimeDelta = (float)60 / (float)1000;

			// 이동
			curVel -= VELOCITY_DRAG * TimeDelta;
			if (curVel < 0.3f) { curVel = 0.3f; }
			forwardDir = forwardDir - new Vector3(0, (GRAVITY_DRAG * TimeDelta * 0.25f), 0);
			Vector3 nextPos = curPos + forwardDir * curVel;

			if (Map.instance.ApplyMove(ObjId, nextPos) && nextPos.Y > 0)
			{
				// 이동 가능할 때
				curPos = nextPos;
				S_BroadcastMove movePacket = new S_BroadcastMove();
				movePacket.objId = ObjId;
				movePacket.speed = TimeDelta;
				movePacket.posX = curPos.X; movePacket.posY = curPos.Y; movePacket.posZ = curPos.Z;
				movePacket.AngX = curAng.X; movePacket.AngY = curAng.Y; movePacket.ANgZ = curAng.Z;
				Room.BroadcastDirect(movePacket);
			}
			else
			{
				// 부딫쳣을 때 = 폭팔
				DestroySelf();
				Room.Push(Room.MissleBomb, this);
			}
		}
	}
}
