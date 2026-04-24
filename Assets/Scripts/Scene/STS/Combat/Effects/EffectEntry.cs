using System;

[Serializable]
public class EffectEntry
{
    public EffectType type;
    public int value;
    public int duration;
    public bool targetSelf=false;
}