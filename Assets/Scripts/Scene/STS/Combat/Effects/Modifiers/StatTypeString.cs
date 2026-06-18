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
            default:
                return type.ToString();
        }
    }
}