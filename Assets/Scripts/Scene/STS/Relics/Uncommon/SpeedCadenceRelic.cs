using System.Collections.Generic;

public class SpeedCadenceRelic : Relic
{
    private int attacksThisTurn;

    public SpeedCadenceRelic()
    {
        rarity = RelicRarity.Uncommon;
        name = "Cadence agile";
        description = "Gagnez 1 de Vitesse tous les 3 Attaques jouées dans un tour.";
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
            player.AddStatus(StatusEffect.Factory(StatusType.Speed, 1, -1));
        }
    }
}