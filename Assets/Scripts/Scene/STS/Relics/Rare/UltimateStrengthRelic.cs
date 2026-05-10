public class UltimateStrengthRelic : Relic
{
    public UltimateStrengthRelic()
    {
        name = "Force ultime";
        description = "Donne 1 de Force à chaque début de tour.";
        rarity=RelicRarity.Rare;
    }
    public override void OnTurnStart(Character player)
    {
        base.OnTurnStart(player);
        player.AddStatus(StatusEffect.Factory(StatusType.Strength,1,-1));
    }
}