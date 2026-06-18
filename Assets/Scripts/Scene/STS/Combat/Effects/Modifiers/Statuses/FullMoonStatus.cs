using System.Collections;
using System.Collections.Generic;
public class FullMoonStatus : StatusEffect
{
    public FullMoonStatus()
    {
        Value = 1;
        Duration = -1;
        Name = "Pleine lune";
        buff=true;
        framed=true;
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Donne {Value} de Force temporaire en infligeant des dégâts à un ennemi";
        }
        return $"L'ennemi gagne {Value} de Force temporaire quand il vous inflige des dégâts";
    }
    public override void OnDamageDealt(Character source, Character target, ref int damage)
    {
        if (damage > 0)
        {
            source.AddStatus(new StrengthStatus(Value));
            source.AddStatus(new FullMoonTempEffect(Value));
        }
    }
}