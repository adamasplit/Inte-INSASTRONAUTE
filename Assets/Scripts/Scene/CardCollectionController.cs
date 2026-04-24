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
    private bool GetInfos = false;
    
    private void OnEnable()
    {
        //RefreshCollection(inCollection, inDeck, true);
        Debug.Log("Refreshing card collection UI...");
    }

    // Set the mode before calling RefreshCollection
    public void SetMode(bool inCollection = false, bool GetInfos = false)
    {
        this.inCollection = inCollection;
        this.GetInfos = GetInfos;
        Debug.Log($"[CardCollectionController] Mode set - inCollection: {inCollection}, GetInfos: {GetInfos}");
    }

    // Refresh using current mode (backward compatible with optional parameters)
    public void RefreshCollection(bool? inCollection = null, bool? GetInfos = null)
    {
        // Update mode if parameters are provided
        if (inCollection.HasValue) this.inCollection = inCollection.Value;
        if (GetInfos.HasValue) this.GetInfos = GetInfos.Value;
        
        Debug.Log($"[CardCollectionController] Refreshing - inCollection: {this.inCollection}, GetInfos: {this.GetInfos}");
        
        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        foreach (var card in CardDatabase.Instance.cards)
        {
            //Debug.Log($"[CardCollectionController] Checking card: {card.cardId}");
            if ((PlayerProfileStore.CARD_COLLECTION.TryGetValue(card.cardId, out int qty)) || this.GetInfos)
            {
                //Debug.Log($"[CardCollectionController] Adding card to UI: {card.cardId} with quantity {qty}");
                var item = Instantiate(cardPrefab, cardContainer);

                item.SetCardData(
                    qty,
                    card.sprite,
                    card,
                    this.inCollection,
                    this.GetInfos
                );
            }
        }
    }
}