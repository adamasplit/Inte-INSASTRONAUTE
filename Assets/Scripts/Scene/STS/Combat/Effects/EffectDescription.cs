using UnityEngine;
using System;
public static class EffectDescription
{
    public static string Get(EffectEntry effect, EffectContext ctx)
    {
        string desc = effect.description;
        if (effect.description != null&& effect.description != "" && effect.description != "/")
        {
        
        }
        else
        {
            desc=GetConditionDescription(effect, ctx);
            string effectDesc = GetEffectDescription(effect, ctx);
            if (desc != "")
            {
                desc+=effectDesc[0].ToString().ToLower() + effectDesc.Substring(1);
            }
            else
            {
                desc=effectDesc;
            }
        }
        // Only add " X fois" if card is X-cost and we haven't already used "X" for quantity
        if (ctx.card!=null&&ctx.card.data.xCost)
        {
            bool usedXAsQuantity = desc.Contains(" X de ") || desc.Contains(" X cartes") || desc.Contains(" X aléatoires");
            if (!usedXAsQuantity)
            {
                desc +=" X fois";
            }
        }
        desc+=".";
        return desc;

    }
    public static string GetConditionDescription(EffectEntry effect, EffectContext ctx)
    {
        if (!effect.conditional) return "";
        switch (effect.conditionType)
        {
            case ConditionType.KillingBlow:
                return "Si la cible est tuée, ";
            case ConditionType.ArmorBreak:
                return "Si vous brisez l'armure de la cible, ";
            case ConditionType.FirstTimePlayingThisCardThisTurn:
                return "Si c'est la première fois que vous jouez cette carte ce tour-ci, ";
            case ConditionType.FirstTimePlayingThisCardThisCombat:
                return "Si c'est la première fois que vous jouez cette carte ce combat, ";
            case ConditionType.TargetHasStatus:
                {
                    return $"Si la cible a {StatusEffect.Factory(Enum.Parse<StatusType>(effect.conditionValue), 0, 0, "").Name}, ";
                }
                return "";
            case ConditionType.TargetHasNoStatus:
                {
                    return $"Si la cible n'a pas {StatusEffect.Factory(Enum.Parse<StatusType>(effect.conditionValue), 0, 0, "").Name}, ";
                }
                return "";
            case ConditionType.SelfArmorThreshold:
                {
                    return $"Si vous avez au moins {effect.conditionValue} d'armure, ";
                }
            case ConditionType.TargetArmorThreshold:
                {
                    return $"Si la cible a au moins {effect.conditionValue} d'armure, ";
                }
            case ConditionType.EnergyGainedThreshold:
                {
                    return $"Si vous avez gagné au moins {effect.conditionValue} d'énergie ce tour-ci, ";
                }
            case ConditionType.EnergySpentThreshold:
                {
                    return $"Si vous avez dépensé au moins {effect.conditionValue} d'énergie ce tour-ci, ";
                }
            case ConditionType.TargetWillAttack:
                {
                    return $"Si la cible s'apprête à attaquer, ";
                }
            case ConditionType.TargetWillNotAttack:
                {
                    return $"Si la cible ne va pas attaquer, ";
                }
            default:
                return "";
        }
    }
    public static string GetEffectDescription(EffectEntry effect, EffectContext ctx)
    {
        bool allCharacters = ctx.card!=null && ctx.card.targetingMode == TargetingMode.AllCharacters;
        bool allEnemies = ctx.card!=null && ctx.card.targetingMode == TargetingMode.AllEnemies;
        bool multipleTargets=allCharacters || allEnemies;
        switch (effect.type)
        {
            case EffectType.Damage:
            {
                string dmg = BattleCalculator.GetModifiedDescription(effect.value, StatType.Damage, ctx);
                if (effect.targetSelf)
                {
                    return $"Subissez {dmg} dégâts";
                }
                return $"Infligez {dmg} dégâts"+(multipleTargets?" à toutes les cibles":"");
            }
            case EffectType.Multihit:
            {
                string dmg = BattleCalculator.GetModifiedDescription(effect.value, StatType.Damage, ctx);
                return $"Infligez {dmg} dégâts {effect.duration} fois";
            }

            case EffectType.Armor:
            {
                string armor = BattleCalculator.GetModifiedDescription(effect.value, StatType.Armor, ctx);
                return (effect.targetSelf?"Gagnez ":"Donnez ") + $"{armor} d'Armure" + (effect.targetSelf?"":" à la cible");
            }

            case EffectType.Heal:
            {
                string heal = BattleCalculator.GetModifiedDescription(effect.value, StatType.Heal, ctx);
                return $"Récupérez {heal} PV";
            }
            case EffectType.Status:
            {
                int val = BattleCalculator.GetModifiedValue(effect.value, StatType.StatusPotency, ctx);
                int dur = BattleCalculator.GetModifiedValue(effect.duration, StatType.StatusDuration, ctx);
                StatusEffect stat=StatusEffect.Factory(effect.statusType,val,dur,effect.cardID,effect.index);
                if (stat.generic) 
                {
                    int usedValue=stat.Duration>0?stat.Duration:stat.Value;
                    string usedValueText = FormatQuantityForDescription(usedValue, ctx);
                    if (effect.targetSelf)
                    {
                        return $"Gagnez {usedValueText} de {stat.Name}";
                    }
                    else
                    {
                        return $"Appliquez {usedValueText} d{(stat.Name[0]=='A' || stat.Name[0]=='E'|| stat.Name[0]=='I' || stat.Name[0]=='O' || stat.Name[0]=='U'||stat.Name[0]=='É' ? "'" : "e ")}{stat.Name}";
                    }
                }
                else if (effect.statusType==StatusType.Strength||effect.statusType==StatusType.Dexterity||effect.statusType==StatusType.Speed)
                    {
                        string valueText = FormatQuantityForDescription(Mathf.Abs(stat.Value), ctx);
                        if (effect.targetSelf)
                        {
                            if (stat.Value >= 0)
                                return $"Gagnez {valueText} de {stat.Name}";
                            else 
                                return $"Perdez {valueText} de {stat.Name}";
                        }
                        else
                        {
                            if (stat.Value >= 0)
                                return (multipleTargets?"Toutes les cibles gagnent":"La cible gagne") + $" {valueText} de {stat.Name}";
                            else
                                return (multipleTargets?"Toutes les cibles perdent":"La cible perd") + $" {valueText} de {stat.Name}";
                        }
                    }
                else
                    {
                        //Remove last character if it's a dot or a plus sign
                        string desc= stat.Desc(effect.targetSelf);
                        if (desc.EndsWith(".") || desc.EndsWith("+"))
                        {
                            desc = desc.Substring(0, desc.Length - 1);
                        }
                        return desc;
                    }
            }

            case EffectType.DeleteNextTurn:
            {
                return multipleTargets?"Supprimez le prochain tour de toutes les cibles":"Supprimez le prochain tour de la cible";
            }

            case EffectType.AdvanceTurn:
            {
                string turns = BattleCalculator.GetModifiedDescription(effect.value, StatType.TurnManipulationAdvance, ctx);
                if (effect.targetSelf)
                    return $"Avancez votre prochain tour ({turns}%)";
                return (multipleTargets?"Avancez les prochains tours de toutes les cibles":"Avancez le prochain tour de la cible") + $" ({turns}%)";
            }

            case EffectType.DelayTurn:
            {
                string turns = BattleCalculator.GetModifiedDescription(effect.value, StatType.TurnManipulationDelay, ctx);
                if (effect.targetSelf)
                    return $"Retardez votre prochain tour ({turns}%)";
                return (multipleTargets?"Retardez les prochains tours de toutes les cibles":"Retardez le prochain tour de la cible") + $" ({turns}%)";
            }
            case EffectType.CutInTurn:
            {
                if (effect.targetSelf)
                {
                    return "Placez votre prochain tour juste après celui de la cible";
                }
                return "Placez le prochain tour de la cible juste après le vôtre";
            }
            case EffectType.Draw:
            {
                return $"Piochez {FormatCardCountForDescription(effect.value, ctx)}";
            }
            case EffectType.Discard:
            {
                return $"Défaussez {FormatCardCountForDescription(effect.value, ctx)} au hasard";
            }
            case EffectType.Exhaust:
            {
                return $"Epuisez {FormatCardCountForDescription(effect.value, ctx)} au hasard de votre main";
            }
            case EffectType.LoseHP:
            {
                return $"Perdez "+transform(effect.value,"tous vos")+" PV";
            }
            case EffectType.GainEnergy:
            {
                return $"Gagnez {BattleCalculator.GetModifiedDescription(effect.value, StatType.EnergyGain, ctx)} d'énergie";
            }
            case EffectType.AddCardToHand:
            {
                return $"Ajoutez {FormatQuantityForDescription(effect.value, ctx)} <color=green>{effect.cardID}</color> à votre main";
            }
            case EffectType.StealBuff:
            {
                return $"Volez "+dispel(effect.duration)+transform(effect.value, " tous les")+" buff"+(effect.value!=1?"s":"")+" de la cible"+(effect.trueEffect?" (y compris ceux normalement indissipables)":"");
            }
            case EffectType.TransferDebuff:
            {
                return $"Transférez "+dispel(effect.duration)+transform(effect.value, " tous vos")+" debuff"+(effect.value!=1?"s":"")+" de vous à la cible"+(effect.trueEffect?" (y compris ceux normalement indissipables)":"");
            }
            case EffectType.DispelBuff:
            {
                return $"Dissipez "+dispel(effect.duration)+transform(effect.value, (effect.targetSelf?" tous vos":"tous les"))+" buff"+(effect.value!=1?"s":"")+(effect.value>0?(effect.targetSelf?" sur vous":" sur la cible"):"")+(effect.trueEffect?" (y compris ceux normalement indissipables)":"");
            }
            case EffectType.DispelDebuff:
            {
                return $"Dissipez "+dispel(effect.duration)+transform(effect.value, (effect.targetSelf?" tous vos":"tous les"))+" debuff"+(effect.value!=1?"s":"")+(effect.value>0?(effect.targetSelf?" sur vous":" sur la cible"):"")+(effect.trueEffect?" (y compris ceux normalement indissipables)":"");
            }
            case EffectType.EndTurn:
            {
                if (effect.targetSelf)
                {
                    return $"Terminez votre tour";
                }
                return $"Terminez le tour de la cible de force";
            }
            case EffectType.Gravity:
            {
                return  multipleTargets?$"Les cibles perdent {effect.value}% de leurs PV":$"La cible perd {effect.value}% de ses PV";
            }
            case EffectType.Break:
            {
                if (effect.targetSelf)
                {
                    return $"Perdez votre Armure";
                }
                    return $"Brisez l'Armure de la cible";
            }
            case EffectType.CardSelection:
            {
                string cft = DescribeCardFilters(effect.cardFilterTags,effect.value!=1);
                string source = effect.cardSelectionSource switch
                {
                    CardSelectionSource.Hand => "votre main",
                    CardSelectionSource.DrawPile => "votre pioche",
                    CardSelectionSource.DiscardPile => "votre défausse",
                    CardSelectionSource.ExhaustPile => "votre pile de cartes épuisées",
                    CardSelectionSource.All => "toutes vos piles de cartes",
                    CardSelectionSource.AllExceptExhaustPile => "votre main, votre pioche et votre défausse",
                    _ => effect.cardSelectionSource.ToString()
                };
                string filterSuffix = cft != "" ? "" + cft.TrimEnd() : "";

                if (effect.value == -1)
                {
                    return effect.cardSelectionEffect switch
                    {
                        CardSelectionEffect.Exhaust => $"Épuisez toutes les cartes{filterSuffix} de {source}",
                        CardSelectionEffect.Discard => $"Défaussez toutes les cartes{filterSuffix} de {source}",
                        CardSelectionEffect.Transform => $"Transformez toutes les cartes{filterSuffix} de {source}",
                        CardSelectionEffect.Merge => $"Fusionnez toutes les cartes{filterSuffix} de {source}",
                        CardSelectionEffect.ReturnToHand => $"Ajoutez à votre main toutes les cartes{filterSuffix} de {source}",
                        CardSelectionEffect.Enchant => $"Enchantez toutes les cartes{filterSuffix} de {source}",
                        CardSelectionEffect.Unenchant => $"Désenchantez toutes les cartes{filterSuffix} de {source}",
                        CardSelectionEffect.TopOfDrawPile => source == "votre pioche"
                            ? $"Placez toutes les cartes{filterSuffix} sur le dessus de votre pioche"
                            : $"Placez toutes les cartes{filterSuffix} sur le dessus de votre pioche depuis {source}",
                        CardSelectionEffect.ConsumeAndDealDamageToAll=> $"Consommez toutes les cartes{filterSuffix} de {source} et infligez {effect.duration} dégâts à toutes les cibles pour chaque carte consommée",
                        CardSelectionEffect.ConsumeAndGainArmor=> $"Consommez toutes les cartes{filterSuffix} de {source} et gagnez {effect.duration} d'Armure pour chaque carte consommée",
                        CardSelectionEffect.ReduceCost => $"Réduisez le coût de toutes les cartes{filterSuffix} de {source} de {effect.duration}",
                        CardSelectionEffect.ReduceCostTemp => $"Réduisez temporairement le coût de toutes les cartes{filterSuffix} de {source} de {effect.duration}",
                        CardSelectionEffect.IncreaseDamage => $"Augmentez les dégâts de toutes les cartes{filterSuffix} de {source} de {effect.duration}",
                        CardSelectionEffect.IncreaseDamageTemp => $"Augmentez temporairement les dégâts de toutes les cartes{filterSuffix} de {source} de {effect.duration}",
                        _ => $"Appliquez l'effet {effect.cardSelectionEffect} à toutes les cartes{filterSuffix} de {source}",
                        
                    };
                }

                string pl=effect.value!=1?"les":"la";
                string effectDesc= effect.cardSelectionEffect switch
                {
                    CardSelectionEffect.Exhaust => "épuisez-"+pl,
                    CardSelectionEffect.Discard => "défaussez-"+pl,
                    CardSelectionEffect.Transform => "transformez-"+pl,
                    CardSelectionEffect.Merge => "fusionnez-"+pl,
                    CardSelectionEffect.ReturnToHand => "ajoutez-"+pl+" à votre main",
                    CardSelectionEffect.Enchant => "enchantez-"+pl,
                    CardSelectionEffect.Unenchant => "désenchantez-"+pl,
                    CardSelectionEffect.TopOfDrawPile => "placez-"+pl+" sur le dessus de votre pioche",
                    CardSelectionEffect.ReduceCost => $"réduisez {(effect.value!=1?"leur":"son")} coût de {effect.duration}",
                    CardSelectionEffect.ReduceCostTemp => $"réduisez temporairement {(effect.value!=1?"leur":"son")} coût de {effect.duration}",
                    CardSelectionEffect.IncreaseDamage => $"augmentez {(effect.value!=1?"leur":"son")} dégâts de {effect.duration}",
                    CardSelectionEffect.IncreaseDamageTemp => $"augmentez temporairement {(effect.value!=1?"leur":"son")} dégâts de {effect.duration}",
                    _ => effect.cardSelectionEffect.ToString()
                };
                string cardStr = effect.value!=-1? $"Choisissez {effect.value.ToString()}" : "Prenez toutes les";
                return cardStr+" carte"+(effect.value!=1?"s":"")+(cft!= "" ? " "+cft : "")+" dans "+source+" et "+effectDesc;
            }
            case EffectType.AddRandomCard:
            {
                string countText = FormatCardCountForDescription(effect.value, ctx) + " aléatoire"+(effect.value!=1?"s":"");
                string filterText = DescribeCardFilters(effect.cardFilterTags);
                string destinationText = GetDestinationText(effect.cardSelectionSource);

                return $"Ajoutez {countText}{filterText} {destinationText}";
            }
            case EffectType.AddCardToDrawPile:
            {
                return $"Ajoutez {FormatQuantityForDescription(effect.value, ctx)} <color=green>{effect.cardID}</color> à votre pioche";
            }
            case EffectType.AddCardToDiscardPile:
            {
                return $"Ajoutez {FormatQuantityForDescription(effect.value, ctx)} <color=green>{effect.cardID}</color> à votre défausse";
            }
            case EffectType.ForceNextCard:
            {
                return $"La cible jouera <color=green>{effect.cardID}</color> à "+(effect.value==1?"son prochain tour":$"ses {FormatQuantityForDescription(effect.value, ctx)} prochains tours");
            }
            case EffectType.DoubleDebuffs:
            {
                return $"Doublez la valeur de tous les debuffs de la cible";
            }
            case EffectType.SetStatusToMaxValue:
            {
                StatusEffect stat=StatusEffect.Factory(effect.statusType,0,0,effect.cardID,effect.index);
                return $"Mettez {stat.Name} à sa valeur maximale";
            }
            case EffectType.ExtendStatuses:
            {
                if (effect.targetSelf)
                {
                    return $"Prolongez la durée de tous vos effets de statut de {effect.value} tour"+(effect.value!=1?"s":"");
                }
                else
                {
                    return $"Prolongez la durée de tous les effets de statut de la cible de {effect.value} tour"+(effect.value!=1?"s":"");
                }
            }
            case EffectType.DispelDebuffsIntoDamage:
            {
                string dmg = BattleCalculator.GetModifiedDescription(effect.value, StatType.Damage, ctx);
                return $"Dissipez tous les debuffs de la cible et infligez {dmg} dégâts pour chaque debuff dissipé";
            }
            case EffectType.DispelBuffsIntoStatus:
            {
                StatusEffect stat=StatusEffect.Factory(effect.statusType,0,0,effect.cardID,effect.index);
                if (effect.targetSelf)
                {
                    return $"Transformez tous vos buffs en {stat.Name}";
                }
                else
                {
                    return $"Dissipez tous les buffs de la cible et appliquez {stat.Name} pour chaque buff dissipé";
                }
            }
            case EffectType.DispelSpecificStatus:
            {
                StatusEffect stat=StatusEffect.Factory(effect.statusType,0,0,effect.cardID,effect.index);
                if (effect.targetSelf)
                {
                    return $"Dissipez votre {stat.Name}";
                }
                else
                {
                    return $"Dissipez les {stat.Name}s";
                }
            }
            case EffectType.AddCopyOfCard:
            {
                string destinationText = GetDestinationText(effect.cardSelectionSource);
                return $"Ajoutez {FormatQuantityForDescription(effect.value, ctx)} copie"+(effect.value!=1?"s":"")+$" de cette carte {destinationText}";
            }
            default:
                return "Effet inconnu..";
        }
    }
    private static string dispel(int remainingPercentage)
    {
        if (remainingPercentage==0)
        {
            return "";
        }
        if (remainingPercentage<=25)
        {
            return "en faible partie ";
        }
        if (remainingPercentage<=50)
        {
            return "à moitié ";
        }
        if (remainingPercentage<=75)
        {
            return "en bonne partie ";
        }
        return "en majeure partie ";
    }
    private static string transform(int value, string all)
    {
        return (value != -1 ? value.ToString() :all);
    }

    private static bool ShouldDisplayAsX(int value, EffectContext ctx)
    {
        return value == 1 && ctx != null && ctx.card != null && ctx.card.data.xCost;
    }

    private static string FormatQuantityForDescription(int value, EffectContext ctx)
    {
        return ShouldDisplayAsX(value, ctx) ? "X" : value.ToString();
    }

    private static string FormatCardCountForDescription(int value, EffectContext ctx)
    {
        return ShouldDisplayAsX(value, ctx) ? "X cartes" : $"{value} carte" + (value != 1 ? "s" : "");
    }
    private static string GetDestinationText(CardSelectionSource cardSelectionSource)
    {
        return cardSelectionSource switch
        {
            CardSelectionSource.Hand => "à votre main",
            CardSelectionSource.DiscardPile => "à votre défausse",
            CardSelectionSource.DrawPile => "à votre pioche",
            CardSelectionSource.ExhaustPile => "à votre pile de cartes épuisées",
            CardSelectionSource.All => "à toutes vos piles de cartes",
            CardSelectionSource.AllExceptExhaustPile => "à votre main, votre pioche et votre défausse",
            _ => "à votre main"
        };
    }
    private static string DescribeCardFilters(System.Collections.Generic.List<CardFilterTag> tags,bool plural=false)
    {
        if (tags == null || tags.Count == 0)
            return string.Empty;

        var parts = new System.Collections.Generic.List<string>();
        foreach (var tag in tags)
        {
            parts.Add(tag switch
            {
                CardFilterTag.Attack => "Attaque",
                CardFilterTag.Skill => "Compétence",
                CardFilterTag.Power => "Pouvoir",
                CardFilterTag.Retain => "Retenue",
                CardFilterTag.Upgraded => "améliorée"+(plural?"s":""),
                CardFilterTag.Unupgraded => "non améliorée"+(plural?"s":""),
                CardFilterTag.Cost0 => "coût 0",
                CardFilterTag.Cost1 => "coût 1",
                CardFilterTag.Cost2 => "coût 2",
                CardFilterTag.Cost3Plus => "coût 3+",
                CardFilterTag.Atom => "Atome",
                CardFilterTag.Molecule => "Molécule",
                CardFilterTag.Norm => "Norme",
                _ => tag.ToString()
            });
        }

        return " " + string.Join(", ", parts);
    }
}