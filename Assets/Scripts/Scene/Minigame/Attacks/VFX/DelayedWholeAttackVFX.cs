using UnityEngine;
using System.Collections.Generic;
public class DelayedWholeAttackVFX : MonoBehaviour, IVFX
{
    public float delay = 0.8f;
    public ParticleSystem hitEffect;
    public void Fire(Vector3 startPos, List<Enemy> targetPos, CardData card)
    {
        bool vfxEnabled = GameSettings.VFXEnabled;
        foreach (ParticleSystem particleSystem in GetComponentsInChildren<ParticleSystem>(true))
        {
            var emission = particleSystem.emission;
            emission.enabled = vfxEnabled;
        }
        foreach (Enemy enemy in targetPos)
        {
            if (enemy == null) continue;
            enemy.Halt();
        }
        StartCoroutine(DelayedFire(startPos, targetPos, card));
    }

    private IEnumerator<WaitForSeconds> DelayedFire(Vector3 startPos, List<Enemy> targetPos, CardData card)
    {
        Debug.Log("DelayedWholeAttackVFX will hit in " + delay + " seconds.");
        yield return new WaitForSeconds(delay);
        foreach (Enemy enemy in targetPos)
        {
            if (enemy == null) continue;
            enemy.TakeDamage(card.baseDamage, card.element);
            enemy.Resume();
        }
    }
}