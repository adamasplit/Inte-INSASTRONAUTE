using UnityEngine;
public enum Element
{
    Fire,
    Water,
    Earth,
    Air
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

    public static Color GetElementColor(Element elem)
    {
        return elem switch
        {
            Element.Fire => Color.red,
            Element.Water => new Color(0f, 0f, 1f),
            Element.Earth => Color.green,
            Element.Air => new Color(0.2f, 0.6f, 1f),
            _ => Color.white,
        };
    }

    static bool IsStrong(Element a, Element b)
    {
        return (a == Element.Fire && b == Element.Earth)
            || (a == Element.Water && b == Element.Fire)
            || (a == Element.Earth && b == Element.Air)
            || (a == Element.Air && b == Element.Water);
    }

    static bool IsWeak(Element a, Element b) => IsStrong(b, a);
}
