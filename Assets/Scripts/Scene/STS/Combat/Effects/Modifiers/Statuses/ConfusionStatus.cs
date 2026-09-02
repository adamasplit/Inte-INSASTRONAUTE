using UnityEngine;

/// <summary>
/// Confusion : quelle que soit la cible désignée, c'est une autre qui est frappée.
///
/// <para>Le tirage garde le bord de la cible voulue — une carte qui vise un allié en frappera un
/// autre, jamais un ennemi — faute de quoi une carte de soin confuse irait soigner l'adversaire,
/// ce qui n'est plus de la confusion mais une autre carte.</para>
///
/// <para>Le choix de la cible appartient au moteur autoritatif, qui seul décide qui est frappé ;
/// ce statut porte le cadre, le nom et la description que le client affiche. Cadre d'or, sans
/// valeur ni durée montrée, et parti de toute façon à la fin du tour.</para>
/// </summary>
public class ConfusionStatus : StatusEffect
{
    public ConfusionStatus()
    {
        Name = "Confusion";
        Duration = 1;
        debuff = true;
        framed = true;
        goldFrame = true;
        inextendable = true;
    }

    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return "Vos cartes frappent une cible au hasard jusqu'à la fin du tour.";
        }
        return "Les cartes de ce personnage frappent une cible au hasard jusqu'à la fin du tour.";
    }
}
