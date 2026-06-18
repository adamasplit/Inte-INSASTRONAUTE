using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class JumpStatus:StatusEffect
{
    public JumpStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Saut";
        buff=true;
        framed=true;
        goldFrame=true;
        modifierType = ModifierType.Override;
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Portez une attaque puissante sur un ennemi au début de votre prochain tour. Vous ne pouvez pas subir de dégâts avant cela.";
        }
        return $"L'ennemi portera une attaque puissante sur vous au début de son prochain tour. Il ne peut pas subir de dégâts avant cela.";
    }
    public override void OnTurnEnd(Character character)
    {
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.target!=null && ctx.target.statusEffects.Contains(this);
    }
    public override int Modify(int damage, EffectContext ctx)
    {
        return 0;
    }
    public override void OnTurnStart(Character character)
    {
        mustExpire = true; 
        Character target=character.GetCombatManager().GetAdversaries(character).Find(e => e.statusEffects.Any(s => s is TargetingStatus));
        TargetingStatus targeting = target?.statusEffects.OfType<TargetingStatus>().FirstOrDefault();
        if (targeting != null)
        {
            targeting.mustExpire = true;
        }
        if (target == null)
        {
            target=character.GetCombatManager().GetAdversaries(character)[Random.Range(0,character.GetCombatManager().GetAdversaries(character).Count)];
        }
        CardInstance card = new CardInstance(STSCardDatabase.Get("Atterrissage"));
        character.GetCombatManager().PlayCard(character,card,character.GetCombatManager().AutoCardTargets(card.targetingMode,character,target),false,true);
    }
}