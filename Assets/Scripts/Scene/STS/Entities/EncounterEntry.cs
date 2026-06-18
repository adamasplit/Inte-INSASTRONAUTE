using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public class EncounterEntry
{
    public string displayName;
    public List<EnemyData> enemies;
    public int minFloor;
    public int maxFloor;
    public int minAct=-1;
    public int maxAct=-1;

    [Range(0, 1)]
    public float weight = 1f;

    public bool elite;
    public bool boss;
}