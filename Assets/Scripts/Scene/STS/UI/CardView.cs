using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;
public class CardView : MonoBehaviour,IPointerClickHandler
{
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
    public Image cardImage;
    public RawImage glowOverlay;
    public TextMeshProUGUI cardTypeText;
    CombatManager combat;
    UIManager ui;
    Character currentTarget;
    bool isInitialized = false;
    public bool isAnimating;
    public RectTransform rootRect;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (GlobalUIManager.Instance != null && GlobalUIManager.Instance.selectionMode)
        {
            SelectionManager.Instance.OnCardClicked(cardInstance.data);
            return;
        }
        if (ui!=null)
        {
            ui.SelectCard(this);
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
            cardBg.color = SelectableCharacterUtils.getCharacterColor(card.data.favoredCharacter);
            if (card.data.favoredCharacter == SelectableCharacter.Impossible )
            {
                specialCardOverlay.SetActive(true);
            }
            SetName(card.data.cardName);
            
            cardTypeText.text = card.data.type.ToString();
            nameText.text+= "\n<i><color=grey>" + (card.data.collectionCard != null && card.data.collectionCard.cardName != card.data.cardName ? card.data.collectionCard.cardName : "")+ "</color></i>";
            cardImage.sprite = card.data.collectionCard != null ? card.data.collectionCard.sprite : null;
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
            costText.text = cost>cardInstance.data.cost ? $"<color=red>{cost}</color>" : cost<cardInstance.data.cost ? $"<color=green>{cost}</color>" : cost.ToString();
            costText.text = cost.ToString()+"</color>";
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
    void LateUpdate()
    {
        if (enchantTooltipContainer != null)
            enchantTooltipContainer.rotation = Quaternion.identity;
        if (enchantTooltipContainerLeft != null)
            enchantTooltipContainerLeft.rotation = Quaternion.identity;
    }
    public void Select(bool rightSide)
    {
        ShowEnchantTooltips(rightSide);
    }
    public void Deselect()
    {
        HideEnchantTooltips();
    }
    public void ShowEnchantTooltips(bool leftSide=true)
    {
        foreach (Transform child in enchantTooltipContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in enchantTooltipContainerLeft)
        {
            Destroy(child.gameObject);
        }
        if (cardInstance == null) return;
        foreach (CardEnchantment enchant in cardInstance.enchantments)
        {
            GameObject tooltipObj = Instantiate(enchantTooltipPrefab, leftSide ? enchantTooltipContainerLeft : enchantTooltipContainer);
            Tooltip tooltip = tooltipObj.GetComponent<Tooltip>();
            tooltip.SetTooltip(null,enchant.data.name, enchant.data.description);
        }
    }
    public void HideEnchantTooltips()
    {
        foreach (Transform child in enchantTooltipContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in enchantTooltipContainerLeft)
        {
            Destroy(child.gameObject);
        }
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


}