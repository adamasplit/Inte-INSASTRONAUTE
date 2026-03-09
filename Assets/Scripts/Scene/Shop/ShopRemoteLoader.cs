using UnityEngine;
using Unity.Services.RemoteConfig;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ShopRemoteLoader : MonoBehaviour
{
    public static List<ShopOffer> CurrentOffers = new();

    public void UpdateShopFromRemote()
    {
        // NOTE: the fetch is async; ApplyShopConfig will update the UI once the response
        // arrives. Do NOT read CurrentOffers synchronously here — it is still empty.
        void OnFetched(ConfigResponse response)
        {
            RemoteConfigService.Instance.FetchCompleted -= OnFetched;
            ApplyShopConfig(response);
            GetComponent<ShopController>().offers = CurrentOffers;
            GetComponent<ShopController>().RefreshShop();

            if (ShopDatabase.Instance != null)
                ShopDatabase.Instance.SetOffers(CurrentOffers);
            else
                Debug.LogError("[ShopRemoteLoader] ShopDatabase.Instance is null!");
        }

        RemoteConfigService.Instance.FetchCompleted += OnFetched;
        RemoteConfigService.Instance.FetchConfigs(new UserAttributes(), new AppAttributes());
    }

    public async Task UpdateShopFromRemoteTask()
    {
        var tcs = new TaskCompletionSource<bool>();

        // On WebGL, Unity Remote Config may fire FetchCompleted twice: once immediately
        // from the browser HTTP cache (which can return a stale/empty response) and then
        // again with the live network result. We only resolve the TCS when we receive a
        // response that actually contains valid offers, so the pipeline is never unblocked
        // with an empty shop. A 10-second timeout guards against a permanently absent key.
        void Handler(ConfigResponse response)
        {
            ApplyShopConfig(response);
            if (CurrentOffers.Count > 0)
            {
                RemoteConfigService.Instance.FetchCompleted -= Handler;
                tcs.TrySetResult(true);
            }
            else
            {
                Debug.LogWarning("[ShopRemoteLoader] FetchCompleted but no offers parsed; " +
                    "waiting for next response (possible stale cache response).");
            }
        }

        RemoteConfigService.Instance.FetchCompleted += Handler;
        RemoteConfigService.Instance.FetchConfigs(
            new UserAttributes(),
            new AppAttributes()
        );

        // Wait up to 10 s for a valid config; if it never arrives keep going with empty shop.
        var timeout = Task.Delay(10_000);
        await Task.WhenAny(tcs.Task, timeout);

        if (!tcs.Task.IsCompleted)
        {
            RemoteConfigService.Instance.FetchCompleted -= Handler;
            Debug.LogWarning("[ShopRemoteLoader] Timed out waiting for Remote Config with valid offers.");
        }

        GetComponent<ShopController>().offers = CurrentOffers;
        GetComponent<ShopController>().RefreshShop();

        if (ShopDatabase.Instance != null)
            ShopDatabase.Instance.SetOffers(CurrentOffers);
        else
            Debug.LogError("[ShopRemoteLoader] ShopDatabase.Instance is null in UpdateShopFromRemoteTask!");
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
