using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Combat/Enemy Pool")]
public class EnemyPool : ScriptableObject
{
    public List<EncounterEntry> enemies;
}