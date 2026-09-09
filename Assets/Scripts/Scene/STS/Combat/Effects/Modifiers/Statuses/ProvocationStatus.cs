public class ProvocationStatus : StatusEffect
{
    public ProvocationStatus(int value)
    {
        Name = "Provocation";
        Value = value;
        Duration = -1;
        debuff = true;
        framed = true;
        generic=true;
    }

    public override string Desc(bool isPlayer)
    {
        return $"Ce personnage sera ciblé par les {Value} prochaines attaques à cible unique de n'importe quel personnage.";
    }
}