using UnityEngine;
public class PoisonStatus : StatusEffect
{
    public PoisonStatus(int duration)
    {
        Duration = duration;
        Name = "Poison";
        modifierType = ModifierType.Additive;
        debuff=true;
        generic=true;
    }
    public override void OnTurnStart(Character target)
    {
        target.TakeDamage(Duration);
        Tick(target);
    }
    public override void OnTurnEnd(Character target)
    {
    }
    public override string Desc()
    {
        return $"Inflige {Duration} dégâts au début de chaque tour";
    }
}