/// <summary>
/// Le compteur de combo lui-même est tenu par le serveur (une condition remplie le fait monter,
/// une carte jouée sans remplir la sienne le remet à zéro) ; ici, seule la bonification de dégâts
/// que sa valeur courante donne doit être visible dans les prévisions.
/// </summary>
public class SuiteArithmetiqueStatus : StatusEffect
{
    public SuiteArithmetiqueStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Suite arithmétique";
        modifierType = ModifierType.Multiplicative;
        buff = true;
        framed = true;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.source != null && ctx.source.statusEffects.Contains(this);
    }
    public override int Modify(int damage, EffectContext ctx)
    {
        return damage + (damage * Value / 100);
    }
    public override string Desc(bool isPlayer)
    {
        return $"Vos dégâts infligés sont augmentés de {Value}% (cumulable en remplissant une condition de carte, retombe à 0 si une carte ne remplit pas la sienne).";
    }
}
