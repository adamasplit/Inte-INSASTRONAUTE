using System.Collections.Generic;

public class ScholarSequenceRelic : Relic
{
    private CardType previousCardType = CardType.Rien;

    public ScholarSequenceRelic()
    {
        rarity = RelicRarity.Uncommon;
        name = "Séquence savante";
        description = "Quand vous jouez un Pouvoir juste après une Compétence, piochez 2 cartes.";
    }

    public override void OnCombatStart(Character player)
    {
        previousCardType = CardType.Rien;
    }

    public override void OnCardPlayed(Character player, List<Character> targets, CardInstance card)
    {
        if (card != null && card.data != null && card.data.type == CardType.Pouvoir && previousCardType == CardType.Compétence)
        {
            player.DrawCard();
            player.DrawCard();
        }

        previousCardType = card != null && card.data != null ? card.data.type : CardType.Rien;
    }
}