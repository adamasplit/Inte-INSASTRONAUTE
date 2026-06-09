using UnityEngine;
public class ElasticStatus : StatusEffect
{
    public ElasticStatus(int value)
    {
        Value  = value;
        Duration = -1;
        Name = "Élasticité";
        buff=true;
        framed=true;
    }
    public override void OnDamageTaken(Character source, Character target, ref int damage)
    {
        target.AddArmor(Value);
    }
    public override string Desc()
    {
        return $"\nGagnez {Value} d'Armure quand vous subissez des dégâts";
    }
}