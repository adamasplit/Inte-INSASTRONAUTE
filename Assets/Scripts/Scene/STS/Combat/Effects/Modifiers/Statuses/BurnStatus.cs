using UnityEngine;
public class BurnStatus : StatusEffect
{
    public BurnStatus(int duration)
    {
        Duration = duration;
        Name = "Brûlure";
        modifierType = ModifierType.Additive;
        debuff=true;
        generic=true;
    }
    public override void OnTurnEnd(Character target)
    {
        base.OnTurnEnd(target);
        target.TakeDamage(Duration);
        VFXManager.Instance.PlayEffect("Burn", target);
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Inflige {Duration} dégâts à la fin de votre tour";
        }
        return $"Inflige {Duration} dégâts à la fin du tour de la cible";
    }
}