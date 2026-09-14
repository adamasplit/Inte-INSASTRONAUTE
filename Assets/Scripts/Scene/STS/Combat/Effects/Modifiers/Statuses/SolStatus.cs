using UnityEngine;

/// <summary>
/// Solaire : la moitié de ce que le porteur inflige lui revient en soin.
///
/// <para>Se prend sur ce que la carte a réellement porté, une fois les dégâts calculés — pas sur
/// ce qu'elle annonçait. Le moteur autoritatif le fait passer par le même chemin que le vol de vie
/// d'un enchantement, si bien qu'un porteur qui a les deux cumule.</para>
/// </summary>
public class SolStatus : StatusEffect
{
    public SolStatus(int duration)
    {
        Name = "Solaire";
        Duration = duration;
        buff = true;
        framed = true;
    }

    public override string Desc(bool isPlayer)
    {
        string turns = $"{Duration} tour" + (Duration > 1 ? "s" : "");
        if (isPlayer)
        {
            return $"Vous êtes soigné de la moitié des dégâts que vous infligez pendant {turns}.";
        }
        return $"Ce personnage est soigné de la moitié des dégâts qu'il inflige pendant {turns}.";
    }
}
