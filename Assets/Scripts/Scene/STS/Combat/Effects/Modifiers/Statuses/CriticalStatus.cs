using UnityEngine;

/// <summary>
/// Critique : les prochaines attaques du porteur frappent deux fois plus fort.
///
/// <para>Une charge par carte qui a effectivement blessé quelqu'un, et non par coup : une carte
/// qui touche trois cibles est une attaque, pas trois. Une carte qui n'a blessé personne n'en
/// coûte aucune — on ne paie que ce qui a servi, exactement comme la Vigueur.</para>
///
/// <para>Les charges se dépensent dans le moteur autoritatif, qui seul voit ce que la carte a
/// fait de chacune de ses cibles. Ce qui est fait ici est le doublement lui-même, pour que la
/// carte survolée annonce le chiffre qu'elle va porter : une prévisualisation ne consomme rien,
/// et ce modificateur ne touche donc jamais à <see cref="StatusEffect.Value"/>.</para>
/// </summary>
public class CriticalStatus : StatusEffect
{
    public CriticalStatus(int value)
    {
        Name = "Critique";
        Value = value;
        Duration = -1;
        buff = true;
        framed = true;
        type = StatType.Damage;
        modifierType = ModifierType.Multiplicative;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage
            && Value > 0
            && ctx != null
            && ctx.source != null
            && ctx.source.statusEffects != null
            && ctx.source.statusEffects.Contains(this);
    }

    public override int Modify(int damage, EffectContext ctx)
    {
        if (!AppliesTo(StatType.Damage, ctx))
        {
            return damage;
        }
        return damage * 2;
    }

    public override string Desc(bool isPlayer)
    {
        string attacks = $"{Value} prochaine" + (Value > 1 ? "s attaques" : " attaque");
        if (isPlayer)
        {
            return $"Les dégâts de vos {attacks} sont doublés.";
        }
        return $"Les dégâts des {attacks} de ce personnage sont doublés.";
    }
}
