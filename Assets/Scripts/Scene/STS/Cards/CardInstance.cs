using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class CardInstance
{
    public string displayName ;
    public STSCardData data;
    public List<StatModifier> baseModifiers = new();
    public List<StatModifier> addedModifiers = new();
    public List<CardEnchantment> enchantments = new();
    public List<EffectEntry> addedEffects = new();
    public TargetingMode targetingMode;
    public List<CardTag> tags = new();
    public string lastDescription = "";
    public bool HasTag(CardTag tag)
    {
        return tags.Contains(tag) || data.HasTag(tag);
    }
    public void AddTag(CardTag tag)
    {
        if (!tags.Contains(tag))
        {
            tags.Add(tag);
        }
    }
    public CardInstance(STSCardData data)
    {
        this.data = data;
        if (data==null)
        {
            Debug.LogError("Card data is null for card instance.");
            return;
        }
        this.displayName = data.cardName;
        this.targetingMode = data.targetingMode;
        if (data.modifiers != null)
        {
            foreach (var modData in data.modifiers)
            {
                Debug.Log($"Adding base modifier from card data: {modData.type} {modData.value}");
                baseModifiers.Add(modData.CreateModifier());
            }
        }
    }
    public void RemoveTemporaryModifiers()
    {
        addedModifiers.RemoveAll(mod => mod.temporary);
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

    public string GetDescription(EffectContext ctx=null)
    {
        if (ctx == null)
        {
            ctx = new EffectContext();
            ctx.source = null;
            ctx.target = null;
            ctx.combat = null;
        }
        string text = "";
        switch (targetingMode)
        {
            case TargetingMode.Player:
                text += "<color=green>(Soi-même)</color> :\n";
                break;
            case TargetingMode.Enemy:
                text += "<color=red>(Adversaire)</color> :\n";
                break;
            case TargetingMode.AllCharacters:
                text += "<color=blue>(Tous les personnages)</color> :\n";
                break;
            case TargetingMode.AllEnemies:
                text += "<color=red>(Tous les adversaires)</color> :\n";
                break;
            case TargetingMode.RandomEnemy:
                text += "<color=red>(Adversaire aléatoire)</color> :\n";
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
        foreach (var mod in GetModifiers(false,true))
        {
            if (mod.description!=" ")
            {
                if (string.IsNullOrEmpty(mod.description))
                {
                    text += $"{mod.Describe()}\n";
                }
                else
                {
                    text += $"{mod.description}\n";
                }
            }
        }
        if (HasTag(CardTag.Exhaust))
            text += "<color=orange>[Épuisement]</color>\n";
        if (HasTag(CardTag.Retain))
            text += "<color=orange>[Retenue]</color>\n";
        if (HasTag(CardTag.Ethereal))
            text += "<color=orange>[Éthérée]</color>\n";
        if (HasTag(CardTag.Infinite))
            text += "<color=orange>[Infinie]</color>\n";
        if (HasTag(CardTag.Innate))
            text += "<color=red>[Innée]</color>\n";
        foreach (var ench in enchantments)
        {
            // Affiche les enchantements commençant par "Curse" en rouge, les autres en violet
            text += $"<size=90%>"+(ench.data.name.StartsWith("Curse") ? "<color=red>" : "<color=purple>");
            string levelText = ench.data.maxLevel > 1 ? ToRoman(ench.level) : "";
            text += $"{ench.data.name} {levelText}";
            text += "</color></size>\n";
        }
        lastDescription = text.TrimEnd();
        return lastDescription;
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

    public List<EffectEntry> GetEffects(bool includeEnchantments=true,bool includeAdded=true)
    {
        List<EffectEntry> effects = new();
        effects.AddRange(data.effects);
        effects.AddRange(addedEffects);
        if (includeEnchantments)
        {
            foreach (var enchantment in enchantments)
            {
                effects.AddRange(enchantment.GetEffects());
            }
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
    public bool HasEnchantments()
    {
        return enchantments.Count > 0;
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
        foreach (var mod in addedModifiers)
        {
            clone.addedModifiers.Add(mod.Clone());
        }
        foreach (var ench in enchantments)
        {
            clone.enchantments.Add(new CardEnchantment { data = ench.data, level = ench.level });
        }
        foreach (var effect in addedEffects)
        {
            clone.addedEffects.Add(effect);
        }
        foreach (var tag in tags)
        {
            clone.tags.Add(tag);
        }
        return clone;
    }
    public static CardInstance Merge(List<CardInstance> cards)
    {
        if (cards == null || cards.Count == 0) return null;

        STSCardData data = cards[0].data;
        CardInstance merged = new CardInstance(data);
        merged.displayName = "";
        foreach (var card in cards)
        {
            merged.displayName += card.displayName+(cards.IndexOf(card)==cards.Count-1?"":"+");
            if (card.targetingMode==TargetingMode.AllCharacters)
            {
                merged.targetingMode = TargetingMode.AllCharacters;
            }
            if (card.targetingMode==TargetingMode.AllEnemies && merged.targetingMode!=TargetingMode.AllCharacters)
            {
                merged.targetingMode = TargetingMode.AllEnemies;
            }
            if (card.targetingMode==TargetingMode.RandomEnemy && merged.targetingMode!=TargetingMode.AllCharacters && merged.targetingMode!=TargetingMode.AllEnemies)
            {
                merged.targetingMode = TargetingMode.RandomEnemy;
            }
            if (card.targetingMode==TargetingMode.Enemy && merged.targetingMode==TargetingMode.Player)
            {
                merged.targetingMode = TargetingMode.Enemy;
            }
            foreach (var mod in card.GetModifiers(false, true))
            {
                if (!merged.GetModifiers(false, true).Contains(mod))
                {
                    merged.addedModifiers.Add(mod);
                }
            }
            foreach (var effect in card.GetEffects(false, true))
            {
                if (!merged.GetEffects(false, true).Contains(effect))
                {
                    merged.addedEffects.Add(effect);
                }
            }
            foreach (var ench in card.enchantments)
            {
                merged.AddEnchantment(ench);
            }
            foreach (var tag in card.tags)
            {
                if (!merged.tags.Contains(tag))
                {
                    merged.tags.Add(tag);
                }
            }
        }

        return merged;
    }
}