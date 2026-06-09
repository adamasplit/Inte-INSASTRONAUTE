using UnityEngine;
public class SelectableCharacterUtils
{
    public static Color getCharacterColor(SelectableCharacter character)
    {
        switch (character)
        {
            case SelectableCharacter.EP: //Pink
                return new Color(1f, 0.75f, 0.8f);
            case SelectableCharacter.MECA: //Yellow
                return new Color(1f, 1f, 0.5f);
            case SelectableCharacter.GM: //Dark blue
                return new Color(0.2f, 0.2f, 0.8f);
            case SelectableCharacter.ITI: //Light blue
                return new Color(0.5f, 0.8f, 1f);
            case SelectableCharacter.CFI: //Red
                return new Color(1f, 0.5f, 0.5f);
            case SelectableCharacter.MRIE: //Green
                return new Color(0.5f, 1f, 0.5f);
            case SelectableCharacter.GC: //Orange
                return new Color(1f, 0.6f, 0.3f);
            case SelectableCharacter.AI: //Orange
                return new Color(1f, 0.6f, 0.3f);
            case SelectableCharacter.PERF: //Gray
                return new Color(0.5f, 0.5f, 0.5f);
            case SelectableCharacter.Aucun: //White
                return Color.white;
            case SelectableCharacter.Impossible: //Dark gray
                return new Color(0.3f, 0.3f, 0.3f);
            case SelectableCharacter.Starting: //Light gray
                return new Color(0.8f, 0.8f, 0.8f);
            default:
                return Color.white;
        }
    }
}