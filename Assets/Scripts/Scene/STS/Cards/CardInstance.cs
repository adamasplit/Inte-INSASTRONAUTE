using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
public class CardInstance
{
    public string instanceId;
    public string displayName ;
    public STSCardData data;

    /// <summary>
    /// L'identifiant sous lequel le serveur désigne cette carte, quand ce n'est pas celui de sa
    /// définition.
    ///
    /// <para>Un mouvement ennemi s'appelle « enemy-move:{ennemi}:{index} » côté serveur, mais la
    /// carte que le client en fabrique porte le nom du mouvement comme identifiant. Comparer
    /// <c>data.id</c> à ce que le serveur envoie ne pouvait donc jamais correspondre : la carte
    /// était reconstruite à chaque synchronisation d'état, et la vue attachée à l'instance
    /// précédente restait orpheline. C'est le même piège que celui des cartes de MRIE.</para>
    /// </summary>
    public string serverDefinitionId;

    /// <summary>Ce que le serveur appelle cette carte : son identifiant propre, sinon celui de sa définition.</summary>
    public string DefinitionId =>
        !string.IsNullOrEmpty(serverDefinitionId) ? serverDefinitionId : (data != null ? data.id : null);
    public List<StatModifier> baseModifiers = new();
    public List<StatModifier> addedModifiers = new();
    public List<CardEnchantment> enchantments = new();
    public List<EffectEntry> addedEffects = new();
    public TargetingMode targetingMode;
    public List<CardTag> tags = new();

    /// <summary>
    /// La famille de cette copie, quand elle n'est pas celle de sa carte d'origine.
    ///
    /// <para>Une copie volée par ITI reprend les effets d'une carte adverse mais pas son identité :
    /// elle est une Attaque si ces effets frappent, une Compétence sinon, quoi qu'en dise la carte
    /// dont elle vient. Le serveur l'envoie dans <c>cardType</c> sur l'instance.</para>
    /// </summary>
    public CardType? overrideType;

    /// <summary>
    /// Le coût de cette copie, quand il ne vient pas de sa carte d'origine. Un mouvement ennemi
    /// coûte zéro pour l'ennemi qui le joue ; la copie que le joueur en reçoit, elle, se paie.
    /// </summary>
    public int? overrideCost;

    public string lastDescription = "";

    /// <summary>La famille à afficher et à tester : celle de la copie, sinon celle de la carte.</summary>
    public CardType Type => overrideType ?? (data != null ? data.type : CardType.Rien);

    /// <summary>
    /// L'illustration de cette carte.
    ///
    /// <para>Une copie garde celle de la carte dont elle vient quand cette carte en a une. Les
    /// mouvements ennemis n'en ont pas — ils n'en héritent que d'une vraie carte qui les porte —
    /// et retombent alors sur l'icône générique de leur famille. C'est ici que la règle vit, et
    /// pas côté serveur : le catalogue du serveur ne connaît aucune illustration.</para>
    /// </summary>
    public Sprite Icon
    {
        get
        {
            if (data != null && data.icon != null)
                return data.icon;
            return STSCardDatabase.GetGenericIcon(Type);
        }
    }
    public bool HasTag(CardTag tag)
    {
        return tags.Contains(tag) || (data != null && data.HasTag(tag));
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
        this.instanceId = Guid.NewGuid().ToString("N");
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
        if (data == null) return 0;
        if (data.xCost)
        {
            return -1;
        }
        return BattleCalculator.GetModifiedValue(overrideCost ?? data.cost, StatType.Cost, ctx);
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
            case TargetingMode.AnyPlayer:
                text += "<color=green>(Un allié)</color> :\n";
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
                    text += $"{mod.Describe()}{(mod.Describe().EndsWith(".") ? "" : ".")}\n";
                }
                else
                {
                    text += $"{mod.description}{(mod.description.EndsWith(".") ? "" : ".")}\n";
                }
            }
        }
        if (HasTag(CardTag.Unplayable))
            text += "<color=orange>[Injouable]</color>\n";
        if (HasTag(CardTag.Automatic))
            text += "<color=red>[Automatique]</color>\n";
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
        if (data != null && data.effects != null)
            effects.AddRange(data.effects);
        if (includeAdded && addedEffects != null)
            effects.AddRange(addedEffects);
        if (includeEnchantments && enchantments != null)
        {
            foreach (var enchantment in enchantments)
            {
                if (enchantment != null)
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
        clone.instanceId = instanceId;
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
        clone.displayName = displayName;
        clone.overrideType = overrideType;
        clone.overrideCost = overrideCost;
        clone.serverDefinitionId = serverDefinitionId;
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
            if (card.targetingMode==TargetingMode.Enemy && (merged.targetingMode==TargetingMode.Player||merged.targetingMode==TargetingMode.AnyPlayer))
            {
                merged.targetingMode = TargetingMode.Enemy;
            }
            if (card.targetingMode==TargetingMode.AnyPlayer && merged.targetingMode==TargetingMode.Player)
            {
                merged.targetingMode = TargetingMode.AnyPlayer;
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