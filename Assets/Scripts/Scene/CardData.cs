using UnityEngine;
public enum TargetingType
{
    FirstEnemy,
    AllEnemies,
    AllEnemiesAllColumns,
    AllFirstEnemies,
    RandomEnemy
}

public enum AttackType
{
    Instant,
    Beam,
    Projectile,
    ContinuousBeam,
    ProjectileFlurry,
    MultiProjectiles
}
[CreateAssetMenu(menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string cardId;

    [Header("UI")]
    public Sprite sprite;

    [Header("Gameplay")]
    public int rarity;
    public int FirstTimeValue;
    public int SubsequentValue;
    public string cardName;
    public string description;
    public Element element;
    public float baseDamage;
    public float duration=1f; // for beam attacks
    public TargetingType targetingType;
    public AttackType attackType;
    public int projectileCount = 1; // for projectile attacks
}
