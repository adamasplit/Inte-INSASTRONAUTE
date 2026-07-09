using System;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class EncounterEntry
{
    public string displayName;
    public List<EnemyData> enemies;
    public int minFloor;
    public int maxFloor;
    public int minAct=-1;
    public int maxAct=-1;
    public float weight = 1f;

    public bool elite;
    public bool boss;

    public EncounterEntryDTO ToDTO()
    {
        var dto = new EncounterEntryDTO
        {
            displayName = displayName,
            minFloor = minFloor,
            maxFloor = maxFloor,
            minAct = minAct,
            maxAct = maxAct,
            weight = weight,
            elite = elite,
            boss = boss
        };

        if (enemies != null)
        {
            foreach (var enemy in enemies)
            {
                dto.enemyIds.Add(enemy != null ? (!string.IsNullOrEmpty(enemy.id) ? enemy.id : enemy.enemyName) : null);
            }
        }

        return dto;
    }

    public static EncounterEntry FromDTO(EncounterEntryDTO dto)
    {
        var entry = new EncounterEntry
        {
            displayName = dto.displayName,
            minFloor = dto.minFloor,
            maxFloor = dto.maxFloor,
            minAct = dto.minAct,
            maxAct = dto.maxAct,
            weight = dto.weight,
            elite = dto.elite,
            boss = dto.boss,
            enemies = new List<EnemyData>()
        };

        if (dto.enemyIds != null)
        {
            foreach (var enemyId in dto.enemyIds)
            {
                if (string.IsNullOrWhiteSpace(enemyId))
                {
                    continue;
                }

                EnemyData enemy = EnemyDataDatabase.Get(enemyId);
                if (enemy != null)
                {
                    entry.enemies.Add(enemy);
                }
            }
        }

        return entry;
    }
}