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
    private bool inDeck = false;
    public void SetCardData(int number, Sprite sprite,CardData cardData = null,bool inCollection = false,bool inDeck = false)
    {
        if (numberText!=null)
            numberText.text = number.ToString();
        cardImage.sprite = sprite;
        this.cardData = cardData;
        this.inCollection = inCollection;
        this.inDeck = inDeck;
    }
    public void InterpretClick()
    {
        if (inCollection)
        {
            DeckManager.Instance.TryAddCard(cardData);
        }
        else if (inDeck)
        {
            DeckManager.Instance.RemoveCard(cardData);
        }
    }
}