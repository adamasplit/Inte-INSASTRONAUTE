public class StatusNullificationRelic : Relic
{
    public StatusNullificationRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Dépression";
        description = "Vous êtes immunisé contre tous les effets de statut (positifs ou négatifs).";
    }

    public override bool CanApplyStatus(StatusEffect status, Character target)
    {
        return target == null || !target.isPlayer;
    }
}