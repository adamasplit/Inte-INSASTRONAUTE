using UnityEngine;
using Unity.Services.RemoteConfig;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ShopRemoteLoader : MonoBehaviour
{
    public static List<ShopOffer> CurrentOffers = new();

    public void UpdateShopFromRemote()
    {
        RemoteConfigService.Instance.FetchCompleted += ApplyShopConfig;
        RemoteConfigService.Instance.FetchConfigs(
            new UserAttributes(),
            new AppAttributes()
        );
        GetComponent<ShopController>().offers = CurrentOffers;
        GetComponent<ShopController>().RefreshShop();
        ShopDatabase.Instance.SetOffers(CurrentOffers);
    }

    public async Task UpdateShopFromRemoteTask()
    {
        var tcs = new TaskCompletionSource<bool>();

        void Handler(ConfigResponse response)
        {
            ApplyShopConfig(response);
            tcs.SetResult(true);
            RemoteConfigService.Instance.FetchCompleted -= Handler;
        }

        RemoteConfigService.Instance.FetchCompleted += Handler;
        RemoteConfigService.Instance.FetchConfigs(
            new UserAttributes(),
            new AppAttributes()
        );

        await tcs.Task;

        GetComponent<ShopController>().offers = CurrentOffers;
        GetComponent<ShopController>().RefreshShop();
    }

    void ApplyShopConfig(ConfigResponse response)
    {
        if (!RemoteConfigService.Instance.appConfig.HasKey("shop_config"))
        {
            Debug.LogWarning("[ShopRemoteLoader] No shop_config found in Remote Config");
            return;
        }

        var json = RemoteConfigService.Instance.appConfig.GetJson("shop_config");
        var config = JsonUtility.FromJson<ShopConfig>(json);

        CurrentOffers = new List<ShopOffer>();
        if (config == null)
        {
            Debug.LogWarning("[ShopRemoteLoader] Failed to parse shop_config");
            return;
        }
        if (config.offers == null || config.offers.Count == 0)
        {
            Debug.LogWarning("[ShopRemoteLoader] No offers found in shop_config");
            return;
        }
        foreach (var dto in config.offers)
        {
            CurrentOffers.Add(new ShopOffer
            {
                purchaseId = dto.purchaseId,
                title = dto.title,
                type = dto.type == "Pack" ? ShopOfferType.Pack : ShopOfferType.Card,
                rewardId = dto.rewardId,
                amount = dto.amount,
                price = dto.price,
            });

            Debug.Log($"[Shop] Loaded offer: {dto.title}, Type: {dto.type} -> {CurrentOffers[CurrentOffers.Count - 1].type}");
        }
    }
}
