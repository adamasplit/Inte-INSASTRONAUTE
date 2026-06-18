public class ThornsStatus : StatusEffect
{
    public ThornsStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Épines";
        buff=true;
        debuff=false;
        generic = true;
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