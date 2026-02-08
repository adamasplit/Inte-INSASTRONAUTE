using Unity.Services.Economy;
using UnityEngine;
using System.Threading.Tasks;


public static class StoreService
{
    public static async Task BuyAsync(string purchaseId)
    {
        if (PlayerProfileStore.TOKEN < ShopDatabase.Instance.GetOffer(purchaseId).price)
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
