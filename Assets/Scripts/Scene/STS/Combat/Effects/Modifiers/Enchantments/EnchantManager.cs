using System.Collections.Generic;
using UnityEngine;
public static class EnchantManager
{
    public static void ApplyEnchant(CardInstance card, int charges, bool includeTreasureEnchants=true)
    {
        List<(EnchantType, float)> possibleEnchants= new List<(EnchantType, float)>();
        
        if (!card.HasEnchantment("Mécanique")&&!card.HasEnchantment("Infinity")) 
            possibleEnchants.Add((EnchantType.Mechanical, 0.1f));
        if (card.data.type == CardType.Attaque)
        {
            possibleEnchants.Add((EnchantType.Power, 2.0f));
            possibleEnchants.Add((EnchantType.Lifesteal, 0.3f));
            
            possibleEnchants.Add((EnchantType.Flame, 0.5f));
            possibleEnchants.Add((EnchantType.Impaling, 0.5f));
            if (!card.HasEnchantment("Télékinésie"))
                possibleEnchants.Add((EnchantType.Telekinesis, 0.5f));
            if (!card.HasEnchantment("Humanisme"))
                possibleEnchants.Add((EnchantType.Humanism, 0.5f));
        }   
         if (card.data.effects.Exists(e=>e.type==EffectType.DelayTurn))
        {
            possibleEnchants.Add((EnchantType.Punch, 0.6f));
        }
        if (!card.data.effects.Exists(e=>e.type==EffectType.AdvanceTurn))
        {
            possibleEnchants.Add((EnchantType.FeatherFalling, 0.6f));
        }
        if (card.data.effects.Exists(e=>e.type==EffectType.Armor))
        {
            possibleEnchants.Add((EnchantType.Protection, 2.0f));
        }
        if (card.data.effects.Exists(e=>e.type==EffectType.Status&&e.duration>0))
        {
            possibleEnchants.Add((EnchantType.Extension, 0.5f));
        }
        if (card.data.effects.Exists(e=>e.type==EffectType.Status&&e.value>0))
        {
            possibleEnchants.Add((EnchantType.Efficiency, 0.5f));
        }
        if (card.data.exhaust)
        {
            possibleEnchants.Add((EnchantType.Mending, 1.0f));
            possibleEnchants.Add((EnchantType.Unbreaking, 1.0f));
            possibleEnchants.Add((EnchantType.CurseOfVanishing, 0.5f));
        }
        else if (!card.HasEnchantment("Infinity")&&!card.HasEnchantment("Mécanique"))
        {
            possibleEnchants.Add((EnchantType.Infinity, 0.5f));
        }
        if (card.data.targetingMode==TargetingMode.AllEnemies||card.data.targetingMode==TargetingMode.AllCharacters)
        {
            possibleEnchants.Add((EnchantType.SweepingEdge, 0.5f));
        }
        if (card.data.targetingMode==TargetingMode.Enemy||card.data.targetingMode==TargetingMode.RandomEnemy)
        {
            if (!card.data.effects.Exists(e=>e.type==EffectType.Damage)) possibleEnchants.Add((EnchantType.Sharpness, 1.0f));
            if (!card.data.effects.Exists(e=>e.type==EffectType.DelayTurn)) possibleEnchants.Add((EnchantType.Knockback, 0.6f));
        }
        if (!card.HasEnchantment("Écho"))
        {
            possibleEnchants.Add((EnchantType.Astra, 0.05f));
        }
        if (!card.HasEnchantment("Stellaire"))
        {
            possibleEnchants.Add((EnchantType.Replay, 0.05f));
        }
        if (possibleEnchants.Count == 0) return;

        EnchantType type= GetWeightedRandomEnchant(possibleEnchants);
        EnchantmentData edata=GetEnchantByType(type, charges).data;

        int usedCharges = Mathf.Min(charges, edata.maxLevel-card.GetEnchantmentLevel(edata.name),Random.Range(1,4));
        if (usedCharges <= 0) return;
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
            EnchantType.Sharpness => new SharpnessEnchantment(), // Ajoute des dégâts
            EnchantType.Telekinesis => new TelekinesisEnchantment(), // Empêche les dégâts de baisser en dessous d'une certaine valeur
            EnchantType.Humanism => new HumanismEnchantment(), // Les dégâts traversent l'Armure
            EnchantType.Mechanical => new MechanicalEnchantment(), // La carte se joue automatiquement à la fin du tour
            EnchantType.Protection => new ProtectionEnchantment(), // Ajoute de l'Armure
            EnchantType.Lifesteal => new LifestealEnchantment(), // Soigne le joueur en fonction des dégâts infligés
            EnchantType.Mending => new MendingEnchantment(), // La carte revient dans la pioche au moment du mélange
            EnchantType.Unbreaking => new UnbreakingEnchantment(), // La carte peut ne pas s'épuiser
            EnchantType.Replay => new ReplayEnchantment(), // La carte se joue plusieurs fois
            EnchantType.Extension => new ExtensionEnchantment(), // Prolonge la durée des effets de la carte
            EnchantType.Efficiency => new EfficiencyEnchantment(), // Augmente la puissance des effets de la carte
            EnchantType.Power => new PowerEnchantment(), // Augmente les dégâts d'une carte d'attaque
            EnchantType.Knockback => new KnockbackEnchantment(), // Repousse le tour de la cible
            EnchantType.FeatherFalling => new FeatherFallingEnchantment(), // Avance le tour du lanceur de la carte
            EnchantType.CurseOfVanishing => new CurseOfVanishingEnchantment(), // Augmente la chance que la carte s'épuise
            EnchantType.Impaling => new ImpalingEnchantment(), // Augmente les dégâts contre les ennemis avec de l'armure
            EnchantType.SweepingEdge => new SweepingEdgeEnchantment(), // Augmente les dégâts en fonction du nombre d'ennemis touchés
            EnchantType.Infinity => new InfinityEnchantment(), 
            EnchantType.Astra => new AstraEnchantment(),
            EnchantType.Flame=> new FlameEnchantment(),
            EnchantType.Punch=> new PunchEnchantment(),
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
        Protection,
        Replay,
        Extension,
        Efficiency,
        Power,
        Knockback,
        Infinity,
        Astra,
        Flame,
        Punch,
        FeatherFalling,
        CurseOfVanishing,
        Impaling,
        SweepingEdge
    }
}