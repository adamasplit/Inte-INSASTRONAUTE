public class InstabilityStatus : StatusEffect
{
    public InstabilityStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Instabilité";
        debuff = true;
        generic=true;
    }

    public override string Desc(bool isPlayer)
    {
        return $"Après {Value} autres effets de statut, la cible subit 5 dégâts puis l'Instabilité disparaît.";
    }
}
