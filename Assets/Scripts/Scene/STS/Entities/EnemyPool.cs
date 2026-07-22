using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Combat/Enemy Pool")]
public class EnemyPool : ScriptableObject
{
    public int maxAct = -1;
    public float baseHpScaling = 1f;
    public List<float> actHpScaling = new();
    public List<EncounterEntry> enemies;
}