using UnityEngine;

/// <summary>
/// Stellaire : la carte repart quatre fois de plus, mais ses effets sont ramenés à 1.
///
/// <para>Ce que le statut donne en nombre de fois, il le reprend sur la taille. Une carte de 12
/// dégâts jouée cinq fois en fait 5 ; une carte qui fait beaucoup de petites choses, en revanche,
/// les fait cinq fois. C'est une carte à effets multiples qu'il récompense, pas une grosse
/// frappe.</para>
///
/// <para>Le pendant en statut de <see cref="AstraEnchantment"/>, dont il reprend exactement les
/// deux modificateurs : un <see cref="OneModifier"/> sur <see cref="StatType.Any"/> et quatre
/// redites de plus. Les mêmes nombres sont donc touchés — la potence comme la durée d'un statut
/// posé — et les mêmes épargnés : le coût, le délai de tour et le compte de redites, que
/// <see cref="StatTypeChecker"/> tient hors de portée d'un modificateur générique.</para>
///
/// <para>Ramené comme le fait <see cref="OneModifier"/>, en pinçant entre -1 et 1 : le signe dit
/// de quel côté va l'effet, et une durée de -1 est l'écriture du permanent, pas un compte.</para>
/// </summary>
public class AstraStatus : StatusEffect
{
    /// <summary>Combien de fois de plus la carte repart.</summary>
    public const int ExtraReplays = 4;

    /// <summary>
    /// La statistique que <see cref="AppliesTo"/> vient d'accepter.
    ///
    /// <para><see cref="Modify"/> ne reçoit pas de quelle statistique il s'agit, et ce statut en
    /// sert deux qui ne se répondent pas de la même façon. <see cref="BattleCalculator"/> appelle
    /// <see cref="AppliesTo"/> juste avant <see cref="Modify"/>, sur une statistique à la fois :
    /// ce que l'un vient d'accepter est donc ce que l'autre s'apprête à changer.</para>
    /// </summary>
    private StatType lastAsked;

    public AstraStatus()
    {
        Name = "Stellaire";
        Duration = -1;
        buff = true;
        framed = true;
        type = StatType.Any;
        modifierType = ModifierType.Override;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        if (ctx == null || ctx.source == null || ctx.source.statusEffects == null
            || !ctx.source.statusEffects.Contains(this))
        {
            return false;
        }

        // Une carte née d'un suivi ne se rejoue pas, comme pour l'Écho.
        if (stat == StatType.ReplayCount && ctx.card != null && ctx.card.HasTag(CardTag.FollowUp))
        {
            return false;
        }

        lastAsked = stat;
        return stat == StatType.ReplayCount || StatTypeChecker.IsValid(stat);
    }

    public override int Modify(int value, EffectContext ctx)
    {
        if (lastAsked == StatType.ReplayCount)
        {
            return value + ExtraReplays;
        }
        return Mathf.Clamp(value, -1, 1);
    }

    public override string Desc(bool isPlayer)
    {
        string who = isPlayer ? "Vos cartes sont jouées" : "Les cartes de ce personnage sont jouées";
        return $"{who} {ExtraReplays} fois de plus, mais la plupart de leurs effets sont réduits à 1.";
    }
}
