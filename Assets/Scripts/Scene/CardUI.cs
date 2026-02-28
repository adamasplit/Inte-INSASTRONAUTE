using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Lean.Gui;

public class CardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] public Image cardImage;
    private CardData cardData;
    private bool inCollection = false;
    private bool GetInfos = false;
    private bool inDeck = false;
    public void SetCardData(int number, Sprite sprite,CardData cardData = null,bool inCollection = false,bool inDeck = false, bool GetInfos = false)
    {
        if (numberText!=null)
            numberText.text = number.ToString();
        cardImage.sprite = sprite;
        this.cardData = cardData;
        this.inCollection = inCollection;
        this.inDeck = inDeck;        
        this.GetInfos = GetInfos;    
    }
    public void InterpretClick()
    {
        if (GetInfos)
        {
            if (cardData != null && CardInfoBox.Instance != null)
            {
                Debug.Log($"[CardUI] Showing info for card: {cardData.cardId}");
                RectTransform rectTransform = GetComponent<RectTransform>();
                CardInfoBox.Instance.ShowCardInfo(cardData, rectTransform);
            }
        }
        else if (inCollection)
        {
            DeckManager.Instance.TryAddCard(cardData);
        }
        else if (inDeck)
        {
            DeckManager.Instance.RemoveCard(cardData);
        }
    }
}