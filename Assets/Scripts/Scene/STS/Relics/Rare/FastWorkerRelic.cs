using System.Collections;
using System.Collections.Generic;
public class FastWorkerRelic : Relic
{
    private int cardsPlayedThisTurn;
    private bool triggeredThisTurn;

    public FastWorkerRelic()
    {
        rarity = RelicRarity.Rare;
        name = "Travailleur rapide";
        description = "Une fois par tour, après avoir joué 3 compétences, avancez fortement votre tour.";
    }
    public override void OnTurnStart(Character player)
    {
        cardsPlayedThisTurn = 0;
        triggeredThisTurn = false;
    }
    public override void OnCardPlayed(Character player,List<Character> targets,CardInstance card)
    {
        if (triggeredThisTurn||card == null || card.data == null || card.data.type != CardType.Compétence)
        {
            return;
        }
        cardsPlayedThisTurn++;
        if (cardsPlayedThisTurn >= 3)
        {
            triggeredThisTurn = true;
            player.GetCombatManager().turnSystem.ApplyAdvanceAllTurns(player, 6f);
        }
    }
}