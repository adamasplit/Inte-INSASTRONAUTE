public class FullMoonStatus : StatusEffect
{
    public FullMoonStatus()
    {
        Value = 0;
        Duration = -1;
        Name = "Pleine lune";
        buff=true;
    }
    public override string Describe()
    {
        return $"--";
    }
}