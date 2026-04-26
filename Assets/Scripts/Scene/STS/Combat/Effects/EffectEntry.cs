using System;
using UnityEngine;
[Serializable]
public class EffectEntry
{
    public EffectType type;
    public int value;
    public bool targetSelf=false;
    public StatusType statusType;
    public int duration;
}