using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class CardInstance
{
    public STSCardData data;
    public List<StatModifier> baseModifiers = new();
    public List<StatModifier> addedModifiers = new();
    public List<CardEnchantment> enchantments = new();

    public CardInstance(STSCardData data)
    {
        this.data = data;
        if (data.modifiers != null)
        {
            foreach (var modData in data.modifiers)
            {
                baseModifiers.Add(modData.CreateModifier());
            }
        }
    }

    public int Cost(EffectContext ctx=null)
    {
        int cost = data.cost;
        if (data.xCost)
        {
            return -1;
        }
        else
        {
            cost = BattleCalculator.GetModifiedValue(data.cost, StatType.Cost, ctx);
        }
        return cost;
    }

    public string GetDescription(EffectContext ctx)
    {
        string text = "";
        switch (data.targetingMode)
        {
            case TargetingMode.Player:
                text += "<color=green>(Vous)</color> :\n";
                break;
            case TargetingMode.Enemy:
                text += "<color=red>(Ennemi)</color> :\n";
                break;
            case TargetingMode.AllCharacters:
                text += "<color=blue>(Tous les personnages)</color> :\n";
                break;
            case TargetingMode.AllEnemies:
                text += "<color=red>(Tous les ennemis)</color> :\n";
                break;
            case TargetingMode.RandomEnemy:
                text += "<color=red>(Ennemi aléatoire)</color> :\n";
                break;
        }
        foreach (var e in GetEffects())
        {
            string desc=EffectDescription.Get(e,ctx);
            if (desc!=" "&&e.description!=" ")
            {
                text += desc + "\n";
            }
        }
        Debug.Log("modifiers count: "+GetModifiers().Count);
        foreach (var mod in GetModifiers(StatType.Damage,false,false))
        {
            text += $"<color=red>{mod.Describe()}</color>\n";
        }
        foreach (var mod in GetModifiers(StatType.Armor,false,false))
        {
            text += $"<color=blue>{mod.Describe()}</color>\n";
        }
        foreach (var ench in enchantments)
        {
            // Affiche les enchantements commençant par "Curse" en rouge, les autres en violet
            text += $"\n<size=90%>"+(ench.data.name.StartsWith("Curse") ? "<color=red>" : "<color=purple>");
            string levelText = ench.data.maxLevel > 1 ? ToRoman(ench.level) : "";
            text += $"{ench.data.name} {levelText}";
            text += "</color></size>";
        }
        if (data.exhaust)
            text += "<color=orange>[Épuisement]</color>\n";
        if (data.retain)
            text += "<color=orange>[Retenue]</color>\n";
        return text.TrimEnd();
    }
    public List<StatModifier> GetModifiers(StatType type,bool includeEnchantments=true,bool includeAdded=true)
    {
        List<StatModifier> mods = new();
        if (includeAdded)
        {
            mods.AddRange(addedModifiers.Where(m => m.type == type));
        }
        mods.AddRange(baseModifiers.Where(m => m.type == type));
        if (includeEnchantments)
        {
            for (int i = enchantments.Count - 1; i >= 0; i--)
            {
                var enchantmentMods = enchantments[i].GetModifiers();
                mods.AddRange(enchantmentMods.Where(m => m.type == type));
            }
        }
        return mods;
    }
    public List<StatModifier> GetModifiers(bool includeEnchantments=true,bool includeAdded=true)
    {
        List<StatModifier> mods = new();
        mods.AddRange(baseModifiers);
        mods.AddRange(addedModifiers);
        if (includeEnchantments)
        {
            for (int i = enchantments.Count - 1; i >= 0; i--)
            {
                var enchantmentMods = enchantments[i].GetModifiers();
                mods.AddRange(enchantmentMods);
            }
        }
        return mods;
    }

    public List<EffectEntry> GetEffects()
    {
        List<EffectEntry> effects = new();
        effects.AddRange(data.effects);
        foreach (var enchantment in enchantments)
        {
            effects.AddRange(enchantment.GetEffects());
        }
        return effects;
    }

    private string ToRoman(int number)
    {
        if (number < 1) return "";
        if (number >= 10) return "X" + ToRoman(number - 10);
        if (number >= 9) return "IX" + ToRoman(number - 9);
        if (number >= 5) return "V" + ToRoman(number - 5);
        if (number >= 4) return "IV" + ToRoman(number - 4);
        if (number >= 1) return "I" + ToRoman(number - 1);
        return "";
    }
    public void AddModifier(StatModifier mod)
    {
        addedModifiers.Add(mod);
    }
    public bool isEnchanted()
    {
        return enchantments.Count > 0;
    }

    public void AddEnchantment(CardEnchantment enchantment)
    {
        if (enchantments.Exists(e => e.data.name == enchantment.data.name))
        {
            var existing = enchantments.Find(e => e.data.name == enchantment.data.name);
            existing.level = (existing.level+enchantment.level);
        }
        else
        {
            enchantments.Add(enchantment);
        }
    }

    public bool HasEnchantment(string enchantmentName)
    {
        return enchantments.Exists(e => e.data.name == enchantmentName);
    }
    public int GetEnchantmentLevel(string enchantmentName)
    {
        var enchantment = enchantments.Find(e => e.data.name == enchantmentName);
        return enchantment != null ? enchantment.level : 0;
    }

    public CardInstance Clone()
    {
        CardInstance clone = new CardInstance(data);
        foreach (var mod in baseModifiers)
        {
            clone.baseModifiers.Add(mod.Clone());
        }
        foreach (var mod in addedModifiers)
        {
            clone.addedModifiers.Add(mod.Clone());
        }
        foreach (var ench in enchantments)
        {
            clone.enchantments.Add(new CardEnchantment { data = ench.data, level = ench.level });
        }
        return clone;
    }
}