public class FullMoonStatus : StatusEffect
{
    public FullMoonStatus()
    {
        Value = 0;
        Duration = -1;
        Name = "Pleine Lune";
    }
    public override string Describe()
    {
        return $"--";
    }
}