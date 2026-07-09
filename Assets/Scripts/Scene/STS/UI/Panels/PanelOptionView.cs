using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
public class PanelOptionView : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI label;
    public Button button;

    private readonly List<GameObject> cardPreviewObjects = new();
    private RectTransform cardPreviewRoot;

    public void Init(PanelOption option)
    {
        ClearCardPreviews();

        label.text = option.text;
        label.text+="\n<color=grey><size=28>";
        foreach(var entry in option.entries)
        {
            string desc=EventOptionDescription.GetDescription(entry);
            if (!string.IsNullOrEmpty(desc))
            {
                label.text += desc + ".\n";
            }
        }
        label.text += "</size></color>";
        icon.sprite = option.icon;
        if (icon.sprite==null)
        {
            icon.gameObject.SetActive(false);
        }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => {option.action?.Invoke(); });
        if (option.action == null)
        {
            Debug.LogWarning("PanelOptionView initialized with null action for option: " + option.text);
        }

        BuildCardPreviews(option);

        UILayoutHelper.ApplyPreferredSizeAfterFrame(this, transform as RectTransform, fitWidth: true, fitHeight: true, extraWidth: 24f, extraHeight: 16f);
        StartCoroutine(ValidateLayoutConstraintsNextFrame());
    }

    private void BuildCardPreviews(PanelOption option)
    {
        if (option == null || option.previewCardIds == null || option.previewCardIds.Count == 0)
        {
            return;
        }

        UIManager uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager == null || uiManager.cardButtonPrefab == null)
        {
            return;
        }

        cardPreviewRoot = new GameObject("CardPreviewRoot", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        cardPreviewRoot.SetParent(transform, false);

        HorizontalLayoutGroup layout = cardPreviewRoot.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 8f;

        foreach (string cardId in option.previewCardIds)
        {
            STSCardData cardData = STSCardDatabase.Get(cardId);
            if (cardData == null)
            {
                continue;
            }

            GameObject previewObject = Instantiate(uiManager.cardButtonPrefab, cardPreviewRoot);
            previewObject.transform.localScale = Vector3.one * 0.65f;

            CardView cardView = previewObject.GetComponentInChildren<CardView>();
            if (cardView != null)
            {
                cardView.Set(cardData);
            }

            CardDrag drag = previewObject.GetComponentInChildren<CardDrag>(true);
            if (drag != null)
            {
                drag.enabled = false;
            }

            Button previewButton = previewObject.GetComponentInChildren<Button>(true);
            if (previewButton != null)
            {
                previewButton.interactable = false;
            }

            CanvasGroup canvasGroup = previewObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = previewObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            cardPreviewObjects.Add(previewObject);
        }
    }

    private void ClearCardPreviews()
    {
        foreach (GameObject previewObject in cardPreviewObjects)
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
            }
        }

        cardPreviewObjects.Clear();

        if (cardPreviewRoot != null)
        {
            Destroy(cardPreviewRoot.gameObject);
            cardPreviewRoot = null;
        }
    }

    private IEnumerator ValidateLayoutConstraintsNextFrame()
    {
        yield return null;

        GridLayoutGroup grid = GetComponentInParent<GridLayoutGroup>(true);
        if (grid != null)
        {
            Debug.LogWarning("PanelOptionView: parent GridLayoutGroup found. Grid cell size overrides preferred width/height and can force square options.", grid);
        }

        AspectRatioFitter fitter = GetComponentInParent<AspectRatioFitter>(true);
        if (fitter != null)
        {
            Debug.LogWarning("PanelOptionView: parent AspectRatioFitter found. Aspect ratio constraints can force square options.", fitter);
        }
    }
}