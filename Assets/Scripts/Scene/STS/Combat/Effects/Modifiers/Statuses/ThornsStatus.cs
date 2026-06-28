public class ThornsStatus : StatusEffect
{
    public ThornsStatus(int value,int duration)
    {
        Value = value;
        Duration = duration;
        if (Duration==0)
        {
            Duration=-1;
        }
        Name = "Épines";
        buff=true;
        debuff=false;
        generic = true;
    }
    public override void OnTurnStart(Character target)
    {
        Tick(target);
    }
    public override void OnTurnEnd(Character target)
    {
    }
    public override void OnDamageTaken(Character source,Character target,ref int damage)
    {
        base.OnDamageTaken(source,target,ref damage);
        if (damage > 0)
        {
            source.TakeDamage(Value);
            VFXManager.Instance.PlayEffect("Thorns", target);
        }
    }
    public override string Desc(bool isPlayer)
    {
        return $"Vous infligez {Value} de dégâts à l'attaquant lorsqu'il vous inflige des dégâts.";
    }
}