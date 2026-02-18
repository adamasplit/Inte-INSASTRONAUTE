using UnityEngine;
using System.Collections.Generic;
public class AllEnemiesAllColumnsTargeting : MonoBehaviour, ITargetingBehaviour
{
    public List<Enemy> GetTargets(Column column)
    {
        List<Enemy> targets = new List<Enemy>();
        foreach (Column col in gameObject.GetComponentInParent<GridManager>().columns)
        {
            targets.AddRange(col.GetEnemies());
        }
        return targets;
    }
}