using UnityEngine;
using System.Collections.Generic;

public class ProjectileAttack : MonoBehaviour, IAttackBehaviour
{
    public GameObject baseProjectilePrefab;
    public Transform firePoint;

    public void ExecuteAttack(Tower tower, Column column, List<Enemy> targets, CardData card)
    {
        foreach (Enemy target in targets)
        {
            if (target == null) continue;
            GameObject projectilePrefab = baseProjectilePrefab;
            if (Resources.Load<GameObject>("Projectiles/" + card.cardId) is GameObject customProj)
            {
                projectilePrefab = customProj;
            }
            GameObject proj = Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.identity,
                firePoint.parent
            );

            Projectile projectile = proj.GetComponent<Projectile>();

            projectile.Init(target, enemy =>
            {
                enemy.TakeDamage(card.baseDamage, card.element);
            });

            // Couleur du projectile selon l’élément
            var sr = proj.GetComponent<SpriteRenderer>();
            if (sr)
                sr.color = ElementCalculator.GetElementColor(card.element);
        }
    }
}
