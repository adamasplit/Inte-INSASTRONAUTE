using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Threading.Tasks;

public class StarController : MonoBehaviour, IPointerClickHandler
{
    public bool IsSelected { get; private set; }
    private ConstellationController controller;
    public CardData[] cards;
    public Collider2D coll;
    public ParticleSystem explosionPS;
    public ParticleSystem sparklePS;
    public ParticleSystem slightSparklePS;
    public ParticleSystem raysPS;
    public ParticleSystem pulsePS;
    public Image image;
    public Image glowImage;
    private float pulsePower = 5f;
    public Transform visualRoot;
    public CanvasGroup canvasGroup;
    public bool IsVisible => canvasGroup.alpha > 0f;

    public void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
    }

    public async Task FadeIn(float duration)
    {
        visualRoot.localScale = Vector3.one * 0.5f;

        float t = 0f;
        int maxIterations = 1000; // Safety counter for WebGL
        int iterations = 0;
        while (t < 1f && iterations < maxIterations)
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.001f); // Ensure non-zero for WebGL
            t += deltaTime / duration;
            float eased = EaseOutCubic(t);

            canvasGroup.alpha = eased;
            visualRoot.localScale = Vector3.Lerp(
                Vector3.one * 0.5f,
                Vector3.one,
                eased
            );

            await Task.Yield();
            iterations++;
        }
    }

    public async Task Pulse()
    {
        float t = 0f;
        float duration = 0.15f;
        pulsePS.gameObject.SetActive(true);
        pulsePS.Play();
        slightSparklePS.gameObject.SetActive(true);

        Vector3 baseScale = Vector3.one;
        Vector3 pulseScale = Vector3.one * 1.3f;
        var main = slightSparklePS.main;
        int maxIterations = 500; // Safety counter for WebGL
        int iterations = 0;
        while (t < 1f && iterations < maxIterations)
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.001f); // Ensure non-zero for WebGL
            t += deltaTime / duration;
            float s = Mathf.Sin(t * Mathf.PI);

            visualRoot.localScale = Vector3.Lerp(baseScale, pulseScale, s);
            main.startColor = new Color(1f, 1f, 1f, t);
            await Task.Yield();
            iterations++;
        }

        visualRoot.localScale = baseScale;
    }


    public void Init(ConstellationController c)
    {
        controller = c;
        var emission = slightSparklePS.emission;
        emission.rateOverTime = UnityEngine.Random.Range(10, 40)/8f;
        slightSparklePS.gameObject.SetActive(false);
    }
    private int getHighestRarity()
    {
        int highest = 0;
        foreach (var card in cards)
        {
            if (card.rarity > highest)
                highest = card.rarity;
        }
        return highest;
    }
void SetGlow(Image glow, Color color, float intensity)
{
    glow.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(intensity * 0.5f));
    glow.transform.localScale = Vector3.one * (1f + intensity * 0.3f);
}
float EaseOutCubic(float t)
{
    return 1f - Mathf.Pow(1f - t, 3f);
}
private IEnumerator PlayExplosionRoutine(Color color, Image glow)
{
    float flashDuration = 0.1f;
    float t = 0f;

    int maxIterations = 500;
    int iterations = 0;
    while (t < flashDuration && iterations < maxIterations)
    {
        float deltaTime = Mathf.Max(Time.deltaTime, 0.001f);
        t += deltaTime;
        float v = Mathf.Lerp(2f, 0f, t / flashDuration);
        SetGlow(glow, color, v);
        yield return null;
        iterations++;
    }

    if (explosionPS != null)
    {
        var main = explosionPS.main;
        main.startColor = color;
        explosionPS.gameObject.SetActive(true);
        explosionPS.Play();
    }
    else
    {
        Debug.LogError("[StarController] explosionPS is null during PlayExplosionRoutine.");
    }

    float waitBeforeHide = 0.1f;
    t = 0f;
    iterations = 0;
    while (t < waitBeforeHide && iterations < 500)
    {
        float deltaTime = Mathf.Max(Time.deltaTime, 0.001f);
        t += deltaTime;
        yield return null;
        iterations++;
    }

    raysPS.gameObject.SetActive(false);
    controller.GetComponent<Image>().enabled = false;
    controller.HideStars();
    image.enabled = false;
    glowImage.enabled = false;

    float extraWait = explosionPS != null ? Mathf.Max(0f, explosionPS.main.duration - waitBeforeHide) : 0f;
    t = 0f;
    iterations = 0;
    while (t < extraWait && iterations < 3000)
    {
        float deltaTime = Mathf.Max(Time.deltaTime, 0.001f);
        t += deltaTime;
        yield return null;
        iterations++;
    }
}

public void SetPreviewPull(CardData[] pull)
{
    cards = pull;
}
    public void SetInteractable(bool value)
    {
        // activer / désactiver collider ou raycast
        coll.enabled = value;
    }

    public void SetSelected(bool value)
    {
        IsSelected = value;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        controller.OnStarSelected(this);
    }

    public void OnOtherStarSelected()
    {
        if (IsSelected) return;
        // fade out / particles
        gameObject.SetActive(false);
    }

    private float getMoveTimeByRarity(int rarity)
    {
        switch (rarity)
        {
            case 0: return 0.8f;
            case 1: return 0.8f;
            case 2: return 1f;
            case 3: return 1.2f;
            case 4: return 1.9f;
            default: return 1.0f;
        }
    }

    private float getPulsePowerByRarity(int rarity)
    {
        switch (rarity)
        {
            case 0: return 1f;
            case 1: return 3f;
            case 2: return 4f;
            case 3: return 5f;
            case 4: return 10f;
            default: return 5f;
        }
    }

    private float getPulseTimeByRarity(int rarity)
    {
        switch (rarity)
        {
            case 0: return 0f;
            case 1: return 0.2f;
            case 2: return 0.3f;
            case 3: return 0.6f;
            case 4: return 0.9f;
            default: return 0.8f;
        }
    }

    public Task PlayRarityAnimation()
    {
        var tcs = new TaskCompletionSource<bool>();
        StartCoroutine(PlayRarityAnimationRoutine(tcs));
        return tcs.Task;
    }

    private IEnumerator PlayRarityAnimationRoutine(TaskCompletionSource<bool> tcs)
    {
        Vector3 startPos = transform.localPosition;
        Vector3 targetPos = Vector3.zero;

        Image core = GetComponentInChildren<Image>();
        Transform glowTransform = transform.Find("Glow");
        if (glowTransform == null)
        {
            Debug.LogError("[StarController] Glow child not found.");
            tcs.TrySetException(new InvalidOperationException("Glow child not found."));
            yield break;
        }

        Image glow = glowTransform.GetComponent<Image>();
        if (glow == null)
        {
            Debug.LogError("[StarController] Glow image component missing.");
            tcs.TrySetException(new InvalidOperationException("Glow image component missing."));
            yield break;
        }

            int rarity = getHighestRarity();
            Color baseColor = CardDatabase.Instance.GetRarityColor(rarity);
            core.color = baseColor;

            float moveDuration = getMoveTimeByRarity(rarity);
            float pulseDuration = getPulseTimeByRarity(rarity);
            pulsePower = getPulsePowerByRarity(rarity);
            slightSparklePS.gameObject.SetActive(false);
            sparklePS.gameObject.SetActive(true);

            var sparkleMain = sparklePS.main;
            sparkleMain.startSize = 0.5f + rarity * 0.2f;

            var col = sparklePS.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(baseColor, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            float t = 0f;
            int maxIterations = 2000;
            int iterations = 0;
            while (t < moveDuration && iterations < maxIterations)
            {
                float deltaTime = Mathf.Max(Time.deltaTime, 0.001f);
                t += deltaTime;
                float eased = EaseOutCubic(t / moveDuration);

                transform.localPosition = Vector3.Lerp(startPos, targetPos, eased);
                float glowStrength = Mathf.Lerp(0.3f, 1.2f, eased);
                SetGlow(glow, baseColor, glowStrength);

                yield return null;
                iterations++;
            }

            if (iterations >= maxIterations)
            {
                Debug.LogError($"[StarController] Move loop reached safety limit. moveDuration={moveDuration}, elapsed={t}, rarity={rarity}");
            }

            transform.localPosition = targetPos;

            t = 0f;
            bool peakReached = false;
            raysPS.gameObject.SetActive(true);
            maxIterations = 3000;
            iterations = 0;
            while (t < pulseDuration && iterations < maxIterations)
            {
                var emission = raysPS.emission;
                float maxRayRate = rarity switch
                {
                    0 => 0f,
                    1 => 20f,
                    2 => 40f,
                    3 => 80f,
                    4 => 150f,
                    _ => 50f
                };
                float rayRate = Mathf.Lerp(0f, maxRayRate, t / pulseDuration);
                emission.rateOverTime = rayRate;

                float deltaTime = Mathf.Max(Time.deltaTime, 0.001f);
                t += deltaTime;
                float pulse = Mathf.Sin(t * 10f) * 0.5f + 0.5f;
                float intensity = Mathf.Lerp(1.2f, pulsePower * (t / pulseDuration + 1), pulse);

                SetGlow(glow, baseColor, intensity);
                transform.localScale = Vector3.one * (1f + pulse * 0.15f);
                if (!peakReached && pulse > 0.9f)
                {
                    peakReached = true;
                }

                yield return null;
                iterations++;
            }

            if (iterations >= maxIterations)
            {
                Debug.LogError($"[StarController] Pulse loop reached safety limit. pulseDuration={pulseDuration}, elapsed={t}, rarity={rarity}");
            }

            sparklePS.gameObject.SetActive(false);
            yield return PlayExplosionRoutine(baseColor, glow);
            gameObject.SetActive(false);
            tcs.TrySetResult(true);
    }
}

