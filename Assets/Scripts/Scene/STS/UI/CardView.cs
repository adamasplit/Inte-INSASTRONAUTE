using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class CardView : MonoBehaviour
{
    public CardInstance cardInstance;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;
    public Image rarityBorder;
    public Image rarityBorder2;
    public Image cardImage;
    public TextMeshProUGUI cardTypeText;
    CombatManager combat;
    Character currentTarget;
    bool isInitialized = false;
    void Start()
    {
        combat = FindFirstObjectByType<CombatManager>();
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
            switch (card.data.rarity)
            {
                case CardRarity.Common:
                    rarityBorder.color = Color.white;
                    rarityBorder2.color = Color.white;
                    break;
                case CardRarity.Uncommon:
                    rarityBorder.color = Color.green;
                    rarityBorder2.color = Color.green;
                    break;
                case CardRarity.Rare:
                    rarityBorder.color = Color.blue;
                    rarityBorder2.color = Color.blue;
                    break;
                case CardRarity.Epic:
                    rarityBorder.color = Color.magenta;
                    rarityBorder2.color = Color.magenta;
                    break;
                case CardRarity.Legendary:
                    rarityBorder.color = Color.yellow;
                    rarityBorder2.color = Color.yellow;
                    break;
                case CardRarity.Special:
                    rarityBorder.color = Color.red;
                    rarityBorder2.color = Color.red;
                    break;
            }
            RefreshDescription();
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