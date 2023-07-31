using Server.Game;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// 맵 복셀 좌표 데이터
/// </summary>
public struct mapVoxelData
{
	public int objId; // 복셀의 주체 오브젝트ID
	public int voxelType; // 복셀 타입 (충돌, 탱크 오브젝트 등)
	public Vector3 cellPos; // center의 복셀 위치
	public Vector3 center; // center의 world space 좌표
	public Vector3 min; // 오브젝트 collider bound의 최소 범위
	public Vector3 max; // 오브젝트 collider bound의 최대 범위
	public float boundSize; // 오브젝트 collider bound의 지름 범위
}

/// <summary>
/// 게임 오브젝트와 복셀 좌표 매핑
/// </summary>
public class voxelObject
{
	public BasicObject gameObject; // 주체 오브젝트
	public mapVoxelData curVoxelData; // 복셀 데이터
	public voxelObject(BasicObject gameObject)
	{
		this.gameObject = gameObject;
	}
}

namespace Server.Game
{
	public class Map
	{
		public static Map instance;
		public float cellSize; // map area를 어떤 size로 쪼갤건지
		Vector3 pivoiPosition; // map area를 어느 좌표를 (0,0)으로 두고 쪼갤건지
		mapVoxelData[,,]? curMapVoxelData; // map area의 복셀 데이터들
		bool initSucessed = false; // map 복셀 데이터를 load 되었는가?

		Dictionary<int, voxelObject> _players = new Dictionary<int, voxelObject>(); // map area에 배치된 Tank 오브젝트
		Dictionary<int, voxelObject> _missles = new Dictionary<int, voxelObject>(); // map area에 배치된 missle(shell) 오브젝트

		public int maxCellX { get { if (!initSucessed) { return 0; } return curMapVoxelData.GetLength(0); } }
		public int maxCellY { get { if (!initSucessed) { return 0; } return curMapVoxelData.GetLength(1); } }
		public int maxCellZ { get { if (!initSucessed) { return 0; } return curMapVoxelData.GetLength(2); } }

		public Map()
		{
			if (instance == null)
			{
				instance = this;
			}
		}

		/// <summary>
		/// map area에 배치될 오브젝트 추가
		/// 복셀 데이터화 => 충돌 여부를 확인하게끔 함
		/// </summary>
		/// <param name="ObjId"></param>
		/// <param name="gameObject"></param>
		public void ApplyEnter(int ObjId, BasicObject gameObject)
		{
			if (gameObject == null) { return; }

			voxelObject newVoxelObject = new voxelObject(gameObject);
			ObjectType objType = ObjectManager.GetObjectTypeById(ObjId);
			mapVoxelData newData = new mapVoxelData();
			newData.center = gameObject.curPos;
			if (objType == ObjectType.Tank)
			{
				if (_players.ContainsKey(ObjId)) { return; }
				_players.Add(ObjId, newVoxelObject);
				newData.boundSize = 0.5f; // coliider bound 사이즈 임의 지정
			}
			else if (objType == ObjectType.Missle)
			{
				if (_missles.ContainsKey(ObjId)) { return; }
				_missles.Add(ObjId, newVoxelObject);
				newData.boundSize = 0; // coliider bound 사이즈 임의 지정
			}
			newVoxelObject.curVoxelData = newData;
		}

		/// <summary>
		/// 오브젝트가 파괴되었을때, 세션이 나갔을 때 map area에서 삭제
		/// </summary>
		/// <param name="ObjId"></param>
		public void ApplyLeave(int ObjId)
		{
			if (!_players.ContainsKey(ObjId)) { return; }
			ObjectType objType = ObjectManager.GetObjectTypeById(ObjId);
			if (objType == ObjectType.Tank)
			{
				if (_players.ContainsKey(ObjId)) { return; }
				_players.Remove(ObjId);
			}
			else if (objType == ObjectType.Missle)
			{
				if (!_missles.ContainsKey(ObjId)) { return; }
				_missles.Remove(ObjId);
			}
		}

		/// <summary>
		/// 이동할 좌표로 복셀 데이터를 옮겨서 충돌체와 충돌하는지 확인
		/// 충돌 한다면 해당 좌표로 이동하지 않음 return false
		/// 충돌 하지 않는 다면 해당 좌표로 이동 return true
		/// </summary>
		/// <param name="ObjId"></param>
		/// <param name="nextPos"></param>
		/// <returns></returns>
		public bool ApplyMove(int ObjId, Vector3 nextPos)
		{
			voxelObject targetVoxelObject = null;
			ObjectType objType = ObjectManager.GetObjectTypeById(ObjId);
			Tank ownerTank = null;
			if (objType == ObjectType.Tank)
			{
				if (!_players.ContainsKey(ObjId)) { return true; }
				targetVoxelObject = _players[ObjId];
			}
			else if (objType == ObjectType.Missle)
			{
				if (!_missles.ContainsKey(ObjId)) { return true; }
				targetVoxelObject = _missles[ObjId];
				ownerTank = (_missles[ObjId].gameObject as Shell).ownerTank;
			}
			if (targetVoxelObject == null) { return true; }

			Vector3 nextCellPos = PosToVox(nextPos);
			Vector3 minBoundVoxel = new Vector3(nextCellPos.X - (int)targetVoxelObject.curVoxelData.boundSize, nextCellPos.Y - (int)targetVoxelObject.curVoxelData.boundSize, nextCellPos.Z - (int)targetVoxelObject.curVoxelData.boundSize);
			Vector3 maxBoundVoxel = new Vector3(nextCellPos.X + (int)targetVoxelObject.curVoxelData.boundSize, nextCellPos.Y + (int)targetVoxelObject.curVoxelData.boundSize, nextCellPos.Z + (int)targetVoxelObject.curVoxelData.boundSize);

			// map area 충돌체(벽, 건물 등) 복셀과 충돌하는지 검사
			bool isCollidedWithWall = false;
			for (int x = (int)MathF.Max(0, (int)minBoundVoxel.X); x <= MathF.Min((int)maxBoundVoxel.X, maxCellX - 1); x++)
			{
				for (int y = (int)MathF.Max(0, (int)minBoundVoxel.Y); y <= MathF.Min((int)maxBoundVoxel.Y, maxCellY - 1); y++)
				{
					for (int z = (int)MathF.Max(0, (int)minBoundVoxel.Z); z <= MathF.Min((int)maxBoundVoxel.Z, maxCellZ - 1); z++)
					{
						if (curMapVoxelData[x, y, z].voxelType == 2)
						{
							isCollidedWithWall = true;
							break;
						}
					}
					if (isCollidedWithWall) { break; }
				}
				if (isCollidedWithWall) { break; }
			}
			if (isCollidedWithWall) { return false; }

			// map area에 존재하는 다른 Tank 오브젝트와 충돌하는지 검사 
			bool isCollidedWithOtherTank = false;
			foreach (var index in _players.Values)
			{
				if (objType == ObjectType.Tank)
				{
					if (index == _players[ObjId]) { continue; }
				}
				else if (objType == ObjectType.Missle)
				{
					if (index.gameObject == ownerTank) { continue; }
				}
				Vector3 otherCurVoxel = PosToVox(index.gameObject.curPos);
				Vector3 otherMinBoundVoxel = new Vector3(otherCurVoxel.X - (int)index.curVoxelData.boundSize, otherCurVoxel.Y - (int)index.curVoxelData.boundSize, otherCurVoxel.Z - (int)index.curVoxelData.boundSize);
				Vector3 otherMaxBoundVoxel = new Vector3(otherCurVoxel.X + (int)index.curVoxelData.boundSize, otherCurVoxel.Y + (int)index.curVoxelData.boundSize, otherCurVoxel.Z + (int)index.curVoxelData.boundSize);
				isCollidedWithOtherTank = minBoundVoxel.X <= otherMaxBoundVoxel.X &&
		maxBoundVoxel.X >= otherMinBoundVoxel.X &&
		minBoundVoxel.Y <= otherMaxBoundVoxel.Y &&
		maxBoundVoxel.Y >= otherMinBoundVoxel.Y &&
		minBoundVoxel.Z <= otherMaxBoundVoxel.Z &&
		maxBoundVoxel.Z >= otherMinBoundVoxel.Z;
				if (isCollidedWithOtherTank) { break; }
			}
			if (isCollidedWithOtherTank) { return false; }
			return true;
		}

		/// <summary>
		/// world space 좌표를 map area을 기준으로 복셀 좌표로 변환
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		public Vector3 PosToVox(Vector3 pos)
		{
			Vector3 cellPos = (pos - pivoiPosition) / cellSize;
			Vector3 resultPos = new Vector3(MathF.Round(MathF.Abs(cellPos.X)), MathF.Round(MathF.Abs(cellPos.Y)), MathF.Round(MathF.Abs(cellPos.Z)));
			return resultPos;
		}

		/// <summary>
		/// map area 복셀 좌표를 world space 좌표로 변환
		/// </summary>
		/// <param name="vox"></param>
		/// <returns></returns>
		public Vector3 VoxToPos(Vector3 vox)
		{
			Vector3 resultPos = pivoiPosition + (vox * cellSize);
			return resultPos;
		}

		/// <summary>
		/// Unity에서 만든 map voxel 충돌 데이터(*.txt)를 긁어옴
		/// </summary>
		/// <param name="mapId"></param>
		/// <param name="pathPrefix"></param>
		public void LoadMap(string pathPrefix = "../../../../../../Common/MapData")
		{
			string mapName = "CompleteLevelArt";

			// Collision 관련 파일
			string text = File.ReadAllText($"{pathPrefix}/{mapName}.txt");
			StringReader reader = new StringReader(text);

			cellSize = float.Parse(reader.ReadLine());
			float pivotSizeX = float.Parse(reader.ReadLine());
			float pivotSizeY = float.Parse(reader.ReadLine());
			float pivotSizeZ = float.Parse(reader.ReadLine());
			pivoiPosition = new Vector3(pivotSizeX, pivotSizeY, pivotSizeZ);
			int sizeX = int.Parse(reader.ReadLine());
			int sizeY = int.Parse(reader.ReadLine());
			int sizeZ = int.Parse(reader.ReadLine());
			reader.ReadLine();

			curMapVoxelData = new mapVoxelData[sizeX, sizeY, sizeZ];

			for (int z = 0; z < sizeZ; z++)
			{
				for (int y = 0; y < sizeY; y++)
				{
					string testx = "";
					string line = reader.ReadLine();
					for (int x = 0; x < sizeX; x++)
					{
						curMapVoxelData[x, y, z].voxelType = int.Parse(line[x].ToString());
						curMapVoxelData[x, y, z].cellPos = new Vector3(x, y, z);
						curMapVoxelData[x, y, z].center = VoxToPos(curMapVoxelData[x, y, z].cellPos);
						testx += curMapVoxelData[x, y, z].voxelType.ToString();
					}
				}
				reader.ReadLine();
			}
			initSucessed = true;
		}
	}

}
