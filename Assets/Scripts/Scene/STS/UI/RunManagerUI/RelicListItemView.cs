using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RelicListItemView : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Image rarityIndicator;

    public void Init(Relic relic)
    {
        if (nameText != null)
            nameText.text = relic.name;

        if (descriptionText != null)
            descriptionText.text = relic.Describe();

        if (rarityIndicator != null)
        {
            // Set color based on rarity
            Color rarityColor = GetRelicRarityColor(relic.rarity);
            rarityIndicator.color = rarityColor;
        }
    }

    Color GetRelicRarityColor(RelicRarity rarity)
    {
        return rarity switch
        {
            RelicRarity.Common => Color.white,
            RelicRarity.Uncommon => Color.blue,
            RelicRarity.Rare => Color.yellow,
            RelicRarity.Boss => Color.red,
            RelicRarity.Base => Color.green,
            _ => Color.white
        };
    }
}
