using System.Linq;
using UnityEngine;
public static class EffectResolver
{
    public static void Apply(EffectEntry effect, EffectContext ctx)
    {
        TurnSystem turnSystem = ctx.combat!=null?ctx.combat.turnSystem:null;
        switch (effect.type)
        {
            case EffectType.Damage:
            {
                if (ctx.isPreview)
                    break; // Skip actual damage application during preview
                int dmg = BattleCalculator.GetModifiedValue(effect.value, StatType.Damage, ctx);
                if (ctx!=null&&ctx.card!=null&&ctx.card.enchantments.Exists(e=>e.data.name=="Humanisme"))
                {
                    dmg=ctx.target.TakeDamage(dmg,true);
                }
                else
                {
                    dmg=ctx.target.TakeDamage(dmg);
                }
                break;
            }
            case EffectType.Multihit:
            {
                if (ctx.isPreview)
                    break; // Skip actual damage application during preview
                int dmg = BattleCalculator.GetModifiedValue(effect.value, StatType.Damage, ctx);
                for (int i = 0; i < effect.duration; i++)
                {
                    if (ctx!=null&&ctx.card!=null&&ctx.card.enchantments.Exists(e=>e.data.name=="Humanisme"))
                    {
                        dmg=ctx.target.TakeDamage(dmg,true);
                    }
                    else
                    {
                        dmg=ctx.target.TakeDamage(dmg);
                    }
                }
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
            case EffectType.Status:
            {
                if (ctx.isPreview)
                    break;
                int val = BattleCalculator.GetModifiedValue(effect.value, StatType.StatusPotency, ctx);
                int dur = BattleCalculator.GetModifiedValue(effect.duration, StatType.StatusDuration, ctx);
                StatusEffect stat=StatusEffect.Factory(effect.statusType,val,dur);
                ctx.target.AddStatus(stat);
                break;
            }
            case EffectType.DeleteNextTurn:
            {
                var timeline = ctx.timeline;
                if (ctx.timeline == null || ctx.target == null)
                {
                    break;
                }
                var targetEntry = timeline
                    .Where(t => t.character.name == ctx.target.name)
                    .OrderBy(t => t.time)
                    .FirstOrDefault();
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
                            time = targetEntry.time + ctx.target.turnDelay(turnSystem.baseDelay), // Schedule for the next turn
                        });
                    }
                }
                break;
            }
            case EffectType.AdvanceTurn:
            {
                if (ctx.isPreview)
                {
                    ctx.timeline = turnSystem.AdvanceAllTurns(
                        ctx.timeline,
                        ctx.target,
                        effect.value
                    );
                }
                else
                {
                    turnSystem.ApplyAdvanceAllTurns(
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
                    ctx.timeline = turnSystem.DelayAllTurns(
                        ctx.timeline,
                        ctx.target,
                        effect.value
                    );
                }
                else
                {
                    turnSystem.ApplyDelayAllTurns(
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