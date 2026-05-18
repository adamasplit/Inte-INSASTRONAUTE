using System.Linq;
using UnityEngine;
using System.Collections.Generic;
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
                DamageInfo info=new DamageInfo();
                if (ctx!=null&&ctx.card!=null&&ctx.card.enchantments.Exists(e=>e.data.name=="Humanisme"))
                {
                    info=ctx.target.TakeDamage(dmg,true);
                }
                else
                {
                    info=ctx.target.TakeDamage(dmg);
                }
                if (ctx.source != null)
                    {
                        ctx.source.OnDamageDealt(ctx.target, dmg,info.unblocked);
                        ctx.target.OnDamageTaken(ctx.source, dmg,info.unblocked);
                    }
                if (info.armorBroken && ctx.source != null)
                    {
                        ctx.source.OnTargetArmorBroken(ctx.target);
                        ctx.target.OnOwnArmorBroken(ctx.source);
                    }
                if (ctx.card!=null&&ctx.card.enchantments.Exists(e=>e.data.name=="Lifesteal"))
                {
                    CardEnchantment e=ctx.card.enchantments.Find(en=>en.data.name=="Lifesteal");
                    int healAmount=dmg*((LifestealEnchantment)e.data).healPercent(e.level)/100;
                    ctx.source.Heal(healAmount);
                }
                break;
            }
            case EffectType.Multihit:
            {
                for(int i=0;i<effect.duration;i++)
                    {
                        Apply(new EffectEntry
                        {
                            type = EffectType.Damage,
                            value = effect.value,
                            statusType=effect.statusType,
                            duration=effect.duration,
                            targetSelf=effect.targetSelf
                        }, ctx);
                    }
                break;
            }
            case EffectType.Armor:
            {
                if (ctx.isPreview)
                    break; // Skip actual armor application during preview
                int armor = BattleCalculator.GetModifiedValue(effect.value, StatType.Armor, ctx);
                ctx.target.AddArmor(armor);
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
                    .Where(t => t.character.name == ctx.target.name&& t.time > 0)
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
                            uid = TurnEntry.nextUID++
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
            case EffectType.Draw:
            {
                if (ctx.isPreview)
                    break;
                for (int i = 0; i < effect.value; i++)
                {
                    ctx.source.DrawCard();
                }
                break;
            }
            case EffectType.LoseHP:
            {
                if (ctx.isPreview)
                    break;
                ctx.source.TakeDamage(effect.value);
                break;
            }
            case EffectType.GainEnergy:
            {
                if (ctx.isPreview)
                    break;
                ctx.source.GainEnergy(effect.value);
                break;
            }
            case EffectType.AddCardToHand:
            {
                if (ctx.isPreview)
                    break;
                STSCardData cardToAdd = STSCardDatabase.Get(effect.cardID);
                if (cardToAdd != null)
                {
                    ctx.source.GetCombatManager().deck.hand.Add(new CardInstance(cardToAdd));
                }
                break;
            }
            case EffectType.StealBuff:
            {
                if (ctx.isPreview)
                    break;
                List<StatusEffect> buffsToSteal = ctx.target.statusEffects.Where(s => s.buff).ToList();
                for (int i = 0; i < effect.value && buffsToSteal.Count > 0; i++)
                {
                    StatusEffect buff = buffsToSteal[0];
                    buffsToSteal.RemoveAt(0);
                    ctx.target.RemoveStatus(buff);
                    ctx.source.AddStatus(buff);
                }
                break;
            }
            case EffectType.TransferDebuff:
            {
                if (ctx.isPreview)
                    break;
                List<StatusEffect> debuffsToTransfer = ctx.source.statusEffects.Where(s => !s.buff).ToList();
                for (int i = 0; i < effect.value && debuffsToTransfer.Count > 0; i++)
                {
                    StatusEffect debuff = debuffsToTransfer[0];
                    debuffsToTransfer.RemoveAt(0);
                    ctx.source.RemoveStatus(debuff);
                    ctx.target.AddStatus(debuff);
                }
                break;
            }
            case EffectType.DispelBuff:
            {
                if (ctx.isPreview)
                    break;
                List<StatusEffect> buffsToDispel = ctx.target.statusEffects.Where(s => s.buff).ToList();
                for (int i = 0; i < effect.value && buffsToDispel.Count > 0; i++)
                {
                    StatusEffect buff = buffsToDispel[0];
                    buffsToDispel.RemoveAt(0);
                    ctx.target.RemoveStatus(buff);
                }
                break;
            }
            case EffectType.DispelDebuff:
            {
                if (ctx.isPreview)
                    break;
                List<StatusEffect> debuffsToDispel = ctx.target.statusEffects.Where(s => !s.buff).ToList();
                for (int i = 0; i < effect.value && debuffsToDispel.Count > 0; i++)
                {
                    StatusEffect debuff = debuffsToDispel[0];
                    debuffsToDispel.RemoveAt(0);
                    ctx.target.RemoveStatus(debuff);
                }
                break;
            }
            case EffectType.EndTurn:
            {
                if (ctx.isPreview)
                    break;
                if (ctx.source != null && ctx.source.isPlayer)
                {
                    ctx.combat.turnSystem.PlayerEndTurn();
                }
                break;
            }
            default:
                break;
        }
    }
}