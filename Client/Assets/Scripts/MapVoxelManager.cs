using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using Complete;

struct mapVoxelData
{
	public int objId;
	public int voxelType;
	public Vector3 cellPos;
	public Vector3 center;
	public Vector3 min;
	public Vector3 max;
	public float boundSize;
}

/// <summary>
/// map area 복셀 데이터를 에디터에서 확인하기 위함
/// </summary>
public class MapVoxelManager : MonoBehaviour
{
	public static MapVoxelManager instance = null;
	 float cellSize;
	Vector3 pivoiPosition;
	mapVoxelData[,,] curMapVoxelData;
	bool initSucessed = false;
	Dictionary<BasicObject, int> showObjectBounds = new Dictionary<BasicObject, int>();

#if UNITY_EDITOR
	private void Awake()
	{
		instance = this;
		instance.LoadMap();
	}
#endif
	public static void AddObjet(BasicObject target)
	{
		if(instance == null) { return; }
		if(target as TankMovement)
		{
			instance.showObjectBounds.Add(target, 2);
		}
		else
		{
			instance.showObjectBounds.Add(target, 1);
		}
	}

	public Vector3 PosToVox(Vector3 pos)
	{
		Vector3 cellPos = (pos - pivoiPosition) / cellSize;
		Vector3 resultPos = new Vector3(Mathf.RoundToInt(Mathf.Abs(cellPos.x)), Mathf.RoundToInt(Mathf.Abs(cellPos.y)), Mathf.RoundToInt(Mathf.Abs(cellPos.z)));
		return resultPos;
	}

	public Vector3 VoxToPos(Vector3 vox)
	{
		Vector3 resultPos = pivoiPosition + (vox * cellSize);
		return resultPos;
	}

	public void LoadMap(string pathPrefix = "Assets/")
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

	void OnDrawGizmos()
	{
		if (!initSucessed) { return; }

		for (int x = 0; x < curMapVoxelData.GetLength(0); x++)
		{
			for (int y = 0; y < curMapVoxelData.GetLength(1); y++)
			{
				for (int z = 0; z < curMapVoxelData.GetLength(2); z++)
				{
					switch (curMapVoxelData[x, y, z].voxelType)
					{
						case 0:
							break;
						case 1:
							break;
						case 2:
							Gizmos.color = new Color(1, 0, 0, 0.5f);
							Gizmos.DrawCube(curMapVoxelData[x, y, z].center, Vector3.one * cellSize);
							break;
					}
				}
			}
		}

		bool checkMissing = false;
		BasicObject missingObject = null;
		while (!checkMissing)
		{
			missingObject = null;
			foreach (var index in showObjectBounds.Keys)
			{
				if (index == null)
				{
					missingObject = index;
				}
			}
			if(missingObject == null)
			{
				checkMissing = true;
			}
			else
			{
				showObjectBounds.Remove(missingObject);
			}
		}

		foreach (var index in showObjectBounds.Keys)
		{
			Vector3 curPos = index.transform.position;
			Vector3 curVox = PosToVox(curPos);
			Vector3Int curVoxInt = new Vector3Int((int)curVox.x, (int)curVox.y, (int)curVox.z);
			int bound = showObjectBounds[index];
			for (int x = curVoxInt.x - bound; x < curVoxInt.x + bound; x++)
			{
				for (int y = curVoxInt.y - bound; y < curVoxInt.y + bound; y++)
				{
					for (int z = curVoxInt.z - bound; z < curVoxInt.z + bound; z++)
					{
						Gizmos.color = new Color(0, 1, 0, 0.5f);
						Gizmos.DrawCube(VoxToPos(new Vector3(x,y,z)), Vector3.one * cellSize);
					}
				}
			}
		}

		// 	Gizmos.color = new Color(1, 0, 0, 0.5f);
		// 	Gizmos.DrawCube(mVoxelColider.min, Vector3.one * 0.5f);
		// 	Gizmos.DrawCube(mVoxelColider.max, Vector3.one * 0.5f);

		// 	Gizmos.color = new Color(0, 1, 0, 0.5f);
		// 	Gizmos.DrawCube(MapVoxelManager.instance.VoxToPos(mVoxelColider.cellPos), Vector3.one * 0.5f);
		// 	for (int x = Mathf.RoundToInt(mVoxelColider.min.x); x <= Mathf.RoundToInt(mVoxelColider.max.x); x++)
		// 	{
		// 		for (int y = Mathf.RoundToInt(mVoxelColider.min.y); y <= Mathf.RoundToInt(mVoxelColider.max.y); y++)
		// 		{
		// 			for (int z = Mathf.RoundToInt(mVoxelColider.min.z); z <= Mathf.RoundToInt(mVoxelColider.max.z); z++)
		// 			{
		// 				Gizmos.DrawCube(MapVoxelManager.instance.VoxToPos(MapVoxelManager.instance.PosToVox(new Vector3(x, y, z))), Vector3.one * 0.5f);
		// 			}
		// 		}
		// 	}

	}
}