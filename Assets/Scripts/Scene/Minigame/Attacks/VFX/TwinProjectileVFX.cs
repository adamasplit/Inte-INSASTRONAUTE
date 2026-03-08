using UnityEngine;
using System.Collections.Generic;
public class TwinProjectileVFX : MonoBehaviour, IVFX
{
    public GameObject projectilePrefab;
    public void Fire(Vector3 startPos, List<Enemy> targetPos, CardData card)
    {
        if (targetPos.Count == 0) return;

        GameObject projectile1 = Instantiate(projectilePrefab, startPos, Quaternion.identity);
        GameObject projectile2 = Instantiate(projectilePrefab, startPos, Quaternion.identity);
        projectile1.GetComponent<Projectile>().Init(targetPos[0],enemy =>
                {
                    enemy.TakeDamage(card.baseDamage, card.element);
                });
        projectile2.GetComponent<Projectile>().Init(targetPos[0],enemy =>
                {
                    enemy.TakeDamage(card.baseDamage, card.element);
                });
        projectile2.GetComponent<Projectile>().waveAmplitude=-projectile1.GetComponent<Projectile>().waveAmplitude; // Invert the wave for the second projectile
    }
}