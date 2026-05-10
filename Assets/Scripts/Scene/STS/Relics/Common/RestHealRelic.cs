public class RestHealRelic : Relic
{
    public RestHealRelic()
    {
        rarity = RelicRarity.Common;
        name = "Rest Heal";
        description = "Heals when entering a rest site.";
    }

    public override void OnEnterRestSite(Character player)
    {
        player.Heal(10);
    }
}