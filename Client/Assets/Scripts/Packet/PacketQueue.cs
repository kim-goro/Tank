using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacketMessage
{
	public ushort Id { get; set; }
	public IPacket Message { get; set; }
}

/// <summary>
/// 멀티스레딩에서 받은 S_패킷을 유니티 메인스레드에서 동작하도록 패킷을 큐에 주고받음
/// </summary>
public class PacketQueue
{
	public static PacketQueue Instance { get; } = new PacketQueue();

	Queue<PacketMessage> _packetQueue = new Queue<PacketMessage>();
	object _lock = new object();

	public void Push(ushort id, IPacket packet)
	{
		lock (_lock)
		{
			_packetQueue.Enqueue(new PacketMessage() { Id = id, Message = packet });
		}
	}

	public PacketMessage Pop()
	{
		lock (_lock)
		{
			if (_packetQueue.Count == 0)
				return null;

			return _packetQueue.Dequeue();
		}
	}

	public List<PacketMessage> PopAll()
	{
		List<PacketMessage> list = new List<PacketMessage>();

		lock (_lock)
		{
			while (_packetQueue.Count > 0)
				list.Add(_packetQueue.Dequeue());
		}

		return list; // => 유니티 게임스레드 Update()에서 뽑아쓰도록 함
	}
}