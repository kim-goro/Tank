using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

public enum ObjectType
{
	None,
	Tank,
	Missle
}

namespace Server.Game
{

	public class BasicObject
	{
		public int ObjId;
		public ObjectType ObjectType { get; protected set; } = ObjectType.None;
		public ClientSession? ownerSession { get; set; } // 주체 세션
		public GameRoom? Room { get; set; } // 참가중인 게임룸

		public Vector3 curPos; 
		public Vector3 curAng;
		public Vector3 forwardDir; // 3D객체의 정면 방향 (클라이언트에서 계산해서 받아옴)
		protected bool isDestroyed = false;

		public virtual void Init(ClientSession session, GameRoom room)
		{
			this.ownerSession = session;
			this.Room = room;
		}

		public virtual void Update() { if (isDestroyed) { return; } }
		public virtual void Init() { isDestroyed = false; }
		public virtual void DestroySelf() { isDestroyed = true; }
	}
}
