using UnityEngine;
using System.Collections.Generic;
public abstract class StatusEffect : StatModifier
{
    protected Character owner;
    public StatusType statusType;
    public string cardID;
    public string Name;
    public int Value;
    public int maxValue=99;
    public int Duration;
    public int index=0;
    public bool buff=false;
    public bool debuff=false;
    public bool mustExpire=false;
    public bool generic=false; // Status dont seul le nom seront affichés dans la description d'une carte, parce que leur effet est simple (ex: Vulnérable, Affaibli, Force)
    public bool framed=false; // Statuts pouvant être supprimés par des cartes ou des effets de statut (certains indispellables peuvent être supprimés quand même)
    public bool inextendable=false; // Statuts dont la durée ne peut pas être augmentée
    public bool goldFrame=false; // Statuts rares bénéficiant d'une true resistance à la suppression
    public virtual string IconPath()
    {
        // Class name minus "Status" suffix, e.g., "PoisonStatus" becomes "Poison"
        string className = this.GetType().Name;
        if (className.EndsWith("Status"))
        {
            className = className.Substring(0, className.Length - "Status".Length);
        }
        return className;
    }
    public virtual StatusEffect Dispel(int remainingPercentage=0)
    {
        if (remainingPercentage>0)
        {
            StatusEffect dispelled = Factory((StatusType)System.Enum.Parse(typeof(StatusType), this.GetType().Name.Replace("Status","")), Value, Duration, Name);
            dispelled.Value = Mathf.CeilToInt(Value * (remainingPercentage / 100f));
            this.Value -= dispelled.Value;
            
            dispelled.Duration = Duration>0 ? Mathf.CeilToInt(Duration * (remainingPercentage / 100f)) : -1;
            this.Duration -= dispelled.Duration<0 ? 0 : dispelled.Duration;
            
            return dispelled;
        }
        owner.RemoveStatus(this);
        return this;
    }
    public virtual void InsertInto(List<StatusEffect> list)
    {
        StatusEffect other = list.Find(s => s.GetType() == this.GetType()&&Mathf.Sign(s.Duration)==Mathf.Sign(this.Duration));
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
            other.Value = Mathf.Clamp(other.Value + this.Value, -other.maxValue, other.maxValue);
        }
    }
    public virtual void Extend(int duration)
    {
        if (!inextendable&&duration>0&&Duration>0)
            Duration += duration;
    }
    public virtual void Update(Character target){}

    protected void Tick(Character target)
    {
        if (debuff&&target.statusEffects.Exists(s=> s is DebuffHaltStatus&&s!=this))
        {
            return;
        }
        Duration--;
    }

    public virtual void OnApply(Character target)
    {
        owner = target;
    }
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
    public virtual void OnAnyCardPlayed(Character source,CardInstance card) { }
    public virtual void OnTargetedByCard(Character source,Character target, CardInstance card) { }
    public virtual void OnCardDrawn(Character target, CardInstance card) { }
    public virtual int ArmorOnTurnStart(int previousArmor, Character character) { return 0; }
    public virtual bool CanApply(StatusEffect newStatus, Character target)
    {
        return true;
    }
    public virtual string Desc(bool isPlayer){return $"\n{Value} (Description inconnue)";}
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return false;
    }
    public override string Describe()
    {
        return Desc(owner.isPlayer);
    }
    public override int Modify(int value, EffectContext ctx)
    {
        return value;
    }
    public static StatusEffect Factory(StatusType type, int value, int duration,string effectInfo="",int index=0)
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
            StatusType.Divination => new DivinationStatus(value),
            StatusType.Targeting => new TargetingStatus(),
            StatusType.Jump => new JumpStatus(value),
            StatusType.Fragile => new FragileStatus(duration),
            StatusType.Speed => new SpeedStatus(value),
            StatusType.Accelerate=>new AccelerateStatus(value),
            StatusType.Stun=>new StunStatus(duration),
            StatusType.Strengthen=>new StrengthenStatus(value),
            StatusType.Clock=>new ClockStatus(value),
            StatusType.After=>new AfterStatus(value),
            StatusType.Burn=>new BurnStatus(duration),
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
            StatusType.Thorns=>new ThornsStatus(value,duration),
            StatusType.FullBreak=>new FullBreakStatus(duration),
            StatusType.Status=>new StatusStatus(duration),
            StatusType.CostNullify=>new CostNullifyStatus(value),
            StatusType.CardFollowUp=>new CardFollowUpStatus(value,duration,effectInfo,index),
            StatusType.Echo=>new EchoStatus(value,duration,index),
            StatusType.FieldTurnFollowUp=>new FieldTurnFollowUpStatus(value,duration,effectInfo,index),
            StatusType.Vigor=>new VigorStatus(value),
            StatusType.Artifact=>new ArtifactStatus(value),
            StatusType.Filter=>new FilterStatus(value),
            StatusType.DelayedStun=>new DelayedStunStatus(duration),
            StatusType.DebuffHalt=>new DebuffHaltStatus(duration),
            StatusType.Freeze=>new FreezeStatus(duration),
            StatusType.DelayedDeath=>new DelayedDeathStatus(duration),
            StatusType.AreaDmgReduction=>new AreaDmgReductionStatus(value,duration),
            StatusType.ConserveArmor=>new ConserveArmorStatus(value,duration),
            StatusType.EnergyUp=>new EnergyUpStatus(value,duration),
            StatusType.Vulnerabilize=>new VulnerabilizeStatus(value,duration),
            StatusType.Imitate=>new ImitateStatus(),
            StatusType.FullThrottle=>new FullThrottleStatus(value), 
            StatusType.Fatigue=>new FatigueStatus(),
            StatusType.DamageReduction=>new DamageReductionStatus(value,duration),
            StatusType.AnyCardFollowUp=>new AnyCardFollowUpStatus(value,duration,effectInfo,index),
            _ => null
        };
        stat.statusType = type;
        stat.cardID = effectInfo;
        stat.index = index;
        return stat;
    }
}