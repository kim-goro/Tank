using ServerCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Complete;

class PacketHandler
{
	/// <summary>
	/// 플레이어의 Tank 인스턴스를 생성하라
	/// </summary>
	/// <param name="serverSession"></param>
	/// <param name="packet"></param>
	public static void S_BroadcastEnterHandler(ServerSession serverSession, IPacket packet)
	{
		S_BroadcastEnter inputPacket = (S_BroadcastEnter)packet;

		Managers.Network.SessionId = inputPacket.sessionId;
		Managers.Object.Add(inputPacket, myPlayer: inputPacket.isPlayer);
	}

	/// <summary>
	/// 플레이어의 Tank 인스턴스를 삭제하라
	/// </summary>
	/// <param name="serverSession"></param>
	/// <param name="packet"></param>
	public static void S_BroadcastLeaveGameHandler(ServerSession serverSession, IPacket packet)
	{
		S_BroadcastLeaveGame inputPacket = (S_BroadcastLeaveGame)packet;

		GameObject go = Managers.Object.FindById(inputPacket.objId);
		if (go == null)
			return;

		TankMovement se = go.GetComponent<TankMovement>();
		if (se != null)
		{
			se.DestroySelf();
		}

		Managers.Object.Remove(inputPacket.objId);
	}

	/// <summary>
	/// 서버에서 연산처리 결과 이동 패킷을 적용하라
	/// </summary>
	/// <param name="serverSession"></param>
	/// <param name="packet"></param>
	public static void S_BroadcastMoveHandler(ServerSession serverSession, IPacket packet)
	{
		S_BroadcastMove inputPacket = (S_BroadcastMove)packet;

		GameObject go = Managers.Object.FindById(inputPacket.objId);
		if (go == null)
			return;

		BasicObject bo = go.GetComponent<BasicObject>();
		if (bo == null)
			return;

		bo.MoveTo(inputPacket);
	}

	/// <summary>
	/// 미사일 인스턴스를 생성하라
	/// </summary>
	/// <param name="serverSession"></param>
	/// <param name="packet"></param>
	public static void S_BroadcastSpawnMissleHandler(ServerSession serverSession, IPacket packet)
	{
		S_BroadcastSpawnMissle inputPacket = (S_BroadcastSpawnMissle)packet;

		Managers.Object.Add(inputPacket);
	}

	/// <summary>
	/// 미사일 인스턴스를 삭제하라
	/// </summary>
	/// <param name="serverSession"></param>
	/// <param name="packet"></param>
	public static void S_BroadcastDespawnMissleHandler(ServerSession serverSession, IPacket packet)
	{
		S_BroadcastDespawnMissle inputPacket = (S_BroadcastDespawnMissle)packet;

		GameObject go = Managers.Object.FindById(inputPacket.objId);
		if (go == null)
			return;

		ShellExplosion se = go.GetComponent<ShellExplosion>();
		if (se != null)
		{
			se.DestroySelf();
		}

		Managers.Object.Remove(inputPacket.objId);
	}

	/// <summary>
	/// Damage를 Tank에 적용하라
	/// </summary>
	/// <param name="serverSession"></param>
	/// <param name="packet"></param>
	public static void S_BroadcastDamageHandler(ServerSession serverSession, IPacket packet)
	{
		S_BroadcastDamage inputPacket = (S_BroadcastDamage)packet;

		GameObject go = Managers.Object.FindById(inputPacket.objId);
		if (go == null)
			return;

		TankMovement cc = go.GetComponent<TankMovement>();
		if (cc == null)
			return;

		cc.OnDamaged(inputPacket.damage);
	}

	/// <summary>
	/// 각 Session의 Tank 위로 표시된 killCount를 수정하라
	/// </summary>
	/// <param name="serverSession"></param>
	/// <param name="packet"></param>
	public static void S_BroadcastUpdateNewScoreHandler(ServerSession serverSession, IPacket packet)
	{
		S_BroadcastUpdateNewScore inputPacket = (S_BroadcastUpdateNewScore)packet;

		GameObject go = Managers.Object.FindById(inputPacket.objId);
		if (go == null)
			return;

		TankMovement cc = go.GetComponent<TankMovement>();
		if (cc == null)
			return;

		cc.SetKillCountUI(inputPacket.score);
	}
}

