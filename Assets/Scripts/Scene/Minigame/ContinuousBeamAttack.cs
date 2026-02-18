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
        
    }
    public IEnumerator BeamAttackCoroutine(Tower tower, Column column, List<Enemy> targets, CardData card)
    {
        float beamDuration = 1.0f; // Durée du faisceau actif
        float elapsed = 0f;
        while (elapsed < beamDuration)
        {
            elapsed += Time.deltaTime;
            column.DamageEnemies(card,1f/(10f*card.duration)); 
            yield return new WaitForSeconds(0.1f); 
        }
    }
}
