using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerDeckCardItem : MonoBehaviour
{
    [SerializeField] private CardView cardView;
    [SerializeField] private Toggle includeToggle;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private TextMeshProUGUI lockedReasonText;

    private string cardKey;
    private Action<string, bool> onToggleChanged;
    private bool suppressToggleCallback;

    public string CardKey => cardKey;

    private void Awake()
    {
        if (includeToggle != null)
        {
            includeToggle.onValueChanged.RemoveAllListeners();
            includeToggle.onValueChanged.AddListener(HandleToggleChanged);
        }
    }

    public void Bind(
        STSCardData cardData,
        string uniqueKey,
        bool selected,
        bool interactable,
        string lockedReason,
        Action<string, bool> toggleChanged
    )
    {
        cardKey = uniqueKey;
        onToggleChanged = toggleChanged;

        if (cardView == null)
        {
            cardView = GetComponentInChildren<CardView>(true);
        }

        if (cardView != null && cardData != null)
        {
            cardView.gameObject.SetActive(true);
            cardView.SetCard(new CardInstance(cardData));
        }

        if (includeToggle != null)
        {
            suppressToggleCallback = true;
            includeToggle.isOn = selected;
            includeToggle.interactable = interactable;
            suppressToggleCallback = false;
        }

        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!interactable);
        }

        if (lockedReasonText != null)
        {
            lockedReasonText.text = interactable ? string.Empty : lockedReason;
        }
    }

    private void HandleToggleChanged(bool value)
    {
        if (suppressToggleCallback)
            return;

        onToggleChanged?.Invoke(cardKey, value);
    }
}
