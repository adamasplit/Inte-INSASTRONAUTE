using UnityEngine;

/// <summary>
/// Adaptation : le porteur se règle sur ce que joue le camp d'en face.
///
/// <para>Une Compétence adverse lui donne 1 de Force, une Attaque 1 de Dextérité, un Pouvoir
/// 1 de Vitesse. Elle ne répond jamais à ce que joue son propre porteur : ce serait trois
/// caractéristiques par tour sans que personne n'y soit pour rien.</para>
/// </summary>
public class AdaptationStatus : StatusEffect
{
    public AdaptationStatus()
    {
        Name = "Adaptation";
        Duration = -1;
        buff = true;
        framed = true;
    }

    public override void OnAnyCardPlayed(Character source, CardInstance card)
    {
        if (owner == null || source == null || card == null)
            return;
        if (source.isPlayer == owner.isPlayer)
            return;

        StatusEffect gained = card.Type switch
        {
            CardType.Compétence => new StrengthStatus(1),
            CardType.Attaque => new DexterityStatus(1),
            CardType.Pouvoir => new SpeedStatus(1),
            _ => null
        };
        if (gained != null)
            owner.AddStatus(gained);
    }

    public override string Desc(bool isPlayer)
    {
        string who = isPlayer ? "Vous gagnez" : "Le personnage gagne";
        return $"{who} 1 de Force lorsqu'un adversaire joue une Compétence, 1 de Dextérité pour une Attaque et 1 de Vitesse pour un Pouvoir.";
    }
}
