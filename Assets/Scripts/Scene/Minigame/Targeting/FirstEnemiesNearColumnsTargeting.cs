using UnityEngine;
using System.Collections.Generic;
public class FirstEnemiesNearColumnsTargeting : MonoBehaviour, ITargetingBehaviour
{
    public List<Enemy> GetTargets(Column column)
    {
        List<Enemy> targets = new List<Enemy>();
        foreach (Column col in gameObject.GetComponentInParent<GridManager>().columns)
        {
            Enemy firstEnemy = col.GetFirstEnemy();
            if (firstEnemy != null&&Mathf.Abs(col.columnIndex-column.columnIndex)<=1)
                targets.Add(firstEnemy);
        }
        return targets;
    }
}