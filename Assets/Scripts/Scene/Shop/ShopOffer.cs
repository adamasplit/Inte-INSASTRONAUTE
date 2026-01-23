using System.Collections.Generic;
using UnityEngine;
public enum ShopOfferType
{
    Pack,
    Card
}
[System.Serializable]
public class ShopConfig
{
    public int version;
    public List<ShopOfferDTO> offers;
}

[System.Serializable]
public class ShopOffer
{
    public string purchaseId;
    public string title;
    public ShopOfferType type;
    public string rewardId;
    public int amount;
    public bool enabled;
    public int price;
}
[System.Serializable]
public class ShopOfferDTO
{
    public string purchaseId;
    public string title;
    public string type;
    public string rewardId;
    public int amount;
    public bool enabled;
    public int price;
}
