public class FireRelic : Relic
{
    public FireRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Système anti-incendie";
        description = "Vous êtes immunisé contre Brûlure.";
    }

    public override bool CanApplyStatus(StatusEffect status, Character target)
    {
        return target == null || !target.isPlayer || status.statusType != StatusType.Burn;
    }
}