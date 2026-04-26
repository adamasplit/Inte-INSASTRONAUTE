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
                string armor = BattleCalculator.GetModifiedDescription(effect.value, StatType.Block, ctx);
                return $"Donne {armor} d'Armure";
            }

            case EffectType.Heal:
            {
                string heal = BattleCalculator.GetModifiedDescription(effect.value, StatType.Heal, ctx);
                return $"Soigne {heal} PV";
            }

            case EffectType.Strength:
            {
                string str = BattleCalculator.GetModifiedDescription(effect.value, StatType.StatusPotency, ctx);
                string dur = BattleCalculator.GetModifiedDescription(effect.duration, StatType.StatusDuration, ctx);
                return $"Donne {str} de Force";
            }

            case EffectType.Vulnerability:
            {
                int vuln = BattleCalculator.GetModifiedValue(effect.value, StatType.StatusPotency, ctx);
                int dur = BattleCalculator.GetModifiedValue(effect.duration, StatType.StatusDuration, ctx);
                if (dur > 0)
                    return $"Inflige {vuln} de Vulnérabilité pendant {dur} tours";
                return $"Inflige {vuln} de Vulnérabilité";
            }

            case EffectType.Weakness:
            {
                int weak = BattleCalculator.GetModifiedValue(effect.value, StatType.StatusPotency, ctx);
                int dur = BattleCalculator.GetModifiedValue(effect.duration, StatType.StatusDuration, ctx);
                if (dur > 0)
                    return $"Inflige {weak} d'Affaiblissement pendant {dur} tours";
                return $"Inflige {weak} d'Affaiblissement";
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
                return $"Avance le prochain tour de la cible({turns})";
            }

            case EffectType.DelayTurn:
            {
                if (effect.targetSelf)
                    return $"Retarde votre prochain tour ({effect.value})";
                int turns = BattleCalculator.GetModifiedValue(effect.value, StatType.StatusPotency, ctx);
                return $"Retarde le prochain tour de la cible({turns})";
            }

            default:
                return "Effet inconnu...";
        }
    }
}