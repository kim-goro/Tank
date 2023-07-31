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

	Dictionary<ushort, Func<ServerSession, ArraySegment<byte>, IPacket>> _makeFunc = new Dictionary<ushort, Func<ServerSession, ArraySegment<byte>, IPacket>>();
	Dictionary<ushort, Action<ServerSession, IPacket>> _handler = new Dictionary<ushort, Action<ServerSession, IPacket>>();

	public Action<ServerSession, IPacket, ushort> CustomHandler { get; set; } // *유니티 메인스레드에서 뽑아쓸수 있도록 별도의 Queue에 넣어주도록 유도

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

	public void OnRecvPacket(ServerSession session, ArraySegment<byte> buffer, Action<ServerSession, IPacket> onRecvCallback = null)
	{
		ushort count = 0;

		ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
		count += 2;
		ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
		count += 2;

		Func<ServerSession, ArraySegment<byte>, IPacket> func = null;
		if (_makeFunc.TryGetValue(id, out func))
		{
			IPacket packet = func.Invoke(session, buffer);
			if (CustomHandler != null)
			{
				CustomHandler.Invoke(session, packet, id);
			}
			else
			{
				if (onRecvCallback != null)
					onRecvCallback.Invoke(session, packet);
				else
				{
					Action<ServerSession, IPacket> action = null;
					if (_handler.TryGetValue(packet.Protocol, out action))
						action.Invoke(session, packet);
				}
			}
		}
	}

	T MakePacket<T>(ServerSession session, ArraySegment<byte> buffer) where T : IPacket, new()
	{
		T pkt = new T();
		pkt.Read(buffer);
		return pkt;
	}

	public Action<ServerSession, IPacket> GetPacketHandler(ushort id)
	{
		Action<ServerSession, IPacket> action = null;
		if (_handler.TryGetValue(id, out action))
			return action;
		return null;
	}
}