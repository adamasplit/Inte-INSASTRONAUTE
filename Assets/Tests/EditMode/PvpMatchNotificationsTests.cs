using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class PvpMatchNotificationsTests
{
    /// La forme du serveur, telle qu'elle arrive : une liste de notifications dont
    /// `payload` dépend du `type`.
    private const string MatchFound = @"[
        { ""id"": ""n-1"", ""type"": ""QUICK_MATCH_FOUND"", ""read"": false,
          ""createdAt"": ""2026-08-24T10:00:00Z"",
          ""payload"": { ""battleId"": ""b-42"", ""friendly"": false } }
    ]";

    [Test]
    public void AMatchNamesItsBattleAndTheNotificationToAcknowledge()
    {
        PvpMatchNotification match = PvpMatchNotifications.FindQuickMatch(JArray.Parse(MatchFound));

        Assert.That(match.Found, Is.True);
        Assert.That(match.BattleId, Is.EqualTo("b-42"));
        Assert.That(match.NotificationId, Is.EqualTo("n-1"));
    }

    /// Le test qui compte. Les autres types nomment des batailles eux aussi — celle qu'on
    /// vient de finir, celle qu'on nous propose. Lire le premier `battleId` venu ferait
    /// rentrer le joueur dans un combat terminé au lieu de le laisser chercher.
    [Test]
    public void TheOtherTypesNameBattlesToo_AndNoneOfThemIsOurs()
    {
        string others = @"[
            { ""id"": ""n-1"", ""type"": ""BATTLE_UPDATED"", ""read"": false,
              ""payload"": { ""battleId"": ""b-finished"" } },
            { ""id"": ""n-2"", ""type"": ""CHALLENGE_RECEIVED"", ""read"": false,
              ""payload"": { ""battleId"": ""b-offered"" } },
            { ""id"": ""n-3"", ""type"": ""CHALLENGE_DECLINED"", ""read"": false,
              ""payload"": { ""battleId"": ""b-refused"" } },
            { ""id"": ""n-4"", ""type"": ""INFO"", ""read"": false,
              ""payload"": { ""battleId"": ""b-whatever"" } }
        ]";

        Assert.That(PvpMatchNotifications.FindQuickMatch(JArray.Parse(others)).Found, Is.False);
    }

    /// Une notification lue est une notification agie : c'est ce qui fait que
    /// l'acquittement suffit à ne plus jamais revenir dans la bataille qu'on vient de
    /// rejoindre.
    [Test]
    public void AnAcknowledgedMatchIsNotAMatchAnyMore()
    {
        string acknowledged = @"[
            { ""id"": ""n-1"", ""type"": ""QUICK_MATCH_FOUND"", ""read"": true,
              ""payload"": { ""battleId"": ""b-42"" } }
        ]";

        Assert.That(PvpMatchNotifications.FindQuickMatch(JArray.Parse(acknowledged)).Found, Is.False);
    }

    /// Si un appariement passé n'a pas pu être acquitté, c'est le plus récent qui doit
    /// gagner : celui qu'on attend est celui qui vient d'arriver.
    [Test]
    public void TheMostRecentMatchWins()
    {
        string two = @"[
            { ""id"": ""n-old"", ""type"": ""QUICK_MATCH_FOUND"", ""read"": false,
              ""createdAt"": ""2026-08-24T09:00:00Z"",
              ""payload"": { ""battleId"": ""b-old"" } },
            { ""id"": ""n-new"", ""type"": ""QUICK_MATCH_FOUND"", ""read"": false,
              ""createdAt"": ""2026-08-24T10:00:00Z"",
              ""payload"": { ""battleId"": ""b-new"" } }
        ]";

        string reversed = @"[
            { ""id"": ""n-new"", ""type"": ""QUICK_MATCH_FOUND"", ""read"": false,
              ""createdAt"": ""2026-08-24T10:00:00Z"",
              ""payload"": { ""battleId"": ""b-new"" } },
            { ""id"": ""n-old"", ""type"": ""QUICK_MATCH_FOUND"", ""read"": false,
              ""createdAt"": ""2026-08-24T09:00:00Z"",
              ""payload"": { ""battleId"": ""b-old"" } }
        ]";

        // Dans les deux ordres : la date tranche, pas la position dans la liste.
        Assert.That(PvpMatchNotifications.FindQuickMatch(JArray.Parse(two)).BattleId,
            Is.EqualTo("b-new"));
        Assert.That(PvpMatchNotifications.FindQuickMatch(JArray.Parse(reversed)).BattleId,
            Is.EqualTo("b-new"));
    }

    /// Un appariement sans bataille n'est pas une porte d'entrée : il ne faut pas charger
    /// la scène de combat sur un identifiant vide.
    [Test]
    public void AMatchWithoutABattleIsNotSomethingToEnter()
    {
        string noBattle = @"[
            { ""id"": ""n-1"", ""type"": ""QUICK_MATCH_FOUND"", ""read"": false,
              ""payload"": { ""friendly"": true } },
            { ""id"": ""n-2"", ""type"": ""QUICK_MATCH_FOUND"", ""read"": false }
        ]";

        Assert.That(PvpMatchNotifications.FindQuickMatch(JArray.Parse(noBattle)).Found, Is.False);
    }

    [Test]
    public void NothingnessIsNoMatch()
    {
        Assert.That(PvpMatchNotifications.FindQuickMatch(null).Found, Is.False);
        Assert.That(PvpMatchNotifications.FindQuickMatch(JArray.Parse("[]")).Found, Is.False);
        Assert.That(PvpMatchNotifications.FindQuickMatch(JObject.Parse("{}")).Found, Is.False);
        Assert.That(PvpMatchNotification.None.Found, Is.False);
    }

    /// La liste peut arriver nue ou paginée ; l'enveloppe du pont React, elle, est déjà
    /// défaite par `STSApiClient`.
    [Test]
    public void AWrappedListIsStillAList()
    {
        Assert.That(
            PvpMatchNotifications.FindQuickMatch(
                JObject.Parse(@"{ ""notifications"": " + MatchFound + " }")).BattleId,
            Is.EqualTo("b-42"));

        Assert.That(
            PvpMatchNotifications.FindQuickMatch(
                JObject.Parse(@"{ ""content"": " + MatchFound + @", ""totalElements"": 1 }")).BattleId,
            Is.EqualTo("b-42"));
    }

    /// Le joueur qui a reçu son battleId directement doit acquitter lui aussi, sans quoi
    /// sa notification non lue le ramènerait dans ce combat terminé à sa prochaine
    /// recherche. Il n'a que le battleId pour la retrouver.
    [Test]
    public void EveryUnreadMatchForOneBattleCanBeFoundByItsBattleId()
    {
        string mixed = @"[
            { ""id"": ""n-1"", ""type"": ""QUICK_MATCH_FOUND"", ""read"": false,
              ""payload"": { ""battleId"": ""b-42"" } },
            { ""id"": ""n-2"", ""type"": ""QUICK_MATCH_FOUND"", ""read"": true,
              ""payload"": { ""battleId"": ""b-42"" } },
            { ""id"": ""n-3"", ""type"": ""QUICK_MATCH_FOUND"", ""read"": false,
              ""payload"": { ""battleId"": ""b-other"" } },
            { ""id"": ""n-4"", ""type"": ""BATTLE_UPDATED"", ""read"": false,
              ""payload"": { ""battleId"": ""b-42"" } }
        ]";

        Assert.That(PvpMatchNotifications.QuickMatchIdsForBattle(JArray.Parse(mixed), "b-42"),
            Is.EqualTo(new[] { "n-1" }));
        Assert.That(PvpMatchNotifications.QuickMatchIdsForBattle(JArray.Parse(mixed), null),
            Is.Empty);
        Assert.That(PvpMatchNotifications.QuickMatchIdsForBattle(null, "b-42"), Is.Empty);
    }
}
