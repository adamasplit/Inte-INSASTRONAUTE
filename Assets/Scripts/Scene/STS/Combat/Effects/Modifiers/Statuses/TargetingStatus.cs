public class TargetingStatus : StatusEffect
{
    public TargetingStatus()
    {
        Value = 0;
        Duration = -1;
        Name = "Visé";
    }
    public override string Describe()
    {
        return $"Cet ennemi est visé par une attaque puissante.";
    }
}