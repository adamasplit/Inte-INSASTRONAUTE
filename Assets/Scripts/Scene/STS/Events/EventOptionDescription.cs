public static class EventOptionDescription
{
    public static string GetDescription(PanelOptionEntry option)
    {
        switch (option.type)
        {
            case EventOptionType.None:
                return "(Rien ne se passe)";
            case EventOptionType.CardReward:
                if (option.cardRewardProfiles != null && option.cardRewardProfiles.Count > 0)
                {
                    return "Recevez " + option.cardRewardProfiles.Count + " récompense"+(option.cardRewardProfiles.Count>1?"s":"")+" de carte";
                }

                return option.cardRewardProfile != null ? option.cardRewardProfile.GetShortDescription() : "Recevez une carte";
            case EventOptionType.RelicReward:
                return "Recevez une pièce d'équipement";
            case EventOptionType.GoldReward:
                return "Recevez de l'or";
            case EventOptionType.Heal:
                return $"Récupérez {option.value} PV";
            case EventOptionType.MaxHpGain:
                return $"Augmentez vos PV max de {option.value}";
            case EventOptionType.MaxHpLoss:
                return $"Diminuez vos PV max de {option.value}";
            case EventOptionType.Damage:
                return $"Subissez {option.value} dégâts";
            case EventOptionType.UpgradeCard:
                return $"Améliorez {option.value} carte"+(option.value>1?"s":"")+" de votre deck";
            case EventOptionType.RemoveCard:
                return $"Retirez {option.value} carte"+(option.value>1?"s":"")+" de votre deck";
            case EventOptionType.TransformCard:
                return $"Transformez {option.value} carte"+(option.value>1?"s":"")+" de votre deck";
            case EventOptionType.AddCard:
                return $"Ajoutez {option.value} {option.id} à votre deck";
            //case EventOptionType.ReplaceOptions:
            //    return option.targetIds != null && option.targetIds.Count > 0
            //         $"Remplacez {string.Join(", ", option.targetIds)}"
            //        : "Remplacez cette option";
            default:
                return "";
        }
    }
}