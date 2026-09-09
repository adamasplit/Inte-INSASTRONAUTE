public class AvenirStatus : StatusEffect
{
    public AvenirStatus()
    {
        Name = "Avenir";
        Duration = -1;
        buff = true;
        framed = true;
    }

    public override string Desc(bool isPlayer) => "Au début de votre tour, gagnez une Armure égale au quart des dégâts infligés au tour précédent.";
}