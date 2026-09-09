using UnityEngine;
public class PowerPassiveStatus : StatusEffect
{
    public PowerPassiveStatus(string powerId)
    {
        Duration = -1;
        STSCardData power = STSCardDatabase.Get(powerId);
        Name = power != null ? power.cardName : powerId;
        cardID = powerId;
        buff = true;
        framed = true;
        modifierType = ModifierType.Multiplicative;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        if (stat != StatType.Damage)
            return false;
        if (cardID == "beton_precontraint")
            return ctx.target != null && ctx.target.statusEffects.Contains(this) && ctx.target.armor > 0;
        return cardID == "liaison_instable" && ctx.target != null && ctx.source != null
            && ctx.target.statusEffects.Contains(this)
            && ctx.source.statusEffects.Exists(status => status is BrandStatus);
    }

    public override int Modify(int damage, EffectContext ctx)
    {
        if (!AppliesTo(StatType.Damage, ctx))
            return damage;
        return cardID == "beton_precontraint"
            ? Mathf.CeilToInt(damage * 0.85f)
            : Mathf.FloorToInt(damage * 0.85f);
    }

    public override string Desc(bool isPlayer)
    {
        string description = cardID switch
        {
            "mur_de_soutenement" => "Au début de votre tour, gagnez 4 de Garde.",
            "ville_sous_cloche" => "Tous les 3 tours, gagnez Conservation d'armure.",
            "plan_directeur" => "Au début de votre tour, gagnez 2 de Blindage. Quand votre Armure se brise, gagnez 1 de Force.",
            "second_souffle" => "Au début de votre tour, si vous avez au plus la moitié de vos PV, gagnez Régénération 2.",
            "observation_continue" => "Au début de votre tour, si un ennemi prévoit une Attaque, gagnez 4 d'Armure.",
            "preparation" => "Au début de votre premier tour, gagnez 1 Artefact et 1 Filtre.",
            "contrepoids_pouvoir" => "Fin du tour : si vous avez de l'Armure, appliquez Lenteur 1 à un ennemi.",
            "distillation_continue" => "Fin du tour : si vous avez appliqué au moins 2 debuffs, gagnez Régénération 1.",
            "contrefacon" => "La première Attaque adverse de chaque tour ajoute une copie temporaire dans votre main.",
            "detournement" => "Lorsqu'un ennemi joue une carte de suivi, ajoutez-en une copie temporaire dans votre main.",
            "archivage" => "La première carte créée ou copiée obtenue chaque tour coûte 1 de moins.",
            "bureau_des_methodes" => "Jouer une carte créée ou copiée vous donne Vigueur 2.",
            "mimetisme_actif" => "Jouer une carte créée ou copiée applique Vulnérable 1 si c'est une Attaque, sinon Affaibli 1 à sa cible.",
            "chaine_de_montage" => "Votre première Compétence du tour donne 1 Bras mécatronique. À 3, gagnez 2 de Force et revenez à 0.",
            "fortification_mobile" => "Toutes les 3 Compétences jouées, gagnez Conservation d'armure et 1 Dextérité.",
            "memoire_musculaire" => "Toutes les 4 cartes jouées, gagnez 1 Dextérité.",
            "recurrence_pouvoir" => "La première carte de chaque tour gagne Écho 1.",
            "suite_geometrique" => "Chaque Attaque consécutive donne Vigueur 2 à la suivante. Une Compétence ou une Puissance remet la suite à zéro.",
            "discipline" => "Votre première Attaque de chaque tour gagne Vigueur 2.",
            "pont_suspendu" => "Après avoir joué une Attaque, conservez votre Armure jusqu'à votre prochain tour.",
            "permis_accelere" => "Votre première Compétence de chaque tour vous donne Célérité 1.",
            "milieu_reactif" => "Votre première Attaque de chaque tour applique Brûlure 1 à ses cibles.",
            "fondations_profondes" => "La première fois que vous gagnez de l'Armure chaque tour, gagnez Ancrage 1.",
            "armature_vivante" => "La première fois que vous gagnez de l'Armure chaque tour, gagnez 1 de Force. Subir des dégâts non bloqués retire 1 de Force.",
            "dossier_classe" => "La première carte que vous épuisez chaque tour donne 1 Énergie.",
            "structure_sacrificielle" => "La première fois que votre Armure se brise chaque tour, gagnez 1 Artefact.",
            "procedure_d_urgence" => "Quand votre Armure se brise, gagnez 1 Artefact et 1 Énergie.",
            "instinct_de_survie" => "La première fois que vous subissez des dégâts non bloqués chaque tour, gagnez 5 d'Armure.",
            "condensateur_pouvoir" => "La première fois que vous gagnez de l'Énergie chaque tour, gagnez Vigueur 2.",
            "circuit_de_secours" => "Quand votre Énergie atteint 0, gagnez 4 d'Armure.",
            "surcharge_controlee" => "Chaque fois que vous dépensez 3 Énergie, appliquez Affaibli 1 à un ennemi.",
            "derniere_reserve" => "Une fois par combat, quand votre Énergie atteint 0, gagnez 2 Énergie et Affaibli 1.",
            "catalyseur" => "Appliquer un debuff applique aussi Marque 1.",
            "reaction_en_chaine" => "Quand un ennemi atteint le maximum de Cristallisation, gagnez 1 Énergie.",
            "brouillage_permanent" => "Lorsqu'un ennemi gagne un buff, appliquez-lui Brouillage 2.",
            "fenetre_d_intervention" => "Une fois par tour, lorsqu'un ennemi gagne un buff, appliquez-lui Lenteur 1 et Vulnérable 1.",
            "beton_precontraint" => "Tant que vous avez de l'Armure, subissez 15% de dégâts en moins.",
            "liaison_instable" => "Les ennemis Marqués infligent 15% de dégâts en moins.",
            _ => $"Effet passif permanent de {Name}."
        };
        if (isPlayer)
            return description;
        return description
            .Replace("Vous donne", "Le personnage gagne")
            .Replace("vous donne", "le personnage gagne")
            .Replace("Votre", "La")
            .Replace("votre", "sa")
            .Replace("Vous", "Le personnage")
            .Replace("vous", "le personnage");
    }
}
