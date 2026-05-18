using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class DeckSelectionPanel : MonoBehaviour
{
    public Transform gridContainer;
    public GameObject cardPrefab;

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