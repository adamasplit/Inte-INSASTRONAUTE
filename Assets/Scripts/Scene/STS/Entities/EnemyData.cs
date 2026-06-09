using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Combat/Enemy")]
public class EnemyData : ScriptableObject
{
    public string enemyName;

    public int maxHP;

    public List<STSCardData> pattern;
    public List<STSCardData> rewardCards;
    public StatusType startingStatus;
    public int startingStatusDuration;
    public int startingStatusValue;
    #if UNITY_EDITOR
    private void OnValidate()
    {
        enemyName = name;
    }
    #endif
}