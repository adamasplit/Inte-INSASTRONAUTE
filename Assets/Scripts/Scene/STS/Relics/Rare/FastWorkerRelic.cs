using System.Collections;
using System.Collections.Generic;
public class FastWorkerRelic : Relic
{
    public FastWorkerRelic()
    {
        rarity = RelicRarity.Rare;
        name = "Travailleur rapide";
        description = "Quand vous jouez une carte, avancez légèrement votre tour.";
    }
    public override void OnCardPlayed(Character player,List<Character> targets,CardInstance card)
    {
        player.GetCombatManager().turnSystem.ApplyAdvanceAllTurns(player,0.5f);
    }
}