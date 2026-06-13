using System;
using System.Collections.Generic;
[Serializable]
public class EffectEntryDTO
{
    public string type;

    public int value;

    public bool targetSelf;
    public bool targetOthers;
    public string statusType;

    public int duration;

    public string description;
    public string cardID;
    public bool conditional;
    public string conditionType;
    public string conditionValue;
    public bool trueEffect;
    public string cardSelectionSource;
    public List<string> cardFilterTags;
    public string cardSelectionEffect;
}