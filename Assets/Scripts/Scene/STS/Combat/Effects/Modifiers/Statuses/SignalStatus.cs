using System.Linq;
using System.Collections.Generic;
public class SignalStatus : StatusEffect
{
    /// <param name="duration">ignore : Signal vient d'une carte de Puissance et ne s'ecoule pas</param>
    public SignalStatus(int duration)
    {
        Duration = -1;
        Name = "Signal";
        buff = true;
        framed = true;
        modifierType = ModifierType.Additive;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.source != null && ctx.source.statusEffects.Contains(this);
    }
    public override int Modify(int damage, EffectContext ctx)
    {
        List<Character> targets = (ctx.targets != null && ctx.targets.Count > 0) ? ctx.targets : new List<Character> { ctx.target };
        int distinctDebuffs = targets.Where(c => c != null)
            .SelectMany(c => c.statusEffects)
            .Where(s => s.debuff)
            .Select(s => s.GetType())
            .Distinct()
            .Count();
        return damage + distinctDebuffs / 2;
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return "Infligez 1 dégât supplémentaire pour chaque groupe de 2 debuffs distincts présents sur les cibles.";
        }
        return "Le personnage inflige 1 dégât supplémentaire pour chaque groupe de 2 debuffs distincts présents sur les cibles.";
    }
}
