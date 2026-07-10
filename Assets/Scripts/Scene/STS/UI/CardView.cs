using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;
public class CardView : MonoBehaviour,IPointerClickHandler
{
    public GameObject collectionCardRoot;
    public GameObject genericCardRoot;
    public Image collectionCardImage;
    public Image collectionCardDescBg;
    public Image cardBg;
    public GameObject specialCardOverlay;
    public Image whiteOverlay;
    public CardInstance cardInstance;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;
    public Image rarityBorder;
    public Image rarityBorder2;
    public Image rarityBorder3;
    public Image imgBg;
    public Image imgOverlay;
    public Image cardImage;
    public RawImage glowOverlay;
    public TextMeshProUGUI cardTypeText;
    CombatManager combat;
    UIManager ui;
    Character currentTarget;
    bool isInitialized = false;
    public bool isAnimating;
    public RectTransform rootRect;
    public bool selectionPreview;
    public GameObject selectionHighlight;
    public bool isDragging;
    private Coroutine combatClickDeselectRoutine;
    private float lastCombatClickTime = -999f;
    [SerializeField] private float combatDoubleClickThreshold = 0.3f;

    public void toggleSelection()
    {
        selectionPreview = !selectionPreview;
        selectionHighlight.SetActive(selectionPreview);
    }
    private bool IsDescriptionTextClick(PointerEventData eventData)
    {
        if (descriptionText == null || eventData == null)
            return false;

        RectTransform descriptionRect = descriptionText.rectTransform;
        if (descriptionRect != null && RectTransformUtility.RectangleContainsScreenPoint(descriptionRect, eventData.position, eventData.pressEventCamera))
            return true;

        if (collectionCardDescBg != null && collectionCardDescBg.rectTransform != null)
            return RectTransformUtility.RectangleContainsScreenPoint(collectionCardDescBg.rectTransform, eventData.position, eventData.pressEventCamera);

        return false;
    }
    private bool GetTooltipSide()
    {
        return ui != null && ui.handLayout != null && ui.handLayout.cardSide(this);
    }
    private bool IsSelectedCard()
    {
        return ui != null && ui.selectedCard == this;
    }
    private void CancelPendingCombatDeselect()
    {
        if (combatClickDeselectRoutine != null)
        {
            StopCoroutine(combatClickDeselectRoutine);
            combatClickDeselectRoutine = null;
        }
    }
    private IEnumerator DeselectCombatCardAfterDelay()
    {
        yield return new WaitForSecondsRealtime(combatDoubleClickThreshold);
        combatClickDeselectRoutine = null;

        if (ui != null && ui.selectedCard == this)
        {
            ui.Deselect();
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {

        if (SelectionManager.Instance != null && SelectionManager.Instance.selectionMode)
        {
            SelectionManager.Instance.OnCardClicked(cardInstance);
            return;
        }
        if (ui!=null)
        {
            bool isCombatCard = combat != null;
            bool isDoubleClick = isCombatCard && Time.unscaledTime - lastCombatClickTime <= combatDoubleClickThreshold;
            lastCombatClickTime = Time.unscaledTime;

            if (isCombatCard && !IsSelectedCard())
            {
                ui.HideCombatCardPreview();
            }

            if (isDoubleClick)
            {
                CancelPendingCombatDeselect();
                ui.SelectCard(this, true);
                ui.ShowCombatCardPreview(this);
                return;
            }

            if (isCombatCard && IsSelectedCard())
            {
                CancelPendingCombatDeselect();
                combatClickDeselectRoutine = StartCoroutine(DeselectCombatCardAfterDelay());
                return;
            }

            if (IsDescriptionTextClick(eventData)&&IsSelectedCard())
            {
                ShowCardTooltips(GetTooltipSide(), true, true);
                return;
            }
            if (ui.IsSelectingCards()&&combat.currentCard!=cardInstance)
            {
                ui.selectionController.ToggleCard(this);

                ui.RefreshHandLayout();

                return;
            }
            ui.SelectCard(this);
            if (IsDescriptionTextClick(eventData))
            {
                ShowCardTooltips(GetTooltipSide(), true, true);
                return;
            }
        }
        
        RestCardController restCard = GetComponentInParent<RestCardController>();
        if (restCard != null)
        {
            restCard.OnClick();
        }
    }
    void Start()
    {
        combat = FindFirstObjectByType<CombatManager>();
        ui=FindFirstObjectByType<UIManager>();
    }
    public void Set(STSCardData card)
    {
        SetCard(card != null ? new CardInstance(card) : null);
    }
    public void SetCard(CardInstance card)
    {
        cardInstance = card;
        if (card != null)
        {
            if (card.data == null)
            {
                Debug.LogError("CardInstance has null data");
                return;
            }
            if (nameText == null || costText == null || descriptionText == null)
            {
                Debug.LogError("CardView is missing UI references");
                return;
            }
            cardTypeText.text = card.data.type.ToString();
            cardBg.color = SelectableCharacterUtils.getCharacterColor(card.data.favoredCharacter);
            specialCardOverlay.SetActive(card.data.HasTag(CardTag.Unobtainable));
            SetName(card.displayName);
            SetWithCollectionCard(card);
            Color rarityColor = Color.white;
            switch (card.data.rarity)
            {
                case CardRarity.Common: // Light gray
                    rarityColor = new Color(0.8f, 0.8f, 0.8f);
                    break;
                case CardRarity.Uncommon: // White
                    rarityColor = Color.white;
                    break;
                case CardRarity.Rare: //Cyan
                    rarityColor = Color.cyan;
                    break;
                case CardRarity.Epic: // Dark purple
                    rarityColor = new Color(0.5f, 0f, 0.5f);
                    break;
                case CardRarity.Legendary: // Godly gold
                    rarityColor = new Color(1f, 0.84f, 0f);
                    break;
                case CardRarity.Special: // Crimson
                    rarityColor = new Color(0.86f, 0.08f, 0.24f);
                    break;
            }
            rarityBorder.color = rarityColor;
            rarityBorder2.color = rarityColor;
            imgBg.color=rarityColor*0.5f;
            imgOverlay.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.2f);
            
            RefreshDescription();
            if (card.enchantments.Count > 0)
            {
                glowOverlay.gameObject.SetActive(true);
            }
            else
            {
                glowOverlay.gameObject.SetActive(false);
            }
        }
        else
        {
            SetName("");
            SetCost(0);
            SetDescription("");
        }
    }
    public void SetName(string name)
    {
        nameText.text = name;
    }
    public void SetCost(int cost,bool xCost=false)
    {
        if (xCost)
        {
            costText.text = " X";
        }
        else
        {
            if (cardInstance == null || cardInstance.data == null)
            {
                costText.text = cost.ToString();
                return;
            }
            costText.text = cost>cardInstance.data.cost ? $"<color=red>{cost}</color>" : cost<cardInstance.data.cost ? $"<color=green>{cost}</color>" : cost.ToString();
        }
    }
    public void SetDescription(string description)
    {
        descriptionText.text = description;
    }

    public void RefreshDescription(Character target = null, bool force = false, List<Character> targets = null)
    {
        if (combat==null)
            combat = FindFirstObjectByType<CombatManager>();
        if (isInitialized && currentTarget == target && !force)
            return;
        isInitialized = true;
        currentTarget = target;

        if (cardInstance != null)
        {
            EffectContext ctx = new EffectContext
            {
                source = combat==null?null:combat.player,
                target = target,
                combat = combat,
                state = combat==null?null:combat.state,
                card = cardInstance,
                isPreview = true,
                targets = targets ?? (target != null ? new List<Character> { target } : new List<Character>())
            };
            SetCost(cardInstance.Cost(ctx), cardInstance.data.xCost);
            SetDescription(cardInstance.GetDescription(ctx));
        }
    }
    public GameObject enchantTooltipPrefab;
    public Transform enchantTooltipContainer;
    public Transform enchantTooltipContainerLeft;
    public Transform rewardTooltipContainerBelow;
    private struct TooltipData
    {
        public string title;
        public string description;

        public TooltipData(string title, string description)
        {
            this.title = title;
            this.description = description;
        }
    }

    void LateUpdate()
    {
        if (enchantTooltipContainer != null)
            enchantTooltipContainer.rotation = Quaternion.identity;
        if (enchantTooltipContainerLeft != null)
            enchantTooltipContainerLeft.rotation = Quaternion.identity;
        if (rewardTooltipContainerBelow != null)
            rewardTooltipContainerBelow.rotation = Quaternion.identity;
    }
    public void Select(bool rightSide)
    {
        ShowCardTooltips(rightSide, true, false);
    }
    public void Deselect()
    {
        HideCardTooltips();
    }
    public void ShowCardTooltips(bool leftSide=true, bool includeCardTooltip=false,bool showDescription=false)
    {
        if (enchantTooltipContainer != null)
        {
            foreach (Transform child in enchantTooltipContainer)
            {
                Destroy(child.gameObject);
            }
        }
        if (enchantTooltipContainerLeft != null)
        {
            foreach (Transform child in enchantTooltipContainerLeft)
            {
                Destroy(child.gameObject);
            }
        }
        if (cardInstance == null) return;

        Transform targetContainer = leftSide ? enchantTooltipContainerLeft : enchantTooltipContainer;
        if (targetContainer == null)
            targetContainer = leftSide ? enchantTooltipContainer : enchantTooltipContainerLeft;
        if (targetContainer == null || enchantTooltipPrefab == null)
            return;

        List<TooltipData> tooltips = new();

        if (includeCardTooltip)
        {
            EffectContext ctx = new EffectContext
            {
                source = combat == null ? null : combat.player,
                target = null,
                combat = combat,
                state = combat == null ? null : combat.state,
                card = cardInstance,
                isPreview = true,
                targets = new List<Character>()
            };
            //tooltips.Add(new TooltipData(cardInstance.data.cardName, cardInstance.GetDescription(ctx)));

            foreach (var effect in cardInstance.GetEffects())
            {
                AddStatusTooltip(effect, tooltips, ctx);
                AddCreatedCardTooltip(effect, tooltips);
            }
            if (showDescription)
            {
                tooltips.Add(new TooltipData("Description", cardInstance.lastDescription));
            }
        }

        foreach (CardEnchantment enchant in cardInstance.enchantments)
        {
            tooltips.Add(new TooltipData(enchant.data.name, enchant.data.description));
        }
        AddTagTooltips(tooltips);
        foreach (TooltipData tooltipData in tooltips)
        {
            GameObject tooltipObj = Instantiate(enchantTooltipPrefab, targetContainer);
            Tooltip tooltip = tooltipObj.GetComponent<Tooltip>();
            tooltip.SetTooltip(null, tooltipData.title, tooltipData.description);
        }
        // Force layout rebuild to ensure the tooltips are positioned correctly
        LayoutRebuilder.ForceRebuildLayoutImmediate(targetContainer.GetComponent<RectTransform>());
    }
    public void HideCardTooltips()
    {
        if (enchantTooltipContainer != null)
        {
            foreach (Transform child in enchantTooltipContainer)
            {
                Destroy(child.gameObject);
            }
        }
        if (enchantTooltipContainerLeft != null)
        {
            foreach (Transform child in enchantTooltipContainerLeft)
            {
                Destroy(child.gameObject);
            }
        }
        if (rewardTooltipContainerBelow != null)
        {
            foreach (Transform child in rewardTooltipContainerBelow)
            {
                Destroy(child.gameObject);
            }
        }
    }

    public void ShowRewardCardTooltips()
    {
        if (cardInstance == null || enchantTooltipPrefab == null)
            return;

        EnsureRewardTooltipContainerBelow();
        if (rewardTooltipContainerBelow == null)
            return;

        foreach (Transform child in rewardTooltipContainerBelow)
        {
            Destroy(child.gameObject);
        }

        List<TooltipData> tooltips = new();
        EffectContext ctx = new EffectContext
        {
            source = combat == null ? null : combat.player,
            target = null,
            combat = combat,
            state = combat == null ? null : combat.state,
            card = cardInstance,
            isPreview = true,
            targets = new List<Character>()
        };

        //tooltips.Add(new TooltipData(cardInstance.data.cardName, cardInstance.GetDescription(ctx)));

        foreach (var effect in cardInstance.GetEffects())
        {
            AddStatusTooltip(effect, tooltips, ctx);
            AddCreatedCardTooltip(effect, tooltips);
        }

        foreach (CardEnchantment enchant in cardInstance.enchantments)
        {
            tooltips.Add(new TooltipData(enchant.data.name, enchant.data.description));
        }
        AddTagTooltips(tooltips);
        foreach (TooltipData tooltipData in tooltips)
        {
            GameObject tooltipObj = Instantiate(enchantTooltipPrefab, rewardTooltipContainerBelow);
            Tooltip tooltip = tooltipObj.GetComponent<Tooltip>();
            tooltip.SetTooltip(null, tooltipData.title, tooltipData.description);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rewardTooltipContainerBelow.GetComponent<RectTransform>());
    }
    private void AddTagTooltips(List<TooltipData> tooltips)
    {
        if (cardInstance == null || cardInstance.data == null)
            return;

        foreach (CardTag tag in cardInstance.data.tags)
        {
            (string tagName, string tagDescription) = GetTagDescription(tag);
            if (!string.IsNullOrEmpty(tagDescription))
            {
                bool alreadyAdded = tooltips.Exists(t => t.title == tagName && t.description == tagDescription);
                if (!alreadyAdded)
                    tooltips.Add(new TooltipData(tagName, tagDescription));
            }
        }
    }
    private (string, string) GetTagDescription(CardTag tag)
    {
        switch (tag)
        {
            case CardTag.Exhaust:
                return ("Épuisement", "Quand cette carte est jouée, elle disparaît et ne peut pas revenir dans votre pioche pendant ce combat.");
            case CardTag.Retain:
                return ("Retenue", "Cette carte reste dans votre main à la fin du tour.");
            case CardTag.Ethereal:
                return ("Éthérée", "Si cette carte est dans votre main à la fin du tour, elle est épuisée.");
            case CardTag.Infinite:
                return ("Infinie", "Cette carte peut être jouée un nombre illimité de fois dans un tour.");
            case CardTag.Innate:
                return ("Innée", "Cette carte est toujours dans votre main au début du combat.");
            // Add more cases for other tags as needed
            default:
                return ("", "");
        }
    }

    private void EnsureRewardTooltipContainerBelow()
    {
        if (rewardTooltipContainerBelow != null)
            return;

        RectTransform anchorParent = rootRect != null ? rootRect : GetComponent<RectTransform>();
        if (anchorParent == null)
            return;

        GameObject containerObject = new GameObject("RewardTooltipContainerBelow", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        containerObject.transform.SetParent(anchorParent, false);

        RectTransform containerRect = containerObject.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0f);
        containerRect.anchorMax = new Vector2(0.5f, 0f);
        containerRect.pivot = new Vector2(0.5f, 1f);
        containerRect.anchoredPosition = new Vector2(0f, -(anchorParent.rect.height * 0.5f + 12f));

        VerticalLayoutGroup layout = containerObject.GetComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 6f;

        ContentSizeFitter fitter = containerObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        rewardTooltipContainerBelow = containerRect;
    }

    private void AddStatusTooltip(EffectEntry effect, List<TooltipData> tooltips,EffectContext context = null)
    {
        if (effect.type != EffectType.Status)
            return;

        StatusEffect status = StatusEffect.Factory(effect.statusType, effect.value, effect.duration, effect.cardID,effect.index);
        if (status == null || !status.generic)
            return;

        bool alreadyAdded = tooltips.Exists(t => t.title == status.Name && t.description == status.Desc(effect.targetSelf));
        if (!alreadyAdded)
            tooltips.Add(new TooltipData(status.Name, status.Desc(effect.targetSelf)));
    }

    private void AddCreatedCardTooltip(EffectEntry effect, List<TooltipData> tooltips)
    {
        if (string.IsNullOrEmpty(effect.cardID))
            return;

        //if (effect.type != EffectType.AddCardToHand && effect.type != EffectType.AddCardToDrawPile && effect.type != EffectType.AddCardToDiscardPile && effect.type != EffectType.AddRandomCardToHand)
        //    return;

        STSCardData cardData = STSCardDatabase.Get(effect.cardID);
        if (cardData == null)
            return;

        bool alreadyAdded = tooltips.Exists(t => t.title == cardData.cardName);
        if (alreadyAdded)
            return;

        CardInstance previewCard = new CardInstance(cardData);
        EffectContext context = new EffectContext
        {
            card = previewCard,
            isPreview = true,
            targets = new List<Character>()
        };

        tooltips.Add(new TooltipData(cardData.cardName, previewCard.GetDescription(context)));
    }
    bool isFlashing;
    // Only one flash at a time: if a new flash starts while one is already playing, it will restart the animation
    public void Flash()
    {
        if (isFlashing)
        {
            StopAllCoroutines();
            StartCoroutine(FlashWhite());
        }
        else
        {
            StartCoroutine(FlashWhite());
        }
    }
    public IEnumerator FlashWhite()
    {
        whiteOverlay.gameObject.SetActive(true);
        float elapsed = 0f;
        float duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.5f, 0f, elapsed / duration);
            whiteOverlay.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
        whiteOverlay.gameObject.SetActive(false);
    }

    public IEnumerator PlayExhaustAnimation()
    {
        if (whiteOverlay != null)
        {
            whiteOverlay.gameObject.SetActive(true);
        }

        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 startScale = rootRect.localScale;
        Color startOverlayColor = whiteOverlay != null ? whiteOverlay.color : Color.white;
        float startAlpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float xScale = Mathf.Lerp(startScale.x, 0, t);
            float yScale = Mathf.Lerp(startScale.y, startScale.y * 2f, t);
            rootRect.localScale = new Vector3(xScale, yScale, startScale.z);

            if (whiteOverlay != null)
            {
                whiteOverlay.color = new Color(startOverlayColor.r, startOverlayColor.g, startOverlayColor.b, Mathf.Lerp(startAlpha, 1f, t));
            }

            yield return null;
        }

        rootRect.localScale = new Vector3(0f, startScale.y * 2f, startScale.z);

        if (whiteOverlay != null)
        {
            whiteOverlay.color = new Color(startOverlayColor.r, startOverlayColor.g, startOverlayColor.b, 1f);
        }
    }

    public void SetWithCollectionCard(CardInstance card)
    {
        if (card == null || card.data == null)
            return;

        cardImage.sprite = card.data.icon;
        cardImage.preserveAspect = true;
        imgBg.enabled = true;
        imgOverlay.enabled = true;
        collectionCardRoot.SetActive(false);
        genericCardRoot.SetActive(true);
        descriptionText.color = Color.white;
        nameText.color = Color.white;
        nameText.transform.localScale = Vector3.one;

        string collectionCardId = card.data.GetCollectionCardId();
        if (string.IsNullOrWhiteSpace(collectionCardId))
            return;

        ApplyCollectionCardVisualAsync(card, collectionCardId);
    }

    private async void ApplyCollectionCardVisualAsync(CardInstance card, string collectionCardId)
    {
        try
        {
            Sprite sprite = await STSCardDatabase.GetCollectionCardSpriteAsync(collectionCardId);
            if (sprite == null || cardInstance != card || !string.Equals(card.data.GetCollectionCardId(), collectionCardId, StringComparison.Ordinal))
                return;

            ApplyCollectionCardVisual(card, sprite);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load collection card artwork for '{collectionCardId}': {ex}");
        }
    }

    private void ApplyCollectionCardVisual(CardInstance card, Sprite sprite)
    {
        if (card == null || sprite == null)
            return;

        collectionCardImage.sprite = sprite;
        imgBg.enabled = false;
        imgOverlay.enabled = false;
        cardImage.preserveAspect = false;
        collectionCardRoot.SetActive(true);
        genericCardRoot.SetActive(false);
        nameText.transform.localScale = Vector3.one * 0.6f;

        if (collectionCardImage != null && collectionCardImage.sprite != null)
        {
            Sprite s = collectionCardImage.sprite;
            Texture2D tex = s.texture;
            Rect rect = s.textureRect;
            float px = rect.x + rect.width * 0.5f;
            float py = rect.y + rect.height * 0.95f;

            int ix = Mathf.Clamp(Mathf.RoundToInt(px), 0, tex.width - 1);
            int iy = Mathf.Clamp(Mathf.RoundToInt(py), 0, tex.height - 1);

            Color sample = Color.clear;
            try
            {
                sample = tex.GetPixel(ix, iy);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sampling pixel from texture: {e.Message}");
                sample = Color.white;
            }

            collectionCardDescBg.color = sample;
            float brightness = (sample.r + sample.g + sample.b) / 3f;
            Color textColor = brightness < 0.5f ? Color.white : Color.black;
            descriptionText.color = textColor;
            nameText.color = textColor;
        }
    }


}