using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public struct mapVoxelData
{
	public int objId;
	public int voxelType;
	public Vector3 cellPos;
	public Vector3 center;
	public Vector3 min;
	public Vector3 max;
	public float boundSize;
}

public class MapEditor
{
#if UNITY_EDITOR

	public static float cellSize = 1f;
	public static Vector3 pivoiPosition;

	// 위치할 메뉴 설정
	// 단축키 설정 % (Ctrl), # (Shift), & (Alt)
	[MenuItem("Tools/GenerateMap %#g")]
	private static void GenerateMap()
	{
		GenerateByPath("Assets/");
		GenerateByPath("../Common/MapData"); // Server 프로젝트에서 확인하기 위함
	}

	private static void GenerateByPath(string pathPrefix)
	{
		GameObject CompleteLevelArt = GameObject.Find("CompleteLevelArt").gameObject;
		MeshFilter mapArea = Util.FindChild<MeshFilter>(CompleteLevelArt, "mapAreaCube", true);

		// "txt가 아니라 binary데이터로 압축관리 할 수도 있음..."
		using (var writer = File.CreateText($"{pathPrefix }/{CompleteLevelArt.name}.txt"))
		{
			mapVoxelData[,,] curMapVoxelData = CreateMapVoxel(mapArea, cellSize, 8);

			writer.Write(cellSize);
			writer.WriteLine();
			writer.Write(pivoiPosition.x);
			writer.WriteLine();
			writer.Write(pivoiPosition.y);
			writer.WriteLine();
			writer.Write(pivoiPosition.z);
			writer.WriteLine();
			writer.Write(curMapVoxelData.GetLength(0));
			writer.WriteLine();
			writer.Write(curMapVoxelData.GetLength(1));
			writer.WriteLine();
			writer.Write(curMapVoxelData.GetLength(2));
			writer.WriteLine();
			writer.WriteLine();

			for (int z = 0; z < curMapVoxelData.GetLength(0); z++)
			{
				for (int y = 0; y < curMapVoxelData.GetLength(1); y++)
				{
					for (int x = 0; x < curMapVoxelData.GetLength(2); x++)
					{
						switch (curMapVoxelData[x, y, z].voxelType)
						{
							case 2:
								writer.Write("2"); // 충돌 타일
								break;
							default:
								writer.Write("0");
								break;
						}
					}
					writer.WriteLine();
				}
				writer.WriteLine();
			}
		}
	}

	private static mapVoxelData[,,] CreateMapVoxel(MeshFilter mapArea, float cellSize, LayerMask exceptionMask)
	{
		if (mapArea == null) { return null; }

		Bounds bounds = mapArea.sharedMesh.bounds;
		Vector3 v3Center = bounds.center;
		Vector3 v3Extents = bounds.extents;
		Vector3 v3FrontBottomLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);
		Vector3 v3BackTopRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);
		v3FrontBottomLeft = mapArea.transform.TransformPoint(v3FrontBottomLeft);
		v3BackTopRight = mapArea.transform.TransformPoint(v3BackTopRight);

		float distansceX = Mathf.Abs(v3BackTopRight.x - v3FrontBottomLeft.x);
		float distansceY = Mathf.Abs(v3BackTopRight.y - v3FrontBottomLeft.y);
		float distansceZ = Mathf.Abs(v3BackTopRight.z - v3FrontBottomLeft.z);

		pivoiPosition = v3FrontBottomLeft;
		mapVoxelData[,,] curMapVoxelData = new mapVoxelData[Mathf.Max((int)(distansceX / cellSize), 1), Mathf.Max((int)(distansceY / cellSize), 1), Mathf.Max((int)(distansceZ / cellSize), 1)];

		for (int x = 0; x < curMapVoxelData.GetLength(0); x++)
		{
			for (int y = 0; y < curMapVoxelData.GetLength(1); y++)
			{
				for (int z = 0; z < curMapVoxelData.GetLength(2); z++)
				{
					mapVoxelData voxelIndex = new mapVoxelData();
					voxelIndex.cellPos = new Vector3(x, y, z);
					voxelIndex.center = new Vector3(v3FrontBottomLeft.x + (cellSize / 2) + (cellSize * x),
						v3FrontBottomLeft.y + (cellSize / 2) + (cellSize * y),
						v3FrontBottomLeft.z + (cellSize / 2) + (cellSize * z));
					voxelIndex.min = voxelIndex.center - Vector3.one * (cellSize / 2);
					voxelIndex.max = voxelIndex.center + Vector3.one * (cellSize / 2);

					Collider[] hitColliders = Physics.OverlapBox(voxelIndex.center, Vector3.one * cellSize);
					voxelIndex.voxelType = 0;
					foreach (var index in hitColliders)
					{
						if (index.gameObject.layer != exceptionMask)
						{
							voxelIndex.voxelType = 2;
							break;
						}
					}
					curMapVoxelData[x, y, z] = voxelIndex;
				}
			}
		}
		Debug.Log("맵복셀 컴플리트");
		return curMapVoxelData;
	}

#endif
}
