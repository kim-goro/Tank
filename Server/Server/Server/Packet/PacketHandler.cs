using Server;
using Server.Game;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class PacketHandler
{
	/// <summary>
	/// 클라이언트가 종료했을때
	/// Unity 어플리케이션이 종료되었을 때 등
	/// </summary>
	/// <param name="session"></param>
	/// <param name="packet"></param>
	public static void C_LeaveGameHandler(ClientSession clientSession, IPacket packet)
	{
		C_LeaveGame inputPacket = (C_LeaveGame)packet;

		GameRoom room = clientSession.Room;
		if (room == null)
			return;

		room.Push(room.LeaveGame, clientSession);
	}

	/// <summary>
	/// 클라이언트에서 이동 패킷을 보냈을때
	/// Unity에서 Tank 조작 정보를 보냈을 때
	/// </summary>
	/// <param name="session"></param>
	/// <param name="packet"></param>
	public static void C_MoveHandler(ClientSession clientSession, IPacket packet)
	{
        C_Move inputPacket = (C_Move)packet;

        GameRoom room = clientSession.Room;
		if (room == null)
			return;

		room.Push(room.HandleMove, clientSession, inputPacket);
	}

	/// <summary>
	/// 클라이언트에서 발사 패킷을 보냈을때
	/// Unity에서 Tank 조작 정보를 보냈을 때
	/// </summary>
	/// <param name="session"></param>
	/// <param name="packet"></param>
	public static void C_ShootHandler(ClientSession clientSession, IPacket packet)
	{
		C_Shoot inputPacket = (C_Shoot)packet;

		GameRoom room = clientSession.Room;
		if (room == null)
			return;

		room.Push(room.HandleSkill, clientSession, inputPacket);
	}

	/// <summary>
	/// 클라이언트에서 알려주는 할당된 UDP Port
	/// </summary>
	/// <param name="clientSession"></param>
	/// <param name="packet"></param>
	public static void C_ConnectUDPPortHandler(ClientSession clientSession, IPacket packet)
	{
		C_ConnectUDPPort inputPacket = (C_ConnectUDPPort)packet;

		if (clientSession == null)
			return;

		clientSession.BindUDPConnection(inputPacket.availablePort);
		Console.WriteLine("inputPacket.availablePort" + inputPacket.availablePort);

	}
}