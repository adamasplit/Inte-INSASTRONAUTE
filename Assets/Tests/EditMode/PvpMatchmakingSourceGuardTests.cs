using System;
using System.IO;
using NUnit.Framework;

public class PvpMatchmakingSourceGuardTests
{
    private static string ControllerSource() => File.ReadAllText(Path.Combine(
        "Assets", "Scripts", "Scene", "STS", "UI", "MultiplayerMenuController.cs"));

    [Test]
    public void CancellingSearchAlsoLeavesTheServerQueue()
    {
        StringAssert.Contains("CancelQuickMatchPvpAsync", ControllerSource());
        StringAssert.Contains("if (!cancelled", ControllerSource());
        StringAssert.Contains("matchedBattleId", ControllerSource());
    }

    [Test]
    public void WaitingSearchSendsHeartbeatsAndCancelsOnTeardown()
    {
        string source = ControllerSource();
        StringAssert.Contains("HeartbeatQuickMatchPvpAsync", source);
        StringAssert.Contains("void OnDestroy()", source);
    }

    [Test]
    public void EnteringOneBattleLoadsTheCombatSceneOnlyOnce()
    {
        string source = ControllerSource();
        int start = source.IndexOf("Task EnterPvpBattleAsync", StringComparison.Ordinal);
        int end = source.IndexOf("Task AcknowledgeMatchNotificationsAsync", start, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0));
        Assert.That(end, Is.GreaterThan(start));

        string method = source.Substring(start, end - start);
        Assert.That(Count(method, "LoadScene(\"STS_Combat\")"), Is.EqualTo(1));
    }

    [Test]
    public void ChallengeTargetsComeFromTheAcceptedFriendsApi()
    {
        string source = ControllerSource();
        StringAssert.Contains("ListFriendsAsync", source);
        StringAssert.Contains("selectedChallengeFriend", source);
    }

    private static int Count(string source, string needle)
    {
        int count = 0;
        int cursor = 0;
        while ((cursor = source.IndexOf(needle, cursor, StringComparison.Ordinal)) >= 0)
        {
            count++;
            cursor += needle.Length;
        }
        return count;
    }
}
