public class FullMoonStatus : StatusEffect
{
    public FullMoonStatus()
    {
        Value = 1;
        Duration = -1;
        Name = "Pleine lune";
        buff=true;
    }
    public override string Describe()
    {
        return "Donne {Value} de Force en infligeant des dégâts à un ennemi";
    }
}