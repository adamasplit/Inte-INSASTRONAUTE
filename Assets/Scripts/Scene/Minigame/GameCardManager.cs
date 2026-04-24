using UnityEngine;
using System.Collections.Generic;
public class GameCardManager : MonoBehaviour
{
    public List<CardData> availableCards;
    public List<GameObject> playerHand = new List<GameObject>();
    public GameObject cardPrefab;
    public void Init()
    {
        if (FindFirstObjectByType<CardCollectionController>() != null)
        {
            CardCollectionController collectionController = FindFirstObjectByType<CardCollectionController>();
            collectionController.RefreshCollection();
            availableCards = new List<CardData>(DeckManager.Instance.deck);
        }
        else
        {
            Debug.LogError("CardCollectionController not found in the scene.");
            availableCards=new List<CardData>(Resources.LoadAll<CardData>("Cards"));
        }
    }
    public CardData GetRandomCard()
    {
        int index = Random.Range(0, availableCards.Count);
        return availableCards[index];
    }

    void Update()
    {
        if (GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        playerHand.RemoveAll(c => c == null);

        if (playerHand.Count < GameManager.Instance.maxCardsInHand)
        {
            CardData newCard = GetRandomCard();
            GameObject cardObject = Instantiate(cardPrefab, transform);
            GameCardUI cardUI = cardObject.GetComponent<GameCardUI>();
            cardUI.Initialize(newCard);
            playerHand.Add(cardObject);
        }
    }
}