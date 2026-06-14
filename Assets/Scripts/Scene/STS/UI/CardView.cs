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
    public void toggleSelection()
    {
        selectionPreview = !selectionPreview;
        selectionHighlight.SetActive(selectionPreview);
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
            if (ui.IsSelectingCards())
            {
                ui.selectionController.ToggleCard(this);

                ui.RefreshHandLayout();

                return;
            }

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
            cardTypeText.text = card.data.type.ToString();
            cardBg.color = SelectableCharacterUtils.getCharacterColor(card.data.favoredCharacter);
            specialCardOverlay.SetActive(card.data.HasTag(CardTag.Unobtainable));
            SetName(card.data.cardName);
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

    public void SetWithCollectionCard(CardInstance card)
    {
        if (card.data.collectionCard==null||card.data.collectionCard.sprite==null)
        {
            cardImage.sprite = card.data.icon;
            // Conserve aspect ratio
            cardImage.preserveAspect = true;
            imgBg.enabled = true;
            imgOverlay.enabled = true;
            collectionCardRoot.SetActive(false);
            genericCardRoot.SetActive(true);
            descriptionText.color = Color.white;
            nameText.color = Color.white;
        }
        else
        {
            //nameText.text+= "\n<i><color=grey>" + (card.data.collectionCard != null && card.data.collectionCard.cardName != card.data.cardName ? card.data.collectionCard.cardName : "")+ "</color></i>";
            if (card.data.collectionCard.cardName==card.data.cardName)
            {
                nameText.text = "";
            }
            collectionCardImage.sprite = card.data.collectionCard.sprite;
            imgBg.enabled = false;
            imgOverlay.enabled = false;
            cardImage.preserveAspect = false;
            collectionCardRoot.SetActive(true);
            genericCardRoot.SetActive(false);
            // Set the collection card description background color based on a pixel from the collection card's sprite
            if (card.data.collectionCard != null && card.data.collectionCard.sprite != null)
            {
                Sprite s = card.data.collectionCard.sprite;
                Texture2D tex = s.texture;

                // Determine pixel coordinates: middle in width, 95% up in height within the sprite rect
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
                catch (System.Exception e)
                {
                    Debug.LogError($"Error sampling pixel from texture: {e.Message}");
                    // fallback to white if texture isn't readable or other error
                    sample = Color.white;
                }

                collectionCardDescBg.color = sample;
                // Adjust text color based on brightness of the sampled color
                float brightness = (sample.r + sample.g + sample.b) / 3f;
                Color textColor = brightness < 0.5f ? Color.white : Color.black;
                descriptionText.color = textColor;
                nameText.color = textColor;
            }
        }
    }


}