using ServerCore;
using System;
using System.Collections.Generic;

public class PacketManager
{
	#region Singleton
	static PacketManager _instance = new PacketManager();
	public static PacketManager Instance { get { return _instance; } }
	#endregion

	PacketManager()
	{
		Register();
	}

	Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>> _makeFunc = new Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>>();
	Dictionary<ushort, Action<PacketSession, IPacket>> _handler = new Dictionary<ushort, Action<PacketSession, IPacket>>();
		
	public void Register()
	{
		_makeFunc.Add((ushort)PacketID.S_BroadcastEnter, MakePacket<S_BroadcastEnter>);
		_handler.Add((ushort)PacketID.S_BroadcastEnter, PacketHandler.S_BroadcastEnterHandler);
		_makeFunc.Add((ushort)PacketID.S_BroadcastLeaveGame, MakePacket<S_BroadcastLeaveGame>);
		_handler.Add((ushort)PacketID.S_BroadcastLeaveGame, PacketHandler.S_BroadcastLeaveGameHandler);
		_makeFunc.Add((ushort)PacketID.S_BroadcastMove, MakePacket<S_BroadcastMove>);
		_handler.Add((ushort)PacketID.S_BroadcastMove, PacketHandler.S_BroadcastMoveHandler);
		_makeFunc.Add((ushort)PacketID.S_BroadcastSpawnMissle, MakePacket<S_BroadcastSpawnMissle>);
		_handler.Add((ushort)PacketID.S_BroadcastSpawnMissle, PacketHandler.S_BroadcastSpawnMissleHandler);
		_makeFunc.Add((ushort)PacketID.S_BroadcastDespawnMissle, MakePacket<S_BroadcastDespawnMissle>);
		_handler.Add((ushort)PacketID.S_BroadcastDespawnMissle, PacketHandler.S_BroadcastDespawnMissleHandler);
		_makeFunc.Add((ushort)PacketID.S_BroadcastDamage, MakePacket<S_BroadcastDamage>);
		_handler.Add((ushort)PacketID.S_BroadcastDamage, PacketHandler.S_BroadcastDamageHandler);
		_makeFunc.Add((ushort)PacketID.S_BroadcastUpdateNewScore, MakePacket<S_BroadcastUpdateNewScore>);
		_handler.Add((ushort)PacketID.S_BroadcastUpdateNewScore, PacketHandler.S_BroadcastUpdateNewScoreHandler);

	}

	public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer, Action<PacketSession, IPacket> onRecvCallback = null)
	{
		ushort count = 0;

		ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
		count += 2;
		ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
		count += 2;

		Func<PacketSession, ArraySegment<byte>, IPacket> func = null;
		if (_makeFunc.TryGetValue(id, out func))
		{
			IPacket packet = func.Invoke(session, buffer);
			if (onRecvCallback != null)
				onRecvCallback.Invoke(session, packet);
			else
				HandlePacket(session, packet);
		}
	}

	T MakePacket<T>(PacketSession session, ArraySegment<byte> buffer) where T : IPacket, new()
	{
		T pkt = new T();
		pkt.Read(buffer);
		return pkt;
	}

	public void HandlePacket(PacketSession session, IPacket packet)
	{
		Action<PacketSession, IPacket> action = null;
		if (_handler.TryGetValue(packet.Protocol, out action))
			action.Invoke(session, packet);
	}
}