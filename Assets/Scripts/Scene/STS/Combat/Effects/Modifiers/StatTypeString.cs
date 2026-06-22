public static class StatTypeString
{
    public static string ToFrench(this StatType type)
    {
        switch (type)
        {
            case StatType.Damage:
                return "dégâts infligés";
            case StatType.Armor:
                return "armure gagnée";
            case StatType.Heal:
                return "soins prodigués";
            case StatType.TurnManipulationDelay:
                return "retard de tour";
            case StatType.Cost:
                return "coût";
            case StatType.ReplayCount:
                return "nombre d'activations";
            case StatType.Any:
                return "tout";
            case StatType.StatusPotency:
                return "puissance du statut";
            case StatType.StatusDuration:
                return "durée du statut";
            default:
                return type.ToString();
        }
    }
}