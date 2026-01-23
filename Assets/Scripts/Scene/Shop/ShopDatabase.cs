using UnityEngine;
using System.Collections.Generic;

public class ShopDatabase : MonoBehaviour
{
    public static ShopDatabase Instance;

    [Header("Databases")]
    public PackDatabase packDatabase;
    public CardDatabase cardDatabase;

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

        packDatabase.Init();
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
        return packDatabase.Get(offer.rewardId);
    }
}
