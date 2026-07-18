using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class Tooltip : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    private TooltipManager tooltipManager;
    public float padding = 20f;
    public float maxWidth = 300f;
    [SerializeField] private float showDuration = 0.12f;
    [SerializeField] private float hideDuration = 0.1f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine transitionRoutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetTooltip(TooltipManager tooltipManager, string title, string description)
    {
        this.tooltipManager = tooltipManager;
        titleText.text = title;
        descriptionText.text = description;
        float naturalWidth = Mathf.Max(titleText.preferredWidth, descriptionText.preferredWidth);
        float tooltipWidth = Mathf.Min(naturalWidth + padding * 2f, maxWidth);
        descriptionText.GetComponent<LayoutElement>().preferredWidth = tooltipWidth - padding * 2f;
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());

        PlayShowAnimation();
    }
    public void Hide()
    {
        PlayHideAnimation();
    }

    private void PlayShowAnimation()
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        if (rectTransform != null)
            rectTransform.localScale = Vector3.one * 0.94f;

        transitionRoutine = StartCoroutine(AnimateShowRoutine());
    }

    private void PlayHideAnimation()
    {
        if (!isActiveAndEnabled)
        {
            Destroy(gameObject);
            return;
        }

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(AnimateHideRoutine());
    }

    private IEnumerator AnimateShowRoutine()
    {
        float elapsed = 0f;

        while (elapsed < showDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / showDuration);
            float eased = t * t * (3f - 2f * t);
            canvasGroup.alpha = eased;
            if (rectTransform != null)
                rectTransform.localScale = Vector3.Lerp(Vector3.one * 0.94f, Vector3.one, eased);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        if (rectTransform != null)
            rectTransform.localScale = Vector3.one;
        transitionRoutine = null;
    }

    private IEnumerator AnimateHideRoutine()
    {
        float startAlpha = canvasGroup.alpha;
        Vector3 startScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
        float elapsed = 0f;

        while (elapsed < hideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / hideDuration);
            float eased = t * t * (3f - 2f * t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
            if (rectTransform != null)
                rectTransform.localScale = Vector3.Lerp(startScale, Vector3.one * 0.94f, eased);
            yield return null;
        }

        transitionRoutine = null;
        Destroy(gameObject);
    }
}