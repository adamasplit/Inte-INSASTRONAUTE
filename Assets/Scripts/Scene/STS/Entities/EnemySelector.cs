using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class EnemySelector
{
    public static List<EnemyData> GetRandomEncounter(
        int floor,
        bool elite = false,
        bool boss = false)
    {
        return EnemyPoolDatabase.GetRandomEncounter(floor, elite, boss);
    }
}