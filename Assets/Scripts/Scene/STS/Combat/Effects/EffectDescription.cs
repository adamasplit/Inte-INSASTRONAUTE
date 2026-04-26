public static class EffectDescription
{
    public static string Get(EffectEntry effect, EffectContext ctx)
    {
        switch (effect.type)
        {
            case EffectType.Damage:
            {
                string dmg = BattleCalculator.GetModifiedDescription(effect.value, StatType.Damage, ctx);
                return $"Inflige {dmg} dégâts";
            }

            case EffectType.Armor:
            {
                string armor = BattleCalculator.GetModifiedDescription(effect.value, StatType.Armor, ctx);
                return $"Donne {armor} d'Armure";
            }

            case EffectType.Heal:
            {
                string heal = BattleCalculator.GetModifiedDescription(effect.value, StatType.Heal, ctx);
                return $"Soigne {heal} PV";
            }
            case EffectType.Status:
            {
                int val = BattleCalculator.GetModifiedValue(effect.value, StatType.StatusPotency, ctx);
                int dur = BattleCalculator.GetModifiedValue(effect.duration, StatType.StatusDuration, ctx);
                StatusEffect stat=StatusEffect.Factory(effect.statusType,effect.value,effect.duration);
                if (stat.Duration>0)
                {
                    if (effect.targetSelf)
                    {
                        return $"Reçoit {stat.Name} ({stat.Duration})";
                    }
                    else
                    {
                        return $"Applique {stat.Name} ({stat.Duration})";
                    }
                }
                else
                    {
                        return $"Donne {stat.Value} de {stat.Name}";
                    }
            }

            case EffectType.DeleteNextTurn:
            {
                return $"Supprime le prochain tour de la cible";
            }

            case EffectType.AdvanceTurn:
            {
                if (effect.targetSelf)
                    return $"Avance votre prochain tour ({effect.value})";
                int turns = BattleCalculator.GetModifiedValue(effect.value, StatType.StatusPotency, ctx);
                return $"Avance le prochain tour de la cible ({turns})";
            }

            case EffectType.DelayTurn:
            {
                if (effect.targetSelf)
                    return $"Retarde votre prochain tour ({effect.value})";
                int turns = BattleCalculator.GetModifiedValue(effect.value, StatType.StatusPotency, ctx);
                return $"Retarde le prochain tour de la cible ({turns})";
            }

            default:
                return "Effet inconnu...";
        }
    }
}