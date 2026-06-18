public class LifeCycleStatus : StatusEffect
{
    public LifeCycleStatus(int value)    
    {
        Name="Cycle de vie";
        Value = value;
        Duration = 1;
        inextendable=true;
        buff=true;
    }
    public override string Desc(bool isPlayer)
    {
        return $"Quand vous perdez des PV à ce tour, gagnez {Value} énergie.";
    }
    public override void OnDamageTaken(Character source,Character target, ref int damage)
    {
        if (target.isPlayer)
        {
            target.GainEnergy(Value);
        }
    }
}