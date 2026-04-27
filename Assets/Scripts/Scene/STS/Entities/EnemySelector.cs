using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class EnemySelector
{
    public static EnemyPool pool=null;
    public static List<EnemyData> GetRandomEncounter(
        int floor,
        bool elite = false,
        bool boss = false)
    {
        if (pool == null)
        {
            pool=Resources.Load<EnemyPool>("STS/Enemies/EnemyPool");
        }
        var candidates = pool.enemies
            .Where(e =>
                e.minFloor <= floor &&
                e.maxFloor >= floor &&
                e.elite == elite &&
                e.boss == boss)
            .ToList();
        Debug.Log($"Found {candidates.Count} candidates");
        if (candidates.Count == 0)
        {
            Debug.LogError("No enemy found for this config");
            return null;
        }

        float totalWeight = candidates.Sum(c => c.weight);
        float roll = Random.value * totalWeight;

        float current = 0;

        foreach (var e in candidates)
        {
            current += e.weight;
            if (roll <= current)
                return e.enemies;
        }

        return candidates[0].enemies;
    }
}