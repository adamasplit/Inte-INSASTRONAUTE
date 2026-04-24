using System.Linq;
using UnityEngine;
public static class EffectResolver
{
    public static void Apply(EffectEntry effect, EffectContext ctx)
    {
        TurnSystem turnSystem = ctx.combat.turnSystem;
        switch (effect.type)
        {
            case EffectType.Damage:
            {
                if (ctx.isPreview)
                    break; // Skip actual damage application during preview
                int dmg = ctx.combat.GetModifiedValue(effect.value, StatType.Damage, ctx);
                ctx.target.TakeDamage(dmg);
                break;
            }
            case EffectType.Strength:
            {
                if (ctx.isPreview)
                    break; // Skip actual status application during preview
                int str = ctx.combat.GetModifiedValue(effect.value, StatType.StatusPotency, ctx);
                int dur = ctx.combat.GetModifiedValue(effect.duration, StatType.StatusDuration, ctx);
                ctx.target.AddStatus(new StrengthStatus(str,dur));
                break;
            }
            case EffectType.Armor:
            {
                if (ctx.isPreview)
                    break; // Skip actual armor application during preview
                ctx.target.AddArmor(effect.value);
                break;
            }
            case EffectType.Heal:
            {
                if (ctx.isPreview)
                    break; // Skip actual healing during preview
                ctx.target.Heal(effect.value);
                break;
            }
            case EffectType.Vulnerability:
            {
                if (ctx.isPreview)
                    break; // Skip actual status application during preview
                ctx.target.AddStatus(new VulnStatus(effect.value));
                break;
            }
            case EffectType.Weakness:
            {
                if (ctx.isPreview)
                    break; // Skip actual status application during preview
                ctx.target.AddStatus(new WeaknessStatus(effect.value));
                break;
            }
            case EffectType.DeleteNextTurn:
            {
                var timeline = ctx.timeline;
                if (ctx.timeline == null || ctx.target == null)
                {
                    Debug.LogWarning("Timeline or target is null in DeleteNextTurn effect.");
                    break;
                }
                var targetEntry = timeline
                    .Where(t => t.character.name == ctx.target.name)
                    .OrderBy(t => t.time)
                    .FirstOrDefault();
                if (targetEntry != null)
                {
                    targetEntry.visualType = TurnVisualType.Removed;
                }
                if (targetEntry != null)
                {
                    if (ctx.isPreview)
                    {
                        targetEntry.visualType = TurnVisualType.Removed;
                    }
                    else
                    {
                        timeline.Remove(targetEntry);

                        timeline.Add(new TurnEntry
                        {
                            character = ctx.target,
                            time = targetEntry.time + ctx.combat.turnSystem.baseDelay
                        });
                    }
                }
                Debug.Log("SIM CONTAINS REMOVED: " +
                ctx.timeline.Any(t => t.visualType == TurnVisualType.Removed));
                break;
            }
            case EffectType.AdvanceTurn:
            {
                if (ctx.isPreview)
                {
                    ctx.timeline = ctx.combat.turnSystem.AdvanceAllTurns(
                        ctx.timeline,
                        ctx.target,
                        effect.value
                    );
                    Debug.Log("SIM CONTAINS ADVANCED: " +
                    ctx.timeline.Any(t => t.visualType == TurnVisualType.Advanced));
                }
                else
                {
                    ctx.combat.turnSystem.ApplyAdvanceAllTurns(
                        ctx.target,
                        effect.value
                    );
                }
                break;
            }
            case EffectType.DelayTurn:
            {
                if (ctx.isPreview)
                {
                    ctx.timeline = ctx.combat.turnSystem.DelayAllTurns(
                        ctx.timeline,
                        ctx.target,
                        effect.value
                    );
                }
                else
                {
                    ctx.combat.turnSystem.ApplyDelayAllTurns(
                        ctx.target,
                        effect.value
                    );
                }
                break;
            }
            default:
                break;
        }
    }
}