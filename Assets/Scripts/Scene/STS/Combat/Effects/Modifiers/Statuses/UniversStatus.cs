public class UniversStatus : StatusEffect
{
    public UniversStatus()
    {
        Name = "Univers";
        Duration = -1;
        buff = true;
        framed = true;
    }

    public override string Desc(bool isPlayer) => "Quand vous jouez une carte sans remplir ses conditions, gagnez 3 d'Armure.";
}