using UnityEngine;
public class PoisonStatus : StatusEffect
{
    public PoisonStatus(int duration)
    {
        Duration = duration;
        Name = "Poison";
        modifierType = ModifierType.Additive;
        debuff=true;
    }
    public override void OnTurnStart(Character target)
    {
        target.TakeDamage(Duration);
    }
    public override string Desc()
    {
        return $"\nInflige {Duration} dégâts au début de chaque tour";
    }
}