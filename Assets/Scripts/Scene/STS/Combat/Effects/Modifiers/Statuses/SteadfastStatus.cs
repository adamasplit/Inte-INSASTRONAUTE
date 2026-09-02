using UnityEngine;

/// <summary>
/// Ancrage : l'armure du porteur ne peut plus être détruite d'un coup.
///
/// <para>Ne protège que de la destruction instantanée — l'effet Break. On peut toujours user
/// l'armure coup par coup : ce que le statut refuse, c'est qu'elle disparaisse d'un seul
/// geste.</para>
/// </summary>
public class SteadfastStatus : StatusEffect
{
    public SteadfastStatus(int duration)
    {
        Duration = duration;
        Name = "Ancrage";
        buff = true;
        framed = true;
    }

    public override void OnArmorLost(Character target, ref int armor)
    {
        // Une destruction instantanée emporte toute l'armure d'un coup ; c'est celle-là qu'on
        // refuse. Une perte partielle passe comme d'habitude.
        if (target != null && target.armor > 0 && armor >= target.armor)
            armor = 0;
    }

    public override string Desc(bool isPlayer)
    {
        string turns = $"{Duration} tour" + (Duration > 1 ? "s" : "");
        if (isPlayer)
        {
            return $"Votre Armure ne peut pas être détruite instantanément pendant {turns}.";
        }
        return $"L'Armure de ce personnage ne peut pas être détruite instantanément pendant {turns}.";
    }
}
