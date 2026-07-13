using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class RewardCardController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CardView view;
    CardInstance instance;
    CardRewardEntryView rewardManager;
    private bool chosen = false;
    private Vector3 baseScale;
    private float hoverScale = 1.08f;
    private int originalSiblingIndex;

    void Awake()
    {
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
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (chosen || instance == null)
            return;

        rewardManager.FreezeCardsLayoutOnce();
        transform.localScale = baseScale * hoverScale;
        transform.SetAsLastSibling();
        if (view != null)
        {
            view.ShowRewardCardTooltips();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (chosen)
            return;

        ResetHoverState();
    }

    public void OnClick()
    {
        if (!chosen)
        {
            chosen = true;
            transform.localScale = baseScale * hoverScale;
            if (view != null)
            {
                view.HideCardTooltips();
            }
            rewardManager.SelectCard(instance, this);
        }
    }

    public void SetVisualVisible(bool visible)
    {
        if (view != null)
            view.gameObject.SetActive(visible);
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

        Vector3 startScale = Vector3.one * 3f;
        Vector3 endScale = Vector3.one;
        float startAlpha = canvasGroup.alpha;

        Vector2 curveDirection = endPosition - startPosition;
        Vector2 perpendicular = new Vector2(-curveDirection.y, curveDirection.x).normalized;
        Vector2 controlPoint = (startPosition + endPosition) * 0.5f + perpendicular * 90f + Vector2.up * 40f;

        animatedRoot.anchoredPosition = startPosition;
        animatedRoot.localScale = startScale;
        animatedRoot.localRotation = Quaternion.identity;

        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        if (view != null)
        {
            view.HideCardTooltips();
        }

        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t;

            Vector2 position = Mathf.Pow(1f - eased, 2f) * startPosition
                + 2f * (1f - eased) * eased * controlPoint
                + Mathf.Pow(eased, 2f) * endPosition;

            animatedRoot.anchoredPosition = position;
            animatedRoot.localScale = Vector3.Lerp(startScale, endScale, eased);
            animatedRoot.localRotation = Quaternion.identity;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
            yield return null;
        }

        animatedRoot.anchoredPosition = endPosition;
        animatedRoot.localScale = endScale;
        animatedRoot.localRotation = Quaternion.identity;
        canvasGroup.alpha = 0f;
    }

    private void OnDisable()
    {
        ResetHoverState();
    }

    private void ResetHoverState()
    {
        transform.localScale = baseScale;
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