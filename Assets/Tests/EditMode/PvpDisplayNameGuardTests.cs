using System;
using System.IO;
using NUnit.Framework;

/// <summary>
/// La garde de <c>RunManager.ApplyPvpParticipantDisplayNames</c>, lue dans la source.
///
/// <para><c>RunManager</c> est un <c>MonoBehaviour</c> et cet assembly de tests a
/// <c>noEngineReferences</c> : aucun test EditMode ne peut le construire. Cette garde-là
/// se lit donc dans le texte, exactement comme <c>ReactCombatBridgeJslibTests</c> lit
/// celui du <c>.jslib</c>. C'est une vérification faible — elle prouve quel champ décide,
/// pas ce que le jeu affiche — mais c'est la seule disponible, et elle aurait suffi.</para>
///
/// <para><b>Ce qu'elle épingle est un bug vivant, pas une préparation.</b> La garde lisait
/// <c>pvpBattleId</c>, écrit au matchmaking et effacé nulle part sauf en fin de run : la
/// première rencontre PvE jouée après une recherche de match affichait donc le pseudo de
/// l'adversaire PvP sur son premier ennemi.</para>
/// </summary>
public class PvpDisplayNameGuardTests
{
    private static string GuardOfApplyPvpParticipantDisplayNames()
    {
        string source = File.ReadAllText(Path.Combine(
            "Assets", "Scripts", "Scene", "STS", "Core", "RunManager.cs"));

        int method = source.IndexOf(
            "void ApplyPvpParticipantDisplayNames", StringComparison.Ordinal);
        Assert.That(method, Is.GreaterThanOrEqualTo(0),
            "RunManager.ApplyPvpParticipantDisplayNames is gone; this guard needs rewriting");

        int firstReturn = source.IndexOf("return;", method, StringComparison.Ordinal);
        Assert.That(firstReturn, Is.GreaterThan(method),
            "the early-out guard is gone; display names are now applied unconditionally");

        return WithoutComments(source.Substring(method, firstReturn - method));
    }

    /// Ce test juge le code, pas la prose : le commentaire qui explique la fuite la
    /// nomme, et il aurait sinon suffi a faire echouer la garde qu'il documente.
    private static string WithoutComments(string code)
    {
        var kept = new System.Text.StringBuilder();
        foreach (string line in code.Split('\n'))
        {
            int comment = line.IndexOf("//", StringComparison.Ordinal);
            kept.Append(comment >= 0 ? line.Substring(0, comment) : line).Append('\n');
        }
        return kept.ToString();
    }

    /// Seule la bataille effectivement jouée peut renommer un combattant.
    [Test]
    public void DisplayNamesFollowTheBattleBeingPlayed()
    {
        StringAssert.Contains("activePvpBattleId", GuardOfApplyPvpParticipantDisplayNames());
    }

    /// <c>pvpBattleId</c> ne retient que la dernière bataille annoncée par le matchmaking
    /// et survit jusqu'à la fin de la run. S'il redevenait la garde, la fuite reviendrait
    /// avec lui : c'est ce test-ci qui l'aurait attrapée.
    [Test]
    public void TheLastMatchmakingResultNoLongerDecides()
    {
        string guard = GuardOfApplyPvpParticipantDisplayNames();
        string withoutTheSessionField = guard.Replace("activePvpBattleId", string.Empty);

        Assert.That(withoutTheSessionField, Does.Not.Contain("pvpBattleId"),
            "the guard still reads the matchmaking memory instead of the battle being played");
    }
}
