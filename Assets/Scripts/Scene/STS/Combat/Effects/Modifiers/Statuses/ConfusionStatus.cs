using UnityEngine;

/// <summary>
/// Confusion : le porteur ne joue plus son tour, sa main part toute seule.
///
/// <para>Le moteur autoritatif joue les cartes à sa place, tirées au hasard, jusqu'à ce que son
/// énergie ne paie plus rien (CombatOrchestrator / ConfusedTurn côté serveur). Ce n'est donc plus
/// seulement une visée égarée : c'est le tour entier qui lui est retiré.</para>
///
/// <para>Le tirage ne connaît pas les bords : il prend n'importe quel combattant debout, le
/// lanceur compris. Une attaque confuse peut donc revenir sur celui qui la joue, et un soin confus
/// partir à l'adversaire. Il gardait autrefois le bord de la cible voulue, mais en duel, où chaque
/// bord ne compte qu'une tête, ce tirage n'avait rien à choisir et le statut ne faisait rien.</para>
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
        generic = true;
    }

    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return "A votre tour, vous jouez des cartes au hasard sur une cible au hasard.";
        }
        return "Au tour de ce personnage, il joue des cartes au hasard sur une cible au hasard.";
    }
}
