using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardGridItemView : MonoBehaviour, IPointerClickHandler
{
    public CardView cardView;

    private CardInstance cardInstance;
    private DeckGridPanel parentPanel;

    public void Init(CardInstance card, DeckGridPanel panel)
    {
        cardInstance = card;
        parentPanel = panel;

        if (card.data != null)
        {
            cardView.SetCard(card);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"CardGridItemView clicked: {cardInstance?.data?.name}");
        if (parentPanel != null && cardInstance != null)
        {
            parentPanel.SelectCard(cardInstance, this);
        }
    }
}
