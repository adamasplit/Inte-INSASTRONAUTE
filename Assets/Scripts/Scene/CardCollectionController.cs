using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using System.Threading.Tasks;
public class CardCollectionController : MonoBehaviour
{
    public Transform cardContainer;
    public CardUI cardPrefab;

    [Header("Data")]
    public CardData[] allCards;

    void Start()
    {
        allCards = Resources.LoadAll<CardData>("Cards");
    }
    private void OnEnable()
    {
        
        RefreshCollection();
    }

    public void RefreshCollection()
    {
        Debug.Log("Refreshing card collection UI...");
        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        foreach (var card in allCards)
        {
            Debug.Log($"Checking card: {card.cardId}");
            if (PlayerProfileStore.CARD_COLLECTION.TryGetValue(card.cardId, out int qty))
            {
                Debug.Log($"Adding card to UI: {card.cardId} with quantity {qty}");
                var item = Instantiate(cardPrefab, cardContainer);

                item.SetCardData(
                    qty,
                    card.sprite,
                    card.borderColor
                );
            }
        }
    }
    
    //public void AddCardToCollection(int number, string cardSprite, Color? borderColor = null)
    //{
    //    GameObject newCard = Instantiate(cardPrefab, cardContainer);
    //    CardUI cardUI = newCard.GetComponent<CardUI>();
    //    if (cardUI != null)
    //    {
    //        cardUI.SetCardData(number, cardSprite, borderColor);
    //    }
    //}
//
    //void Start()
    //{
    //    // Example usage
    //    PopulateDummyCards();
    //}
//
    //void PopulateDummyCards()
    //{
    //    for (int i = 1; i <= 15; i++)
    //    {
    //        Color? borderColor = (i % 2 == 0) ? (Color?)Color.red : Color.green;
    //        AddCardToCollection(i, "tests" + i, borderColor);
    //    }
    //}
}