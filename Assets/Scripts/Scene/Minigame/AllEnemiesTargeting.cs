using UnityEngine;
using System.Collections.Generic;
public class AllEnemiesTargeting : MonoBehaviour, ITargetingBehaviour
{
    public List<Enemy> GetTargets(Column column)
    {
        return column.GetEnemies(); // signal spécial : attaque globale
    }
}
