using UnityEngine;
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
        if (ctx.card!=null&&ctx.card.data.xCost)
        {
            desc +=" X fois";
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
                return $"Infligez {dmg} dégâts";
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
                StatusEffect stat=StatusEffect.Factory(effect.statusType,val,dur,effect.cardID);
                if (stat.generic) 
                {
                    if (effect.targetSelf)
                    {
                        return $"Gagnez {stat.Duration} de {stat.Name}";
                    }
                    else
                    {
                        return $"Appliquez {stat.Duration} de {stat.Name}";
                    }
                }
                else if (effect.statusType==StatusType.Strength||effect.statusType==StatusType.Dexterity||effect.statusType==StatusType.Speed)
                    {
                        if (effect.targetSelf)
                        {
                            if (stat.Value >= 0)
                                return $"Gagnez {stat.Value} de {stat.Name}";
                            else 
                                return $"Perdez {-stat.Value} de {stat.Name}";
                        }
                        else
                        {
                            if (stat.Value >= 0)
                                return (multipleTargets?"Toutes les cibles gagnent":"La cible gagne") + $" {stat.Value} de {stat.Name}";
                            else
                                return (multipleTargets?"Toutes les cibles perdent":"La cible perd") + $" {-stat.Value} de {stat.Name}";
                        }
                    }
                else
                    {
                        //Remove last character if it's a dot or a plus sign
                        string desc= stat.CardDesc(effect.targetSelf);
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
                    return $"Avancez votre prochain tour ({turns})";
                return (multipleTargets?"Avancez les prochains tours de toutes les cibles":"Avancez le prochain tour de la cible") + $" ({turns})";
            }

            case EffectType.DelayTurn:
            {
                string turns = BattleCalculator.GetModifiedDescription(effect.value, StatType.TurnManipulationDelay, ctx);
                if (effect.targetSelf)
                    return $"Retardez votre prochain tour ({turns})";
                return (multipleTargets?"Retardez les prochains tours de toutes les cibles":"Retardez le prochain tour de la cible") + $" ({turns})";
            }
            case EffectType.Draw:
            {
                return $"Piochez {effect.value} carte"+(effect.value>1?"s":"");
            }
            case EffectType.Discard:
            {
                return $"Défaussez {effect.value} carte"+(effect.value>1?"s":" au hasard");
            }
            case EffectType.Exhaust:
            {
                return $"Epuisez {effect.value} carte"+(effect.value>1?"s":"")+" au hasard de votre main";
            }
            case EffectType.LoseHP:
            {
                return $"Perdez "+transform(effect.value,"tous vos")+" PV";
            }
            case EffectType.GainEnergy:
            {
                return $"Gagnez {effect.value} d'énergie";
            }
            case EffectType.AddCardToHand:
            {
                return $"Ajoutez {effect.value} <color=green>{effect.cardID}</color> à votre main";
            }
            case EffectType.StealBuff:
            {
                return $"Volez "+transform(effect.value, "tous les")+" buff"+(effect.value!=1?"s":"")+" de la cible"+(effect.trueEffect?" (y compris ceux normalement indissipables)":"");
            }
            case EffectType.TransferDebuff:
            {
                return $"Transférez "+transform(effect.value, "tous vos")+" debuff"+(effect.value!=1?"s":"")+" de vous à la cible"+(effect.trueEffect?" (y compris ceux normalement indissipables)":"");
            }
            case EffectType.DispelBuff:
            {
                return $"Dissipez "+transform(effect.value, (effect.targetSelf?"tous vos":"tous les"))+" buff"+(effect.value!=1?"s":"")+(effect.trueEffect?" (y compris ceux normalement indissipables)":"");
            }
            case EffectType.DispelDebuff:
            {
                return $"Dissipez "+transform(effect.value, (effect.targetSelf?"tous vos":"tous les"))+" debuff"+(effect.value!=1?"s":"")+(effect.trueEffect?" (y compris ceux normalement indissipables)":"");
            }
            case EffectType.EndTurn:
            {
                return $"Terminez votre tour";
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
                string pl=effect.value>1?"les":"la";
                string cft = "";
                foreach (var tag in effect.cardFilterTags)
                {
                    cft += tag switch
                    {
                        CardFilterTag.Attack => "Attaque",
                        CardFilterTag.Skill => "Compétence",
                        CardFilterTag.Power => "Pouvoir",
                        CardFilterTag.Cost0 => "de coût 0",
                        _ => tag.ToString()
                    } + " ";
                }
                string source = effect.cardSelectionSource switch
                {
                    CardSelectionSource.Hand => "votre main",
                    CardSelectionSource.DrawPile => "votre pioche",
                    CardSelectionSource.DiscardPile => "votre défausse",
                    _ => effect.cardSelectionSource.ToString()
                };
                string effectDesc= effect.cardSelectionEffect switch
                {
                    CardSelectionEffect.Exhaust => "épuisez-"+pl,
                    CardSelectionEffect.Discard => "défaussez-"+pl,
                    CardSelectionEffect.Transform => "transformez-"+pl,
                    CardSelectionEffect.Merge => "fusionnez-"+pl,
                    CardSelectionEffect.ReturnToHand => "ajoutez-"+pl+" à votre main",
                    _ => effect.cardSelectionEffect.ToString()
                };
                return $"Choisissez {effect.value} carte"+(effect.value>1?"s":"")+(cft!= "" ? " "+cft : "")+" dans "+source+" et "+effectDesc;
            }
            case EffectType.AddRandomCardToHand:
            {
                return $"Ajoutez {effect.value} carte"+(effect.value>1?"s":"")+" aléatoire"+(effect.cardID!=null&&effect.cardID!=""? $" de type <color=green>{effect.cardID}</color>":"")+" à votre main";
            }
            case EffectType.AddCardToDrawPile:
            {
                return $"Ajoutez {effect.value} <color=green>{effect.cardID}</color> à votre pioche";
            }
            case EffectType.AddCardToDiscardPile:
            {
                return $"Ajoutez {effect.value} <color=green>{effect.cardID}</color> à votre défausse";
            }
            default:
                return "Effet inconnu..";
        }
    }
    private static string transform(int value, string all)
    {
        return (value != -1 ? value.ToString() :all);
    }
}