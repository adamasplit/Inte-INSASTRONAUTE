using System.Collections.Generic;

public class SurgeSequenceRelic : Relic
{
    private CardType previousCardType = CardType.Rien;

    public SurgeSequenceRelic()
    {
        rarity = RelicRarity.Uncommon;
        name = "Séquence de surcharge";
        description = "Quand vous jouez une Attaque juste après un Pouvoir, gagnez 1 énergie.";
    }

    public override void OnCombatStart(Character player)
    {
        previousCardType = CardType.Rien;
    }

    public override void OnCardPlayed(Character player, List<Character> targets, CardInstance card)
    {
        if (card != null && card.data != null && card.data.type == CardType.Attaque && previousCardType == CardType.Pouvoir)
        {
            player.GainEnergy(1);
        }

        previousCardType = card != null && card.data != null ? card.data.type : CardType.Rien;
    }
}