using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class CardInfoBox : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI firstTimeValueText;
    [SerializeField] private TextMeshProUGUI subsequentValueText;
    [SerializeField] private TextMeshProUGUI quantityOwnedText;
    [SerializeField] private TextMeshProUGUI totalPCEarnedText;
    [SerializeField] private GameObject infoPanel;

    [Header("Animation Settings")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.15f;
    [SerializeField] private Vector2 offset = new Vector2(150, 0); // Offset from card position

    private static CardInfoBox instance;
    private CanvasGroup canvasGroup;
    private bool isBoxVisible = false;
    private CardData currentCardData = null;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        // Get or add CanvasGroup for animations
        if (infoPanel != null)
        {
            canvasGroup = infoPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null && useAnimation)
            {
                canvasGroup = infoPanel.AddComponent<CanvasGroup>();
            }
            // Force panel to be hidden on start
            infoPanel.SetActive(false);
        }
        
        isBoxVisible = false;
    }

    private void Update()
    {
        // Check for clicks outside the box when it's visible
        if (isBoxVisible && Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            if (infoPanel != null && infoPanel.activeSelf)
            {
                RectTransform infoPanelRect = infoPanel.GetComponent<RectTransform>();
                Canvas canvas = GetComponentInParent<Canvas>();
                
                if (infoPanelRect != null)
                {
                    // Check if click is outside the info panel
                    Vector2 pointerPosition = Pointer.current.position.ReadValue();
                    Vector2 localMousePosition;
                    Camera cam = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                    
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        infoPanelRect,
                        pointerPosition,
                        cam,
                        out localMousePosition))
                    {
                        if (!infoPanelRect.rect.Contains(localMousePosition))
                        {
                            // Click is outside the box
                            Debug.Log("[CardInfoBox] Click outside detected, hiding box");
                            HideInfoBox();
                        }
                    }
                }
            }
        }
    }

    public static CardInfoBox Instance
    {
        get { return instance; }
    }

    public void ShowCardInfo(CardData cardData, RectTransform cardTransform)
    {
        if (cardData == null)
        {
            HideInfoBox();
            return;
        }

        // If clicking on the same card, toggle the box
        if (isBoxVisible && currentCardData != null && currentCardData.cardId == cardData.cardId)
        {
            Debug.Log("[CardInfoBox] Clicking on same card, hiding box");
            HideInfoBox();
            return;
        }

        // Store the current card
        currentCardData = cardData;

        // Update card information
        if (cardImage != null)
            cardImage.sprite = cardData.sprite;
        
        if (cardNameText != null)
            cardNameText.text = cardData.cardName;
        
        if (descriptionText != null)
            descriptionText.text = cardData.description;
        
        if (firstTimeValueText != null)
            firstTimeValueText.text = $"{cardData.FirstTimeValue} PC";
        
        if (subsequentValueText != null)
            subsequentValueText.text = $"{cardData.SubsequentValue} PC";

        // Get quantity owned and calculate total PC earned
        int quantityOwned = 0;
        if (PlayerProfileStore.CARD_COLLECTION.TryGetValue(cardData.cardId, out int qty))
        {
            quantityOwned = qty;
        }

        int totalPCEarned = 0;
        if (quantityOwned > 0)
        {
            totalPCEarned = cardData.FirstTimeValue + Mathf.Max(0, quantityOwned - 1) * cardData.SubsequentValue;
        }

        if (quantityOwnedText != null)
            quantityOwnedText.text = $"Quantité : \n{quantityOwned}";
        
        if (totalPCEarnedText != null)
            totalPCEarnedText.text = $"PC total gagnés : \n{totalPCEarned}";

        // The box keeps its initial position set in Unity Editor
        // No dynamic positioning

        // Show the panel with animation
        if (infoPanel != null)
        {
            if (!isBoxVisible)
            {
                infoPanel.SetActive(true);
                isBoxVisible = true;
                
                if (useAnimation && canvasGroup != null)
                {
                    canvasGroup.alpha = 0;
                    LeanTween.alphaCanvas(canvasGroup, 1f, fadeInDuration).setEaseOutCubic();
                }
                else if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1;
                }
            }
            else
            {
                // Already visible, just ensure it's active
                if (!infoPanel.activeSelf)
                {
                    infoPanel.SetActive(true);
                }
            }
        }
    }

    public void HideInfoBox()
    {
        if (infoPanel != null)
        {
            if (isBoxVisible)
            {
                isBoxVisible = false;
                currentCardData = null;
                
                if (useAnimation && canvasGroup != null)
                {
                    LeanTween.alphaCanvas(canvasGroup, 0f, fadeOutDuration).setEaseInCubic().setOnComplete(() => {
                        infoPanel.SetActive(false);
                    });
                }
                else
                {
                    infoPanel.SetActive(false);
                }
            }
            else
            {
                // Force disable even if not marked as visible
                currentCardData = null;
                infoPanel.SetActive(false);
            }
        }
    }
}
