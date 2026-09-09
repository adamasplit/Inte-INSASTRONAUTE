public class PropulsionStatus : StatusEffect
{
    public PropulsionStatus()
    {
        Name = "Propulsion";
        Duration = 1;
        buff = true;
        framed = true;
        goldFrame = true;
    }

    public override string Desc(bool isPlayer) => "Après le tour de n'importe quel personnage, lancez Roquette sur un ennemi aléatoire. Vous ne pouvez pas subir de dégâts avant votre prochain tour.";
}