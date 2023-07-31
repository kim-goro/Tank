using Server;
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

	Dictionary<ushort, Func<ClientSession, ArraySegment<byte>, IPacket>> _makeFunc = new Dictionary<ushort, Func<ClientSession, ArraySegment<byte>, IPacket>>(); // 패킷 조립
	Dictionary<ushort, Action<ClientSession, IPacket>> _handler = new Dictionary<ushort, Action<ClientSession, IPacket>>(); // 패킷 핸들러 연결
		
	/// <summary>
	/// 수신된 버퍼데이터를 패킷으로 조립했을 때
	/// 패킷 조립 => 핸들러 연결
	/// </summary>
	public void Register()
	{
		_makeFunc.Add((ushort)PacketID.C_LeaveGame, MakePacket<C_LeaveGame>);
		_handler.Add((ushort)PacketID.C_LeaveGame, PacketHandler.C_LeaveGameHandler);
		_makeFunc.Add((ushort)PacketID.C_Move, MakePacket<C_Move>);
		_handler.Add((ushort)PacketID.C_Move, PacketHandler.C_MoveHandler);
		_makeFunc.Add((ushort)PacketID.C_Shoot, MakePacket<C_Shoot>);
		_handler.Add((ushort)PacketID.C_Shoot, PacketHandler.C_ShootHandler);
		_makeFunc.Add((ushort)PacketID.C_ConnectUDPPort, MakePacket<C_ConnectUDPPort>);
		_handler.Add((ushort)PacketID.C_ConnectUDPPort, PacketHandler.C_ConnectUDPPortHandler);
	}

	public void OnRecvPacket(ClientSession session, ArraySegment<byte> buffer, Action<ClientSession, IPacket> onRecvCallback = null)
	{
		ushort count = 0;

		// 버퍼 데이터의 헤더를 파싱하여 조립, 핸들러 연결
		ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
		count += 2;
		ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
		count += 2;

		Func<ClientSession, ArraySegment<byte>, IPacket> func = null;
		if (_makeFunc.TryGetValue(id, out func))
		{
			IPacket packet = func.Invoke(session, buffer);
			if (onRecvCallback != null)
				onRecvCallback.Invoke(session, packet);
			else
				HandlePacket(session, packet);
		}
	}

	T MakePacket<T>(ClientSession session, ArraySegment<byte> buffer) where T : IPacket, new()
	{
		T pkt = new T();
		pkt.Read(buffer);
		return pkt;
	}

	public void HandlePacket(ClientSession session, IPacket packet)
	{
		Action<ClientSession, IPacket> action = null;
		if (_handler.TryGetValue(packet.Protocol, out action))
			action.Invoke(session, packet);
	}
}