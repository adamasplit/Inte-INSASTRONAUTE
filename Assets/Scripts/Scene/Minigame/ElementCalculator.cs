using UnityEngine;
public enum Element
{
    Planet,
    Rocket,
    Star,
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
            Element.Planet => Color.blue,
            Element.Rocket => Color.red,
            Element.Star => Color.yellow,
            Element.Prismatic => Color.white,
            _ => Color.white,
        };
        return color;
    }
    public static Element GetFirstWeakElement(Element elem)
    {
            return elem switch
        {            Element.Planet => Element.Rocket,
            Element.Rocket => Element.Star,
            Element.Star => Element.Planet,
            Element.Prismatic => Element.Prismatic,
            _ => Element.Planet,
        };
    }

    static bool IsStrong(Element a, Element b)
    {
        return (a == Element.Planet && b == Element.Rocket) ||
               (a == Element.Rocket && b == Element.Star) ||
               (a == Element.Star && b == Element.Planet)||
               (a == Element.Prismatic && b != Element.Prismatic);
    }

    static bool IsWeak(Element a, Element b) => IsStrong(b, a);
}
