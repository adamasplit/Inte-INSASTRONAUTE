using System;
using System.Collections.Generic;

/// <summary>
/// Ce qu'on montre au joueur quand le serveur refuse sa commande.
///
/// <para>Le moteur nomme ses refus ; le client les recevait et n'en faisait rien, si bien
/// qu'une carte refusée se contentait de ne pas bouger. Les huit codes sont ceux du
/// moteur, pas ceux du transport : ils décrivent une règle du jeu, et se traduisent donc
/// en une phrase de jeu.</para>
///
/// <para>Un code inconnu obtient le message générique plutôt qu'un vide : le moteur peut
/// en gagner un demain, et un refus muet est le défaut qu'on retire.</para>
/// </summary>
public static class CombatRejectionMessages
{
    public const string Generic = "Le serveur a refusé cette action.";

    private static readonly Dictionary<string, string> MessagesByCode =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["INSUFFICIENT_ENERGY"] = "Pas assez d'énergie.",
            ["CARD_NOT_IN_HAND"] = "Cette carte n'est plus dans votre main.",
            ["INVALID_TARGET"] = "Cible invalide.",
            ["NOT_ACTOR_TURN"] = "Ce n'est pas votre tour.",
            ["OUT_OF_SYNC"] = "Synchronisation en cours…",
            ["COMBAT_NOT_FOUND"] = "Ce combat n'existe plus.",
            ["INVALID_COMMAND"] = "Action impossible.",
            ["INTERNAL_ERROR"] = "Erreur du serveur.",
        };

    private static readonly List<string> Codes = new List<string>(MessagesByCode.Keys);

    public static IReadOnlyList<string> KnownCodes => Codes;

    public static string ForCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Generic;

        return MessagesByCode.TryGetValue(code, out string message) ? message : Generic;
    }

    /// <summary>
    /// Le seul refus qui a déjà son langage visuel : le compteur d'énergie qui rougit,
    /// que le refus local utilise depuis toujours.
    /// </summary>
    public static bool WarrantsEnergyGlow(string code)
    {
        return string.Equals(code, "INSUFFICIENT_ENERGY", StringComparison.Ordinal);
    }
}
