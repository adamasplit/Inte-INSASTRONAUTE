using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class RewardCardController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CardView view;
    public CardInstance instance;
    CardRewardEntryView rewardManager;
    private CanvasGroup canvasGroup;
    private bool chosen = false;
    private Vector3 baseScale;
    private float hoverScale = 1.08f;
    private int originalSiblingIndex;
    private Coroutine scaleRoutine;
    private bool selectionEnabled = true;

    private void PlayPresentationCue(string cueName, Vector3 position)
    {
        if (VFXManager.Instance != null)
            VFXManager.Instance.PlayEffect(cueName, position);

        SFXManager.Instance?.PlaySound(cueName);
    }

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        baseScale = Vector3.one*1.5f;
        
        if (baseScale == Vector3.zero)
            baseScale = Vector3.one;
        transform.localScale = baseScale;
        if (view == null)
        {
            view = GetComponentInChildren<CardView>();
        }
    }

    public void Init(CardInstance card, CardRewardEntryView manager)
    {
        rewardManager = manager;
        instance = card;
        chosen = false;
        originalSiblingIndex = transform.GetSiblingIndex();
        if (baseScale == Vector3.zero)
            baseScale = Vector3.one;
        transform.localScale = baseScale;
        view.SetCard(instance);
        view.enabled=false;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public IEnumerator PlaySpawnArcAnimation(Vector2 targetPosition, float duration, float delay = 0f)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        RectTransform rect = transform as RectTransform;
        if (rect == null)
            yield break;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        Vector2 endPosition = targetPosition;
        float travelDistance = Mathf.Max(Vector2.Distance(rect.anchoredPosition, endPosition), 80f);
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        if (randomDirection == Vector2.zero)
            randomDirection = Vector2.right;

        Vector2 startOffset = randomDirection * Random.Range(travelDistance * 0.55f, travelDistance * 0.95f);
        if (Vector2.Dot(startOffset, endPosition - rect.anchoredPosition) > 0f)
            startOffset = -startOffset;

        Vector2 startPosition = endPosition + startOffset;
        Vector2 travelDirection = (endPosition - startPosition).normalized;
        Vector2 perpendicular = new Vector2(-travelDirection.y, travelDirection.x);
        float arcHeight = Random.Range(travelDistance * 0.25f, travelDistance * 0.5f) * (Random.value < 0.5f ? -1f : 1f);
        Vector2 controlPoint = (startPosition + endPosition) * 0.5f
            + perpendicular * arcHeight
            + travelDirection * Random.Range(-travelDistance * 0.12f, travelDistance * 0.08f);

        Vector3 startScale = baseScale * 0.92f;
        Vector3 endScale = baseScale;
        rect.anchoredPosition = startPosition;
        rect.localScale = startScale;
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);

            Vector2 position = Mathf.Pow(1f - eased, 2f) * startPosition
                + 2f * (1f - eased) * eased * controlPoint
                + Mathf.Pow(eased, 2f) * endPosition;

            rect.anchoredPosition = position;
            rect.localScale = Vector3.Lerp(startScale, endScale, eased);
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);

            yield return null;
        }

        rect.anchoredPosition = endPosition;
        rect.localScale = endScale;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (chosen || instance == null || !selectionEnabled)
            return;

        rewardManager.FreezeCardsLayoutOnce();
        AnimateScaleTo(baseScale * hoverScale, 0.08f);
        transform.SetAsLastSibling();
        if (view != null)
        {
            view.ShowRewardCardTooltips();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (chosen || !selectionEnabled)
            return;

        ResetHoverState();
    }

    public void OnClick()
    {
        if (!chosen && selectionEnabled)
        {
            chosen = true;
            SetScaleInstant(baseScale * hoverScale);
            if (view != null)
            {
                view.HideCardTooltips();
            }
            rewardManager.SelectCard(instance, this);
        }
    }

    public void SetSelectable(bool selectable)
    {
        selectionEnabled = selectable;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.interactable = selectable;
            canvasGroup.blocksRaycasts = selectable;
        }

        if (!selectable && !chosen)
        {
            view?.HideCardTooltips();
            SetScaleInstant(baseScale);
        }
    }

    public void SetVisualVisible(bool visible)
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }

    public IEnumerator PlayRewardSelectionAnimation(
        float holdDuration,
        float duration,
        Vector2 startScreenPosition,
        Vector2 endScreenPosition,
        RectTransform animatedRoot)
    {
        if (animatedRoot == null)
            yield break;

        CanvasGroup canvasGroup = animatedRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = animatedRoot.gameObject.AddComponent<CanvasGroup>();

        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (canvasRect == null)
            yield break;

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreenPosition, uiCamera, out Vector2 startPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, endScreenPosition, uiCamera, out Vector2 endPosition);

        // Use the animated root's existing scale as the baseline. Don't multiply it—
        // the clone should start at the exact holder scale and stay there throughout.
        Vector3 baseline = animatedRoot.localScale;
        Vector3 startScale = baseline; // Start at holder's scale, no multiplier
        Vector3 endScale = baseline; // End at holder's scale

        float startAlpha = canvasGroup.alpha;

        float aspect = (float)Screen.width / Mathf.Max(Screen.height, 1f);
        float portraitFactor = Mathf.Clamp01((1f - aspect) / 0.45f);
        float curveScale = Mathf.Lerp(1f, 0.55f, portraitFactor);
        float maxTilt = Mathf.Lerp(7f, 3.5f, portraitFactor);

        Quaternion startRotation = Quaternion.Euler(0f, 0f, -maxTilt);
        Quaternion endRotation = Quaternion.Euler(0f, 0f, 2f);

        Vector2 travelDirection = (endPosition - startPosition).normalized;
        float travelDistance = Vector2.Distance(startPosition, endPosition);
        Vector2 launchBias = travelDirection * (travelDistance * (0.18f * curveScale) + 24f * curveScale)
            + Vector2.up * (travelDistance * (0.12f * curveScale) + 18f * curveScale);

        Vector2 curveDirection = endPosition - startPosition;
        Vector2 perpendicular = new Vector2(-curveDirection.y, curveDirection.x).normalized;
        Vector2 controlPoint1 = startPosition
            - travelDirection * (travelDistance * (0.22f * curveScale) + 34f * curveScale)
            + perpendicular * (travelDistance * (0.28f * curveScale) + 78f * curveScale);
        Vector2 controlPoint2 = endPosition
            - travelDirection * (travelDistance * (0.16f * curveScale) + 18f * curveScale)
            - perpendicular * (travelDistance * (0.12f * curveScale) + 42f * curveScale)
            + launchBias;

        float totalDuration = Mathf.Max(holdDuration + duration, 0.0001f);

        animatedRoot.anchoredPosition = startPosition;
        animatedRoot.localScale = startScale*2f;
        animatedRoot.localRotation = startRotation;

        PlayPresentationCue("Select", animatedRoot.position);

        if (view != null)
            view.HideCardTooltips();
        if (TooltipManager.Instance != null)
            TooltipManager.Instance.HideTooltip();

        float elapsed = 0f;
        bool tooltipsHidden = false;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / totalDuration);
            float eased = t * t * (3f - 2f * t);

            if (!tooltipsHidden && elapsed >= holdDuration)
            {
                if (view != null)
                    view.HideCardTooltips();

                if (TooltipManager.Instance != null)
                    TooltipManager.Instance.HideTooltip();

                tooltipsHidden = true;
            }

            Vector2 position = EvaluateCubicBezier(startPosition, controlPoint1, controlPoint2, endPosition, eased);
            Vector2 tangent = EvaluateCubicBezierDerivative(startPosition, controlPoint1, controlPoint2, endPosition, eased);
            float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            float clampedAngle = Mathf.Clamp(angle, -maxTilt, maxTilt);

            animatedRoot.anchoredPosition = position;
            animatedRoot.localScale = Vector3.Lerp(startScale, endScale, eased)*2f;
            animatedRoot.localRotation = Quaternion.Lerp(startRotation, Quaternion.Euler(0f, 0f, clampedAngle), eased);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
            yield return null;
        }

        if (!tooltipsHidden)
        {
            if (view != null)
                view.HideCardTooltips();

            if (TooltipManager.Instance != null)
                TooltipManager.Instance.HideTooltip();
        }

        animatedRoot.anchoredPosition = endPosition;
        animatedRoot.localScale = endScale*2f;
        animatedRoot.localRotation = endRotation;
        canvasGroup.alpha = 0f;
    }

    private void OnDisable()
    {
        ResetHoverState();
    }

    private void ResetHoverState()
    {
        SetScaleInstant(baseScale);
        if (!chosen)
            transform.SetSiblingIndex(originalSiblingIndex);

        if (view != null)
        {
            view.HideCardTooltips();
        }

        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }

    private void SetScaleInstant(Vector3 targetScale)
    {
        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        transform.localScale = targetScale;
    }

    private void AnimateScaleTo(Vector3 targetScale, float duration)
    {
        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        scaleRoutine = StartCoroutine(AnimateScaleRoutine(targetScale, duration));
    }

    private IEnumerator AnimateScaleRoutine(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        if (duration <= 0f)
        {
            transform.localScale = targetScale;
            scaleRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            transform.localScale = Vector3.Lerp(startScale, targetScale, eased);
            yield return null;
        }

        transform.localScale = targetScale;
        scaleRoutine = null;
    }

    private static Vector2 EvaluateCubicBezier(Vector2 start, Vector2 control1, Vector2 control2, Vector2 end, float t)
    {
        float oneMinusT = 1f - t;
        float oneMinusTSquared = oneMinusT * oneMinusT;
        float tSquared = t * t;

        return oneMinusTSquared * oneMinusT * start
            + 3f * oneMinusTSquared * t * control1
            + 3f * oneMinusT * tSquared * control2
            + tSquared * t * end;
    }

    private static Vector2 EvaluateCubicBezierDerivative(Vector2 start, Vector2 control1, Vector2 control2, Vector2 end, float t)
    {
        float oneMinusT = 1f - t;

        return 3f * oneMinusT * oneMinusT * (control1 - start)
            + 6f * oneMinusT * t * (control2 - control1)
            + 3f * t * t * (end - control2);
    }

    private bool ShouldShowOnLeft()
    {
        RectTransform rectTransform = view != null && view.rootRect != null
            ? view.rootRect
            : GetComponent<RectTransform>();

        if (rectTransform == null)
            return false;

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) * 0.5f;

        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, center);

        return screenPoint.x > Screen.width * 0.5f;
    }

}