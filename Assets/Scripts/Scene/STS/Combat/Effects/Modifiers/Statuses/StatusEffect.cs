public abstract class StatusEffect : StatModifier
{
    public string Name;
    public int Value;
    public int Duration;

    public virtual void OnApply(Character target) { }
    public virtual void OnExpire(Character target) { }
    public virtual void OnTurnStart(Character target) { }
    public virtual void OnTurnEnd(Character target) { }
    public virtual void OnDamageTaken(Character target, ref int damage) { }
    public virtual void OnHeal(Character target, ref int healAmount) { }
    public virtual void OnAction(Character target) { }
    public virtual string Desc(){return $"{Value}";}
    public override string Describe()
    {
        string res = $"{Name} {Desc()}";
        if (Duration > 0)
            res += $" ({Duration})";
        return res;
    }
    public static StatusEffect Factory(StatusType type, int value, int duration)
    {
        StatusEffect stat = type switch
        {
            StatusType.Regen => new RegenStatus(value, duration),
            StatusType.Strength => new StrengthStatus(value,duration),
            StatusType.Weakness => new WeaknessStatus(duration),
            StatusType.Vuln => new VulnStatus(duration),
            StatusType.Dexterity => new DexterityStatus(value, duration),
            _ => null
        };
        return stat;
    }
}