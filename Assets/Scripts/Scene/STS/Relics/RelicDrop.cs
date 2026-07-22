using System.Collections.Generic;
using UnityEngine;
public static class RelicDrop
{
    public static Relic GetRandomRelic(CombatResult result)
    {
        if (!RelicDatabase.initialized)
        {
            RelicDatabase.Load();
        }
        if (result != null && result.boss)
        {
            return GetRandomRelicOfRarity(RelicRarity.Boss);
        }
        int roll = Random.Range(0, 100);
        if (roll < 60)
        {
            return GetRandomRelicOfRarity(RelicRarity.Common);
        }
        else if (roll < 85)
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
        Relic relic=relics[Random.Range(0, relics.Count)];
        return relic;
    }
}