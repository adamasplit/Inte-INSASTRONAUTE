public class MomentumStatus : StatusEffect
{
    public MomentumStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Élan";
        buff = true;
        framed = true;
    }

    public override string Desc(bool isPlayer)
    {
        return $"Les cartes non-Attaque donnent 1 d'Élan. Votre prochaine Attaque inflige {Value} dégâts supplémentaires et consomme cet Élan.";
    }
}
