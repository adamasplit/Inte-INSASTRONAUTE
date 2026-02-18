using System.Collections.Generic;
using UnityEngine;
public class Column : MonoBehaviour
{
    public Tower tower;
    public Transform enemyContainer;
    public List<Enemy> enemies = new List<Enemy>();
    public Vector3 position => enemyContainer.position;

    public void DamageEnemies(CardData card, float damageMultiplier = 1f)
    {
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            float damage = ElementCalculator.GetDamage(card.element, enemy.element, card.baseDamage) * damageMultiplier;
            enemy.TakeDamage(damage, card.element);
        }
    }
    public Enemy GetFirstEnemy()
    {
        if (enemies.Count > 0)
            return enemies[0];
        return null;
    }
    public List<Enemy> GetEnemies()
    {
        return enemies;
    }
}
