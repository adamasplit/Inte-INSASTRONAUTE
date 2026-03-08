using System.Collections.Generic;
using UnityEngine;
public class Column : MonoBehaviour
{
    public int columnIndex;
    public Tower tower;
    public Transform enemyContainer;
    public List<Enemy> enemies = new List<Enemy>();
    public Vector3 position => enemyContainer.position;
    public Enemy firstEnemy;
    public bool hasAnEnemyInFirstPosition=>firstEnemy!=null;
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
        if (enemies == null || enemies.Count == 0)
            return null;
        Enemy lowestEnemy = null;
        float lowestY = float.MaxValue;
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            float y = enemy.transform.position.y;
            if (y < lowestY)
            {
                lowestY = y;
                lowestEnemy = enemy;
            }
        }
        return lowestEnemy;
    }
    public List<Enemy> GetEnemies()
    {
        return enemies;
    }
}
