using UnityEngine;
using System.Collections.Generic;
public class SadismIIIStatus:StatusEffect
{
    public SadismIIIStatus()
    {
        Duration = -1;
        Name = "Prêt à annihiler";
        debuff=false;
        buff=true;
        framed=true;
        modifierType = ModifierType.Multiplicative;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return ((stat == StatType.Damage||stat==StatType.Armor) && ctx.source.statusEffects.Contains(this));
    }
    public override int Modify(int damage, EffectContext ctx)
    {
        return Mathf.FloorToInt(damage + (damage * 30) / 100);
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"+{30}% dégâts supplémentaires subis et +{30}% d'Armure gagnée. Lorsque vous attaquez un ennemi qui a moins de 50% de sa vie, déclenchez une attaque supplémentaire.";
        }
        return $"+{30}% dégâts supplémentaires subis et +{30}% d'Armure gagnée. Lorsque qu'un adversaire qui a moins de 50% de sa vie est ciblé, cela déclenche une attaque supplémentaire.";
    }
    public override void OnCardPlayed(Character source,Character target,CardInstance card)
    {
        if (card.data.type == CardType.Attaque&&!card.HasTag(CardTag.FollowUp) && target.currentHP <= target.maxHP * 0.5f)
        {
            CardInstance crystalCard = new CardInstance(STSCardDatabase.Get("Acharnement"));
            VFXManager.Instance.PlayEffect("Sadism", target);
            source.GetCombatManager().PlayCard(source,crystalCard,source.GetCombatManager().AutoCardTargets(crystalCard.targetingMode,source,target),false,true);
        }
    }
}