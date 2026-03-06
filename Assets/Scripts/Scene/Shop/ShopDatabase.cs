using UnityEngine;
using System.Collections.Generic;

public class ShopDatabase : MonoBehaviour
{
    public static ShopDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ShopDatabase>();
                if (_instance == null)
                {
                    Debug.Log("[ShopDatabase] Creating ShopDatabase instance programmatically");
                    GameObject go = new GameObject("ShopDatabase");
                    _instance = go.AddComponent<ShopDatabase>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private static ShopDatabase _instance;

    public List<ShopOffer> Offers { get; private set; } = new();

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
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
