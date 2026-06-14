using UnityEngine;
public class DivinationStatus : StatusEffect
{

    public DivinationStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Divination";
        buff=true;
        framed=true;
    }
    public override string Desc()
    {
        return $"Annule les dégâts d'une attaque ennemie. Se déclenche {Value} fois.";
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage &&ctx.target!=null&& ctx.target.statusEffects.Contains(this) && ctx.source != ctx.target;
    }
    public override int Modify(int damage, EffectContext ctx)
    {
        Debug.Log($"activation pour {ctx.card.data.cardName}");
        if (!ctx.isPreview) Value--;
        if (Value <= 0)
        {
            mustExpire=true;
        }
        return 0;
    }
}