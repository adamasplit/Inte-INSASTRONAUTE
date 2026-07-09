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
        if (RunManager.Instance != null && !RunManager.Instance.inCombat)
        {
            RunManager.Instance.ui.FlashRedOverlay();
        }
        var info = new DamageInfo();
        if (combat!=null&& combat.state!=null)
        {
            Debug.Log($"{name} is taking {amount} damage. Current HP: {currentHP}, Armor: {armor}, Ignore Armor: {ignoreArmor}");
            combat.state.damageDealtWithLastAction+= amount;
        }
        if (ignoreArmor)
        {
            currentHP = Mathf.Max(0, currentHP - amount);
            if (combat != null && combat.state != null)
            {
                if (!combat.state.hpLostSinceLastTurn.ContainsKey(this))
                {
                    combat.state.hpLostSinceLastTurn[this] = 0;
                }
                combat.state.hpLostSinceLastTurn[this] += amount;
            }
            info.amount = amount;
            if (currentHP == 0)
            {
                info.killingBlow = true;
            }
            info.unblocked = true;
            if (combat != null && combat.ui != null)
            {
                combat.ui.ShowDamagePopup(this, amount, false, false);
            }
            return info;
        }
        else
        {
            int startingArmor = armor;
            int damageAfterArmor = Mathf.Max(0, amount - armor);
            armor = Mathf.Max(0, armor - amount);
            if (combat != null && combat.state != null)
            {
                if (combat.state.hpLostSinceLastTurn.ContainsKey(this))
                {
                    combat.state.hpLostSinceLastTurn[this] += damageAfterArmor;
                }
                else
                {
                    combat.state.hpLostSinceLastTurn[this] = damageAfterArmor;
                }
            }
            currentHP = Mathf.Max(0, currentHP - damageAfterArmor);
            if (currentHP == 0)
            {
                info.killingBlow = true;
            }
            info.amount = damageAfterArmor;
            info.unblocked = damageAfterArmor > 0;
            info.armorBroken = armor == 0 && amount > 0&& startingArmor > 0;
            if (combat != null && combat.ui != null)
            {
                combat.ui.ShowDamagePopup(this, info.unblocked ? damageAfterArmor : amount, false, !info.unblocked);
            }
            return info;
        }
    }

    public void GainEnergy(int amount)
    {
        resources.energy += amount;
        combat.state.energyGainedThisTurn += amount;
        if (isPlayer)
        {
            VFXManager.Instance.AnimateEnergyGain();
        }
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
        int previousHP = currentHP;
        if (RunManager.Instance != null && !RunManager.Instance.inCombat)
        {
            RunManager.Instance.ui.FlashGreenOverlay();
        }
        if (isPlayer)
        {
            foreach (var relic in RunManager.Instance.relics)
            {
                amount = relic.OnHeal(this, amount);
            }
        }
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        if (combat != null && combat.ui != null)
        {
            combat.ui.ShowDamagePopup(this, currentHP - previousHP, true, false);
        }
    }
    public void AddStatus(StatusEffect status)
    {
        // Check if the status can be applied (e.g. if the character intercepts a given amount of negative statuses)
        foreach (var relic in RunManager.Instance.relics)
        {
            if (!relic.CanApplyStatus(status, this))
            {
                return; // Status application is blocked by a relic
            }
        }
        foreach (var existingStatus in statusEffects)
        {
            if (!existingStatus.CanApply(status,this))
            {
                return; 
            }
        }
        status.InsertInto(statusEffects);
        status.OnApply(this);
    }
    public void RemoveStatus(StatusEffect status)
    {
        statusEffects.Remove(status);
        status.OnExpire(this);
    }
    public bool HasStatus(string statusName)
    {
        return statusEffects.Any(s => s.Name == statusName);
    }
    public void AfterAction(Character actor,CardInstance card)
    {
        ExpireStatuses();
        foreach (var status in statusEffects.ToList())
        {
            status.OnAnyCardPlayed(actor, card);
        }
    }
    public void ExpireStatuses()
    {
        var toRemove = new List<StatusEffect>();
        foreach (var status in statusEffects.ToList())
        {
            if (status.Duration == 0||status.mustExpire)
            {
                status.OnExpire(this);
                toRemove.Add(status);
            }
        }
        foreach (var status in toRemove)
        {
            statusEffects.Remove(status);
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
            foreach (var status in statusEffects.ToList())
            {
                newArmor = Mathf.Max(newArmor, status.ArmorOnTurnStart(armor,this));
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
        foreach (var status in statusEffects.ToList())
        {
            status.OnTurnStart(this);
        }
        foreach (var relic in RunManager.Instance.relics)
        {
            relic.OnAnyTurnStart(this);
        }
        ExpireStatuses();
        combat.state.ResetTurnStartFlags(this);
    }
    public void EndTurn()
    {
        onTurn = false;
        foreach (var status in statusEffects.ToList())
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
        combat.state.ResetTurnEndFlags(this);
        combat.FieldTurnEnd();
    }
    public void FieldTurnEnd()
    {
        foreach (var status in statusEffects.ToList())
        {
            status.OnFieldTurnEnd(this);
        }
        foreach (var relic in RunManager.Instance.relics)
        {
            relic.OnFieldTurnEnd(this);
        }
    }
    public void SpendEnergy(int amount)
    {
        if (amount==-1)
        {
            amount = resources.energy;
        }
        combat.state.energySpentThisTurn += amount;
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

        cm.deck.Draw(1,cm.state.turnCount==1);
    }
    public void DiscardCard()
    {
        if (!isPlayer)
            return;

        var cm = GetCombatManager();

        if (cm == null)
            return;

        cm.deck.Discard();
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
            status.OnHPLoss(this, damage);
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
        foreach (var status in statusEffects.ToList())
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
        foreach (var status in statusEffects.ToList())
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