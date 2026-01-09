using UnityEngine;
using System.Collections.Generic;
using Lean.Gui;

public class BottomMenuController : MonoBehaviour
{
    [Header("Screens container")]
    public RectTransform screensContainer;

    [Header("Screens (ordre du menu)")]
    public List<RectTransform> screens;

    // Optionally, reference a LeanDrag if present on the screensContainer
    private Lean.Gui.LeanDrag leanDrag;

    void Awake()
    {
        // Try to get LeanDrag if it exists on the screensContainer
        if (screensContainer != null)
        {
            leanDrag = screensContainer.GetComponent<Lean.Gui.LeanDrag>();
        }
    }

    public void GoToScreen(int index)
    {
        if (index < 0 || index >= screens.Count)
        {
            Debug.LogError("Index écran invalide");
            return;
        }
        Debug.Log("Changement vers l'écran : " + index);
        // Simulate LeanDrag transitions/events if LeanDrag is present
        if (leanDrag != null)
        {
            // Simulate OnBeginDrag
            if (leanDrag.BeginTransitions != null)
                leanDrag.BeginTransitions.Begin();
            if (leanDrag.OnBegin != null)
                leanDrag.OnBegin.Invoke();
        }

        // Calculate offset based on anchors (works for any number of screens with normalized anchors)
        float offset = -screensContainer.rect.width * (screens[index].anchorMin.x);
        screensContainer.anchoredPosition = new Vector2(offset, screensContainer.anchoredPosition.y);

        if (leanDrag != null)
        {
            // Simulate OnEndDrag
            if (leanDrag.EndTransitions != null)
                leanDrag.EndTransitions.Begin();
            if (leanDrag.OnEnd != null)
                leanDrag.OnEnd.Invoke();
        }

        // If the selected screen or its children have a CardCollectionController, call RefreshCollection
        var cardCollection = screens[index].GetComponentInChildren<CardCollectionController>(true);
        if (cardCollection != null)
        {
            cardCollection.RefreshCollection();
        }
        var leaderboardController = screens[index].GetComponentInChildren<LeaderboardController>(true);
        if (leaderboardController != null)
        {
            //leaderboardController.RefreshLeaderboard();
        }
        var packCollection = screens[index].GetComponentInChildren<PackCollectionController>(true);
        if (packCollection != null)
        {
            packCollection.RefreshCollection();
        }
    }
}
