using System.Collections.Generic;
public class TrapStatus : StatusEffect
{
    public TrapStatus(int value,int duration)    
    {
        Name="Piège";
        Value = value;
        Duration = duration;
        inextendable = true;
        debuff=true;
    }
    public override void InsertInto(List<StatusEffect> list)
    {
        list.Add(this);
    }
    public override string Desc()
    {
        return $"\nLa cible subit {Value} dégâts à la fin de son tour pendant {Duration} tours.";
    }
    public override void OnTurnEnd(Character target)
    {
        base.OnTurnEnd(target);
        target.TakeDamage(Value);
        VFXManager.Instance.PlayEffect("Trap", target);
    }
}