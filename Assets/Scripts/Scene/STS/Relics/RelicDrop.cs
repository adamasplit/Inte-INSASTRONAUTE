using System.Collections.Generic;
using UnityEngine;
public static class RelicDrop
{
    public static Relic GetRandomRelic()
    {
        int roll = Random.Range(0, 100);
        if (roll < 50)
        {
            return GetRandomRelicOfRarity(RelicRarity.Common);
        }
        else if (roll < 80)
        {
            return GetRandomRelicOfRarity(RelicRarity.Uncommon);
        }
        else
        {
            return GetRandomRelicOfRarity(RelicRarity.Rare);
        }
    }

    private static Relic GetRandomRelicOfRarity(RelicRarity rarity)
    {
        List<Relic> relics = RelicDatabase.GetRelicsByRarity(rarity);
        if (relics.Count == 0) return null;
        return relics[Random.Range(0, relics.Count)];
    }
}