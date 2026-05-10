using System.Collections.Generic;
public static class RelicDatabase
{
    private static List<Relic> relics;
    public static void Load()
    {
        relics = new List<Relic>
        {
            new EPRelic(),
            new GCRelic(),
            new GMRelic(),
            new ITIRelic(),
            new MECARelic(),
            new MRIERelic(),
            new AIRelic(),
            new PERFRelic(),
            new CFIRelic(),
            new RestChargesRelic(),
            new RestHealRelic()
        };
    }
    public static List<Relic> GetRelicsByRarity(RelicRarity rarity)
    {
        return relics.FindAll(r => r.rarity == rarity);
    }
}