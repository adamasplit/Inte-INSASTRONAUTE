public class InfaillibleStatus : StatusEffect
{
    public InfaillibleStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Infaillible";
        buff = true;
        generic = true;
    }

    public override string Desc(bool isPlayer)
    {
        return Value == 1
            ? "Votre prochaine condition de carte est garantie."
            : $"Vos {Value} prochaines conditions de carte sont garanties.";
    }
}
