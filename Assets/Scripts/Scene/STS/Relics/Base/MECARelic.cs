using UnityEngine;

/// <summary>
/// Autoréparation : les soins reçus alimentent une réserve qui relève son porteur.
///
/// <para>La réserve ne retenait plus le soin : le porteur ne regagnait pas un point de vie de
/// tout le combat et n'avait en échange qu'une réserve qu'il ne touchait qu'en mourant. Le soin
/// passe maintenant normalement <em>et</em> alimente la réserve, ce qui fait de la relique un
/// bonus plutôt qu'un marché.</para>
///
/// <para>Elle ne compte plus non plus les fois où elle a servi : ce qu'elle a rendu, elle ne l'a
/// plus, et la relever une seconde fois se paie en soins. Un combat long où l'on se soigne
/// beaucoup peut donc la voir servir plusieurs fois.</para>
/// </summary>
public class MECARelic:BaseRelic
{
    private int value;
    public MECARelic():base()
    {
        value=20;
        namesByStage[0] = "Autoréparation";
        RefreshDescription();
        Upgrade(0);
    }

    private void RefreshDescription()
    {
        descriptionsByStage[0] = $"Les PV que vous récupérez en combat s'ajoutent à une réserve. Lorsque vos PV atteignent 0, cette réserve vous est rendue et tous les ennemis en subissent la moitié. (Valeur actuelle : {value})";
    }

    public override int OnHeal(Character target, int amount)
    {
        // Le soin compte pour la réserve et se rend quand même : la relique n'échange plus l'un
        // contre l'autre.
        value += amount;
        RefreshDescription();
        Upgrade(stage); // Met à jour le nom et la description en fonction de la valeur actuelle
        return amount;
    }
    public override void OnDeath(Character target)
    {
        if (target.currentHP > 0)
            return; // Ne s'active que si le personnage meurt

        if (value <= 0)
            return; // Ne s'active que s'il y a des PV à restituer

        // Restitue les PV et inflige des dégâts aux ennemis
        int healAmount = value;
        target.currentHP = Mathf.Min(target.maxHP, target.currentHP + healAmount);
        var combat = target.combat;
        foreach (var enemy in combat.enemies)
        {
            enemy.TakeDamage(healAmount / 2, ignoreArmor: true);
        }
        // Vider la réserve est la seule chose qui empêche la relique de relever son porteur
        // indéfiniment : la fois suivante se paie en soins.
        value = 0;
        VFXManager.Instance.PlayEffect("MECARelicActivate", target);
        RefreshDescription();
        Upgrade(stage); // Met à jour le nom et la description en fonction de la valeur actuelle
    }
}
