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
        baseScale = transform.localScale;
        if (baseScale == Vector3.zero)
            baseScale = Vector3.one;
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
            view.ShowCardTooltips(ShouldShowOnLeft(), true,true);
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