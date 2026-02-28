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
    
    // Current mode settings
    private bool inCollection = false;
    private bool inDeck = false;
    private bool GetInfos = false;
    
    private void OnEnable()
    {
        //RefreshCollection(inCollection, inDeck, true);
        Debug.Log("Refreshing card collection UI...");
    }

    // Set the mode before calling RefreshCollection
    public void SetMode(bool inCollection = false, bool inDeck = false, bool GetInfos = false)
    {
        this.inCollection = inCollection;
        this.inDeck = inDeck;
        this.GetInfos = GetInfos;
        Debug.Log($"[CardCollectionController] Mode set - inCollection: {inCollection}, inDeck: {inDeck}, GetInfos: {GetInfos}");
    }

    // Refresh using current mode (backward compatible with optional parameters)
    public void RefreshCollection(bool? inCollection = null, bool? inDeck = null, bool? GetInfos = null)
    {
        // Update mode if parameters are provided
        if (inCollection.HasValue) this.inCollection = inCollection.Value;
        if (inDeck.HasValue) this.inDeck = inDeck.Value;
        if (GetInfos.HasValue) this.GetInfos = GetInfos.Value;
        
        Debug.Log($"[CardCollectionController] Refreshing - inCollection: {this.inCollection}, inDeck: {this.inDeck}, GetInfos: {this.GetInfos}");
        
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
                    this.inCollection,
                    this.inDeck,
                    this.GetInfos
                );
            }
        }
    }
}