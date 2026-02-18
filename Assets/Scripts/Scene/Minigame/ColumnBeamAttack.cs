using UnityEngine;
using System.Collections.Generic;
public class ColumnBeamAttack : MonoBehaviour, IAttackBehaviour
{
    public void ExecuteAttack(Tower tower, Column column, List<Enemy> targets, CardData card)
    {
        column.DamageEnemies(card);
    }
}
