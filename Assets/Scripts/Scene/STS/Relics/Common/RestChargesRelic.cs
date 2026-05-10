public class RestChargesRelic : Relic
{
    public RestChargesRelic()
    {
        rarity = RelicRarity.Common;
        name = "Rest Charges";
        description = "Gagne 1 charge en plus lorsque vous entrez dans un site de repos.";
    }

    public void OnEnterRestSite(Character player)
    {
        RunManager.Instance.restCharges++;
    }
}