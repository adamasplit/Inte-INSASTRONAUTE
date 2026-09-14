using UnityEngine;

/// <summary>
/// Chaos : son porteur ne blesse plus personne, mais tout ce qu'il inflige pèse le double.
///
/// <para>Les deux moitiés vont ensemble : on renonce aux dégâts pour que tout le reste parte
/// double. Le doublement se lit sur le bord visé et non sur l'intention — tout effet dirigé vers
/// l'autre camp compte, statut posé, tour repoussé ou avancé, PV drainés, et jusqu'au soin qu'on
/// irait faire à l'adversaire. Ce que le porteur se réserve garde sa taille.</para>
///
/// <para>Les dégâts en sont exclus, puisqu'ils sont déjà supprimés, et l'armure aussi : Chaos ne
/// récompense pas d'en donner en face. Une durée permanente n'est pas un compte et ne double pas
/// — deux fois un permanent reste un permanent — et un statut qui ne lit pas sa durée laisse de
/// toute façon tomber celle qu'on lui double.</para>
///
/// <para>Le moteur autoritatif décide des deux moitiés — la remise à zéro des dégâts comme le
/// doublement du reste ; ce statut porte le cadre, le nom et la description que le client
/// affiche.</para>
/// </summary>
public class ChaoticStatus : StatusEffect
{
    public ChaoticStatus()
    {
        Name = "Chaos";
        Duration = -1;
        buff = true;
        framed = true;
    }

    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return "Vous n'infligez plus de dégâts, mais tous vos autres effets visant un "
                + "adversaire sont 2 fois plus puissants.";
        }
        return "Ce personnage n'inflige plus de dégâts, mais tous ses autres effets visant un "
            + "adversaire sont 2 fois plus puissants.";
    }
}
