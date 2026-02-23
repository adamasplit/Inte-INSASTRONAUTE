using UnityEngine;
using System.Collections.Generic;

public class ShopController : MonoBehaviour
{
    public Transform packContainer;
    public Transform cardContainer;
    public LoadingScreen loadingScreen;

    public ShopItemUI itemPrefab;

    public List<ShopOffer> offers;

    void OnEnable()
    {
        RefreshShop();
    }

    public void RefreshShop()
    {
        if (loadingScreen ==null)
        {
            Debug.LogError("ShopController: loadingScreen is null");
            loadingScreen = FindFirstObjectByType<PlayerStatusController>()?.loadingScreen;
        }
        Clear(packContainer);
        Clear(cardContainer);

        foreach (var offer in offers)
        {
            var parent = (offer.type == ShopOfferType.Pack
                ? packContainer
                : cardContainer);

            var item = Instantiate(itemPrefab, parent);
            item.Setup(offer);
        }
    }

    void Clear(Transform t)
    {
        foreach (Transform c in t)
            Destroy(c.gameObject);
    }
}
