using System.Collections;
using System.Collections.Generic;
public class VulnerabilizeStatus : StatusEffect
{
    public VulnerabilizeStatus(int value, int duration)
    {
        Value = value;
        Duration = duration;
        Name = "Vulnérabilisation";
        buff=true;
        framed=true;
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Pendant {Duration} tours, chaque fois que vous infligez des dégâts à un ennemi, appliquez {Value} de Vulnérable.";
        }
        return $"Pendant {Duration} tours, chaque fois que l'ennemi vous inflige des dégâts, vous subissez {Value} de Vulnérable.";
    }
    public override void OnDamageDealt(Character source, Character target, ref int damage)
    {
        target.AddStatus(new VulnStatus(Value));
    }
}