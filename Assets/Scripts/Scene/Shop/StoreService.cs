using Unity.Services.Economy;
using UnityEngine;
using System.Threading.Tasks;


public static class StoreService
{
    public static async Task BuyAsync(string purchaseId)
    {
        // Null check for ShopDatabase - can happen on WebGL due to scene loading order
        if (ShopDatabase.Instance == null)
        {
            Debug.LogError("[StoreService] CRITICAL: ShopDatabase.Instance is null! Cannot process purchase. " +
                "Ensure ShopDatabase exists in the scene and is initialized before attempting a purchase.");
            return;
        }
        
        var offer = ShopDatabase.Instance.GetOffer(purchaseId);
        if (offer == null)
        {
            Debug.LogError($"[StoreService] Offer not found: {purchaseId}");
            return;
        }
        
        if (PlayerProfileStore.TOKEN < offer.price)
        {
            Debug.LogWarning("[StoreService] Not enough TOKEN to make purchase: " + purchaseId);
            return;
        }
        
        try
        {
            await EconomyService.Instance.Purchases
                .MakeVirtualPurchaseAsync(purchaseId.ToUpper());

            Debug.Log("[StoreService] Purchase OK: " + purchaseId);

            await ApplyRewardAsync(purchaseId);
        }
        catch (EconomyException e)
        {
            Debug.LogError($"[StoreService] Purchase failed: {e.Reason}");
        }
        await GameObject.FindFirstObjectByType<PlayerStatusController>().RefreshStatusAsync();
        foreach (var tokenUI in GameObject.FindObjectsByType<UpdateDataUI>(FindObjectsSortMode.None))
        {
            tokenUI.RefreshDataUI();
            //if (tokenUI.dataKey == "TOKEN")
            //{
            //    tokenUI.alterDataUI(-ShopDatabase.Instance.GetOffer(purchaseId).price);
            //}
        }
        
    }

    static async Task ApplyRewardAsync(string purchaseId)
    {
        if (ShopDatabase.Instance == null)
        {
            Debug.LogError("[StoreService] CRITICAL: ShopDatabase.Instance is null in ApplyRewardAsync!");
            return;
        }
        
        var offer = ShopDatabase.Instance.GetOffer(purchaseId);
        Debug.Log("[StoreService] Applying reward for offer: " + purchaseId);

        if (offer == null)
        {
            Debug.LogError("[StoreService] Offer not found: " + purchaseId);
            return;
        }
        switch (offer.type)
        {
            case ShopOfferType.Pack:
                await PlayerProfileStore.AddPackAsync(
                    PackDatabase.Instance.Get(offer.rewardId),
                    offer.amount
                );
                break;

            case ShopOfferType.Card:
                await PlayerProfileStore.AddCardAsync(
                    CardDatabase.Instance.Get(offer.rewardId).cardId,
                    offer.amount
                );
                break;
        }
    }
}
