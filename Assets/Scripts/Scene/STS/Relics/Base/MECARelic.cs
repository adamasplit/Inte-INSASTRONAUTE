using UnityEngine;
public class MECARelic:BaseRelic
{
    private int value;
    public MECARelic():base()
    {
        value=20;
        namesByStage[0] = "Autoréparation";
        descriptionsByStage[0] = $"Emmagasine les PV soignés. Lorsque vos PV atteignent 0, ces PV sont restitués et tous les ennemis en subissent la moitié. (Valeur actuelle : {value})";
        Upgrade(0);
    }
    private bool usedThisCombat=false;
    public override void OnCombatStart(Character target)
    {
        usedThisCombat=false;
    }
    public override int OnHeal(Character target, int amount)
    {
        // Annule le soin et stocke les PV à restituer
        value += amount;
        descriptionsByStage[0] = $"Emmagasine les PV soignés. Lorsque vos PV atteignent 0, ces PV sont restitués et tous les ennemis en subissent la moitié. (Valeur actuelle : {value})";
        Upgrade(stage); // Met à jour le nom et la description en fonction de la valeur actuelle
        return 0; // Retourne 0 pour annuler le soin
    }
    public override void OnDeath(Character target)
    {
        if (target.currentHP > 0 || usedThisCombat)
            return; // Ne s'active que si le personnage meurt et n'a pas déjà été utilisé ce combat

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
        value = 0; // Réinitialise la valeur après utilisation
        VFXManager.Instance.PlayEffect("MECARelicActivate", target);
        descriptionsByStage[0] = $"Emmagasine les PV soignés. Lorsque vos PV atteignent 0, ces PV sont restitués et tous les ennemis en subissent la moitié. (Valeur actuelle : {value})";
        Upgrade(stage); // Met à jour le nom et la description en fonction de la valeur actuelle
        usedThisCombat = true;
    }
}