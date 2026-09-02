using UnityEngine;

/// <summary>
/// Aveuglé : les prochaines instances de dégâts du porteur sont annulées.
///
/// <para>Sa durée ne compte pas des tours mais des coups annulés, ce qui le distingue
/// d'Étourdissement ou de Divination : Aveuglé (2) annule exactement deux instances de dégâts,
/// même si l'armure de la cible les aurait bloquées de toute façon, puis s'en va. Il ne s'écoule
/// donc pas en fin de tour — un porteur qui n'attaque pas le garde.</para>
/// </summary>
public class BlindStatus : StatusEffect
{
    public BlindStatus(int duration)
    {
        Duration = duration;
        Name = "Aveuglé";
        modifierType = ModifierType.Override;
        debuff = true;
        generic = true;
        inextendable = true;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.source != null && ctx.source.statusEffects.Contains(this);
    }

    public override int Modify(int damage, EffectContext ctx)
    {
        if (!AppliesTo(StatType.Damage, ctx))
            return damage;

        // Une prévisualisation ne consomme rien : elle demande ce que coûterait le coup, elle
        // ne le porte pas. Sans ce garde-fou, survoler une carte userait l'aveuglement.
        if (!ctx.isPreview && damage > 0)
        {
            Duration--;
            if (Duration <= 0)
                mustExpire = true;
        }
        return 0;
    }

    /// <summary>Rien : sa durée ne s'écoule qu'au rythme des coups qu'il annule.</summary>
    public override void OnTurnEnd(Character target)
    {
    }

    public override string Desc(bool isPlayer)
    {
        string hits = Duration > 1 ? $"{Duration} prochaines instances de dégâts" : "prochaine instance de dégâts";
        if (isPlayer)
        {
            return $"Vos {hits} sont annulées.";
        }
        return $"Les {hits} de ce personnage sont annulées.";
    }
}
