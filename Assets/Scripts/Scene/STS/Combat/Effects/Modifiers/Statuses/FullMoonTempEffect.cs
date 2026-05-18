using System.Collections;
using System.Collections.Generic;
public class FullMoonTempEffect : StatusEffect
{
    public FullMoonTempEffect(int value)
    {
        Duration = 1;
        Value = value;
        Name = "Pleine lune (temporaire)";
        buff=true;
    }
    public override void InsertInto(List<StatusEffect> list)
    {
        StatusEffect other = list.Find(s => s.GetType() == this.GetType());
        if (other != null)
        {
            other.Duration = 1;
            other.Value = other.Value + this.Value;
        }
        else
        {
            list.Add(this);
        }
    }
    public override string Describe()
    {
        return $"Retire {Value} de Force à la fin du tour";
    }
    public override void OnExpire(Character target)
    {
        base.OnExpire(target);
        target.AddStatus(new StrengthStatus(-Value));
    }
}