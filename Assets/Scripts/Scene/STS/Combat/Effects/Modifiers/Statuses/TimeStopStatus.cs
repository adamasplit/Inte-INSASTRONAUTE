using UnityEngine;
public class TimeStopStatus : StatusEffect
{
    private int accumulatedDamage = 0;
    private bool releasing = false;

    public TimeStopStatus(int duration)
    {
        Duration = duration;
        Name = "Arrêt du temps";
        debuff = true;
        framed = true;
    }
    // Blocks actual HP loss instead of altering the Damage modifier, so damage previews stay accurate.
    public override int ValidateHPLoss(int damage, Character target)
    {
        if (releasing || damage <= 0)
            return damage;
        accumulatedDamage += damage;
        return 0;
    }
    public override void OnExpire(Character target)
    {
        base.OnExpire(target);
        if (accumulatedDamage <= 0)
            return;
        releasing = true;
        target.TakeDamage(Mathf.CeilToInt(accumulatedDamage * 1.5f), true);
        releasing = false;
        accumulatedDamage = 0;
    }
    public override string Desc(bool isPlayer)
    {
        string turns = $"{Duration} tour" + (Duration > 1 ? "s" : "");
        if (isPlayer)
        {
            return $"Vous ne perdez pas de PV pendant {turns}, mais les dégâts accumulés vous seront infligés (×1.5) à la fin de votre prochain tour.";
        }
        return $"Le personnage ne perd pas de PV pendant {turns}, mais les dégâts accumulés lui seront infligés (×1.5) à la fin de son prochain tour.";
    }
}
