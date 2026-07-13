public class SpeedRelic : Relic
{
    public SpeedRelic()
    {
        name = "Propulseur";
        description = "Au début du combat, gagnez 1 de Vitesse.";
        rarity=RelicRarity.Uncommon;
    }
    public override void OnCombatStart(Character player)
    {
        player.AddStatus(StatusEffect.Factory(StatusType.Speed,1,-1));
    }
}