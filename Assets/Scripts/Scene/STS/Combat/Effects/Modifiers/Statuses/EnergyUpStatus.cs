using UnityEngine;
public class EnergyUpStatus : StatusEffect
{
    public EnergyUpStatus(int value, int duration)
    {
        Value = value;
        Duration = duration;
        Name = "Énergie+";
        modifierType = ModifierType.Additive;
        buff=true;
    }
    public override void OnTurnStart(Character character)
    {
        Duration--;
    }
    public override void OnTurnEnd(Character character)
    {
    }
    public override void OnExpire(Character character)
    {
        character.resources.energy += Value;
    }
}