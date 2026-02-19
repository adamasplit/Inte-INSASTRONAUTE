using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public class ProjectileFlurryAttack : MonoBehaviour, IAttackBehaviour
{
    public GameObject projectilePrefab;
    public void ExecuteAttack(Tower tower, Column column, List<Enemy> targets, CardData card)
    {
        StartCoroutine(FlurryCoroutine(tower, targets, card));
    }
    private IEnumerator FlurryCoroutine(Tower tower, List<Enemy> targets, CardData card)
    {
        int randomIndex;
        int loopCount= 0;
        for (int i = 0; i < card.projectileCount; i++)
        {
            loopCount=0;
            do
            {
                randomIndex = Random.Range(0, targets.Count);
                loopCount++;
            } while ((randomIndex >= targets.Count || targets[randomIndex] == null||targets.Count == 0)&&loopCount<100);
            Enemy target = targets[randomIndex];
            GameObject projObj = Instantiate(projectilePrefab, tower.transform.position, Quaternion.identity);
            ProjectileArc proj = projObj.GetComponent<ProjectileArc>();
            proj.Init(target, enemy =>
            {
                enemy.TakeDamage(card.baseDamage, card.element);
            });
            yield return new WaitForSeconds(card.duration / card.projectileCount); // Delay between projectiles
        }
    }
}