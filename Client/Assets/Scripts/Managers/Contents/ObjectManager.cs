using Complete;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ObjectType
{
	None,
	Tank,
	Missle
}

/// <summary>
///  인터렉팅 가능한 게임오브젝트들을 id로 관리함
/// </summary>
public class ObjectManager
{
	Dictionary<int, GameObject> _objects = new Dictionary<int, GameObject>();

	public static ObjectType GetObjectTypeById(int id)
	{
		int type = (id >> 24) & 0x7F;
		return (ObjectType)type;
	}

	public void Add(S_BroadcastEnter inputPacket, bool myPlayer = false)
	{
		if (_objects.ContainsKey(inputPacket.objId)) { return; }

		// 오브젝트 풀링
		GameObject go = Managers.Resource.Instantiate("CompleteTank_velocity");
		go.name = inputPacket.objId.ToString() + (myPlayer ? "Player" : "Enemy");
		TankMovement newTank = go.GetComponent<TankMovement>();
		newTank.Init(inputPacket);
		if (myPlayer) { 
			go.GetOrAddComponent<MyTankController>();
		}
		else
		{
			GameObject.DestroyImmediate(go.GetComponent<TankShooting>());
		}

		// 카메라 셋팅
		_objects.Add(inputPacket.objId, go);
		List<Transform> amountOfTanks = new List<Transform>();
		foreach(var index in _objects.Keys)
		{
			if (GetObjectTypeById(index) == ObjectType.Tank)
			{
				amountOfTanks.Add(_objects[index].transform);
			}
		}
		CameraControl.SetTankPlayerToFollow(amountOfTanks.ToArray());

		TankRenderer tr = go.GetComponent<TankRenderer>();
		if(tr != null)
		{
			tr.SetColor(myPlayer ? Color.blue : Color.red);
		}
	}

	public void Add(S_BroadcastSpawnMissle inputPacket)
	{
		if (_objects.ContainsKey(inputPacket.objId)) { return; }

		GameObject go = Managers.Resource.Instantiate("CompleteShell_velocity");
		go.name = inputPacket.objId.ToString();
		ShellExplosion newShell = go.GetComponent<ShellExplosion>();
		newShell.Init(inputPacket);
		_objects.Add(inputPacket.objId, go);
	}

	public void Remove(int id)
	{
		// 중복처리 방지
		if (_objects.ContainsKey(id) == false)
			return;

		GameObject go = FindById(id);
		if (go == null)
			return;

		_objects.Remove(id);
		List<Transform> amountOfTanks = new List<Transform>();
		foreach (var index in _objects.Keys)
		{
			if (GetObjectTypeById(index) == ObjectType.Tank)
			{
				amountOfTanks.Add(_objects[index].transform);
			}
		}
		
		// 풀링에 다시 집어넣음
		CameraControl.SetTankPlayerToFollow(amountOfTanks.ToArray());
		Managers.Resource.Destroy(go);
	}

	public GameObject FindById(int id)
	{
		GameObject go = null;
		_objects.TryGetValue(id, out go);
		return go;
	}
}
