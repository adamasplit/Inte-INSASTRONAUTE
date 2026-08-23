using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class CombatantSnapshotReaderTests
{
    /// <summary>
    /// The ids and teams below are the ones the server actually emits in PvE:
    /// StsAuthoritativeCombatService uses "player"/"player" for the human and
    /// "enemy-{index}"/"enemy" for each entry of activeEncounter.enemyIds.
    /// </summary>
    private const string PveSnapshot = @"{
        ""combatants"": [
            { ""combatantId"": ""player"",  ""teamId"": ""player"",
              ""controllerType"": ""HUMAN"", ""hp"": 60 },
            { ""combatantId"": ""enemy-0"", ""teamId"": ""enemy"",
              ""controllerType"": ""AI"",   ""hp"": 0 },
            { ""combatantId"": ""enemy-1"", ""teamId"": ""enemy"",
              ""controllerType"": ""AI"",   ""hp"": 12 }
        ]
    }";

    [Test]
    public void ReadsEveryCombatantInSnapshotOrder()
    {
        IReadOnlyList<CombatantDescriptor> combatants =
            CombatantSnapshotReader.ReadCombatants(JObject.Parse(PveSnapshot), "player");

        Assert.That(combatants, Has.Count.EqualTo(3));
        Assert.That(combatants[0].CombatantId, Is.EqualTo("player"));
        Assert.That(combatants[1].CombatantId, Is.EqualTo("enemy-0"));
        Assert.That(combatants[2].CombatantId, Is.EqualTo("enemy-1"));
    }

    /// <summary>
    /// A dead combatant stays in the server's state with hp 0 and must stay registered:
    /// its presence is what stops its neighbours' identities from shifting. Cf. spec §3.3.
    /// </summary>
    [Test]
    public void KeepsDeadCombatantsSoThatIdentitiesDoNotShift()
    {
        IReadOnlyList<CombatantDescriptor> combatants =
            CombatantSnapshotReader.ReadCombatants(JObject.Parse(PveSnapshot), "player");

        Assert.That(combatants[1].CombatantId, Is.EqualTo("enemy-0"));
    }

    [Test]
    public void ReadsTeamAndControllerType()
    {
        IReadOnlyList<CombatantDescriptor> combatants =
            CombatantSnapshotReader.ReadCombatants(JObject.Parse(PveSnapshot), "player");

        Assert.That(combatants[0].TeamId, Is.EqualTo("player"));
        Assert.That(combatants[0].Controller, Is.EqualTo(CombatantController.Human));
        Assert.That(combatants[2].TeamId, Is.EqualTo("enemy"));
        Assert.That(combatants[2].Controller, Is.EqualTo(CombatantController.Ai));
    }

    [Test]
    public void MarksExactlyTheLocalCombatant()
    {
        IReadOnlyList<CombatantDescriptor> combatants =
            CombatantSnapshotReader.ReadCombatants(JObject.Parse(PveSnapshot), "player");

        Assert.That(combatants[0].IsLocal, Is.True);
        Assert.That(combatants[1].IsLocal, Is.False);
        Assert.That(combatants[2].IsLocal, Is.False);
    }

    [Test]
    public void TreatsAnUnknownControllerTypeAsAi()
    {
        // A combatant with no stated controller is not the local player: assuming a
        // human would hand it the turn. AI is the safe default.
        string snapshot = @"{ ""combatants"": [
            { ""combatantId"": ""enemy-0"", ""teamId"": ""enemy"" } ] }";

        IReadOnlyList<CombatantDescriptor> combatants =
            CombatantSnapshotReader.ReadCombatants(JObject.Parse(snapshot), "player");

        Assert.That(combatants[0].Controller, Is.EqualTo(CombatantController.Ai));
    }

    [Test]
    public void SkipsMalformedCombatantsRatherThanInventingThem()
    {
        string snapshot = @"{ ""combatants"": [
            { ""teamId"": ""enemy"", ""controllerType"": ""AI"" },
            { ""combatantId"": ""enemy-1"", ""controllerType"": ""AI"" },
            { ""combatantId"": ""enemy-2"", ""teamId"": ""enemy"",
              ""controllerType"": ""AI"" } ] }";

        IReadOnlyList<CombatantDescriptor> combatants =
            CombatantSnapshotReader.ReadCombatants(JObject.Parse(snapshot), "player");

        Assert.That(combatants, Has.Count.EqualTo(1));
        Assert.That(combatants[0].CombatantId, Is.EqualTo("enemy-2"));
    }

    [Test]
    public void ReturnsNothingForAnEmptyOrShapelessSnapshot()
    {
        Assert.That(CombatantSnapshotReader.ReadCombatants(null, "player"), Is.Empty);
        Assert.That(
            CombatantSnapshotReader.ReadCombatants(JObject.Parse("{}"), "player"),
            Is.Empty);
    }
}
