using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CardSelectionController : MonoBehaviour
{
    public GameObject root;
    public TextMeshProUGUI messageText;
    public Button confirmButton;

    public bool Active => currentRequest != null;

    CardSelectionRequest currentRequest;
    void Awake()
    {
        root.SetActive(false);
    }

    public void Open(CardSelectionRequest request)
    {
        currentRequest = request;

        root.SetActive(true);

        messageText.text = request.message;

        confirmButton.interactable = false;

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(Confirm);
    }

    public void Close()
    {
        currentRequest = null;

        root.SetActive(false);
    }

    public void ToggleCard(CardView card)
    {
        if (currentRequest == null)
            return;

        var instance = card.cardInstance;

        if (currentRequest.selectedCards.Contains(instance))
        {
            currentRequest.selectedCards.Remove(instance);
            card.selectionPreview = false;
        }
        else
        {
            if (currentRequest.selectedCards.Count >= currentRequest.amount)
                return;

            currentRequest.selectedCards.Add(instance);
            card.selectionPreview = true;
        }

        confirmButton.interactable =
            currentRequest.selectedCards.Count ==
            currentRequest.amount;
    }

    void Confirm()
    {
        currentRequest.completed = true;
        Close();
    }

    public IEnumerator WaitForSelection()
    {
        while (currentRequest != null &&
               !currentRequest.completed)
        {
            yield return null;
        }
    }
}