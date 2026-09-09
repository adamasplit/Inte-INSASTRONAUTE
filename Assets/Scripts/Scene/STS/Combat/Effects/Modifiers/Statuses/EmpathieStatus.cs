public class EmpathieStatus : StatusEffect
{
    public EmpathieStatus()
    {
        Name = "Empathie";
        Duration = -1;
        buff = true;
        framed = true;
    }

    public override string Desc(bool isPlayer) => "Quand vous vous infligez des dégâts, vos adversaires en subissent la moitié.";
}