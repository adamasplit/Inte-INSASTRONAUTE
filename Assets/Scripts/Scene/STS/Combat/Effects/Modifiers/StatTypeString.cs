public static class StatTypeString
{
    public static string ToFrench(this StatType type,int value,ModifierType modifierType = ModifierType.Additive,bool positive=true)
    {
        if (modifierType==ModifierType.Additive||modifierType==ModifierType.Multiplicative)
        {
            if (positive)
            {
                return ToFrenchPositive(type,value,modifierType==ModifierType.Multiplicative);
            }
            else
            {
                return ToFrenchNegative(type,value,modifierType==ModifierType.Multiplicative);
            }
        }
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
    private static string ToFrenchPositive(StatType type, int value, bool isPercentage)
    {
        string plural=(value != 1) ? "s" : "";
        switch (type)
        {
            case StatType.Damage:
                return $"Inflige {value}{(isPercentage ? "% de" : "")} dégât{plural} en plus";
            case StatType.Armor:
                return $"Donne {value}{(isPercentage ? "%" : "")} d'armure en plus";
            case StatType.Heal:
                return $"Rend {value}{(isPercentage ? "% de" : "")} PV en plus";
            case StatType.TurnManipulationDelay:
                return $"Le retard augmente de {value}{(isPercentage ? "%" : "")}";
            case StatType.Cost:
                return $"Coûte {value}{(isPercentage ? "%" : "")} d'énergie supplémentaire";
            case StatType.ReplayCount:
                return $"Se rejoue {value}{(isPercentage ? "% de" : "")} fois supplémentaire{plural}";
            case StatType.StatusDuration:
                return $"La durée de l'effet augmente de {value}{(isPercentage ? "%" : "")}";
            default:
                return $"{type}: {value}";
        }
    }
    private static string ToFrenchNegative(StatType type, int value, bool isPercentage)
    {
        switch (type)
        {
            case StatType.Damage:
                return $"Inflige {value}{(isPercentage ? "% de" : "")} dégâts en moins";
            case StatType.Armor:
                return $"Donne {value}{(isPercentage ? "%" : "")} d'armure en moins";
            case StatType.Heal:
                return $"Prodigue {value}{(isPercentage ? "% de" : "")} soins en moins";
            default:
                return $"{type}: {value}";
        }
    }
}