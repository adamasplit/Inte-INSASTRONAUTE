using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class ContinuousBeamAttack : MonoBehaviour, IAttackBehaviour
{
    public void ExecuteAttack(Tower tower, Column column, List<Enemy> targets, CardData card)
    {
        ParticleSystem volChild = tower.transform.Find("Vol").GetComponent<ParticleSystem>();
        if (volChild)
        {
            var main = volChild.main;
            Color elementColor = ElementCalculator.GetElementColor(card.element);
            main.startColor = elementColor;
            main.startLifetime = card.duration;
        }
        StartCoroutine(BeamAttackCoroutine(tower, column, targets, card));
        GameObject vfx=Resources.Load<GameObject>("VFX/"+card.cardId);
        if (vfx != null)
        {
            GameObject instance = Instantiate(vfx, tower.transform);
            IVFX ivfx=instance.GetComponent<IVFX>();
            ivfx?.Fire(tower.transform.position, targets, card);
        }
        
    }
    public IEnumerator BeamAttackCoroutine(Tower tower, Column column, List<Enemy> targets, CardData card)
    {
        float elapsed = 0f;
        while (elapsed < card.duration)
        {
            elapsed += Time.deltaTime;
            column.DamageEnemies(card,Time.deltaTime); // Adjust damage based on time to ensure consistent damage over duration
            yield return null; // Wait for the next frame
        }
    }
}
