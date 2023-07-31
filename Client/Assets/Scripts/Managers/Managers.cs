using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Managers : MonoBehaviour
{
    static Managers s_instance; // 유일성이 보장된다
    public static Managers Instance { get { Init(); return s_instance; } } // 유일한 매니저를 갖고온다

    #region Contents
    ObjectManager _obj = new ObjectManager();
    NetworkManager _network = new NetworkManager();
    public static ObjectManager Object { get { return Instance._obj; } }
    public static NetworkManager Network { get { return Instance._network; } }
    #endregion

    #region Core
    PoolManager _pool = new PoolManager();
    ResourceManager _resource = new ResourceManager();

    public static PoolManager Pool { get { return Instance._pool; } }
    public static ResourceManager Resource { get { return Instance._resource; } }
    #endregion

    void Start()
    {
        Init();

        Screen.SetResolution(640, 480, false);
    }

    void Update()
    {
        _network.Update();
    }

    static void Init()
    {
        if (s_instance == null)
        {
            GameObject go = GameObject.Find("@Managers");
            if (go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.AddComponent<Managers>();
            }

            DontDestroyOnLoad(go);
            s_instance = go.GetComponent<Managers>();

            s_instance._network.Init();
            s_instance._pool.Init();
        }
    }

    public static void Clear()
    {
        Pool.Clear();
	}

	void OnApplicationQuit()
	{
        _network.Disconnect();
	}
}
