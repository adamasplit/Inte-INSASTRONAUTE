using System.Collections.Generic;
public class RewardGenerator
{
    public List<STSCardData> GenerateCardChoices(CombatResult result)
    {
        List<STSCardData> pool = BuildCardPool(result);

        List<STSCardData> choices = new List<STSCardData>();

        for (int i = 0; i < 3; i++)
        {
            choices.Add(GetRandomCard(pool));
        }

        return choices;
    }
    List<STSCardData> BuildCardPool(CombatResult result)
    {
        List<STSCardData> pool = new List<STSCardData>();

        if (result.enemies != null&&RunManager.Instance.relics.Exists(r => r is ITIRelic))
        {
            foreach (var enemy in result.enemies)
        {
            pool.AddRange(enemy.rewardCards);
        }
        }

        pool.AddRange(GetFloorCards(result.floor));

        return pool;
    }
    public STSCardData GetRandomCard(List<STSCardData> pool)
    {
        if (pool.Count == 0) return null;
        int index = UnityEngine.Random.Range(0, pool.Count);
        return pool[index];
    }
    List<STSCardData> GetFloorCards(int floor)
    {
        // Implementation for getting floor-specific cards
        return STSCardDatabase.allCards;
    }
}