public class ExposureStatus : StatusEffect
{
    public ExposureStatus(int value, int duration)
    {
        Value = value;
        Duration = duration;
        Name = "Exposition";
        debuff = true;
    }

    public override string Desc(bool isPlayer)
    {
        return $"La cible subit {Value}% de dégâts supplémentaires de la famille de cartes choisie pendant {Duration} tour(s).";
    }
}
