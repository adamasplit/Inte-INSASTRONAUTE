public class FragmentationStatus : StatusEffect
{
    public FragmentationStatus()
    {
        Name = "Fragmentation";
        Duration = -1;
        debuff = true;
        framed = true;
    }

    public override string Desc(bool isPlayer) => "Quand Cristallisation s'active, tous les adversaires de la cible gagnent 5 d'Armure.";
}