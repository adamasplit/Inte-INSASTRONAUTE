public class JuryRelic : Relic
{
    public JuryRelic()
    {
        name = "PV du jury";
        description = "Au début de chaque combat, gagnez 1 de Force et 1 de Dextérité.";
        rarity=RelicRarity.Rare;
    }
    public override void OnCombatStart(Character player)
    {
        player.AddStatus(StatusEffect.Factory(StatusType.Strength,1,-1));
        player.AddStatus(StatusEffect.Factory(StatusType.Dexterity,1,-1));
    }
}