using System.Collections.Generic;
public class CardInstance
{
    public STSCardData data;
    public List<StatModifier> baseModifiers = new();
    public List<StatModifier> addedModifiers = new();

    public CardInstance(STSCardData data)
    {
        this.data = data;
        if (data.modifiers != null)
        {
            foreach (var modData in data.modifiers)
            {
                baseModifiers.Add(modData.CreateModifier());
            }
        }
    }

    public string GetDescription(EffectContext ctx)
    {
        string text = "";
        foreach (var e in data.effects)
        {
            text += EffectDescription.Get(e,ctx) + "\n";
        }
        foreach (var mod in GetModifiers(StatType.Damage))
        {
            text += $"<color=red>{mod.Describe()}</color>\n";
        }
        if (data.exhaust)
            text += "<color=orange>[Épuisement]</color>\n";
        return text.TrimEnd();
    }
    public List<StatModifier> GetModifiers(StatType type)
    {
        List<StatModifier> mods = new();
        mods.AddRange(baseModifiers.FindAll(m => m.type == type));
        mods.AddRange(addedModifiers.FindAll(m => m.type == type));
        return mods;
    }
}