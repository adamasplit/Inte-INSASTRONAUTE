using System.Collections.Generic;
using UnityEngine;
public static class EnchantManager
{
    public static void ApplyEnchant(CardInstance card, int charges)
    {
        List<(EnchantType, float)> possibleEnchants= new List<(EnchantType, float)>();
        
        if (!card.HasEnchantment("Mécanique"))
            possibleEnchants.Add((EnchantType.Mechanical, 0.5f));
        if (card.data.type == CardType.Attaque)
        {
            possibleEnchants.Add((EnchantType.Sharpness, 2.0f));
            possibleEnchants.Add((EnchantType.Lifesteal, 0.3f));
            if (!card.HasEnchantment("Télékinésie"))
                possibleEnchants.Add((EnchantType.Telekinesis, 0.5f));
            if (!card.HasEnchantment("Humanisme"))
                possibleEnchants.Add((EnchantType.Humanism, 0.5f));
        }   
        if (card.data.type == CardType.Compétence&&card.data.effects.Exists(e=>e.type==EffectType.Armor))
        {
            possibleEnchants.Add((EnchantType.Protection, 2.0f));
        }

        EnchantType type= GetWeightedRandomEnchant(possibleEnchants);
        EnchantmentData edata=GetEnchantByType(type, charges).data;

        int usedCharges = Mathf.Min(charges, edata.maxLevel);
        card.AddEnchantment(GetEnchantByType(type, usedCharges));
        if (usedCharges < charges)
        {
            ApplyEnchant(card, charges - usedCharges);
        }
        

    }

    public static EnchantType GetWeightedRandomEnchant(List<(EnchantType, float)> enchants)
    {
        float totalWeight = 0f;
        foreach (var enchant in enchants)
        {
            totalWeight += enchant.Item2;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        foreach (var enchant in enchants)
        {
            cumulativeWeight += enchant.Item2;
            if (randomValue <= cumulativeWeight)
            {
                return enchant.Item1;
            }
        }

        return enchants[enchants.Count - 1].Item1; // Fallback
    }

    public static CardEnchantment GetEnchantByType(EnchantType type, int level)
    {
        EnchantmentData edata = type switch
        {
            EnchantType.Sharpness => new SharpnessEnchantment(),
            EnchantType.Telekinesis => new TelekinesisEnchantment(),
            EnchantType.Humanism => new HumanismEnchantment(),
            EnchantType.Mechanical => new MechanicalEnchantment(),
            EnchantType.Protection => new ProtectionEnchantment(),
            EnchantType.Lifesteal => new LifestealEnchantment(),
            _ => null
        };
        if (edata == null) return null;
        return new CardEnchantment { data = edata, level = level };
    }

    public enum EnchantType
    {
        Sharpness,
        Telekinesis,
        Lifesteal,
        AllIn,
        Humanism,
        HeatTransfer,
        Swiftness,
        Unbreaking,
        Mending,
        Mechanical,
        Protection
    }
}