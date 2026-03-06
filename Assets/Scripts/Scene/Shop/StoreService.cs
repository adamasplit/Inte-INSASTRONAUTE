using Unity.Services.Economy;
using UnityEngine;
using System.Threading.Tasks;


public static class StoreService
{
    public readonly struct StorePurchaseResult
    {
        public bool Success { get; }
        public string Message { get; }

        public StorePurchaseResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }

    public static async Task<StorePurchaseResult> BuyAsync(string purchaseId)
    {
        // Null check for ShopDatabase - can happen on WebGL due to scene loading order
        if (ShopDatabase.Instance == null)
        {
            Debug.LogError("[StoreService] CRITICAL: ShopDatabase.Instance is null! Cannot process purchase. " +
                "Ensure ShopDatabase exists in the scene and is initialized before attempting a purchase.");
            await RefreshUiAsync();
            return new StorePurchaseResult(false, "Purchase failed: shop data is unavailable.");
        }
        
        var offer = ShopDatabase.Instance.GetOffer(purchaseId);
        if (offer == null)
        {
            Debug.LogError($"[StoreService] Offer not found: {purchaseId}");
            await RefreshUiAsync();
            return new StorePurchaseResult(false, "Purchase failed: offer not found.");
        }
        
        if (PlayerProfileStore.TOKEN < offer.price)
        {
            Debug.LogWarning("[StoreService] Not enough TOKEN to make purchase: " + purchaseId);
            await RefreshUiAsync();
            return new StorePurchaseResult(false, $"Purchase failed: not enough TOKEN ({offer.price} required).");
        }
        
        try
        {
            await EconomyService.Instance.Purchases
                .MakeVirtualPurchaseAsync(purchaseId.ToUpper());

            Debug.Log("[StoreService] Purchase OK: " + purchaseId);

            var rewardResult = await ApplyRewardAsync(purchaseId);
            if (!rewardResult.Success)
            {
                await RefreshUiAsync();
                return rewardResult;
            }

            await RefreshUiAsync();
            return new StorePurchaseResult(true, $"Purchase successful: {offer.title}");
        }
        catch (EconomyException e)
        {
            Debug.LogError($"[StoreService] Purchase failed: {e.Reason}");
            await RefreshUiAsync();
            return new StorePurchaseResult(false, $"Purchase failed: {e.Reason}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StoreService] Unexpected purchase error: {e.Message}");
            await RefreshUiAsync();
            return new StorePurchaseResult(false, "Purchase failed: unexpected error.");
        }
    }

    static async Task RefreshUiAsync()
    {
        var playerStatusController = GameObject.FindFirstObjectByType<PlayerStatusController>();
        if (playerStatusController != null)
        {
            await playerStatusController.RefreshStatusAsync();
        }

        foreach (var tokenUI in GameObject.FindObjectsByType<UpdateDataUI>(FindObjectsSortMode.None))
        {
            tokenUI.RefreshDataUI();
            //if (tokenUI.dataKey == "TOKEN")
            //{
            //    tokenUI.alterDataUI(-ShopDatabase.Instance.GetOffer(purchaseId).price);
            //}
        }
        
    }

    static async Task<StorePurchaseResult> ApplyRewardAsync(string purchaseId)
    {
        if (ShopDatabase.Instance == null)
        {
            Debug.LogError("[StoreService] CRITICAL: ShopDatabase.Instance is null in ApplyRewardAsync!");
            return new StorePurchaseResult(false, "Purchase failed: shop data became unavailable.");
        }
        
        var offer = ShopDatabase.Instance.GetOffer(purchaseId);
        Debug.Log("[StoreService] Applying reward for offer: " + purchaseId);

        if (offer == null)
        {
            Debug.LogError("[StoreService] Offer not found: " + purchaseId);
            return new StorePurchaseResult(false, "Purchase failed: offer not found while applying reward.");
        }
        switch (offer.type)
        {
            case ShopOfferType.Pack:
                await PlayerProfileStore.AddPackAsync(
                    PackDatabase.Instance.Get(offer.rewardId),
                    offer.amount
                );
                return new StorePurchaseResult(true, string.Empty);

            case ShopOfferType.Card:
                await PlayerProfileStore.AddCardAsync(
                    CardDatabase.Instance.Get(offer.rewardId).cardId,
                    offer.amount
                );
                return new StorePurchaseResult(true, string.Empty);

            default:
                return new StorePurchaseResult(false, "Purchase failed: unsupported reward type.");
        }
    }
}
