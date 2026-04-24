using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Lean.Gui;
using System.Collections;

public class CardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] public Image cardImage;
    private CardData cardData;
    private bool inCollection = false;
    private bool GetInfos = false;
    public void SetCardData(int number, Sprite sprite,CardData cardData = null,bool inCollection = false, bool GetInfos = false)
    {
        if (numberText!=null)
            numberText.text = number.ToString();
        cardImage.sprite = sprite;
        this.cardData = cardData;
        this.inCollection = inCollection;      
        this.GetInfos = GetInfos;    
        if (number==0)
        {
            cardImage.sprite=Resources.Load<Sprite>("Sprites/Cartes/DosCarte");
            if (cardImage.sprite == null)
            {
                Debug.LogError("Failed to load card back sprite for GetInfos mode.");
            }
        }
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
    }

    public IEnumerator BlackFlash()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Color originalColor = cardImage.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = elapsed / duration;
            cardImage.color = new Color(alpha, alpha, alpha, 1f);
            yield return null;
        }
    }
}