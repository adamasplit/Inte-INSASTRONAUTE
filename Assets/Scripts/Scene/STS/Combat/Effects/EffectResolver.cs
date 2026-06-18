using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public static class EffectResolver
{
    public static void Preview(EffectEntry effect, EffectContext ctx)
    {
        TurnSystem turnSystem = ctx.combat!=null?ctx.combat.turnSystem:null;
        switch (effect.type)
        {
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
        }
    }
    public static IEnumerator Apply(EffectEntry effect, EffectContext ctx)
    {
        UIManager ui = null;
        if (ctx.combat != null)
        {
            ui = ctx.combat.ui;
        }
        if (ctx.isPreview)
        {
            yield break;
        }
        TurnSystem turnSystem = ctx.combat!=null?ctx.combat.turnSystem:null;
        switch (effect.type)
        {
            case EffectType.Damage:
            {
                if (ctx.isPreview)
                    yield break; // Skip actual damage application during preview
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
                yield break;
            }
            case EffectType.Multihit:
            {
                for(int i=0;i<effect.duration;i++)
                    {
                        yield return Apply(new EffectEntry
                        {
                            type = EffectType.Damage,
                            value = effect.value,
                            statusType=effect.statusType,
                            duration=effect.duration,
                            targetSelf=effect.targetSelf
                        }, ctx);
                    }
                yield break;
            }
            case EffectType.Armor:
            {
                if (ctx.isPreview)
                    yield break; // Skip actual armor application during preview
                int armor = BattleCalculator.GetModifiedValue(effect.value, StatType.Armor, ctx);
                ctx.target.AddArmor(armor);
                yield break;
            }
            case EffectType.Heal:
            {
                if (ctx.isPreview)
                    yield break; // Skip actual healing during preview
                ctx.target.Heal(effect.value);
                yield break;
            }
            case EffectType.Status:
            {
                if (ctx.isPreview)
                    yield break;
                int val = BattleCalculator.GetModifiedValue(effect.value, StatType.StatusPotency, ctx);
                int dur = BattleCalculator.GetModifiedValue(effect.duration, StatType.StatusDuration, ctx);
                StatusEffect stat=StatusEffect.Factory(effect.statusType,val,dur,effect.cardID);
                ctx.target.AddStatus(stat);
                yield break;
            }
            case EffectType.DeleteNextTurn:
            {
                var timeline = ctx.timeline;
                if (ctx.timeline == null || ctx.target == null)
                {
                    yield break;
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
                yield break;
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
                yield break;
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
                yield break;
            }
            case EffectType.Draw:
            {
                if (ctx.isPreview)
                    yield break;
                for (int i = 0; i < effect.value; i++)
                {
                    ctx.source.DrawCard();
                }
                yield break;
            }
            case EffectType.Discard:
            {
                if (ctx.isPreview)
                    yield break;
                for (int i = 0; i < effect.value; i++)
                {
                    ctx.source.DiscardCard();
                }
                yield break;
            }
            case EffectType.LoseHP:
            {
                if (ctx.isPreview)
                    yield break;
                ctx.source.TakeDamage((effect.value== -1 ? ctx.source.maxHP : effect.value),true);
                ctx.source.OnDamageTaken(null, (effect.value== -1 ? ctx.source.maxHP : effect.value),true);
                yield break;
            }
            case EffectType.GainEnergy:
            {
                if (ctx.isPreview)
                    yield break;
                ctx.source.GainEnergy(effect.value);
                yield break;
            }
            case EffectType.AddCardToHand:
            {
                if (ctx.isPreview)
                    yield break;
                ctx.source.GetCombatManager().deck.AddCardToHand(effect.cardID);
                yield break;
            }
            case EffectType.StealBuff: // Steal non-framed buffs from target and give them to source
            {
                if (ctx.isPreview)
                    yield break;
                List<StatusEffect> buffsToSteal = ctx.target.statusEffects.Where(s => s.buff && (!s.framed||effect.trueEffect)&&!s.goldFrame).ToList();
                for (int i = 0; (i < effect.value || effect.value == -1) && buffsToSteal.Count > 0; i++)
                {
                    StatusEffect buff = buffsToSteal[0];
                    buffsToSteal.RemoveAt(0);
                    StatusEffect dispelled=buff.Dispel(effect.duration);
                    ctx.source.AddStatus(dispelled);
                }
                yield break;
            }
            case EffectType.TransferDebuff: // Transfer non-framed debuffs from source to target
            {
                if (ctx.isPreview)
                    yield break;
                List<StatusEffect> debuffsToTransfer = ctx.source.statusEffects.Where(s => !s.buff && (!s.framed||effect.trueEffect)&&!s.goldFrame).ToList();
                for (int i = 0; (i < effect.value || effect.value == -1) && debuffsToTransfer.Count > 0; i++)
                {
                    StatusEffect debuff = debuffsToTransfer[0];
                    debuffsToTransfer.RemoveAt(0);
                    StatusEffect dispelled=debuff.Dispel(effect.duration);
                    ctx.target.AddStatus(dispelled);
                }
                yield break;
            }
            case EffectType.DispelBuff:
            {
                if (ctx.isPreview)
                    yield break;
                bool removedAny = false;
                List<StatusEffect> buffsToDispel = ctx.target.statusEffects.Where(s => s.buff && (!s.framed||effect.trueEffect)&&!s.goldFrame).ToList();
                for (int i = 0; (i < effect.value || effect.value == -1) && buffsToDispel.Count > 0; i++)
                {
                    StatusEffect buff = buffsToDispel[0];
                    buffsToDispel.RemoveAt(0);
                    buff.Dispel(effect.duration);
                    removedAny = true;
                }
                ctx.combat?.tutorial?.NotifyDispelResult(ctx.card?.data?.cardName, removedAny);
                yield break;
            }
            case EffectType.DispelDebuff:
            {
                if (ctx.isPreview)
                    yield break;
                bool removedAny = false;
                List<StatusEffect> debuffsToDispel = ctx.target.statusEffects.Where(s => !s.buff && (!s.framed||effect.trueEffect)&&!s.goldFrame).ToList();
                for (int i = 0; (i < effect.value || effect.value == -1) && debuffsToDispel.Count > 0; i++)
                {
                    StatusEffect debuff = debuffsToDispel[0];
                    debuffsToDispel.RemoveAt(0);
                    debuff.Dispel(effect.duration);
                    removedAny = true;
                }
                ctx.combat?.tutorial?.NotifyDispelResult(ctx.card?.data?.cardName, removedAny);
                yield break;
            }
            case EffectType.EndTurn:
            {
                if (ctx.isPreview)
                    yield break;
                if (ctx.source != null && ctx.source.isPlayer)
                {
                    ctx.combat.turnSystem.PlayerEndTurn();
                }
                yield break;
            }
            case EffectType.Gravity:
            {
                if (ctx.isPreview)
                    yield break;
                // Reduce target's HP by the value% of their current HP, ignoring armor
                int dmg = ctx.target.currentHP * effect.value / 100;
                ctx.target.TakeDamage(dmg, true);
                 if (ctx.source != null)
                    {
                        ctx.source.OnDamageDealt(ctx.target, dmg);
                        ctx.target.OnDamageTaken(ctx.source, dmg);
                    }
                yield break;
            }
            case EffectType.Break:
                {
                    if (ctx.isPreview||ctx.target.armor <= 0)
                        yield break;
                    //Break the target's armor, ignoring resistances
                    ctx.target.armor = 0;
                    if (ctx.source != null) ctx.source.OnTargetArmorBroken(ctx.target);
                    ctx.target.OnOwnArmorBroken(ctx.source);
                    ctx.state.armorBroken=true;
                    yield break;
                }
            case EffectType.CardSelection:
                {
                    // Build predicate from tags (OR semantics)
                    System.Predicate<CardInstance> predicate = c => true;
                    if (effect.cardFilterTags != null && effect.cardFilterTags.Count > 0)
                    {
                        predicate = c =>
                        {
                            foreach (var tag in effect.cardFilterTags)
                            {
                                switch (tag)
                                {
                                    case CardFilterTag.Attack:
                                        if (c.data.type == CardType.Attaque) return true;
                                        break;
                                    case CardFilterTag.Skill:
                                        if (c.data.type == CardType.Compétence) return true;
                                        break;
                                    case CardFilterTag.Power:
                                        if (c.data.type == CardType.Pouvoir) return true;
                                        break;
                                    case CardFilterTag.Retain:
                                        if (c.data.HasTag(CardTag.Retain)) return true;
                                        break;
                                    case CardFilterTag.Cost0:
                                        if (c.data.cost == 0) return true;
                                        break;
                                    case CardFilterTag.Cost1:
                                        if (c.data.cost == 1) return true;
                                        break;
                                    case CardFilterTag.Cost2:
                                        if (c.data.cost == 2) return true;
                                        break;
                                    case CardFilterTag.Cost3Plus:
                                        if (c.data.cost >= 3) return true;
                                        break;
                                    default:
                                        // Unsupported tags fallthrough
                                        break;
                                }
                            }
                            return false;
                        };
                    }

                    int amount = effect.value!=-1? effect.value : int.MaxValue; // If value is -1, allow selecting all cards
                    var deck = ctx.source.GetCombatManager().deck;
                    List<CardInstance> candidates = effect.cardSelectionSource switch
                    {
                        CardSelectionSource.Hand => deck.hand.Where(c => predicate(c)).ToList(),
                        CardSelectionSource.DiscardPile => deck.discardPile.Where(c => predicate(c)).ToList(),
                        CardSelectionSource.DrawPile => deck.drawPile.Where(c => predicate(c)).ToList(),
                        CardSelectionSource.ExhaustPile => deck.exhaustPile.Where(c => predicate(c)).ToList(),
                        _ => new List<CardInstance>()
                    };

                    if (candidates.Count == 0)
                        yield break;

                    List<CardInstance> selectedInstances = new();

                    if (effect.cardSelectionSource == CardSelectionSource.Hand)
                    {
                        CardSelectionRequest request = new CardSelectionRequest
                        {
                            amount = amount,
                            message = $"Choisissez {amount} carte" + (amount > 1 ? "s" : ""),
                            filter = ci => predicate(ci)
                        };

                        yield return ui.RequestCardSelection(request, cards =>
                        {
                            selectedInstances = cards;
                        });
                    }
                    else if (candidates.Count <= amount)
                    {
                        selectedInstances = candidates;
                    }
                    else
                    {
                        // Show the pile in the deck grid and use SelectionManager for selection
                        var panel = RunManager.Instance.ui.deckGridPanel;
                        string title = effect.cardSelectionSource switch
                        {
                            CardSelectionSource.DiscardPile => "Défausse",
                            CardSelectionSource.DrawPile => "Pioche",
                            CardSelectionSource.ExhaustPile => "Exil",
                            _ => "Cartes"
                        };

                        panel.Show(candidates, title);

                        SelectionManager.Instance.StartSelection(amount, instances => selectedInstances = instances);

                        while (SelectionManager.Instance.selectionMode)
                            yield return null;

                        panel.Hide();
                    }

                    // Apply the chosen card-selection effect
                        if (effect.cardSelectionEffect == CardSelectionEffect.Merge){
                        deck.AddToHand(CardInstance.Merge(selectedInstances));
                        foreach (var card in selectedInstances)
                        {
                            deck.Delete(card);
                        }
                    }       
                    else
                    {
                        foreach (var card in selectedInstances)
                        {
                            switch (effect.cardSelectionEffect)
                            {
                                case CardSelectionEffect.Exhaust:
                                    deck.Exhaust(card);
                                    break;
                                case CardSelectionEffect.Discard:
                                    if (deck.hand.Contains(card)) deck.Discard(card);
                                    else
                                    {
                                        // move from other piles to discard
                                        deck.drawPile.Remove(card);
                                        deck.exhaustPile.Remove(card);
                                        if (!deck.discardPile.Contains(card)) deck.discardPile.Add(card);
                                    }
                                    break;
                                case CardSelectionEffect.ReturnToHand:
                                    // Remove from any pile and add to hand
                                    deck.drawPile.Remove(card);
                                    deck.discardPile.Remove(card);
                                    deck.exhaustPile.Remove(card);
                                    if (!deck.hand.Contains(card)) deck.AddToHand(card);
                                    break;
                                case CardSelectionEffect.Enchant:
                                    EnchantManager.ApplyEnchant(card, Random.Range(1, 15));
                                    break;
                                case CardSelectionEffect.Unenchant:
                                    card.enchantments.Clear();
                                    break;
                                case CardSelectionEffect.Transform:
                                    STSCardData data = STSCardDatabase.GetRandomCard(RunManager.Instance.selectedCharacter);
                                    CardInstance newCard = new CardInstance(data);
                                    ctx.combat.ui.TransformCard(card, newCard);
                                    break;
                                case CardSelectionEffect.None:
                                default:
                                    break;
                            }
                        }
                    }
                    ui.RefreshUI();
                    yield break;
                }
            case EffectType.AddRandomCardToHand:
                {
                    if (ctx.isPreview)
                        yield break;
                    for (int i = 0; i < effect.value; i++)
                    {
                        STSCardData data = STSCardDatabase.GetRandomCard(RunManager.Instance.selectedCharacter);
                        ctx.source.GetCombatManager().deck.AddCardToHand(data.cardName);
                    }
                    yield break;
                }
            case EffectType.AddCardToDrawPile:
                {
                    if (ctx.isPreview)
                        yield break;
                    for (int i = 0; i < effect.value; i++)
                    {
                        if (effect.cardID == null || effect.cardID == "")
                        {
                            STSCardData data = STSCardDatabase.GetRandomCard(RunManager.Instance.selectedCharacter);
                            ctx.source.GetCombatManager().deck.drawPile.Add(new CardInstance(data));
                        }
                        else
                        {
                            STSCardData data = STSCardDatabase.Get(effect.cardID);
                            ctx.source.GetCombatManager().deck.drawPile.Add(new CardInstance(data));
                        }
                    }
                    yield break;
                }
            case EffectType.AddCardToDiscardPile:
                {
                    if (ctx.isPreview)
                        yield break;
                    for (int i = 0; i < effect.value; i++)
                    {
                        if (effect.cardID == null || effect.cardID == "")
                        {
                            STSCardData data = STSCardDatabase.GetRandomCard(RunManager.Instance.selectedCharacter);
                            ctx.source.GetCombatManager().deck.discardPile.Add(new CardInstance(data));
                        }
                        else
                        {
                            STSCardData data = STSCardDatabase.Get(effect.cardID);
                            ctx.source.GetCombatManager().deck.discardPile.Add(new CardInstance(data));
                        }
                    }
                    yield break;
                }
            case EffectType.ForceNextCard:
                {
                    if (ctx.isPreview)
                        yield break;
                    if (effect.cardID == null || effect.cardID == ""||ctx.target.isPlayer)
                    {
                        yield break;
                    }
                    else
                    {
                        ((Enemy)ctx.target).ForceNextAction(effect.cardID);
                    }
                    yield break;
                }
            default:
                yield break;
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