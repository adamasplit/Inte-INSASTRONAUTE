using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

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

    private CardGridItemView selectedItemView;
    private GameObject animatingCardObj;
    private bool isAnimating = false;

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    public void Show(List<CardInstance> deck,string name)
    {
        titleText.text = name;
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
        }

        // Rebuild layout
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)gridContainer);

        // Show panel
        gameObject.SetActive(true);
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.blocksRaycasts = true;
        }

        // Hide preview initially
        HidePreview();
    }

    public void SelectCard(CardInstance card, CardGridItemView itemView)
    {
        if (isAnimating) return;
        //Hide previously selected card's preview if any
        if (selectedItemView != null && selectedItemView != itemView)
        {
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
}
