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
                        ctx.state.armorBroken=true;
                    }
                if (info.killingBlow && ctx.source != null)
                    {
                        //ctx.source.OnTargetKilled(ctx.target);
                        //ctx.target.OnKilled(ctx.source);
                        ctx.state.killingBlow=true;
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
                    .Where(t => t.character.name == ctx.target.name&& t!=timeline.First()) // Exclude the current turn entry
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
                        //timeline.Remove(targetEntry);
                        //timeline.Add(new TurnEntry
                        //{
                        //    character = ctx.target,
                        //    time = targetEntry.time + ctx.target.turnDelay(turnSystem.baseDelay), // Schedule for the next turn
                        //    uid = TurnEntry.nextUID++
                        //});
                        turnSystem.ApplyDelayAllTurns(ctx.target, ctx.target.turnDelay(turnSystem.baseDelay));
                    }
                }
                break;
            }
            case EffectType.AdvanceTurn:
            {
                int advanceAmount = BattleCalculator.GetModifiedValue(effect.value, StatType.TurnManipulationAdvance, ctx);
                if (ctx.isPreview)
                {
                    ctx.timeline = turnSystem.AdvanceAllTurns(
                        ctx.timeline,
                        ctx.target,
                        advanceAmount
                    );
                }
                else
                {
                    turnSystem.ApplyAdvanceAllTurns(
                        ctx.target,
                        advanceAmount
                    );
                }
                break;
            }
            case EffectType.DelayTurn:
            {
                int delayAmount = BattleCalculator.GetModifiedValue(effect.value, StatType.TurnManipulationDelay, ctx);
                if (ctx.isPreview)
                {
                    ctx.timeline = turnSystem.DelayAllTurns(
                        ctx.timeline,
                        ctx.target,
                        delayAmount
                    );
                }
                else
                {
                    turnSystem.ApplyDelayAllTurns(
                        ctx.target,
                        delayAmount
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
            case EffectType.Discard:
            {
                if (ctx.isPreview)
                    break;
                for (int i = 0; i < effect.value; i++)
                {
                    ctx.source.DiscardCard();
                }
                break;
            }
            case EffectType.LoseHP:
            {
                if (ctx.isPreview)
                    break;
                ctx.source.TakeDamage((effect.value== -1 ? ctx.source.maxHP : effect.value),true);
                ctx.source.OnDamageTaken(null, (effect.value== -1 ? ctx.source.maxHP : effect.value),true);
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
                ctx.source.GetCombatManager().deck.AddCardToHand(effect.cardID);
                break;
            }
            case EffectType.StealBuff: // Steal non-framed buffs from target and give them to source
            {
                if (ctx.isPreview)
                    break;
                List<StatusEffect> buffsToSteal = ctx.target.statusEffects.Where(s => s.buff && (!s.framed||effect.trueEffect)).ToList();
                for (int i = 0; (i < effect.value || effect.value == -1) && buffsToSteal.Count > 0; i++)
                {
                    StatusEffect buff = buffsToSteal[0];
                    buffsToSteal.RemoveAt(0);
                    ctx.target.RemoveStatus(buff);
                    ctx.source.AddStatus(buff);
                }
                break;
            }
            case EffectType.TransferDebuff: // Transfer non-framed debuffs from source to target
            {
                if (ctx.isPreview)
                    break;
                List<StatusEffect> debuffsToTransfer = ctx.source.statusEffects.Where(s => !s.buff && (!s.framed||effect.trueEffect)).ToList();
                for (int i = 0; (i < effect.value || effect.value == -1) && debuffsToTransfer.Count > 0; i++)
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
                List<StatusEffect> buffsToDispel = ctx.target.statusEffects.Where(s => s.buff && (!s.framed||effect.trueEffect)).ToList();
                for (int i = 0; (i < effect.value || effect.value == -1) && buffsToDispel.Count > 0; i++)
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
                List<StatusEffect> debuffsToDispel = ctx.target.statusEffects.Where(s => !s.buff && (!s.framed||effect.trueEffect)).ToList();
                for (int i = 0; (i < effect.value || effect.value == -1) && debuffsToDispel.Count > 0; i++)
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
            case EffectType.Gravity:
            {
                if (ctx.isPreview)
                    break;
                // Reduce target's HP by the value% of their current HP, ignoring armor
                int dmg = ctx.target.currentHP * effect.value / 100;
                ctx.target.TakeDamage(dmg, true);
                 if (ctx.source != null)
                    {
                        ctx.source.OnDamageDealt(ctx.target, dmg);
                        ctx.target.OnDamageTaken(ctx.source, dmg);
                    }
                break;
            }
            case EffectType.Break:
                {
                    if (ctx.isPreview||ctx.target.armor <= 0)
                        break;
                    //Break the target's armor, ignoring resistances
                    ctx.target.armor = 0;
                    if (ctx.source != null) ctx.source.OnTargetArmorBroken(ctx.target);
                    ctx.target.OnOwnArmorBroken(ctx.source);
                    ctx.state.armorBroken=true;
                    break;
                }
            default:
                break;
        }
    }
    public static bool VerifyCondition(ConditionType type, string strValue, EffectContext ctx)
    {
        Debug.Log($"Vérification de la condition {type} avec la valeur {strValue}");
        switch (type)
        {
            case ConditionType.KillingBlow:
                return ctx.state.killingBlow;
            case ConditionType.ArmorBreak:
                return ctx.state.armorBroken;
            default:
                return false;
        }
    }
}