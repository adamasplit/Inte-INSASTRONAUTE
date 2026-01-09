using UnityEngine;

public class CardDebugGrant : MonoBehaviour
{
    [Header("Debug packs (Editor only)")]
    public bool grantOnStart = true;
    public CardData[] cardsToGrant;
    public int amountPerCard = 3;

    private async void Start()
    {
    #if UNITY_EDITOR
        if (!grantOnStart) return;

        foreach (var card in cardsToGrant)
        {
            if (card != null)
            {
                Debug.Log($"[CardDebugGrant] Granting card {card.cardId} x{amountPerCard}");
                //await PlayerProfileStore.AddCardAsync(card.cardId, amountPerCard);
            }
        }

        Debug.Log("[CardDebugGrant]DEBUG: Cards granted on start");
    #endif
    }
}
