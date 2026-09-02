using UnityEngine;

/// <summary>
/// Brouillage : le porteur ne peut plus retirer de statut, ni sur lui ni sur autrui.
///
/// <para>Le refus porte sur celui qui dissipe, jamais sur celui qui est dissipé : ce que le
/// statut empêche, c'est de retirer, pas d'être retiré. Il vaut donc aussi pour le porteur qui
/// voudrait se nettoyer lui-même.</para>
/// </summary>
public class DispelBlockStatus : StatusEffect
{
    public DispelBlockStatus(int duration)
    {
        Duration = duration;
        Name = "Brouillage";
        debuff = true;
        framed = true;
    }

    public override string Desc(bool isPlayer)
    {
        string turns = $"{Duration} tour" + (Duration > 1 ? "s" : "");
        if (isPlayer)
        {
            return $"Vous ne pouvez retirer aucun statut, sur personne, pendant {turns}.";
        }
        return $"Ce personnage ne peut retirer aucun statut, sur personne, pendant {turns}.";
    }
}
