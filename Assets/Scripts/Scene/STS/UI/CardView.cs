using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class CardView : MonoBehaviour,IPointerClickHandler
{
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
        if (ui!=null)
            ui.SelectCard(this);
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
            SetName(card.data.cardName);
            SetCost(card.data.cost);
            cardTypeText.text = card.data.type.ToString();
            nameText.text+= "\n<i><color=grey>" + (card.data.collectionCard != null && card.data.collectionCard.cardName != card.data.cardName ? card.data.collectionCard.cardName : "")+ "</color></i>";
            cardImage.sprite = card.data.collectionCard != null ? card.data.collectionCard.sprite : null;
            Color rarityColor = Color.white;
            switch (card.data.rarity)
            {
                case CardRarity.Common:
                    rarityColor = Color.white;
                    break;
                case CardRarity.Uncommon:
                    rarityColor = Color.green;
                    break;
                case CardRarity.Rare:
                    rarityColor = Color.blue;
                    break;
                case CardRarity.Epic:
                    rarityColor = Color.magenta;
                    break;
                case CardRarity.Legendary:
                    rarityColor = Color.yellow;
                    break;
                case CardRarity.Special:
                    rarityColor = Color.red;
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
    public void SetCost(int cost)
    {
        costText.text = cost.ToString();
    }
    public void SetDescription(string description)
    {
        descriptionText.text = description;
    }

    public void RefreshDescription(Character target = null, bool force = false)
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
                card = cardInstance
            };

            SetDescription(cardInstance.GetDescription(ctx));
        }
    }


}