using UnityEngine;
using System.Collections.Generic;
public abstract class StatusEffect : StatModifier
{
    public string Name;
    public int Value;
    public int Duration;
    public bool buff=false;
    public bool debuff=false;
    public virtual void InsertInto(List<StatusEffect> list)
    {
        StatusEffect other = list.Find(s => s.GetType() == this.GetType());
        if (other != null)
        {
            other.Duration += this.Duration;
            other.Value += this.Value;
        }
        else
        {
            list.Add(this);
        }
    }

    public virtual void OnApply(Character target) { }
    public virtual void OnExpire(Character target) { }
    public virtual void OnTurnStart(Character target) { }
    public virtual void OnTurnEnd(Character target) { }
    public virtual void OnDamageTaken(Character target, ref int damage) { }
    public virtual void OnHeal(Character target, ref int healAmount) { }
    public virtual void BeforeAction(Character target) { }
    public virtual void AfterAction(Character target) { }
    public virtual string Desc(){return $"\n{Value} (Description inconnue)";}
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return false;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        return value;
    }
    public override string Describe()
    {
        string res = "";
        if (Duration > 0)
            res += $" ({Duration})";
        res += Desc();
        return res;
    }
    public static StatusEffect Factory(StatusType type, int value, int duration)
    {
        StatusEffect stat = type switch
        {
            StatusType.Poison => new PoisonStatus(duration),
            StatusType.Regen => new RegenStatus(duration),
            StatusType.Strength => new StrengthStatus(value),
            StatusType.Weakness => new WeaknessStatus(duration),
            StatusType.Vuln => new VulnStatus(duration),
            StatusType.Dexterity => new DexterityStatus(value),
            StatusType.Awakening => new AwakeningStatus(value,duration),
            StatusType.FullMoon => new FullMoonStatus(),
            StatusType.Slow => new SlowStatus(duration),
            StatusType.Haste => new HasteStatus(duration),
            StatusType.Divination => new DivinationStatus(),
            _ => null
        };
        return stat;
    }
}