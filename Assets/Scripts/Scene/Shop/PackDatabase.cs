using UnityEngine;
using System.Collections.Generic;

public class PackDatabase : MonoBehaviour
{
    public List<PackData> packs;

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
