public static class EffectDescription
{
    public static string Get(EffectEntry effect, EffectContext ctx)
    {
        switch (effect.type)
        {
            case EffectType.Damage:
            {
                int dmg = ctx.combat.GetModifiedValue(effect.value, StatType.Damage, ctx);
                return $"Inflige {dmg} dégâts";
            }

            case EffectType.Armor:
            {
                int armor = ctx.combat.GetModifiedValue(effect.value, StatType.Block, ctx);
                return $"Donne {armor} d'Armure";
            }

            case EffectType.Heal:
            {
                int heal = ctx.combat.GetModifiedValue(effect.value, StatType.Heal, ctx);
                return $"Soigne {heal} PV";
            }

            case EffectType.Strength:
            {
                int str = ctx.combat.GetModifiedValue(effect.value, StatType.StatusPotency, ctx);
                int dur = ctx.combat.GetModifiedValue(effect.duration, StatType.StatusDuration, ctx);
                if (dur > 0)
                    return $"Donne {str} de Force pendant {dur} tours";
                return $"Donne {str} de Force";
            }

            case EffectType.Vulnerability:
            {
                int vuln = ctx.combat.GetModifiedValue(effect.value, StatType.StatusPotency, ctx);
                int dur = ctx.combat.GetModifiedValue(effect.duration, StatType.StatusDuration, ctx);
                if (dur > 0)
                    return $"Inflige {vuln} de Vulnérabilité pendant {dur} tours";
                return $"Inflige {vuln} de Vulnérabilité";
            }

            case EffectType.Weakness:
            {
                int weak = ctx.combat.GetModifiedValue(effect.value, StatType.StatusPotency, ctx);
                int dur = ctx.combat.GetModifiedValue(effect.duration, StatType.StatusDuration, ctx);
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
                int turns = ctx.combat.GetModifiedValue(effect.value, StatType.StatusPotency, ctx);
                return $"Avance le prochain tour de la cible({turns})";
            }

            case EffectType.DelayTurn:
            {
                if (effect.targetSelf)
                    return $"Retarde votre prochain tour ({effect.value})";
                int turns = ctx.combat.GetModifiedValue(effect.value, StatType.StatusPotency, ctx);
                return $"Retarde le prochain tour de la cible({turns})";
            }

            default:
                return "Effet inconnu...";
        }
    }
}