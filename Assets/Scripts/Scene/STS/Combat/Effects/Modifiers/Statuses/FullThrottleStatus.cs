public class FullThrottleStatus : StatusEffect
{
    public FullThrottleStatus(int value)    
    {
        Name="Plein régime";
        Value = value;
        Duration = -1;
        inextendable=true;
        buff=true;
    }
    public override string Desc(bool isPlayer)
    {
        return $"Quand vous perdez des PV pendant votre tour, gagnez {Value} de Force.";
    }
    
    public override void OnDamageTaken(Character source,Character target, ref int damage)
    {
        if (target.isPlayer && target.onTurn)
        {
            target.AddStatus(new StrengthStatus(Value));
        }
    }
}