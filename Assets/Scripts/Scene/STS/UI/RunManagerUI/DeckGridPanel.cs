using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class DeckGridPanel : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public GridLayoutGroup gridLayout;
    public Transform gridContainer;
    public GameObject cardGridItemPrefab;
    
    [Header("Preview")]
    public Transform previewContainer;
    public GameObject previewCardPrefab;
    public GameObject previewPanel;
    public CanvasGroup previewCanvasGroup;
    
    [Header("Controls")]
    public Button closeButton;
    public CanvasGroup panelCanvasGroup;

    [Header("Animation")]
    [SerializeField] private float entranceDuration = 0.25f;
    [SerializeField] private float entranceOffset = 180f;
    [SerializeField] private bool enableEntranceAnimation = false;

    [Header("Content Padding")]
    [SerializeField] private float contentPadding = 48f;

    [Header("Depth")]
    [SerializeField] private bool normalizeCardDepth = true;
    [SerializeField] private float cardLocalZ = 0f;

    private CardGridItemView selectedItemView;
    private GameObject animatingCardObj;
    private bool isAnimating = false;
    private bool refreshQueued = false;
    private GridLayoutGroup.Constraint initialGridConstraint;
    private int initialGridConstraintCount;

    void Awake()
    {
        if (gridLayout != null)
        {
            initialGridConstraint = gridLayout.constraint;
            initialGridConstraintCount = gridLayout.constraintCount;
        }
    }

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    public void Show(List<CardInstance> deck,string name)
    {
        titleText.text = name;

        gameObject.SetActive(true);
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.blocksRaycasts = true;
        }

        // Clear existing grid items
        foreach (Transform child in gridContainer)
            Destroy(child.gameObject);

        selectedItemView = null;

        // Create grid items
        foreach (var card in deck)
        {
            var obj = Instantiate(cardGridItemPrefab, gridContainer);
            var itemView = obj.GetComponentInChildren<CardGridItemView>();
            if (itemView != null)
            {
                itemView.Init(card, this);
            }
            EnsureItemVisible(obj);
        }

        // Rebuild layout once the panel is active so the scroll content gets its real size.
        QueueGridRefresh();

        UpdateCloseButtonState();
        // Hide preview initially
        HidePreview();
    }

    private void EnsureItemVisible(GameObject item)
    {
        if (item == null)
            return;

        item.SetActive(true);
        item.transform.localScale = Vector3.one;
        item.transform.SetAsLastSibling();

        RectTransform rect = item.transform as RectTransform;
        if (rect != null)
        {
            Vector3 localPos = rect.localPosition;
            float z = normalizeCardDepth ? cardLocalZ : localPos.z;
            rect.localPosition = new Vector3(localPos.x, localPos.y, z);

            if (gridLayout != null)
            {
                if (rect.rect.width <= 1f)
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, gridLayout.cellSize.x);
                if (rect.rect.height <= 1f)
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, gridLayout.cellSize.y);
            }
        }

        NormalizeItemDepth(item.transform);

        CanvasGroup[] canvasGroups = item.GetComponentsInChildren<CanvasGroup>(true);
        foreach (CanvasGroup cg in canvasGroups)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }

    public void SelectCard(CardInstance card, CardGridItemView itemView)
    {
        if (isAnimating) return;
        if (card == null)
            return;

        if (selectedItemView == itemView)
        {
            // Deselect the card if it's already selected
            if (selectedItemView != null)
                selectedItemView.gameObject.SetActive(true);
            HidePreview();
            selectedItemView = null;
            return;
        }
        //Hide previously selected card's preview if any
        if (selectedItemView != null && selectedItemView != itemView)
        {
            Debug.Log($"DeckGridPanel: Hiding previous selection for card {selectedItemView?.cardView?.cardInstance?.data?.cardName}");
            selectedItemView.gameObject.SetActive(true);
            HidePreview();
        }
        selectedItemView = itemView;
        StartCoroutine(AnimateCardToPreview(itemView, card));
    }

    void ShowPreview(CardInstance card)
    {
        // No longer used in this flow, but kept for compatibility if needed elsewhere
    }

    System.Collections.IEnumerator AnimateCardToPreview(CardGridItemView itemView, CardInstance card)
    {
        isAnimating = true;
        // Clone the card prefab at the grid item's position
        var cardObj = Instantiate(cardGridItemPrefab, gridContainer.parent); // parent to gridContainer's parent (should be Canvas)
        animatingCardObj = cardObj;
        var cardView = cardObj.GetComponentInChildren<CardView>();
        if (cardView != null)
            cardView.SetCard(card);

        // Get start and end positions/scales
        var startRect = itemView.GetComponent<RectTransform>();
        var animRect = cardObj.GetComponent<RectTransform>();
        var canvas = GetComponentInParent<Canvas>();
        Vector3 startWorldPos = startRect.transform.position;
        animRect.position = startWorldPos;
        animRect.localScale = startRect.localScale;

        // Target: center of previewPanel (or screen)
        RectTransform previewRect = previewPanel.GetComponent<RectTransform>();
        Vector3 targetWorldPos = previewRect.transform.position;
        // Target scale: make the card about 80% of preview panel height (keep aspect)
        float cardAspect = animRect.rect.width / animRect.rect.height;
        float previewHeight = previewRect.rect.height * 0.8f;
        float previewWidth = previewHeight * cardAspect;
        Vector3 targetScale = 0.7f*new Vector3(previewWidth / animRect.rect.width, previewHeight / animRect.rect.height, 1f);

        // Hide the original grid card during animation
        itemView.gameObject.SetActive(false);

        float duration = 0.15f;
        float elapsed = 0f;
        Vector3 initialPos = animRect.position;
        Vector3 initialScale = animRect.localScale;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            animRect.position = Vector3.Lerp(initialPos, targetWorldPos, t);
            animRect.localScale = Vector3.Lerp(initialScale, targetScale, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        animRect.position = targetWorldPos;
        animRect.localScale = targetScale;

        // Show the preview panel (background, etc), but do not instantiate a new preview card
        if (previewCanvasGroup != null)
        {
            previewCanvasGroup.alpha = 1f;
            previewCanvasGroup.blocksRaycasts = true;
        }
        previewPanel.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)previewContainer);

        isAnimating = false;
    }

    void HidePreview()
    {
        if (previewCanvasGroup != null)
        {
            previewCanvasGroup.alpha = 0f;
            previewCanvasGroup.blocksRaycasts = false;
        }
        previewPanel.SetActive(false);
        foreach (Transform child in previewContainer)
            Destroy(child.gameObject);
        // Also destroy the animating card if present
        if (animatingCardObj != null)
        {
            Destroy(animatingCardObj);
            animatingCardObj = null;
        }
    }

    public void Hide()
    {
        if (SelectionManager.Instance != null && SelectionManager.Instance.selectionMode)
            return;

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
        // Restore grid card if needed
        if (selectedItemView != null)
            selectedItemView.gameObject.SetActive(true);
    }
    public void ClearSelection()
    {
        // Restore grid card if needed
        if (selectedItemView != null)
        {
            selectedItemView.gameObject.SetActive(true);
        }
        HidePreview();
        selectedItemView = null;
    }

    private void OnEnable()
    {
        QueueGridRefresh();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled)
            QueueGridRefresh();
    }

    private void QueueGridRefresh()
    {
        if (refreshQueued || !isActiveAndEnabled)
            return;

        refreshQueued = true;
        if (enableEntranceAnimation)
            StartCoroutine(RefreshGridContentSizeAfterFrame());
        else
            StartCoroutine(RefreshGridContentSizeAfterFrameImmediate());
    }

    private IEnumerator RefreshGridContentSizeAfterFrame()
    {
        yield return null;
        RefreshGridContentSize();

        RectTransform gridContainerRect = gridContainer as RectTransform;
        if (enableEntranceAnimation && gridContainerRect != null)
            yield return AnimateGridEntrance(gridContainerRect);
    }

    private IEnumerator RefreshGridContentSizeAfterFrameImmediate()
    {
        yield return null;
        RefreshGridContentSize();
    }

    private void RefreshGridContentSize()
    {
        Canvas.ForceUpdateCanvases();

        RectTransform gridContainerRect = gridContainer as RectTransform;
        if (gridContainerRect == null || gridLayout == null)
        {
            refreshQueued = false;
            return;
        }

        RectTransform viewportRect = GetViewportRect();
        if (viewportRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRect);

        LayoutRebuilder.ForceRebuildLayoutImmediate(gridContainerRect);

        int itemCount = gridContainer.childCount;
        if (itemCount <= 0)
        {
            refreshQueued = false;
            return;
        }

        NormalizeAllItemsDepth();

        RectTransform sizingRect = viewportRect != null ? viewportRect : gridContainerRect;
        int columns = GetColumnCount(sizingRect, itemCount);
        int rows = Mathf.CeilToInt((float)itemCount / columns);

        float height = gridLayout.padding.top + gridLayout.padding.bottom;
        if (rows > 0)
        {
            height += rows * gridLayout.cellSize.y;
            height += Mathf.Max(0, rows - 1) * gridLayout.spacing.y;
        }

        height += contentPadding * 2f;

        gridContainerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        Canvas.ForceUpdateCanvases();
        refreshQueued = false;
    }

    private void NormalizeAllItemsDepth()
    {
        if (!normalizeCardDepth || gridContainer == null)
            return;

        foreach (Transform child in gridContainer)
            NormalizeItemDepth(child);
    }

    private void NormalizeItemDepth(Transform root)
    {
        if (!normalizeCardDepth || root == null)
            return;

        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform r in rects)
        {
            Vector3 p = r.localPosition;
            r.localPosition = new Vector3(p.x, p.y, cardLocalZ);
        }
    }

    private IEnumerator AnimateGridEntrance(RectTransform gridContainerRect)
    {
        Vector2 targetPosition = gridContainerRect.anchoredPosition;
        Vector2 startPosition = targetPosition + Vector2.down * Mathf.Max(entranceOffset, gridContainerRect.rect.height * 0.5f);

        gridContainerRect.anchoredPosition = startPosition;

        float elapsed = 0f;
        while (elapsed < entranceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / entranceDuration);
            float eased = t * t * (3f - 2f * t);
            gridContainerRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, eased);
            yield return null;
        }

        gridContainerRect.anchoredPosition = targetPosition;
    }

    private RectTransform GetViewportRect()
    {
        ScrollRect scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect != null && scrollRect.viewport != null)
            return scrollRect.viewport;

        Transform parentTransform = gridContainer != null ? gridContainer.parent : null;
        return parentTransform as RectTransform;
    }

    private int GetColumnCount(RectTransform availableAreaRect, int itemCount)
    {
        if (initialGridConstraint == GridLayoutGroup.Constraint.FixedColumnCount)
            return Mathf.Max(1, initialGridConstraintCount);

        if (initialGridConstraint == GridLayoutGroup.Constraint.FixedRowCount)
            return Mathf.Max(1, Mathf.CeilToInt((float)itemCount / Mathf.Max(1, initialGridConstraintCount)));

        float availableWidth = availableAreaRect.rect.width - gridLayout.padding.left - gridLayout.padding.right;
        float cellAndSpacingWidth = gridLayout.cellSize.x + gridLayout.spacing.x;
        int calculated = Mathf.FloorToInt((availableWidth + gridLayout.spacing.x) / Mathf.Max(1f, cellAndSpacingWidth));
        return Mathf.Max(1, calculated);
    }

    void Update()
    {
        UpdateCloseButtonState();
    }

    private void UpdateCloseButtonState()
    {
        if (closeButton == null)
            return;

        bool isSelecting = SelectionManager.Instance != null && SelectionManager.Instance.selectionMode;
        closeButton.interactable = !isSelecting;
    }
}
