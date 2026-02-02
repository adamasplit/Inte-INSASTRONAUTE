using System.Collections;
using UnityEngine;

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
        if (trailParticles) trailParticles.Play();
        

        // Appliquer dégâts / effets
        column.DamageEnemies(card);

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
}

        
