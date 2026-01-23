using System.Collections.Generic;
using UnityEngine;
public class Column : MonoBehaviour
{
    public Tower tower;
    public Transform enemyContainer;
    public List<Enemy> enemies = new List<Enemy>();
    public Vector3 position => enemyContainer.position;

    public void DamageEnemies(CardData card)
    {
        foreach (var enemy in enemies)
        {
            float damage = ElementCalculator.GetDamage(card.element, enemy.element, card.baseDamage);
            enemy.TakeDamage(damage, card.element);
        }
    }
}
