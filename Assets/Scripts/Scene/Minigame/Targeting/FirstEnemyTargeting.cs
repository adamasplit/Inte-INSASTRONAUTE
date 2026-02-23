using UnityEngine;
using System.Collections.Generic;
public class FirstEnemyTargeting : MonoBehaviour, ITargetingBehaviour
{
    public List<Enemy> GetTargets(Column column)
    {
        Enemy firstEnemy = column.GetFirstEnemy();
        return firstEnemy ? new List<Enemy> { firstEnemy } : new List<Enemy>();
    }
}
