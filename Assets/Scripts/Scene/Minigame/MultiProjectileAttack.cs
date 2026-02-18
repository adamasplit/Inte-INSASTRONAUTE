using UnityEngine;
using System.Collections.Generic;
public class MultiProjectileAttack : MonoBehaviour, IAttackBehaviour
{
    public GameObject projectilePrefab;
    public void ExecuteAttack(Tower tower, Column column, List<Enemy> targets, CardData card)
    {
        if (targets.Count == 0) return;
        for (int i = 0; i < targets.Count; i++)
        {
            Enemy target = targets[i];
            for (int j = 0; j < card.projectileCount; j++)
            {
                GameObject projObj = Instantiate(projectilePrefab, tower.transform.position, Quaternion.identity);
                ProjectileArc proj = projObj.GetComponent<ProjectileArc>();
                proj.Init(target, enemy =>
                {
                    enemy.TakeDamage(card.baseDamage, card.element);
                });
            }
        }
    }
}