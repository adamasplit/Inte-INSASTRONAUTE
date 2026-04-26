using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu]
public class STSCardData : ScriptableObject
{
    public string cardName;
    public int cost;
    public CardType type;
    public CardRarity rarity;
    public List<EffectEntry> effects;
    public TargetingMode targetingMode;
    public List<ModifierData> modifiers = new();
    public bool exhaust=false;
    public bool retain=false;
}