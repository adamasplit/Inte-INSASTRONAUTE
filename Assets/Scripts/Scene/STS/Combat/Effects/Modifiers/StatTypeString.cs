public static class StatTypeString
{
    public static string ToFrench(this StatType type)
    {
        switch (type)
        {
            case StatType.Damage:
                return "Dégâts";
            case StatType.Armor:
                return "Armure";
            case StatType.Heal:
                return "Soins";
            default:
                return type.ToString();
        }
    }
}