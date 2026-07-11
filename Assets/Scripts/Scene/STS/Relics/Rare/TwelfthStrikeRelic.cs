using System.Collections.Generic;
using UnityEngine;

public class TwelfthStrikeRelic : Relic
{
    private int attackCount;
    private readonly List<StatModifier> modifiers;

    public TwelfthStrikeRelic()
    {
        rarity = RelicRarity.Rare;
        name = "Douzième heure";
        description = "Chaque 12e Attaque inflige le double de dégâts.";
        modifiers = new List<StatModifier> { new TwelfthStrikeDamageModifier(this) };
    }

    public override List<StatModifier> GetStatModifiers(EffectContext ctx)
    {
        return modifiers;
    }

    public override void OnCardPlayed(Character player, List<Character> targets, CardInstance card)
    {
        if (card != null && card.data != null && card.data.type == CardType.Attaque)
        {
            attackCount++;
        }
    }

    private bool IsCurrentAttackEmpowered(EffectContext ctx)
    {
        return ctx != null && ctx.source != null && ctx.source.isPlayer && ctx.card != null && ctx.card.data != null
            && ctx.card.data.type == CardType.Attaque
            && ((attackCount + 1) % 12 == 0);
    }

    private class TwelfthStrikeDamageModifier : StatModifier
    {
        private readonly TwelfthStrikeRelic relic;

        public TwelfthStrikeDamageModifier(TwelfthStrikeRelic relic)
        {
            this.relic = relic;
            type = StatType.Damage;
            modifierType = ModifierType.Multiplicative;
            description = "Double les dégâts de chaque 12e attaque.";
        }

        public override bool AppliesTo(StatType stat, EffectContext ctx)
        {
            return stat == StatType.Damage && relic.IsCurrentAttackEmpowered(ctx);
        }

        public override int Modify(int value, EffectContext ctx)
        {
            return value * 2;
        }

        public override string Describe()
        {
            return description;
        }
    }
}