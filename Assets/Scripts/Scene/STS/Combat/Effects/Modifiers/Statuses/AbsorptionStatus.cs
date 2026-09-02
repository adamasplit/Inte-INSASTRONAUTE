using UnityEngine;

/// <summary>
/// Absorption : la moitié de chaque perte de PV est reportée à la fin du tour.
///
/// <para>Ce n'est pas une réduction : ce qui est retenu est rendu, d'un coup, quand le tour se
/// ferme. Le compteur est vidé au passage, faute de quoi la même dette serait payée à chaque
/// tour suivant.</para>
/// </summary>
public class AbsorptionStatus : StatusEffect
{
    public const int Share = 2;

    private int deferred = 0;
    private bool releasing = false;

    public AbsorptionStatus()
    {
        Name = "Absorption";
        Duration = -1;
        buff = true;
        framed = true;
    }

    public override int ValidateHPLoss(int damage, Character target)
    {
        if (releasing || damage <= 0)
            return damage;
        int held = damage / Share;
        deferred += held;
        return damage - held;
    }

    public override void OnTurnEnd(Character target)
    {
        base.OnTurnEnd(target);
        if (deferred <= 0)
            return;
        releasing = true;
        target.TakeDamage(deferred, true);
        releasing = false;
        deferred = 0;
    }

    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return "La moitié des PV que vous perdez vous est infligée à la fin de votre tour à la place.";
        }
        return "La moitié des PV que ce personnage perd lui est infligée à la fin de son tour à la place.";
    }
}
