public class TemporalAnchorRelic : Relic
{
    public TemporalAnchorRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Ancre temporelle";
        description = "Le tour du porteur ne peut pas être déplacé.";
    }

    public override bool CanTurnBeMoved(Character target, bool isAdvance, bool isCutIn)
    {
        return target == null || !target.isPlayer;
    }
}