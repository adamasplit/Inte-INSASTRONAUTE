using UnityEngine;

/// <summary>
/// Ferveur : les soins reçus sont retenus, puis rendus au double quand elle s'en va.
///
/// <para>Le pendant exact de <see cref="TimeStopStatus"/>, qui fait la même chose des dégâts.
/// Le multiplicateur est plus généreux que le sien : retenir un soin est un pari, pas une
/// punition, et il faut qu'il vaille la peine d'attendre.</para>
/// </summary>
public class FervorStatus : StatusEffect
{
    public const int Multiplier = 2;

    private int accumulatedHeal = 0;
    private bool releasing = false;

    public FervorStatus(int duration)
    {
        Duration = duration;
        Name = "Ferveur";
        buff = true;
        framed = true;
    }

    public override void OnHeal(Character target, ref int healAmount)
    {
        if (releasing || healAmount <= 0)
            return;
        accumulatedHeal += healAmount;
        healAmount = 0;
    }

    public override void OnExpire(Character target)
    {
        base.OnExpire(target);
        if (accumulatedHeal <= 0)
            return;
        releasing = true;
        target.Heal(accumulatedHeal * Multiplier);
        releasing = false;
        accumulatedHeal = 0;
    }

    public override string Desc(bool isPlayer)
    {
        string turns = $"{Duration} tour" + (Duration > 1 ? "s" : "");
        if (isPlayer)
        {
            return $"Vous ne recevez pas de soin pendant {turns}, mais les soins accumulés vous seront rendus (×{Multiplier}) à la fin.";
        }
        return $"Le personnage ne reçoit pas de soin pendant {turns}, mais les soins accumulés lui seront rendus (×{Multiplier}) à la fin.";
    }
}
