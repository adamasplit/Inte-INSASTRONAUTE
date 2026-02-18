using UnityEngine;
using System.Collections.Generic;
public class InstantDamageAttack : MonoBehaviour, IAttackBehaviour
{
    public void ExecuteAttack(Tower tower,Column column, List<Enemy> targets, CardData card)
    {
        foreach (Enemy target in targets)
        {
            if (target == null) continue;
            target.TakeDamage(card.baseDamage, card.element);
        }
    }
}
