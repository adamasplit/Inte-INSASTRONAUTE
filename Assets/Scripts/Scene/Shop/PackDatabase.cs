using UnityEngine;
using System.Collections.Generic;

public class PackDatabase : MonoBehaviour
{
    public List<PackData> packs;
    
    public static PackDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<PackDatabase>();
                if (_instance == null)
                {
                    Debug.Log("[PackDatabase] Creating PackDatabase instance programmatically");
                    GameObject go = new GameObject("PackDatabase");
                    _instance = go.AddComponent<PackDatabase>();
                    DontDestroyOnLoad(go);
                    _instance.Init();
                }
            }
            return _instance;
        }
    }
    
    private static PackDatabase _instance;
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        Init();
    }
    
    private Dictionary<string, PackData> _byId;

    public void Init()
    {
        packs = new List<PackData>(Resources.LoadAll<PackData>("Packs"));
        _byId = new Dictionary<string, PackData>();
        foreach (var pack in packs)
        {
            if (pack != null && !string.IsNullOrEmpty(pack.packId))
                _byId[pack.packId] = pack;
        }
    }

    public PackData Get(string packId)
    {
        if (_byId == null) Init();
        _byId.TryGetValue(packId, out var pack);
        return pack;
    }
}
