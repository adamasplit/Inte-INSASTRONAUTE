using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class DeckSelectionPanel : MonoBehaviour
{
    public Transform gridContainer;
    public GameObject cardPrefab;

    [Header("Depth")]
    [SerializeField] private bool normalizeCardDepth = true;
    [SerializeField] private float cardLocalZ = 0f;

    public TextMeshProUGUI counterText;
    public Button confirmButton;

    int requiredCount;

    List<CardInstance> selectedCards = new();

    System.Action<List<CardInstance>> onComplete;
    public static DeckSelectionPanel Instance;
    void Awake()
    {
        Instance = this;
    }

    public void Open(
        string title,
        int count,
        System.Action<List<CardInstance>> callback)
    {
        gameObject.SetActive(true);

        requiredCount = count;
        onComplete = callback;

        selectedCards.Clear();

        BuildDeck();

        RefreshCounter();
    }

    void BuildDeck()
    {
        foreach (Transform child in gridContainer)
            Destroy(child.gameObject);

        foreach (var card in RunManager.Instance.deck)
        {
            var obj = Instantiate(cardPrefab, gridContainer);

            var ctrl = obj.GetComponent<DeckSelectionCardController>();
            ctrl.Init(card, this);

            EnsureItemVisible(obj);
        }

        UILayoutHelper.RebuildAfterFrame(this, gridContainer as RectTransform);
    }

    private void EnsureItemVisible(GameObject item)
    {
        if (item == null)
            return;

        item.SetActive(true);
        item.transform.localScale = Vector3.one;
        item.transform.SetAsLastSibling();

        NormalizeItemDepth(item.transform);

        CanvasGroup[] canvasGroups = item.GetComponentsInChildren<CanvasGroup>(true);
        foreach (CanvasGroup cg in canvasGroups)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        NormalizeAllItemsDepth();
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

    public void OnCardToggled(
        DeckSelectionCardController ctrl,
        CardInstance card,
        bool selected)
    {
        if (selected)
        {
            if (selectedCards.Count >= requiredCount)
            {
                ctrl.selectionOutline.SetActive(false);
                return;
            }

            selectedCards.Add(card);
        }
        else
        {
            selectedCards.Remove(card);
        }

        RefreshCounter();
    }

    void RefreshCounter()
    {
        counterText.text =
            $"{selectedCards.Count}/{requiredCount} selected";

        confirmButton.interactable =
            selectedCards.Count == requiredCount;
    }

    public void OnConfirm()
    {
        onComplete?.Invoke(selectedCards);

        Close();
    }

    public void OnCancel()
    {
        Close();
    }

    void Close()
    {
        gameObject.SetActive(false);
    }
}