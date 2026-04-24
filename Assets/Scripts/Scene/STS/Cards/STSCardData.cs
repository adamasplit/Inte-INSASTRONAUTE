using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu]
public class STSCardData : ScriptableObject
{
    public string cardName;
    public int cost;
    public CardType type;
    public List<EffectEntry> effects;
    public TargetingMode targetingMode;
    public bool exhaust=false;
    public bool retain=false;
}