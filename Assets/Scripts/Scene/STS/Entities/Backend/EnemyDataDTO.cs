using System;
using System.Collections.Generic;

[Serializable]
public class EnemyMoveEntryDTO
{
    public string cardId;
    public string moveName;
    public List<EffectEntryDTO> effects = new();
    public List<int> nextMoveIndices = new();
    public int weight = 1;
}

[Serializable]
public class EnemyDataDTO
{
    public string id;
    public string enemyName;
    public string displayName;
    public int maxHP;
    public bool randomStart;
    public List<string> patternCardIds = new();
    public List<EnemyMoveEntryDTO> movePattern = new();
    public List<string> rewardCardIds = new();
    public string startingStatus;
    public int startingStatusDuration;
    public int startingStatusValue;
    public string startingStatusInfo;
}