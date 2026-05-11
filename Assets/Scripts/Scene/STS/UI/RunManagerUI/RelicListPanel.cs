using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class RelicListPanel : MonoBehaviour
{
    public ScrollRect scrollRect;
    public Transform container;
    public GameObject relicItemPrefab;
    public Button closeButton;
    public CanvasGroup canvasGroup;

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    public void Show(List<Relic> relics)
    {
        // Clear existing items
        foreach (Transform child in container)
            Destroy(child.gameObject);

        // Add relic items
        foreach (var relic in relics)
        {
            var obj = Instantiate(relicItemPrefab, container);
            var view = obj.GetComponent<RelicListItemView>();
            if (view != null)
                view.Init(relic);
        }

        // Rebuild layout
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)container);

        // Show panel
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }
}
