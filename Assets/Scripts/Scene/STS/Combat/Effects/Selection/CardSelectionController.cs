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

    /// <summary>Une carte que la demande accepte, et que le joueur peut donc désigner.</summary>
    /// <remarks>
    /// La demande porte un filtre — telle famille, tel coût, telle étiquette — et le panneau de
    /// sélection le respectait déjà en ne montrant que les cartes retenues. Choisir dans sa main
    /// se fait sans panneau, carte par carte, et ce chemin-là ne consultait rien : n'importe
    /// quelle carte se laissait cocher. Le serveur, lui, refusait la sélection, et le joueur ne
    /// s'en sortait qu'en devinant les cartes attendues.
    /// </remarks>
    public bool Accepts(CardInstance card)
    {
        if (currentRequest == null || card == null)
            return false;

        return currentRequest.filter == null || currentRequest.filter(card);
    }

    public void ToggleCard(CardView card)
    {
        if (currentRequest == null)
            return;

        var instance = card.cardInstance;

        if (!Accepts(instance))
            return;

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