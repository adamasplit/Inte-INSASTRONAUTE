using System;
using System.Collections.Generic;

[Serializable]
public class STSCardDataDTO
{
    public string id;
    public string collectionCardId;

    public int cost;

    public string type;

    public string rarity;

    public string targetingMode;

    public bool exhaust;

    public bool retain;

    public List<EffectEntryDTO> effects = new();

    public List<ModifierDTO> modifiers = new();
}