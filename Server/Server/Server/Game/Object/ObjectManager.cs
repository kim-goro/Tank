
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
	public class ObjectManager
	{
		public static ObjectManager Instance { get; } = new ObjectManager();

		object _lock = new object();
		Dictionary<int, Tank> _players = new Dictionary<int, Tank>();

		// [UNUSED(1)][TYPE(7)][ID(24)] => 비트플래그, int의 32bit를 쪼갬
		// 오브젝트 타입(TYPE)을 '포함하여' id 순번을 갱신함
		int _counter = 0;

		/// <summary>
		/// 오브젝트 추가, 오브젝트 ID 갱신
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T Add<T>() where T : BasicObject, new()
		{
			T gameObject = new T();

			lock (_lock)
			{
				gameObject.ObjId = GenerateId(gameObject.ObjectType);

				if (gameObject.ObjectType == ObjectType.Tank)
				{
					_players.Add(gameObject.ObjId, gameObject as Tank);
				}
			}

			return gameObject;
		}

		int GenerateId(ObjectType type)
		{
			lock (_lock)
			{
				// type은 int로 캐스팅해서 24bit만큼 밀어줌 => 반환되는 정보는 [UNUSED(1)](int)type][counter]
				return ((int)type << 24) | (_counter++);
			}
		}

		public static ObjectType GetObjectTypeById(int id)
		{
			// 뒷부분 [TYPE]만 반환
			int type = (id >> 24) & 0x7F;
			return (ObjectType)type;
		}

		public bool Remove(int objectId)
		{
			ObjectType objectType = GetObjectTypeById(objectId);

			lock (_lock)
			{
				if (objectType == ObjectType.Tank)
					return _players.Remove(objectId);
			}

			return false;
		}

		public Tank Find(int objectId)
		{
			ObjectType objectType = GetObjectTypeById(objectId);

			lock (_lock)
			{
				if (objectType == ObjectType.Tank)
				{
					Tank player = null;
					if (_players.TryGetValue(objectId, out player))
						return player;
				}
			}

			return null;
		}

		public Tank Find(ClientSession serssion)
		{
			lock (_lock)
			{
				foreach(var index in  _players.Values)
				{
					if (serssion == index.ownerSession)
					{
						return index;
					}
				}
			}
			return null;
		}
	}
}
