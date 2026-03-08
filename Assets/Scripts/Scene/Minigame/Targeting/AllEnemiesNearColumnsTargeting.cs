using UnityEngine;
using System.Collections.Generic;
public class AllEnemiesNearColumnsTargeting : MonoBehaviour, ITargetingBehaviour
{
    public List<Enemy> GetTargets(Column column)
    {
        List<Enemy> targets = new List<Enemy>();
        foreach (Column col in gameObject.GetComponentInParent<GridManager>().columns)
        {
            if (Mathf.Abs(col.columnIndex - column.columnIndex) <= 1)
            {
                targets.AddRange(col.enemies);
            }
        }
        return targets;
    }
}