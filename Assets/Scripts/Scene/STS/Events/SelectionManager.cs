using System.Collections.Generic;
using UnityEngine;
public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance;

    public List<STSCardData> selectedCards = new();

    System.Action<List<STSCardData>> onComplete;

    void Awake()
    {
        Instance = this;
    }

    public void StartSelection(
        int count,
        System.Action<List<STSCardData>> callback)
    {
        selectedCards.Clear();
        onComplete = callback;

        // activer UI deck selection mode
        GlobalUIManager.Instance.EnableCardSelection(count);
    }

    public void OnCardClicked(STSCardData card)
    {
        selectedCards.Add(card);

        if (selectedCards.Count >= GlobalUIManager.Instance.requiredCount)
        {
            onComplete?.Invoke(selectedCards);
            GlobalUIManager.Instance.DisableCardSelection();
        }
    }
}