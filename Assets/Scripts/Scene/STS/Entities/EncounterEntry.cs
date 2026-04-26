using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public class EncounterEntry
{
    public string displayName;
    public List<EnemyData> enemies;
    public int minFloor;
    public int maxFloor;

    [Range(0, 1)]
    public float weight = 1f;

    public bool elite;
    public bool boss;
}