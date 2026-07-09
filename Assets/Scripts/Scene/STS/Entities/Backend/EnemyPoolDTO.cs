using System;
using System.Collections.Generic;

[Serializable]
public class EncounterEntryDTO
{
    public string displayName;
    public List<string> enemyIds = new();
    public int minFloor;
    public int maxFloor;
    public int minAct = -1;
    public int maxAct = -1;
    public float weight = 1f;
    public bool elite;
    public bool boss;
}

[Serializable]
public class EnemyPoolDTO
{
    public List<EncounterEntryDTO> enemies = new();
}