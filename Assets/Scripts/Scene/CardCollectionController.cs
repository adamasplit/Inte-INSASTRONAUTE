using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Collections;
public class CardCollectionController : MonoBehaviour
{
    public Transform cardContainer;
    public CardUI cardPrefab;
    public bool inCollection = false;
    public bool inDeck = false;
    private void OnEnable()
    {
        RefreshCollection(inCollection, inDeck);
        Debug.Log("Refreshing card collection UI...");
    }

    public void RefreshCollection(bool inCollection=false, bool inDeck=false)
    {
        Debug.Log("[CardCollectionController] Refreshing card collection UI...");
        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        foreach (var card in CardDatabase.Instance.cards)
        {
            //Debug.Log($"[CardCollectionController] Checking card: {card.cardId}");
            if (PlayerProfileStore.CARD_COLLECTION.TryGetValue(card.cardId, out int qty))
            {
                //Debug.Log($"[CardCollectionController] Adding card to UI: {card.cardId} with quantity {qty}");
                var item = Instantiate(cardPrefab, cardContainer);

                item.SetCardData(
                    qty,
                    card.sprite,
                    card,
                    inCollection,
                    inDeck
                );
            }
        }
    }
}