using UnityEngine;
using System.Collections.Generic;
public class ColumnBeamAttack : MonoBehaviour, IAttackBehaviour
{
    private List<Column> hitColumns = new List<Column>();
    public void ExecuteAttack(Tower tower, Column column, List<Enemy> targets, CardData card)
    {
        hitColumns.Clear();
        column.DamageEnemies(card);
        hitColumns.Add(column);
        foreach (var enemy in targets)
        {
            if (enemy != null && enemy.column != column&& !hitColumns.Contains(enemy.column))
            {
                enemy.column.DamageEnemies(card);
                hitColumns.Add(enemy.column);
            }
        }
    }
}
