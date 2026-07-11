using System.Collections.Generic;

public class CounterStanceRelic : Relic
{
    private CardType previousCardType = CardType.Rien;
    private readonly List<StatModifier> modifiers;

    public CounterStanceRelic()
    {
        rarity = RelicRarity.Uncommon;
        name = "Posture contre-offensive";
        description = "Jouer une Compétence qui donne de l'Armure juste après une Attaque donne 2 Armure supplémentaire.";
        modifiers = new List<StatModifier> { new CounterStanceArmorModifier(this) };
    }

    public override void OnCombatStart(Character player)
    {
        previousCardType = CardType.Rien;
    }

    public override List<StatModifier> GetStatModifiers(EffectContext ctx)
    {
        return modifiers;
    }

    public override void OnCardPlayed(Character player, List<Character> targets, CardInstance card)
    {
        previousCardType = card != null && card.data != null ? card.data.type : CardType.Rien;
    }

    private bool ShouldBoostArmor(EffectContext ctx)
    {
        return ctx != null && ctx.source != null && ctx.source.isPlayer && ctx.card != null && ctx.card.data != null
            && ctx.card.data.type == CardType.Compétence
            && previousCardType == CardType.Attaque;
    }

    private class CounterStanceArmorModifier : StatModifier
    {
        private readonly CounterStanceRelic relic;

        public CounterStanceArmorModifier(CounterStanceRelic relic)
        {
            this.relic = relic;
            type = StatType.Armor;
            modifierType = ModifierType.Additive;
            description = "+2 Armure après une attaque.";
        }

        public override bool AppliesTo(StatType stat, EffectContext ctx)
        {
            return stat == StatType.Armor && relic.ShouldBoostArmor(ctx);
        }

        public override int Modify(int value, EffectContext ctx)
        {
            return value + 2;
        }

        public override string Describe()
        {
            return description;
        }
    }
}