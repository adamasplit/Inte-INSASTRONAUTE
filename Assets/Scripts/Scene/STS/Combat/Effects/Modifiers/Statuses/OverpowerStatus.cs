using UnityEngine;

/// <summary>
/// Domination : une attaque entièrement bloquée repart une fois.
///
/// <para>« Entièrement bloquée » veut dire que l'armure a tout pris et qu'aucun point de vie n'est
/// tombé sur aucune des cibles — pas qu'il ne s'est rien passé. Une attaque qui n'a touché
/// personne, ou qui a fait ne serait-ce qu'un point à l'une de ses cibles, ne rejoue pas.</para>
///
/// <para>Le second coup est décidé par le moteur autoritatif, qui seul voit ce que la carte a
/// fait de chacune de ses cibles ; ce statut porte le cadre et la description.</para>
/// </summary>
public class OverpowerStatus : StatusEffect
{
    public OverpowerStatus()
    {
        Name = "Domination";
        Duration = -1;
        buff = true;
        framed = true;
    }

    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return "Lorsqu'une de vos attaques est entièrement bloquée par toutes ses cibles, elle les frappe une fois de plus.";
        }
        return "Lorsqu'une attaque de ce personnage est entièrement bloquée par toutes ses cibles, elle les frappe une fois de plus.";
    }
}
