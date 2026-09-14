using UnityEngine;

/// <summary>
/// Lunaire : les attaques du porteur passent outre l'Armure et les réductions de dégâts.
///
/// <para>Les deux défenses qu'un coup rencontre ne sont pas au même endroit — l'armure se
/// soustrait une fois le nombre arrêté, les réductions agissent sur le nombre lui-même — et
/// Lunaire écarte les deux.</para>
///
/// <para>Ce qui <em>aggrave</em> le coup reste en place : ignorer les réductions n'est pas ignorer
/// la cible, et une Vulnérabilité sur elle compte toujours.</para>
/// </summary>
public class LunaStatus : StatusEffect
{
    public LunaStatus(int duration)
    {
        Name = "Lunaire";
        Duration = duration;
        buff = true;
        framed = true;
    }

    public override string Desc(bool isPlayer)
    {
        string turns = $"{Duration} tour" + (Duration > 1 ? "s" : "");
        if (isPlayer)
        {
            return $"Vos attaques ignorent l'Armure et les réductions de dégâts de la cible "
                + $"pendant {turns}.";
        }
        return $"Les attaques de ce personnage ignorent l'Armure et les réductions de dégâts de "
            + $"la cible pendant {turns}.";
    }
}
