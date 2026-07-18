using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class STSMainMenuIntroSequence : MonoBehaviour
{
    [Header("Roots")]
    public CanvasGroup blackoutGroup;
    public CanvasGroup titleScreenGroup;
    public Image flashOverlay;

    [Header("Title Beats")]
    public RectTransform titleTextRect;
    public TextMeshProUGUI titleText;
    public RectTransform titleStar;
    public RectTransform titleLine;

    [Header("Particles")]
    public ParticleSystem[] preFlashParticles;
    public ParticleSystem[] revealParticles;

    [Header("Timing")]
    public bool playOnStart = false;
    public float introHoldDelay = 0.35f;
    public float sparkDelay = 0.15f;
    public float starRiseDuration = 0.8f;
    public float lineGrowDuration = 0.95f;
    public float flashInDuration = 0.12f;
    public float flashOutDuration = 0.22f;
    public float revealDelay = 0.04f;
    public bool playOnce = true;

    [Header("Scale")]
    public float starStartScaleMultiplier = 0.15f;
    public float starPeakScaleMultiplier = 1.85f;
    public float starFinalScaleMultiplier = 1.2f;
    public float starSpinDegreesPerSecond = 720f;
    public bool useCurrentLineWidthAsTarget = true;
    public float lineTargetWidth = 720f;

    [Header("Flash")]
    public Color flashColor = new Color(0.6f, 0.85f, 1f, 1f);
    public float flashPeakAlpha = 0.95f;

    [Header("Title Pulse")]
    public bool pulseTitleAfterIntro = true;
    public float titlePulseSpeed = 1.2f;
    public float titlePulseIntensity = 0.45f;
    public float titlePulseScale = 1.015f;
    public Color titlePulseColor = new Color(0.82f, 0.92f, 1f, 1f);
    public Color titlePulseGlowColor = new Color(0.55f, 0.75f, 1f, 1f);
    public float titlePulseGlowPower = 0.18f;
    public float titlePulseOutlineWidth = 0.08f;

    [Header("Debug")]
    public bool allowEditorReplayOnSpace = true;

    Coroutine sequenceRoutine;
    Coroutine titlePulseRoutine;
    bool hasPlayed;
    bool introSkipping;
    Vector3 starBaseScale = Vector3.one;
    Quaternion starBaseRotation = Quaternion.identity;
    Vector2 starBaseAnchoredPosition = Vector2.zero;
    Vector2 lineBaseAnchoredPosition = Vector2.zero;
    Vector2 lineBaseSize = Vector2.zero;
    float cachedLineTargetWidth;
    Color titleBaseColor = Color.white;
    Material titleMaterialInstance;
    bool titleHasFaceColor;
    bool titleHasGlowColor;
    bool titleHasGlowPower;
    bool titleHasOutlineWidth;

    void Awake()
    {
        if (titleText == null && titleTextRect != null)
        {
            titleText = titleTextRect.GetComponent<TextMeshProUGUI>();
        }

        CacheStartingValues();
        CacheTitleMaterialState();
        ApplyInitialState();
    }

    void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    void Update()
    {
        if (Application.isEditor && allowEditorReplayOnSpace && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Replay();
            return;
        }

        if (sequenceRoutine != null && WasSkipPressedThisFrame())
        {
            SkipToFinalState();
        }
    }

    public void Play()
    {
        StartSequence(false);
    }

    public void Replay()
    {
        StartSequence(true);
    }

    void StartSequence(bool forceRestart)
    {
        if (!forceRestart && playOnce && hasPlayed)
        {
            ApplyFinalState();
            return;
        }

        introSkipping = false;

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        sequenceRoutine = StartCoroutine(PlayRoutine());
    }

    void SkipToFinalState()
    {
        if (introSkipping)
        {
            return;
        }

        introSkipping = true;

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        PlayParticles(revealParticles);
        ApplyFinalState();
    }

    void CacheTitleMaterialState()
    {
        if (titleText == null)
        {
            return;
        }

        titleBaseColor = titleText.color;

        titleMaterialInstance = titleText.fontMaterial;
        if (titleMaterialInstance != null)
        {
            titleHasFaceColor = titleMaterialInstance.HasProperty("_FaceColor");
            titleHasGlowColor = titleMaterialInstance.HasProperty("_GlowColor");
            titleHasGlowPower = titleMaterialInstance.HasProperty("_GlowPower");
            titleHasOutlineWidth = titleMaterialInstance.HasProperty("_OutlineWidth");
        }
    }

    void StopTitlePulse()
    {
        if (titlePulseRoutine != null)
        {
            StopCoroutine(titlePulseRoutine);
            titlePulseRoutine = null;
        }

        if (titleText == null)
        {
            return;
        }

        titleText.color = titleBaseColor;
        if (titleMaterialInstance != null)
        {
            if (titleHasFaceColor)
            {
                titleMaterialInstance.SetColor("_FaceColor", titleBaseColor);
            }

            if (titleHasGlowColor)
            {
                titleMaterialInstance.SetColor("_GlowColor", Color.clear);
            }

            if (titleHasGlowPower)
            {
                titleMaterialInstance.SetFloat("_GlowPower", 0f);
            }

            if (titleHasOutlineWidth)
            {
                titleMaterialInstance.SetFloat("_OutlineWidth", 0f);
            }
        }
    }

    void StartTitlePulse()
    {
        if (!pulseTitleAfterIntro || titleText == null)
        {
            return;
        }

        StopTitlePulse();
        titlePulseRoutine = StartCoroutine(TitlePulseRoutine());
    }

    IEnumerator TitlePulseRoutine()
    {
        float phaseOffset = Random.Range(0f, Mathf.PI * 2f);

        while (true)
        {
            float rawPulse = (Mathf.Sin((Time.unscaledTime * titlePulseSpeed * Mathf.PI * 2f) + phaseOffset) + 1f) * 0.5f;
            float intensity = Mathf.Pow(rawPulse, 1.8f) * titlePulseIntensity;

            if (titleText != null)
            {
                titleText.color = Color.Lerp(titleBaseColor, titlePulseColor, intensity);
            }

            if (titleMaterialInstance != null)
            {
                if (titleHasFaceColor)
                {
                    titleMaterialInstance.SetColor("_FaceColor", Color.Lerp(titleBaseColor, titlePulseColor, intensity));
                }

                if (titleHasGlowColor)
                {
                    titleMaterialInstance.SetColor("_GlowColor", Color.Lerp(Color.clear, titlePulseGlowColor, intensity));
                }

                if (titleHasGlowPower)
                {
                    titleMaterialInstance.SetFloat("_GlowPower", Mathf.Lerp(0f, titlePulseGlowPower, intensity));
                }

                if (titleHasOutlineWidth)
                {
                    titleMaterialInstance.SetFloat("_OutlineWidth", Mathf.Lerp(0f, titlePulseOutlineWidth, intensity));
                }
            }

            yield return null;
        }
    }

    bool WasSkipPressedThisFrame()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }

        if (Pen.current != null && Pen.current.tip.wasPressedThisFrame)
        {
            return true;
        }

        return Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
    }

    void CacheStartingValues()
    {
        if (titleStar != null)
        {
            starBaseScale = titleStar.localScale;
            starBaseRotation = titleStar.localRotation;
            starBaseAnchoredPosition = titleStar.anchoredPosition;
        }

        if (titleLine != null)
        {
            lineBaseSize = titleLine.sizeDelta;
            lineBaseAnchoredPosition = titleLine.anchoredPosition;
            cachedLineTargetWidth = GetTargetLineWidth();
        }
        else
        {
            cachedLineTargetWidth = GetTargetLineWidth();
        }
    }

    float GetTargetLineWidth()
    {
        if (titleTextRect != null)
        {
            return Mathf.Max(0f, titleTextRect.rect.width);
        }

        if (useCurrentLineWidthAsTarget && titleLine != null)
        {
            return Mathf.Max(0f, titleLine.rect.width);
        }

        return Mathf.Max(0f, lineTargetWidth);
    }

    Vector2 GetTitleCenterAnchoredPosition()
    {
        if (titleTextRect != null)
        {
            return titleTextRect.anchoredPosition;
        }

        if (titleLine != null)
        {
            return titleLine.anchoredPosition;
        }

        if (titleStar != null)
        {
            return titleStar.anchoredPosition;
        }

        return Vector2.zero;
    }

    void ApplyInitialState()
    {
        StopTitlePulse();

        if (blackoutGroup != null)
        {
            blackoutGroup.alpha = 1f;
            blackoutGroup.interactable = false;
            blackoutGroup.blocksRaycasts = true;
        }

        if (titleScreenGroup != null)
        {
            titleScreenGroup.alpha = 0f;
            titleScreenGroup.interactable = false;
            titleScreenGroup.blocksRaycasts = false;
        }

        if (flashOverlay != null)
        {
            flashOverlay.gameObject.SetActive(true);
            flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        }

        if (titleStar != null)
        {
            titleStar.gameObject.SetActive(true);
            titleStar.localScale = starBaseScale * starStartScaleMultiplier;
            titleStar.localRotation = starBaseRotation;
            titleStar.anchoredPosition = GetTitleCenterAnchoredPosition();
        }

        if (titleLine != null)
        {
            titleLine.gameObject.SetActive(true);
            titleLine.sizeDelta = new Vector2(0f, lineBaseSize.y);
            titleLine.anchoredPosition = lineBaseAnchoredPosition;
        }
    }

    void ApplyFinalState()
    {
        if (blackoutGroup != null)
        {
            blackoutGroup.alpha = 0f;
            blackoutGroup.blocksRaycasts = false;
        }

        if (titleScreenGroup != null)
        {
            titleScreenGroup.alpha = 1f;
            titleScreenGroup.interactable = true;
            titleScreenGroup.blocksRaycasts = true;
        }

        if (flashOverlay != null)
        {
            flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
            flashOverlay.gameObject.SetActive(false);
        }

        if (titleStar != null)
        {
            titleStar.localRotation = Quaternion.identity;
            titleStar.gameObject.SetActive(false);
            titleStar.anchoredPosition = GetTitleCenterAnchoredPosition();
        }

        if (titleLine != null)
        {
            titleLine.sizeDelta = new Vector2(cachedLineTargetWidth, lineBaseSize.y);
            titleLine.anchoredPosition = lineBaseAnchoredPosition;
        }
        StopParticles(preFlashParticles);

        StartTitlePulse();
    }

    public void HideTitleLine()
    {
        if (titleLine != null)
        {
            titleLine.gameObject.SetActive(false);
        }
    }

    IEnumerator PlayRoutine()
    {
        hasPlayed = true;
        ApplyInitialState();

        yield return WaitUnscaled(introHoldDelay);
        PlayParticles(preFlashParticles);
        yield return WaitUnscaled(sparkDelay);

        yield return AnimateTitleBeat();
        yield return WaitUnscaled(revealDelay);

        PlayParticles(revealParticles);
        yield return FlashRevealRoutine();

        ApplyFinalState();
        sequenceRoutine = null;
        introSkipping = false;
    }

    IEnumerator AnimateTitleBeat()
    {
        float elapsed = 0f;
        Vector3 starStartScale = starBaseScale * starStartScaleMultiplier;
        Vector3 starPeakScale = starBaseScale * starPeakScaleMultiplier;
        Vector3 starFinalScale = starBaseScale * starFinalScaleMultiplier;
        float lineStartWidth = 0f;
        Vector2 titleCenter = GetTitleCenterAnchoredPosition();
        float totalDuration = Mathf.Max(starRiseDuration, lineGrowDuration);
        float spinTurnCount = Mathf.Max(1f, Mathf.Ceil((starSpinDegreesPerSecond * totalDuration) / 360f));
        float targetSpinAngle = spinTurnCount * 360f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (titleStar != null)
            {
                float starT = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, starRiseDuration));
                float easedStarT = EaseOutCubic(starT);
                float spinT = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, totalDuration));
                Vector3 targetScale = elapsed < starRiseDuration * 0.7f
                    ? Vector3.LerpUnclamped(starStartScale, starPeakScale, easedStarT)
                    : Vector3.LerpUnclamped(starPeakScale, starFinalScale, Mathf.InverseLerp(starRiseDuration * 0.7f, starRiseDuration, elapsed));
                titleStar.localScale = targetScale;
                titleStar.localRotation = starBaseRotation * Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, targetSpinAngle, EaseOutCubic(spinT)));
                titleStar.anchoredPosition = titleCenter;
            }

            if (titleLine != null)
            {
                float lineT = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, lineGrowDuration));
                float easedLineT = EaseOutCubic(lineT);
                float width = Mathf.LerpUnclamped(lineStartWidth, cachedLineTargetWidth, easedLineT);
                titleLine.sizeDelta = new Vector2(width, lineBaseSize.y);
                titleLine.anchoredPosition = lineBaseAnchoredPosition;
            }

            yield return null;
        }

        if (titleStar != null)
        {
            titleStar.localRotation = starBaseRotation * Quaternion.Euler(0f, 0f, targetSpinAngle);
            titleStar.anchoredPosition = titleCenter;
        }

        if (titleLine != null)
        {
            titleLine.sizeDelta = new Vector2(cachedLineTargetWidth, lineBaseSize.y);
            titleLine.anchoredPosition = lineBaseAnchoredPosition;
        }
    }

    IEnumerator FlashRevealRoutine()
    {
        if (flashOverlay == null && blackoutGroup == null && titleScreenGroup == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < flashInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, flashInDuration));
            float eased = EaseOutCubic(t);
            SetFlashAlpha(Mathf.Lerp(0f, flashPeakAlpha, eased));
            SetScreenRevealAlpha(0f);
            yield return null;
        }

        SetFlashAlpha(flashPeakAlpha);
        StopParticles(preFlashParticles);
        yield return WaitUnscaled(revealDelay);
        if (titleStar != null)
        {
            titleStar.gameObject.SetActive(false);
        }
        SetScreenRevealAlpha(1f);

        elapsed = 0f;
        while (elapsed < flashOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, flashOutDuration));
            float eased = EaseInCubic(t);
            SetFlashAlpha(Mathf.Lerp(flashPeakAlpha, 0f, eased));
            SetScreenRevealAlpha(1f);
            yield return null;
        }

        SetFlashAlpha(0f);
        SetScreenRevealAlpha(1f);

        StartTitlePulse();
    }

    void SetFlashAlpha(float alpha)
    {
        if (flashOverlay == null)
        {
            return;
        }

        flashOverlay.gameObject.SetActive(alpha > 0.001f);
        flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, Mathf.Clamp01(alpha));
    }

    void SetScreenRevealAlpha(float alpha)
    {
        float revealAlpha = Mathf.Clamp01(alpha);

        if (blackoutGroup != null)
        {
            blackoutGroup.alpha = 1f - revealAlpha;
            blackoutGroup.blocksRaycasts = revealAlpha < 0.99f;
        }

        if (titleScreenGroup != null)
        {
            titleScreenGroup.alpha = revealAlpha;
            titleScreenGroup.interactable = revealAlpha >= 0.99f;
            titleScreenGroup.blocksRaycasts = revealAlpha >= 0.99f;
        }
    }

    static void PlayParticles(ParticleSystem[] particleSystems)
    {
        if (particleSystems == null)
        {
            return;
        }

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (particleSystem != null)
            {
                particleSystem.Play(true);
            }
        }
    }
    static void StopParticles(ParticleSystem[] particleSystems)
    {
        if (particleSystems == null)
        {
            return;
        }

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (particleSystem != null)
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    static IEnumerator WaitUnscaled(float duration)
    {
        if (duration <= 0f)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    static float EaseInCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }
}