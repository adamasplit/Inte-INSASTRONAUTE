public class TargetingStatus : StatusEffect
{
    public TargetingStatus()
    {
        Value = 0;
        Duration = -1;
        Name = "Ciblage";
        framed=true;
    }
    public override string Desc(bool isPlayer)
    {
        return $"Ce personnage est visé par une attaque puissante.";
    }
}