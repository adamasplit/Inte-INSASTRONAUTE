using UnityEngine;
using TMPro;
public class CardView : MonoBehaviour
{
    public CardInstance cardInstance;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;
    CombatManager combat;
    Character currentTarget;
    bool isInitialized = false;
     void Start()
    {
        combat = FindFirstObjectByType<CombatManager>();
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

    public void RefreshDescription(Character target = null)
    {
        if (combat==null)
            combat = FindFirstObjectByType<CombatManager>();
        if (isInitialized && currentTarget == target)
            return;
        isInitialized = true;
        currentTarget = target;

        if (cardInstance != null)
        {
            EffectContext ctx = new EffectContext
            {
                source = combat.player,
                target = target,
                combat = combat,
                state = combat.state,
                card = cardInstance
            };

            SetDescription(cardInstance.GetDescription(ctx));
        }
    }
}