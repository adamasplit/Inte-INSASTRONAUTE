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
                RectTransform rectTransform = GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x / 2, rectTransform.sizeDelta.y);
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

    void OnBuyClicked()
    {
        var ui = FindFirstObjectByType<MainUIBinder>();
        if (ui == null)
        {
            Debug.LogWarning("[ShopItemUI] MainUIBinder not found, skipping confirmation popup.");
            _ = ExecutePurchaseAsync();
            return;
        }

        ui.ShowConfirmation(
            "Confirmer l'achat",
            $"Acheter {offer.title} pour {offer.price} TOKEN ?",
            async () => await ExecutePurchaseAsync(),
            () => Debug.Log("[ShopItemUI] Purchase cancelled by user.")
        );
    }

    async System.Threading.Tasks.Task ExecutePurchaseAsync()
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
        var purchaseResult = await StoreService.BuyAsync(offer.purchaseId);

        var mainUi = FindFirstObjectByType<MainUIBinder>();
        if (mainUi != null)
        {
            mainUi.ShowNotification(purchaseResult.Message);
        }
        else
        {
            Debug.LogWarning("[ShopItemUI] MainUIBinder not found, cannot display purchase notification.");
        }

        FindFirstObjectByType<ShopController>().loadingScreen.CompleteLoading();
        FindFirstObjectByType<ShopController>().loadingScreen.HideWithFade();
    }
}
