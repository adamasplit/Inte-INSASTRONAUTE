using System.Collections.Generic;

public class StrengthCadenceRelic : Relic
{
    private int attacksThisTurn;

    public StrengthCadenceRelic()
    {
        rarity = RelicRarity.Uncommon;
        name = "Cadence offensive";
        description = "Gagnez 1 de Force tous les 3 Attaques jouées dans un tour.";
    }

    public override void OnTurnStart(Character player)
    {
        attacksThisTurn = 0;
    }

    public override void OnCardPlayed(Character player, List<Character> targets, CardInstance card)
    {
        if (card == null || card.data == null || card.data.type != CardType.Attaque)
        {
            return;
        }

        attacksThisTurn++;
        if (attacksThisTurn % 3 == 0)
        {
            player.AddStatus(StatusEffect.Factory(StatusType.Strength, 1, -1));
        }
    }
}