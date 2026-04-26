[System.Serializable]
public class ModifierData
{
    public StatType type;
    public ModifierKind kind;
    public int value;
    public StatModifier CreateModifier()
    {
        switch (kind)
        {
            case ModifierKind.Flat:
                return new FlatModifier(type, value);
            case ModifierKind.Discard:
                return new DiscardModifier(type, value);
            case ModifierKind.Played:
                return new PlayedModifier(type, value);
            default:
                throw new System.Exception("Unknown modifier kind: " + kind);
        }
    }
}