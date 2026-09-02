using UnityEngine;

/// <summary>
/// Fumigène : chaque statut positif qui s'éteint sur le porteur coûte 1 PV à un ennemi au hasard.
/// </summary>
public class SmokeStatus : StatusEffect
{
    public SmokeStatus()
    {
        Name = "Fumigène";
        Duration = -1;
        buff = true;
        framed = true;
    }

    /// <summary>Fait mordre le fumigène pour un statut positif qui vient de s'éteindre.</summary>
    public void OnBuffExpired(Character target)
    {
        if (target == null || target.combat == null)
            return;

        var candidates = target.combat.characters.FindAll(
            c => c != null && c.IsAlive && c.isPlayer != target.isPlayer);
        if (candidates.Count == 0)
            return;

        candidates[Random.Range(0, candidates.Count)].LoseHP(1);
    }

    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return "Chaque fois qu'un de vos statuts positifs expire, un ennemi au hasard perd 1 PV.";
        }
        return "Chaque fois qu'un statut positif de ce personnage expire, un ennemi au hasard perd 1 PV.";
    }
}
