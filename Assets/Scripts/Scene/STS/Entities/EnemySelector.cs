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
        Debug.Log($"Selecting enemy for floor {floor} (elite={elite}, boss={boss})");
        if (pool == null)
        {
            pool=Resources.Load<EnemyPool>("STS/Enemies/EnemyPool");
        }
        Debug.Log($"Loaded enemy pool with {pool.enemies.Count} entries");
        Debug.Log($"Filtering enemies for floor {floor}...");
        Debug.Log($"Enemies with correct floor: {pool.enemies.Count(e => e.minFloor <= floor && e.maxFloor >= floor)}");
        Debug.Log($"Enemies with correct elite flag: {pool.enemies.Count(e => e.elite == elite)}");
        Debug.Log($"Enemies with correct boss flag: {pool.enemies.Count(e => e.boss == boss)}");
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