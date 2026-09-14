using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Combat/Enemy Pool")]
public class EnemyPool : ScriptableObject
{
    public int maxAct = -1;

    /// L'acte sur lequel une nouvelle run demarre, numerote comme le joueur le lit :
    /// 1 est l'Acte 1, 3 demarre directement sur l'Acte 3. Les minAct/maxAct des
    /// EncounterEntry restent, eux, des index internes commencant a zero.
    public int startingAct = 1;

    public float baseHpScaling = 1f;
    public List<float> actHpScaling = new();
    public List<EncounterEntry> enemies;
}