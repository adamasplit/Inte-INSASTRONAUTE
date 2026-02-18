using UnityEngine;
public enum Element
{
    Rite,
    Asso,
    Event,
    Bar_boite,
    Liste,
    Personne,
    Galere_spam,
    Fire,
    Water,
    Earth,
    Air,
    Prismatic
}
public enum Effectiveness
{
    Normal,
    Strong,
    Weak
}

public static class ElementCalculator
{
    public static Effectiveness GetEffectiveness(Element card, Element enemy)
    {
        if (card == enemy)
            return Effectiveness.Normal;

        if (IsStrong(card, enemy))
            return Effectiveness.Strong;

        if (IsWeak(card, enemy))
            return Effectiveness.Weak;

        return Effectiveness.Normal;
    }
    public static float GetDamage(Element card, Element enemy, float baseDamage)
    {
        if (card == enemy)
            return baseDamage;

        if (IsStrong(card, enemy))
            return baseDamage * 1.5f;

        if (IsWeak(card, enemy))
            return baseDamage * 0.5f;

        return baseDamage;
    }

    public static Color GetElementColor(Element elem, bool enemy = false)
    {
        var color = elem switch
        {
            Element.Fire => Color.red,
            Element.Water => Color.blue,
            Element.Earth => Color.green,
            Element.Air => (enemy? Color.cyan : new Color(0.2f, 0.6f, 1f)),
            Element.Rite => Color.blue,
            Element.Asso => Color.orange,
            Element.Event => Color.red,
            Element.Bar_boite => Color.yellow,
            Element.Liste => Color.cyan,
            Element.Personne => Color.magenta,
            Element.Galere_spam => Color.grey,
            _ => Color.white,
        };
        return color;
    }
    public static Element GetFirstWeakElement(Element elem)
    {
        return elem switch
        {
            Element.Fire => Element.Water,
            Element.Water => Element.Earth,
            Element.Earth => Element.Air,
            Element.Air => Element.Fire,
            Element.Rite => Element.Asso,
            Element.Asso => Element.Event,
            Element.Event => Element.Bar_boite,
            Element.Bar_boite => Element.Liste,
            Element.Liste => Element.Personne,
            Element.Personne => Element.Galere_spam,
            Element.Galere_spam => Element.Rite,
            Element.Prismatic => Element.Prismatic,
            _ => Element.Fire,
        };
    }

    static bool IsStrong(Element a, Element b)
    {
        return (a == Element.Fire && b == Element.Earth)
            || (a == Element.Water && b == Element.Fire)
            || (a == Element.Earth && b == Element.Air)
            || (a == Element.Air && b == Element.Water)
            || (a == Element.Galere_spam && b == Element.Personne)
            || (a == Element.Personne && b == Element.Liste)
            || (a == Element.Liste && b == Element.Bar_boite)
            || (a == Element.Bar_boite && b == Element.Event)
            || (a == Element.Event && b == Element.Asso)
            || (a == Element.Asso && b == Element.Rite)
            || (a == Element.Rite && b == Element.Galere_spam)
            || (a == Element.Prismatic && b != Element.Prismatic);
    }

    static bool IsWeak(Element a, Element b) => IsStrong(b, a);
}
