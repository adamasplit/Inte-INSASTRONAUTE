public class StatusNullificationRelic : Relic
{
    public StatusNullificationRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Nullifieur d'état";
        description = "Le porteur ne peut recevoir aucun effet de statut (positif ou négatif).";
    }

    public override bool CanApplyStatus(StatusEffect status, Character target)
    {
        return target == null || !target.isPlayer;
    }
}