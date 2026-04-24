using System.Collections.Generic;
public class CardInstance
{
    public STSCardData data;
    public List<StatModifier> modifiers = new();

    public CardInstance(STSCardData data)
    {
        this.data = data;
    }

    public string GetDescription(EffectContext ctx)
    {
        string text = "";
        foreach (var e in data.effects)
        {
            text += EffectDescription.Get(e,ctx) + "\n";
        }
        return text.TrimEnd();
    }
}