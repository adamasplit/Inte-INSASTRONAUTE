using System.Collections;
using UnityEngine;
using System.Collections.Generic;
public class Tower : MonoBehaviour
{
    public Column column;
    
    public Transform rocket;
    public ParticleSystem takeoffParticles;
    public ParticleSystem trailParticles;

    public float recoilDistance = 50f;   // en unités locales visibles à l'écran
    public float recoilTime = 0.2f;
    public float launchDistance = 800f;  // traverse l'écran
    public float launchTime = 0.2f;
    public float returnTime = 0.5f;
    private CardData card;

    private Vector3 originalScale;
    private Vector3 startLocalPos;
    private ITargetingBehaviour targeting;
    private IAttackBehaviour attack;

    void Awake()
    {
        originalScale = transform.localScale;
        startLocalPos = rocket.localPosition;
        if (takeoffParticles) takeoffParticles.Stop();
        if (trailParticles) trailParticles.Stop();
    }

    public void Activate(CardData card)
    {
        this.card = card;
        ConfigureFromCard(card);
        transform.localScale = originalScale;
        StartCoroutine(LaunchSequence());
    }

    private IEnumerator LaunchSequence()
    {
        // --- Phase 1 : recul vers le bas ---
        if (takeoffParticles) takeoffParticles.Play();
        Vector3 recoilPos = startLocalPos - Vector3.up * recoilDistance;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / recoilTime;
            rocket.localPosition = Vector3.Lerp(startLocalPos, recoilPos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        // --- Phase 2 : décollage instantané ---


        // Appliquer dégâts / effets
        if (attack != null)        {
            List<Enemy> targets = targeting.GetTargets(column);
            foreach (Enemy target in targets)
            {
                if (target == null) targets.Remove(target);
            }
            attack.ExecuteAttack(this, column, targets, card);
        }


        Vector3 launchPos = startLocalPos + Vector3.up * launchDistance;
        rocket.localPosition = launchPos;
        trailParticles.transform.position = rocket.position;
        // Set trail color
        trailParticles.gameObject.SetActive(true);
        if (trailParticles && card != null)
        {
            var trail = trailParticles.trails;
            var main = trailParticles.main;
            Color elementColor = ElementCalculator.GetElementColor(card.element);
            trail.colorOverTrail = new ParticleSystem.MinMaxGradient(elementColor);
            main.startColor = elementColor;
        }
        if (trailParticles&&(this.card.attackType==AttackType.Beam||this.card.attackType==AttackType.ContinuousBeam)) trailParticles.Play();
        yield return new WaitForSeconds(launchTime);

        // --- Phase 3 : retour depuis le bas ---
        Vector3 belowStart = startLocalPos - Vector3.up * (recoilDistance + 200f); // départ depuis le bas
        rocket.localPosition = belowStart;

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / returnTime;
            rocket.localPosition = Vector3.Lerp(belowStart, startLocalPos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        rocket.localPosition = startLocalPos;

        // Stop particules
        trailParticles.transform.position = rocket.position;
        if (trailParticles) trailParticles.Stop();
        if (takeoffParticles) takeoffParticles.Stop();
    }
    public void ConfigureFromCard(CardData card)
    {
        targeting = card.targetingType switch
        {
            TargetingType.FirstEnemy => GetComponent<FirstEnemyTargeting>(),
            TargetingType.AllEnemies => GetComponent<AllEnemiesTargeting>(),
            TargetingType.AllEnemiesAllColumns => GetComponent<AllEnemiesAllColumnsTargeting>(),
            TargetingType.AllFirstEnemies => GetComponent<AllFirstEnemiesTargeting>(),
            _ => null
        };

        attack = card.attackType switch
        {
            AttackType.Instant => GetComponent<InstantDamageAttack>(),
            AttackType.Beam => GetComponent<ColumnBeamAttack>(),
            AttackType.ContinuousBeam => GetComponent<ContinuousBeamAttack>(),
            AttackType.Projectile => GetComponent<ProjectileAttack>(),
            AttackType.MultiProjectiles => GetComponent<MultiProjectileAttack>(),
            AttackType.ProjectileFlurry => GetComponent<ProjectileFlurryAttack>(),

            _ => null
        };
    }

}

        
