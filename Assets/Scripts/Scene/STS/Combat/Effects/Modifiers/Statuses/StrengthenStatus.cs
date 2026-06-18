public class StrengthenStatus : StatusEffect
{
    public StrengthenStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Renforcement";
        modifierType = ModifierType.Additive;
        buff=true;
        framed=true;
    }
    public override string Desc(bool isPlayer)
    {
        return $"Donne {Value} de Force à chaque fin de tour";
    }
    public override void OnTurnEnd(Character character)
    {
        character.AddStatus(new StrengthStatus(Value));
    }
}