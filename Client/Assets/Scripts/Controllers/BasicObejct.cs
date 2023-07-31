using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Complete;
using static Define;

public class BasicObject : Poolable
{
	[HideInInspector] public int ObjId = 1;
	protected bool m_Dead = false;

	public virtual void Init(int ObjId)
	{
		this.ObjId = ObjId;
	}

	public virtual void MoveTo(S_BroadcastMove inputPacket)
	{
		Vector3 nextPos = new Vector3(inputPacket.posX, inputPacket.posY, inputPacket.posZ);
		Vector3 nextRot = new Vector3(inputPacket.AngX, inputPacket.AngY, inputPacket.ANgZ);
		transform.position = new Vector3(inputPacket.posX, inputPacket.posY, inputPacket.posZ);
		transform.rotation = Quaternion.Euler(new Vector3(inputPacket.AngX, inputPacket.AngY, inputPacket.ANgZ));
	}

	public virtual void DestroySelf()
	{
		if (m_Dead) { return; }
		m_Dead = true;
	}

	protected virtual void FixedUpdate()
	{
	}
}
