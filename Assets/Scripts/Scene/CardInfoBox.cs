using TMPro;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    [SerializeField] private TextMeshProUGUI miniGameEffectText;
    [SerializeField] private TextMeshProUGUI deckStatusText;
    [SerializeField] private Button deckActionButton;
    [SerializeField] private TextMeshProUGUI deckActionButtonText;
    [SerializeField] private GameObject digitalOnlyGroup;
    [SerializeField] private GameObject infoPanel;

    [Header("3D Preview")]
    [SerializeField] private Transform card3DPreviewRoot;
    [SerializeField] private GameObject fallback3DPrefab;
    [SerializeField] private RectTransform previewInteractionArea;
    [SerializeField] private bool autoRotate3D = false;
    [SerializeField] private float autoRotateSpeed = 30f;

    [Header("3D Drag Tilt")]
    [SerializeField] private float maxTiltAngle = 10f;
    [SerializeField] private float tiltSensitivity = 0.06f;
    [SerializeField] private float tiltSmoothing = 12f;
    [SerializeField] private float tiltReturnSpeed = 8f;

    [Header("Animation Settings")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.15f;

    [Header("Card Image Zoom")]
    [SerializeField] private float zoomScale = 1.5f;
    [SerializeField] private float zoomDuration = 0.2f;

    private static CardInfoBox instance;
    private CanvasGroup canvasGroup;
    private bool isBoxVisible;
    private CardData currentCardData;
    private bool isImageZoomed;
    private Vector3 originalImageScale;
    private GameObject current3DPreview;
    private GameObject current3DSourcePrefab;
    private Card3DPreviewVisual current3DVisual;
    private CollectionDisplayMode currentMode = CollectionDisplayMode.Digital;
    private bool currentCanAddToDeck;
    private bool isDraggingPreview;
    private Vector2 lastPointerPosition;
    private Vector2 targetTilt;
    private Vector2 smoothedTilt;

    public static CardInfoBox Instance => instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (cardImage != null)
        {
            originalImageScale = cardImage.transform.localScale;
            EventTrigger trigger = cardImage.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = cardImage.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(_ => ToggleImageZoom());
            trigger.triggers.Add(entry);
        }

        if (deckActionButton != null)
            deckActionButton.onClick.AddListener(OnDeckActionClicked);

        if (infoPanel != null)
        {
            canvasGroup = infoPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null && useAnimation)
                canvasGroup = infoPanel.AddComponent<CanvasGroup>();
            infoPanel.SetActive(false);
        }

        isBoxVisible = false;
    }

    private void OnDestroy()
    {
        if (deckActionButton != null)
            deckActionButton.onClick.RemoveListener(OnDeckActionClicked);
    }

    private void Update()
    {
        if (isBoxVisible && current3DPreview != null)
        {
            UpdatePreviewDrag();
            UpdatePreviewTiltAndRotation();
        }

        if (isBoxVisible && Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            if (infoPanel != null && infoPanel.activeSelf)
            {
                RectTransform infoPanelRect = infoPanel.GetComponent<RectTransform>();
                Canvas canvas = GetComponentInParent<Canvas>();

                if (infoPanelRect != null)
                {
                    Vector2 pointerPosition = Pointer.current.position.ReadValue();
                    Camera cam = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(infoPanelRect, pointerPosition, cam, out Vector2 localMousePosition))
                    {
                        if (!infoPanelRect.rect.Contains(localMousePosition))
                            HideInfoBox();
                    }
                }
            }
        }
    }

    public void ShowCardInfo(CardData cardData, RectTransform cardTransform)
    {
        ShowCardInfo(cardData, cardTransform, CollectionDisplayMode.Digital, false);
    }

    public void ShowCardInfo(CardData cardData, RectTransform cardTransform, CollectionDisplayMode displayMode, bool canAddToDeck)
    {
        if (cardData == null)
        {
            HideInfoBox();
            return;
        }

        if (isBoxVisible && currentCardData != null && currentCardData.cardId == cardData.cardId && currentMode == displayMode)
        {
            HideInfoBox();
            return;
        }

        currentCardData = cardData;
        currentMode = displayMode;
        currentCanAddToDeck = canAddToDeck;

        if (isImageZoomed)
        {
            isImageZoomed = false;
            if (cardImage != null)
                LeanTween.scale(cardImage.gameObject, originalImageScale, zoomDuration * 0.5f);
        }

        int quantityOwned = displayMode == CollectionDisplayMode.Physical
            ? PlayerProfileStore.GetPhysicalCardQuantity(cardData.cardId)
            : PlayerProfileStore.GetCardQuantity(cardData.cardId);

        if (cardImage != null)
            cardImage.sprite = quantityOwned == 0
                ? Resources.Load<Sprite>("Sprites/Cartes/DosCarte")
                : cardData.sprite;

        if (cardNameText != null)
            cardNameText.text = quantityOwned == 0 ? "???" : cardData.cardName;

        if (descriptionText != null)
            descriptionText.text = quantityOwned == 0
                ? "Obtenez cette carte pour révéler sa description."
                : cardData.description;

        if (firstTimeValueText != null)
            firstTimeValueText.text = $"{cardData.FirstTimeValue} PC";

        if (subsequentValueText != null)
            subsequentValueText.text = $"{cardData.SubsequentValue} PC";

        int totalPCEarned = quantityOwned > 0
            ? cardData.FirstTimeValue + Mathf.Max(0, quantityOwned - 1) * cardData.SubsequentValue
            : 0;

        if (quantityOwnedText != null)
            quantityOwnedText.text = $"Quantite :\n{quantityOwned}";

        if (totalPCEarnedText != null)
            totalPCEarnedText.text = $"PC total gagnes :\n{totalPCEarned}";

        if (digitalOnlyGroup != null)
            digitalOnlyGroup.SetActive(displayMode == CollectionDisplayMode.Digital);

        if (miniGameEffectText != null)
        {
            string effect = string.IsNullOrWhiteSpace(cardData.miniGameEffectDescription)
                ? cardData.description
                : cardData.miniGameEffectDescription;
            miniGameEffectText.text = displayMode == CollectionDisplayMode.Digital && quantityOwned > 0
                ? effect
                : "Effet de mini-jeu indisponible pour cette carte.";
        }

        RefreshDeckSection(cardData, quantityOwned);
        Rebuild3DPreview(cardData, quantityOwned > 0);

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
            else if (!infoPanel.activeSelf)
            {
                infoPanel.SetActive(true);
            }
        }
    }

    private void RefreshDeckSection(CardData cardData, int quantityOwned)
    {
        bool isDigital = currentMode == CollectionDisplayMode.Digital;
        bool canUseDeckAction = isDigital && currentCanAddToDeck && quantityOwned > 0;

        if (deckActionButton != null)
            deckActionButton.gameObject.SetActive(canUseDeckAction);

        if (deckStatusText != null)
        {
            if (!isDigital)
            {
                deckStatusText.text = "Deck builder reserve a la collection digitale.";
            }
            else
            {
                int copies = PlayerProfileStore.GetDeckCopies(cardData.cardId);
                int size = PlayerProfileStore.DECK_SELECTION.Count;
                deckStatusText.text = $"Deck: {size}/{GameConstants.MaxDeckSize} | Cette carte: {copies}/{GameConstants.MaxCopiesPerCard}";
            }
        }

        if (deckActionButtonText != null && isDigital)
        {
            bool canAddMore = PlayerProfileStore.GetDeckCopies(cardData.cardId) < GameConstants.MaxCopiesPerCard;
            deckActionButtonText.text = canAddMore ? "Ajouter au deck" : "Retirer du deck";
        }
    }

    private async void OnDeckActionClicked()
    {
        if (currentCardData == null || currentMode != CollectionDisplayMode.Digital)
            return;

        bool canAddMore = PlayerProfileStore.GetDeckCopies(currentCardData.cardId) < GameConstants.MaxCopiesPerCard;

        if (canAddMore)
        {
            if (!PlayerProfileStore.TryAddCardToDeck(currentCardData.cardId, out string reason))
            {
                NotificationSystem.Instance?.ShowNotification(reason);
                return;
            }
        }
        else
        {
            if (!PlayerProfileStore.TryRemoveCardFromDeck(currentCardData.cardId))
            {
                NotificationSystem.Instance?.ShowNotification("Carte absente du deck.");
                return;
            }
        }

        await PlayerProfileStore.SaveDeckSelectionAsync();
        RefreshDeckSection(currentCardData, PlayerProfileStore.GetCardQuantity(currentCardData.cardId));
    }

    private void Rebuild3DPreview(CardData cardData, bool isOwned)
    {
        if (card3DPreviewRoot == null)
            return;

        if (!isOwned)
        {
            Clear3DPreview();
            return;
        }

        GameObject prefab = cardData.card3DPrefab != null ? cardData.card3DPrefab : fallback3DPrefab;
        if (prefab == null)
            return;

        if (current3DPreview == null || current3DSourcePrefab != prefab)
        {
            Clear3DPreview();
            current3DPreview = Instantiate(prefab, card3DPreviewRoot);
            current3DSourcePrefab = prefab;
            current3DVisual = current3DPreview.GetComponentInChildren<Card3DPreviewVisual>(true);
        }

        current3DPreview.transform.localPosition = Vector3.zero;
        current3DPreview.transform.localRotation = Quaternion.identity;

        ApplyPreviewSprite(cardData.sprite);
        current3DVisual?.ApplyRarity(cardData.rarity);
        targetTilt = Vector2.zero;
        smoothedTilt = Vector2.zero;
    }

    private void ApplyPreviewSprite(Sprite sprite)
    {
        if (current3DPreview == null) return;

        if (current3DVisual != null)
        {
            current3DVisual.SetSprite(sprite);
            return;
        }

        var uiFace = current3DPreview.GetComponentsInChildren<Image>(true)
            .FirstOrDefault(img => img.gameObject.name.ToLowerInvariant().Contains("face"));
        if (uiFace == null)
            uiFace = current3DPreview.GetComponentInChildren<Image>(true);
        if (uiFace != null)
            uiFace.sprite = sprite;

        var spriteFace = current3DPreview.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(sr => sr.gameObject.name.ToLowerInvariant().Contains("face"));
        if (spriteFace == null)
            spriteFace = current3DPreview.GetComponentInChildren<SpriteRenderer>(true);
        if (spriteFace != null)
            spriteFace.sprite = sprite;
    }

    private void UpdatePreviewDrag()
    {
        if (Pointer.current == null || !Pointer.current.press.isPressed)
        {
            isDraggingPreview = false;
            targetTilt = Vector2.Lerp(targetTilt, Vector2.zero, Time.deltaTime * tiltReturnSpeed);
            return;
        }

        var pointer = Pointer.current.position.ReadValue();

        if (!isDraggingPreview)
        {
            if (Pointer.current.press.wasPressedThisFrame && IsPointerInsidePreviewArea(pointer))
            {
                isDraggingPreview = true;
                lastPointerPosition = pointer;
            }
            return;
        }

        Vector2 delta = pointer - lastPointerPosition;
        lastPointerPosition = pointer;

        targetTilt.x = Mathf.Clamp(targetTilt.x - delta.y * tiltSensitivity, -maxTiltAngle, maxTiltAngle);
        targetTilt.y = Mathf.Clamp(targetTilt.y + delta.x * tiltSensitivity, -maxTiltAngle, maxTiltAngle);
    }

    private bool IsPointerInsidePreviewArea(Vector2 pointer)
    {
        var area = previewInteractionArea;
        if (area == null)
            area = card3DPreviewRoot as RectTransform;

        if (area == null)
            return false;

        Canvas canvas = area.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(area, pointer, cam);
    }

    private void UpdatePreviewTiltAndRotation()
    {
        smoothedTilt = Vector2.Lerp(smoothedTilt, targetTilt, Time.deltaTime * tiltSmoothing);

        Quaternion tiltRotation = Quaternion.Euler(smoothedTilt.x, smoothedTilt.y, 0f);

        if (autoRotate3D && !isDraggingPreview)
        {
            tiltRotation *= Quaternion.Euler(0f, Time.deltaTime * autoRotateSpeed, 0f);
        }

        current3DPreview.transform.localRotation = tiltRotation;
    }

    private void Clear3DPreview()
    {
        if (current3DPreview != null)
            Destroy(current3DPreview);

        current3DPreview = null;
        current3DSourcePrefab = null;
        current3DVisual = null;
        isDraggingPreview = false;
        targetTilt = Vector2.zero;
        smoothedTilt = Vector2.zero;
    }

    private void ToggleImageZoom()
    {
        if (cardImage == null) return;
        isImageZoomed = !isImageZoomed;
        Vector3 targetScale = isImageZoomed ? originalImageScale * zoomScale : originalImageScale;
        LeanTween.scale(cardImage.gameObject, targetScale, zoomDuration).setEaseOutBack();
    }

    public void HideInfoBox()
    {
        if (infoPanel != null)
        {
            if (isBoxVisible)
            {
                isBoxVisible = false;
                currentCardData = null;

                if (isImageZoomed && cardImage != null)
                {
                    isImageZoomed = false;
                    LeanTween.scale(cardImage.gameObject, originalImageScale, zoomDuration * 0.5f);
                }

                Clear3DPreview();

                if (useAnimation && canvasGroup != null)
                {
                    LeanTween.alphaCanvas(canvasGroup, 0f, fadeOutDuration).setEaseInCubic().setOnComplete(() =>
                    {
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
                currentCardData = null;
                if (isImageZoomed && cardImage != null)
                {
                    isImageZoomed = false;
                    cardImage.transform.localScale = originalImageScale;
                }

                Clear3DPreview();

                infoPanel.SetActive(false);
            }
        }
    }
}
