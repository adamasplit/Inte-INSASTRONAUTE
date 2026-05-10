using UnityEngine;
using System.Collections.Generic;
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
    public Character(string name, int maxHP)
    {
        this.name = name;
        this.maxHP = maxHP;
        portrait=Resources.Load<Sprite>("STS/Portraits/" + name); // Assumes portraits are in Resources/STS/Portraits and named after the character
        this.currentHP = maxHP;
        this.armor = 0;
    }
    public int TakeDamage(int amount, bool ignoreArmor=false)
    {
        if (ignoreArmor)
        {
            currentHP = Mathf.Max(0, currentHP - amount);
            return amount;
        }
        else
        {
            int damageAfterArmor = Mathf.Max(0, amount - armor);
            armor = Mathf.Max(0, armor - amount);
            currentHP = Mathf.Max(0, currentHP - damageAfterArmor);
            return damageAfterArmor;
        }
    }

    public void GainEnergy(int amount)
    {
        resources.energy += amount;
    }

    public void AddArmor(int amount)
    {
        armor += amount;
    }
    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
    }
    public void AddStatus(StatusEffect status)
    {
        Debug.Log($"Adding status {status.Name} to {name} with potency {status.Value} and duration {status.Duration}");
        status.InsertInto(statusEffects);
        status.OnApply(this);
        Debug.Log($"{name} status effects: {string.Join(", ", statusEffects.ConvertAll(s => s.Name + "(" + s.Duration + ")"))}");
    }
    public void RemoveStatus(StatusEffect status)
    {
        statusEffects.Remove(status);
        status.OnExpire(this);
    }
    public void StartTurn()
    {
        armor=0;
        foreach (var status in statusEffects)
        {
            status.OnTurnStart(this);
        }
    }
    public void EndTurn()
    {
        foreach (var status in statusEffects)
        {
            status.OnTurnEnd(this);
            if (status.Duration > 0)
            {
                status.Duration--;
            }
        }
        statusEffects.RemoveAll(s => s.Duration == 0);
        if (isPlayer)
        {
            foreach (var relic in RunManager.Instance.relics)
            {
                relic.OnTurnEnd(this);
            }
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
}