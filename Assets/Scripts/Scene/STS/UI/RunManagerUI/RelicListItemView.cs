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
            RelicRarity.Uncommon => Color.green,
            RelicRarity.Rare => Color.blue,
            RelicRarity.Epic => new Color(1f, 0f, 1f), // Magenta
            RelicRarity.Legendary => Color.yellow,
            _ => Color.white
        };
    }
}
