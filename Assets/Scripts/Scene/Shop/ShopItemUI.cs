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
                icon.sprite = CardDatabase.Instance.Get(data.rewardId).sprite;
            }
            else // Pack
            {
                if (PackDatabase.Instance == null)
                {
                    Debug.LogError("PackDatabase.Instance is null");
                }
                else
                {
                    var pack = PackDatabase.Instance.Get(data.rewardId);
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
        ShopController shopController = FindFirstObjectByType<ShopController>();
        if (shopController == null)
        {
            Debug.LogError("ShopController is null");
            return;
        }
        if (shopController.loadingScreen == null)
        {
            Debug.LogError("ShopController's loadingScreen is null");
            shopController.loadingScreen = FindFirstObjectByType<PlayerStatusController>()?.loadingScreen;
        }
        shopController.loadingScreen.gameObject.SetActive(true);

        FindFirstObjectByType<ShopController>().loadingScreen.Initialize(1);
        Debug.Log("[ShopItemUI]Buying offer: " + offer.purchaseId.ToUpper());
        await StoreService.BuyAsync(offer.purchaseId);
        FindFirstObjectByType<ShopController>().loadingScreen.CompleteLoading();
        FindFirstObjectByType<ShopController>().loadingScreen.HideWithFade();
    }
}
