using UnityEngine;
using System.Collections.Generic;
public class RandomEnemyTargeting : MonoBehaviour, ITargetingBehaviour
{
    public List<Enemy> GetTargets(Column column)
    {
        List<Enemy> targets = new List<Enemy>();
        List<Enemy> allEnemies = new List<Enemy>();
        foreach (Column col in gameObject.GetComponentInParent<GridManager>().columns)
        {
            allEnemies.Add(col.GetFirstEnemy());
        }
        if (allEnemies.Count > 0)
        {
            int randomIndex = Random.Range(0, allEnemies.Count);
            targets.Add(allEnemies[randomIndex]);
        }
        return targets;
    }
}