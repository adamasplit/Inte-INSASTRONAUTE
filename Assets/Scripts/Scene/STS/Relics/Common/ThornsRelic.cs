public class ThornsRelic : Relic
{
    public ThornsRelic()
    {
        rarity = RelicRarity.Common;
        name = "Épines";
        description = "Au début de chaque combat, gagnez 3 d'Épines.";
    }

    public override void OnCombatStart(Character player)
    {
        player.AddStatus(new ThornsStatus(3));
    }
}