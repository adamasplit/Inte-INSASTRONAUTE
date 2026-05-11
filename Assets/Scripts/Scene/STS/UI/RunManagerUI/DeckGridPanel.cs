using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class DeckGridPanel : MonoBehaviour
{
    public GridLayoutGroup gridLayout;
    public Transform gridContainer;
    public GameObject cardGridItemPrefab;
    
    [Header("Preview")]
    public Transform previewContainer;
    public GameObject previewCardPrefab;
    public CanvasGroup previewCanvasGroup;
    
    [Header("Controls")]
    public Button closeButton;
    public CanvasGroup panelCanvasGroup;

    private CardGridItemView selectedItemView;

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    public void Show(List<CardInstance> deck)
    {
        // Clear existing grid items
        foreach (Transform child in gridContainer)
            Destroy(child.gameObject);

        selectedItemView = null;

        // Create grid items
        foreach (var card in deck)
        {
            var obj = Instantiate(cardGridItemPrefab, gridContainer);
            var itemView = obj.GetComponent<CardGridItemView>();
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
        selectedItemView = itemView;
        ShowPreview(card);
    }

    void ShowPreview(CardInstance card)
    {
        // Clear existing preview
        foreach (Transform child in previewContainer)
            Destroy(child.gameObject);

        // Create preview card
        var previewObj = Instantiate(previewCardPrefab, previewContainer);
        var previewView = previewObj.GetComponent<CardView>();
        if (previewView != null)
        {
            previewView.SetCard(card);
        }

        // Show preview
        if (previewCanvasGroup != null)
        {
            previewCanvasGroup.alpha = 1f;
            previewCanvasGroup.blocksRaycasts = true;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)previewContainer);
    }

    void HidePreview()
    {
        if (previewCanvasGroup != null)
        {
            previewCanvasGroup.alpha = 0f;
            previewCanvasGroup.blocksRaycasts = false;
        }

        foreach (Transform child in previewContainer)
            Destroy(child.gameObject);
    }

    public void Hide()
    {
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }
}
