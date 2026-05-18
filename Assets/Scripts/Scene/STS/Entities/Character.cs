using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class Character
{
    public Sprite portrait; // à assigner dans l'inspecteur ou via code
    public string name;
    public int maxHP;
    public int currentHP;
    public int armor;
    public bool IsAlive => currentHP > 0;
    public ResourceSet resources = new ResourceSet();
    public bool isPlayer;
    public List<StatusEffect> statusEffects = new List<StatusEffect>();
    public CombatManager combat;
    public bool onTurn = false;
    public Character(string name, int maxHP)
    {
        this.name = name;
        this.maxHP = maxHP;
        portrait=Resources.Load<Sprite>("STS/Portraits/" + name); // Assumes portraits are in Resources/STS/Portraits and named after the character
        this.currentHP = maxHP;
        this.armor = 0;
    }
    public bool dead => currentHP <= 0;
    public void GainMaxHP(int amount)
    {
        maxHP += amount;
        currentHP += amount;
    }
    public void LoseMaxHP(int amount)
    {
        maxHP = Mathf.Max(1, maxHP - amount);
        currentHP = Mathf.Min(currentHP, maxHP);
    }
    public DamageInfo TakeDamage(int amount, bool ignoreArmor=false)
    {
        var info = new DamageInfo();
        if (ignoreArmor)
        {
            currentHP = Mathf.Max(0, currentHP - amount);
            info.amount = amount;
            if (currentHP == 0)
            {
                info.killingBlow = true;
            }
            info.unblocked = true;
            return info;
        }
        else
        {
            int startingArmor = armor;
            int damageAfterArmor = Mathf.Max(0, amount - armor);
            armor = Mathf.Max(0, armor - amount);
            currentHP = Mathf.Max(0, currentHP - damageAfterArmor);
            if (currentHP == 0)
            {
                info.killingBlow = true;
            }
            info.amount = damageAfterArmor;
            info.unblocked = damageAfterArmor > 0;
            info.armorBroken = armor == 0 && amount > 0&& startingArmor > 0;
            return info;
        }
    }

    public void GainEnergy(int amount)
    {
        resources.energy += amount;
    }

    public void AddArmor(int amount)
    {
        armor += amount;
        foreach (var relic in RunManager.Instance.relics)
        {
            relic.OnAnyArmorGain(this, amount);
        }
    }
    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
    }
    public void AddStatus(StatusEffect status)
    {
        //Debug.Log($"Adding status {status.Name} to {name} with potency {status.Value} and duration {status.Duration}");
        status.InsertInto(statusEffects);
        status.OnApply(this);
        //Debug.Log($"{name} status effects: {string.Join(", ", statusEffects.ConvertAll(s => s.Name + "(" + s.Duration + ")"))}");
    }
    public void RemoveStatus(StatusEffect status)
    {
        statusEffects.Remove(status);
        status.OnExpire(this);
    }
    public void AfterAction()
    {
        ExpireStatuses();
    }
    public void ExpireStatuses()
    {
        var toRemove = new List<StatusEffect>();
        foreach (var status in statusEffects)
        {
            if (status.Duration == 0||status.mustExpire)
            {
                status.OnExpire(this);
                toRemove.Add(status);
            }
        }
        foreach (var status in toRemove)
        {
            if (!RunManager.Instance.relics.Exists(r => r is AIRelic)||status.mustExpire)
            {
                statusEffects.Remove(status);
            }
        }
    
    }
    public void StartTurn()
    {
        onTurn = true;
        int newArmor = 0;
        if (isPlayer)
        {
            foreach (var relic in RunManager.Instance.relics)
            {
                newArmor =Mathf.Max(newArmor, relic.ArmorOnTurnStart(armor, this));
            }
        }
        armor=newArmor;
        resources.energy = 3;
        if (isPlayer)
        {
            int newEnergy = 3;
            foreach (var relic in RunManager.Instance.relics)
            {
                newEnergy += relic.EnergyOnTurnStart(resources.energy, this);
            }
            resources.energy = newEnergy;
        }
        foreach (var status in statusEffects)
        {
            status.OnTurnStart(this);
        }
        foreach (var relic in RunManager.Instance.relics)
        {
            relic.OnAnyTurnStart(this);
        }

        ExpireStatuses();
    }
    public void EndTurn()
    {
        onTurn = false;
        foreach (var status in statusEffects)
        {
            status.OnTurnEnd(this);
        }
        // Collect status effects to remove, then remove after iteration
        ExpireStatuses();
        if (isPlayer)
        {
            foreach (var relic in RunManager.Instance.relics)
            {
                relic.OnTurnEnd(this);
            }
        }
        foreach (var relic in RunManager.Instance.relics)
        {
            relic.OnAnyTurnEnd(this);
        }
        if (combat != null)
        {
            combat.state.playerLastTurn = isPlayer;
        }
    }
    public void SpendEnergy(int amount)
    {
        resources.energy -= amount;
    }
    public CombatManager GetCombatManager()
    {
        if (combat==null)
        {
            return GameObject.FindFirstObjectByType<CombatManager>();
        }
        return combat;
    }
    public int turnDelay(int baseDelay)
    {
        return BattleCalculator.GetModifiedValue(baseDelay, StatType.TurnDelay, new EffectContext { source = this, target = this });
    }

    public void DrawCard()
    {
        if (!isPlayer)
            return;

        var cm = GetCombatManager();

        if (cm == null)
            return;

        cm.deck.Draw();
    }

    public void OnDamageDealt(Character target, int damage,bool unblocked=false)
    {
        foreach (var status in statusEffects.ToList())
        {
            status.OnDamageDealt(this, target, ref damage);
        }
        if (isPlayer)
        {
            foreach (var relic in RunManager.Instance.relics)
            {
                relic.OnDamageDealt(this, target, damage);
            }
        }
    }
    public void OnDamageTaken(Character source, int damage,bool unblocked=false)
    {
        foreach (var status in statusEffects.ToList())
        {
            status.OnDamageTaken(source, this, ref damage);
        }
        if (isPlayer)
        {
            foreach (var relic in RunManager.Instance.relics)
            {
                relic.OnDamageTaken(source, this, damage);
            }
        }
    }
    public void OnTargetArmorBroken(Character target)
    {
        foreach (var status in statusEffects)
        {
            status.OnTargetArmorBroken(this, target);
        }
        if (isPlayer)
        {
            foreach (var relic in RunManager.Instance.relics)
            {
                relic.OnTargetArmorBroken(this, target);
            }
        }
    }
    public void OnOwnArmorBroken(Character source)
    {
        foreach (var status in statusEffects)
        {
            status.OnOwnArmorBroken(source, this);
        }
        if (isPlayer)
        {
            foreach (var relic in RunManager.Instance.relics)
            {
                relic.OnOwnArmorBroken(source, this);
            }
        }
    }
}