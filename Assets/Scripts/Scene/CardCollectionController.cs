using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class CardCollectionController : MonoBehaviour
{
    public Transform cardContainer;
    public GameObject cardPrefab;
    public void AddCardToCollection(int number, string cardSprite, Color? borderColor = null)
    {
        GameObject newCard = Instantiate(cardPrefab, cardContainer);
        CardUI cardUI = newCard.GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.SetCardData(number, cardSprite, borderColor);
        }
    }

    void Start()
    {
        // Example usage
        PopulateDummyCards();
    }

    void PopulateDummyCards()
    {
        for (int i = 1; i <= 15; i++)
        {
            Color? borderColor = (i % 2 == 0) ? (Color?)Color.red : Color.green;
            AddCardToCollection(i, "tests" + i, borderColor);
        }
    }
}