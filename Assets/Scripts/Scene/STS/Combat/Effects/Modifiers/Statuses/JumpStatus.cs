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
        modifierType = ModifierType.Override;
    }
    public override string Desc()
    {
        return $"\nPorte une attaque puissante sur un ennemi au début de votre prochain tour. Vous ne pouvez pas subir de dégâts avant cela.";
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
        Character target=character.GetCombatManager().enemies.Find(e => e.statusEffects.Any(s => s is TargetingStatus));
        TargetingStatus targeting = target?.statusEffects.OfType<TargetingStatus>().FirstOrDefault();
        if (targeting != null)
        {
            targeting.mustExpire = true;
        }
        if (target == null)
        {
            target=character.GetCombatManager().enemies[Random.Range(0,character.GetCombatManager().enemies.Count)];
        }
        CardInstance card = new CardInstance(STSCardDatabase.Get("Atterrissage"));
        character.GetCombatManager().PlayCard(character,card,new List<Character>(){target},false,false);
    }
}