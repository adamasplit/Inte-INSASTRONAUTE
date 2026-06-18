public class RestChargesRelic : Relic
{
    public RestChargesRelic()
    {
        rarity = RelicRarity.Common;
        name = "Expérience";
        description = "Gagnez 1 charge en plus lorsque vous entrez dans un site de repos.";
    }

    public override void OnEnterRestSite(Character player)
    {
        RunManager.Instance.restCharges++;
    }
}