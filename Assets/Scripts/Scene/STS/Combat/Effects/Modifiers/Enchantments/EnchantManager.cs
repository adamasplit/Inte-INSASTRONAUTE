using System.Collections.Generic;
using UnityEngine;
public static class EnchantManager
{
    public static void ApplyEnchant(CardInstance card, int charges)
    {
        List<EnchantType> possibleEnchants= new List<EnchantType>();
        
        if (!card.HasEnchantment("Mécanique"))
            possibleEnchants.Add(EnchantType.Mechanical);
        if (card.data.type == CardType.Attaque)
        {
            possibleEnchants.Add(EnchantType.Sharpness);
            if (!card.HasEnchantment("Télékinésie"))
                possibleEnchants.Add(EnchantType.Telekinesis);
            if (!card.HasEnchantment("Humanisme"))
                possibleEnchants.Add(EnchantType.Humanism);
        }   

        EnchantType type= possibleEnchants[Random.Range(0, possibleEnchants.Count)];
        EnchantmentData edata=GetEnchantByType(type, charges).data;

        int usedCharges = Mathf.Min(charges, edata.maxLevel);
        card.AddEnchantment(GetEnchantByType(type, usedCharges));
        if (usedCharges < charges)
        {
            ApplyEnchant(card, charges - usedCharges);
        }
        

    }

    public static CardEnchantment GetEnchantByType(EnchantType type, int level)
    {
        EnchantmentData edata = type switch
        {
            EnchantType.Sharpness => new SharpnessEnchantment(),
            EnchantType.Telekinesis => new TelekinesisEnchantment(),
            EnchantType.Humanism => new HumanismEnchantment(),
            EnchantType.Mechanical => new MechanicalEnchantment(),
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