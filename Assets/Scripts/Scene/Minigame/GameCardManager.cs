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
            availableCards.AddRange(collectionController.allCards);
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
        if (playerHand.Count < GameManager.Instance.maxCardsInHand)
        {
            Debug.Log("Adding a new card to hand");
            CardData newCard = GetRandomCard();
            // Instantiate cardPrefab and set its data to newCard
            GameObject cardObject = Instantiate(cardPrefab, transform);
            GameCardUI cardUI = cardObject.GetComponent<GameCardUI>();
            cardUI.Initialize(newCard);
            playerHand.Add(cardObject);
        }
        else
        {
            foreach (var cardObj in playerHand)
            {
                if (cardObj == null)
                {
                    playerHand.Remove(cardObj);
                    break;
                }
            }
        }
    }
}