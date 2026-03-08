using System.Collections;
using UnityEngine;
using System.Collections.Generic;
public class Tower : MonoBehaviour
{
    public Column column;
    
    public Transform rocket;
    public ParticleSystem takeoffParticles;
    public ParticleSystem trailParticles;
    public ParticleSystem overdriveParticles;
    public SpriteRenderer overlayRenderer; 

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
    public bool isAttacking = false;
    public bool overdrive = false;

    void Awake()
    {
        originalScale = transform.localScale;
        startLocalPos = rocket.localPosition;
        if (takeoffParticles) takeoffParticles.Stop();
        if (trailParticles) trailParticles.Stop();
        if (overdriveParticles) overdriveParticles.Stop();
    }

    void Update()
    {
        if (overdrive)
        {
            float flicker = Mathf.Sin(Time.time * 5f) * 0.5f + 0.5f; // Flicker between 0 and 1
            Color c = overlayRenderer.color;
            c.a = flicker;
            overlayRenderer.color = c;
        }
    }

    public void Activate(CardData card)
    {
        overlayRenderer.gameObject.SetActive(false);
        this.card = card;
        ConfigureFromCard(card);
        transform.localScale = originalScale;
        StartCoroutine(LaunchSequence());
    }

    private IEnumerator LaunchSequence()
    {
        isAttacking = true;
        // --- Phase 1 : recul vers le bas ---
        if (takeoffParticles) {
            takeoffParticles.gameObject.SetActive(true);
            takeoffParticles.Play();
        }
        if (overdriveParticles&&overdrive) {
            overdriveParticles.gameObject.SetActive(true);
            overdriveParticles.Play();
        }
        Vector3 recoilPos = startLocalPos - Vector3.up * recoilDistance;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / recoilTime;
            if (overdrive)
            {
                float scaleMultiplier = 1f + 0.5f * Mathf.Sin(Time.time * 10f); // Pulsation de l'overdrive
                transform.localScale = originalScale * scaleMultiplier;
            }
            rocket.localPosition = Vector3.Lerp(startLocalPos, recoilPos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        transform.localScale = originalScale;

        // --- Phase 2 : décollage instantané ---

        takeoffParticles.gameObject.SetActive(false);
        overdriveParticles.gameObject.SetActive(false);
        // Appliquer dégâts / effets
        if (attack != null)        
        {
            List<Enemy> targets = targeting.GetTargets(column);
            if (overdrive)
            {
                // En overdrive, cible tous les ennemis
                targets = GetComponent<AllEnemiesAllColumnsTargeting>().GetTargets(column);
            }
            attack.ExecuteAttack(this, column, targets, card);
        }


        Vector3 launchPos = startLocalPos + Vector3.up * launchDistance;
        rocket.localPosition = launchPos;
        
        // Set trail color
        
        if (trailParticles&&(this.card.attackType==AttackType.Beam||this.card.attackType==AttackType.ContinuousBeam)) {
            trailParticles.transform.position = rocket.position;
            trailParticles.gameObject.SetActive(true);  
            var trail = trailParticles.trails;
            var main = trailParticles.main;
            Color elementColor = ElementCalculator.GetElementColor(card.element);
            trail.colorOverTrail = new ParticleSystem.MinMaxGradient(elementColor);
            main.startColor = elementColor;
            trailParticles.Play();
        }
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
        overdrive = false;
        if (Random.Range(0, 100) < 20) 
        {
            overdrive = true;
            overlayRenderer.gameObject.SetActive(true);
        }
        // Stop particules
        trailParticles.transform.position = rocket.position;
        if (trailParticles) trailParticles.Stop();
        if (takeoffParticles) takeoffParticles.Stop();
        isAttacking = false;
    }
    public void ShowTargets(CardData card)
    {
        ConfigureFromCard(card);
        if (targeting == null) return;
        List<Enemy> targets = targeting.GetTargets(column);
        if (overdrive&&!(attack is ContinuousBeamAttack))
        {
            // En overdrive, cible tous les ennemis
            targets = GetComponent<AllEnemiesAllColumnsTargeting>().GetTargets(column);
        }
        foreach (Enemy e in targets)
        {
            if (e != null)
                e.ShowTargetIndicator();
        }
    }
    public void UnshowTargets(CardData card)
    {
        ConfigureFromCard(card);
        if (targeting == null) return;
        List<Enemy> targets = GetComponent<AllEnemiesAllColumnsTargeting>().GetTargets(column);
        foreach (Enemy e in targets)
        {
            if (e != null)
                e.HideTargetIndicator();
        }
    }
    public void ConfigureFromCard(CardData card)
    {
        targeting = card.targetingType switch
        {
            TargetingType.FirstEnemy => GetComponent<FirstEnemyTargeting>(),
            TargetingType.AllEnemies => GetComponent<AllEnemiesTargeting>(),
            TargetingType.AllEnemiesAllColumns => GetComponent<AllEnemiesAllColumnsTargeting>(),
            TargetingType.AllFirstEnemies => GetComponent<AllFirstEnemiesTargeting>(),
            TargetingType.RandomEnemy => GetComponent<RandomEnemyTargeting>(),
            TargetingType.AllEnemiesNearColumn => GetComponent<AllEnemiesNearColumnsTargeting>(),
            TargetingType.FirstEnemiesNearColumn => GetComponent<FirstEnemiesNearColumnsTargeting>(),
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
            AttackType.HaltEnemy => GetComponent<HaltEnemyAttack>(),
            _ => null
        };
    }

}

        
