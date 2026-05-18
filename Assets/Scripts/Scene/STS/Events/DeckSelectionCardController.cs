using UnityEngine;
using TMPro;
using System.Collections.Generic;
public class DeckSelectionCardController : MonoBehaviour
{
    public CardView view;
    public GameObject selectionOutline;

    CardInstance card;
    DeckSelectionPanel panel;

    bool selected;

    public void Init(CardInstance card, DeckSelectionPanel owner)
    {
        this.card = card;
        panel = owner;

        view.SetCard(card);

        selectionOutline.SetActive(false);
    }

    public void OnClick()
    {
        selected = !selected;

        selectionOutline.SetActive(selected);

        panel.OnCardToggled(this, card, selected);
    }
}