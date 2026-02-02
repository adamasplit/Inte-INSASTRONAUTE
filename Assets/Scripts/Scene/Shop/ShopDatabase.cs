using UnityEngine;
using System.Collections.Generic;

public class ShopDatabase : MonoBehaviour
{
    public static ShopDatabase Instance;

    public List<ShopOffer> Offers { get; private set; } = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetOffers(List<ShopOffer> offers)
    {
        Offers = offers;
    }

    public ShopOffer GetOffer(string purchaseId)
    {
        return Offers.Find(o => o.purchaseId == purchaseId);
    }

    public PackData ResolvePack(ShopOffer offer)
    {
        return PackDatabase.Instance.Get(offer.rewardId);
    }
}
