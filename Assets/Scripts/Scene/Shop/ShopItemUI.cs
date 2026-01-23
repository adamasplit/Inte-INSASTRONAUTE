using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public Image icon;
    public TextMeshProUGUI priceText;
    public Button buyButton;
    private ShopOffer offer;

    public void Setup(ShopOffer data)
    {
        offer = data;
        titleText.text = data.title;
        if (icon != null)
        {
            if (data.type == ShopOfferType.Card)
            {
                icon.sprite = ShopDatabase.Instance.cardDatabase.Get(data.rewardId).sprite;
            }
            else // Pack
            {
                if (ShopDatabase.Instance == null)
                {
                    Debug.LogError("ShopDatabase.Instance is null");
                }
                else if (ShopDatabase.Instance.packDatabase == null)
                {
                    Debug.LogError("ShopDatabase.Instance.packDatabase is null");
                }
                else
                {
                    var pack = ShopDatabase.Instance.packDatabase.Get(data.rewardId);
                    if (pack == null)
                    {
                        Debug.LogError($"Pack with rewardId {data.rewardId} is null");
                    }
                    else if (pack.packSprite == null)
                    {
                        Debug.LogError($"pack.packSprite for rewardId {data.rewardId} is null");
                    }
                    else
                    {
                        icon.sprite = pack.packSprite;
                        Debug.Log($"Successfully set icon.sprite for rewardId {data.rewardId}");
                    }
                }
            }
        }
        priceText.text = data.price.ToString();

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    async void OnBuyClicked()
    {
        Debug.Log("[ShopItemUI]Buying offer: " + offer.purchaseId.ToUpper());
        await StoreService.BuyAsync(offer.purchaseId);
    }
}
