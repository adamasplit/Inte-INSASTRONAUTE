using System.Collections;
using System.Collections.Generic;
public class FatigueStatus : StatusEffect
{
    public FatigueStatus()
    {
        Value = 1;
        Duration = -1;
        Name = "Fatigue";
        generic=true;
        debuff=true;
        framed=true;
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Vous perdez {Value} de Vitesse quand vous infligez des dégâts";
        }
        return $"L'ennemi perd {Value} de Vitesse quand il vous inflige des dégâts";
    }
    public override void OnDamageDealt(Character source, Character target, ref int damage)
    {
        if (damage > 0)
        {
            source.AddStatus(new SpeedStatus(-Value));
        }
    }
}