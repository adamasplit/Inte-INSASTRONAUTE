using System.Collections.Generic;
using UnityEngine;
public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance;

    public List<CardInstance> selectedCards = new();

    System.Action<List<CardInstance>> onComplete;
    public bool selectionMode;
    public int requiredCount;

    void Awake()
    {
        Instance = this;
    }

    public void StartSelection(
        int count,
        System.Action<List<CardInstance>> callback)
    {
        selectedCards.Clear();
        onComplete = callback;

        // activer UI deck selection mode
        EnableCardSelection(count);
    }

    public void OnCardClicked(CardInstance card)
    {
        selectedCards.Add(card);

        if (selectedCards.Count >= requiredCount)
        {
            onComplete?.Invoke(selectedCards);
            DisableCardSelection();
        }
    }

    public void EnableCardSelection(int count)
    {
        selectionMode = true;
        requiredCount = count;
    }

    public void DisableCardSelection()
    {
        selectionMode = false;
        requiredCount = 0;
    }
}