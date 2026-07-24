public class RestHealRelic : Relic
{
    public RestHealRelic()
    {
        rarity = RelicRarity.Common;
        name = "Chaise de massage";
        description = "Vous regagnez 5 PV lorsque vous entrez dans un site de repos.";
    }

    public override void OnEnterRestSite(Character player)
    {
        player.Heal(5);
    }
}