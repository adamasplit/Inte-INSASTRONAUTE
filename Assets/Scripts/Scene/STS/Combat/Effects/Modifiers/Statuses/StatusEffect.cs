using UnityEngine;
using System.Collections.Generic;
public abstract class StatusEffect : StatModifier
{
    public string Name;
    public int Value;
    public int maxValue=99;
    public int Duration;
    public bool buff=false;
    public bool debuff=false;
    public bool mustExpire=false;
    public bool generic=false; // Status dont seul le nom seront affichés dans la description d'une carte, parce que leur effet est simple (ex: Vulnérable, Affaibli, Force)
    public bool framed=false; // Statuts pouvant être supprimés par des cartes ou des effets de statut (certains indispellables peuvent être supprimés quand même)
    public bool inextendable=false; // Statuts dont la durée ne peut pas être augmentée
    public bool goldFrame=false; // Statuts rares bénéficiant d'une true resistance à la suppression
    public virtual void InsertInto(List<StatusEffect> list)
    {
        StatusEffect other = list.Find(s => s.GetType() == this.GetType());
        if (other != null)
        {
            this.Merge(other);
        }
        else
        {
            list.Add(this);
        }
    }
    public virtual void Merge(StatusEffect other)
    {
        if (Duration > 0)
        {
            other.Duration += this.Duration;
            other.Value = Mathf.Max(other.Value, this.Value);
        }
        else
        {
            other.Value = Mathf.Min(other.Value + this.Value, other.maxValue);
        }
    }
    public virtual void Extend(int duration)
    {
        if (!inextendable&&duration>0)
            Duration += duration;
    }
    public virtual void Update(Character target){}

    protected void Tick(Character target)
    {
        Duration--;
    }

    public virtual void OnApply(Character target) { }
    public virtual void OnExpire(Character target) { }
    public virtual void OnTurnStart(Character target) { }
    public virtual void OnTurnEnd(Character target)
    {
        Tick(target);
    }
    public virtual void OnFieldTurnStart(Character target) { }
    public virtual void OnFieldTurnEnd(Character target) { }
    public virtual void OnDamageDealt(Character source,Character target, ref int damage) { }
    public virtual void OnDamageTaken(Character source,Character target, ref int damage) { }
    public virtual void OnHPLoss(Character target, int damage) { }
    public virtual void OnArmorGained(Character target, ref int armor) { }
    public virtual void OnArmorLost(Character target, ref int armor) { }
    public virtual void OnOwnArmorBroken(Character source,Character target) { }
    public virtual void OnTargetArmorBroken(Character source,Character target) { }
    public virtual void OnHeal(Character target, ref int healAmount) { }
    public virtual void BeforeAction(Character target) { }
    public virtual void AfterAction(Character target) { }
    public virtual void OnCardPlayed(Character source,Character target,CardInstance card) { }
    public virtual void OnTargetedByCard(Character source,Character target, CardInstance card) { }
    public virtual void OnCardDrawn(Character target, CardInstance card) { }
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
        //if (Duration > 0)
        //    res += $" ({Duration})";
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
            StatusType.Targeting => new TargetingStatus(),
            StatusType.Jump => new JumpStatus(value),
            StatusType.Fragile => new FragileStatus(duration),
            StatusType.Speed => new SpeedStatus(value),
            StatusType.Accelerate=>new AccelerateStatus(value),
            StatusType.Stun=>new StunStatus(value),
            StatusType.Strengthen=>new StrengthenStatus(value),
            StatusType.Clock=>new ClockStatus(),
            StatusType.After=>new AfterStatus(value),
            StatusType.Burn=>new BurnStatus(value),
            StatusType.Sadism=>new SadismStatus(),
            StatusType.MechaArm=>new MechaArmStatus(value),
            StatusType.Energy=>new EnergyStatus(value),
            StatusType.Continuous=>new ContinuousStatus(value,duration),
            StatusType.Crystallize=>new CrystallizeStatus(duration),
            StatusType.Brand=>new BrandStatus(value,duration),
            StatusType.LifeCycle=>new LifeCycleStatus(value),
            StatusType.RiskManagement=>new RiskManagementStatus(value),
            StatusType.Trap=>new TrapStatus(value,duration),
            StatusType.Elastic=>new ElasticStatus(value),
            StatusType.Shielding=>new ShieldingStatus(duration),
            StatusType.Plating=>new PlatingStatus(duration),
            StatusType.DelayImmunity=>new DelayImmunityStatus(),
            StatusType.ArmorBlock=>new ArmorBlockStatus(duration),
            StatusType.Thorns=>new ThornsStatus(value),
            StatusType.FullBreak=>new FullBreakStatus(duration),
            StatusType.Status=>new StatusStatus(duration),
            StatusType.CostNullify=>new CostNullifyStatus(value),
            _ => null
        };
        return stat;
    }
}