using System;

/// <summary>
/// Ce que le client est en train de jouer, dit explicitement.
///
/// <para>Jusqu'ici le mode se déduisait d'un effet de bord : le client était autoritatif
/// « parce qu'un état était arrivé » (<c>UsesAuthoritativeCombat</c> lisait
/// <c>RunManager.activeCombat</c>, champ que l'application d'état venait d'écrire). Ça
/// tenait tant qu'il n'existait qu'une seule sorte de combat distant. Un duel, lui, doit
/// être autoritatif avant d'avoir reçu quoi que ce soit — sinon sa première frappe
/// partirait dans le moteur local.</para>
/// </summary>
public enum CombatMode
{
    /// Aucun serveur ne tranche : le tutoriel, et lui seul aujourd'hui.
    Local,

    /// Un combat de run, arbitré par le serveur, adressé par le runId.
    Pve,

    /// Un duel, arbitré par le serveur, adressé par le battleId.
    Pvp
}

public static class CombatModes
{
    public const string PveWireName = "PVE";
    public const string PvpWireName = "PVP";

    /// <summary>
    /// Le nom que le pont React lit pour choisir ses destinations. Il n'y en a que deux,
    /// et un combat local n'en a aucun.
    /// </summary>
    public static string ToWireName(CombatMode mode)
    {
        switch (mode)
        {
            case CombatMode.Pve: return PveWireName;
            case CombatMode.Pvp: return PvpWireName;
            default: throw new ArgumentOutOfRangeException(
                nameof(mode), "A local combat has no server mode");
        }
    }

    public static CombatMode? Parse(string wireName)
    {
        if (string.IsNullOrWhiteSpace(wireName))
            return null;

        switch (wireName.Trim().ToUpperInvariant())
        {
            case PveWireName: return CombatMode.Pve;
            case PvpWireName: return CombatMode.Pvp;
            default: return null;
        }
    }
}
