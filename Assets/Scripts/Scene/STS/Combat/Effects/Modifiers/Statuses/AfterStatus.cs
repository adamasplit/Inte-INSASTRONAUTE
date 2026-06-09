public class AfterStatus : StatusEffect
{
    public AfterStatus(int value)    
    {
        Name="After";
        Value = value;
        Duration = 1;
        inextendable = true;
        debuff=true;
    }
    public override string Desc()
    {
        return $"\nInflige {Value} dégâts à la fin du tour.";
    }
    public override void OnTurnEnd(Character target)
    {
        base.OnTurnEnd(target);
        target.TakeDamage(Value);
    }
}