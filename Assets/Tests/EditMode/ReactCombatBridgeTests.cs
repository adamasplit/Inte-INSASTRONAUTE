using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class ReactCombatBridgeTests
{
    private const string Snapshot =
        "{\"protocolVersion\":1,\"combatId\":\"combat-1\",\"revision\":\"42\",\"type\":\"COMBAT_SNAPSHOT\",\"payload\":{}}";

    private static string Event(string revision, string actionId = null, string type = "CARD_PLAYED")
    {
        string causation = actionId == null ? "" : ",\"causationActionId\":\"" + actionId + "\"";
        return "{\"protocolVersion\":1,\"combatId\":\"combat-1\",\"revision\":\"" + revision
            + "\",\"type\":\"" + type + "\"" + causation + ",\"payload\":{}}";
    }

    [Test]
    public void AuthoritativeConnectionUsesRunIdAsTransportId()
    {
        JToken activeCombat = JToken.Parse(
            "{\"combatId\":\"combat-713d\",\"revision\":0}");

        Assert.That(
            AuthoritativeCombatIdentity.GetTransportId("run-42", activeCombat),
            Is.EqualTo("run-42"));
    }

    [Test]
    public void SnapshotThenConsecutiveEventRaisesGameplayEvents()
    {
        var core = new ReactCombatBridgeCore(() => "action-1");
        int delivered = 0;
        core.CombatEventReceived += _ => delivered++;
        core.Connect("combat-1");

        Assert.That(core.HandleCombatEvent(Snapshot), Is.True);
        Assert.That(core.HandleCombatEvent(Event("43")), Is.True);
        Assert.That(core.CurrentRevision, Is.EqualTo("43"));
        Assert.That(delivered, Is.EqualTo(2));
    }

    [Test]
    public void DuplicateStaleAndGapEventsAreNotDelivered()
    {
        var core = new ReactCombatBridgeCore(() => "action-1");
        int delivered = 0;
        core.CombatEventReceived += _ => delivered++;
        core.Connect("combat-1");
        core.HandleCombatEvent(Snapshot);

        Assert.That(core.HandleCombatEvent(Event("42")), Is.False);
        Assert.That(core.HandleCombatEvent(Event("41")), Is.False);
        Assert.That(core.HandleCombatEvent(Event("44")), Is.False);
        Assert.That(delivered, Is.EqualTo(1));
        Assert.That(core.CurrentRevision, Is.EqualTo("42"));
    }

    [Test]
    public void CommandContainsVersionIdentityRevisionTypeAndPayload()
    {
        var core = new ReactCombatBridgeCore(() => "action-fixed");
        core.Connect("combat-1");
        core.HandleCombatEvent(Snapshot);

        ReactCombatCommand command = core.CreateCommand("END_TURN", new { reason = "PLAYER" });

        Assert.That(command.ActionId, Is.EqualTo("action-fixed"));
        StringAssert.Contains("\"protocolVersion\":1", command.Json);
        StringAssert.Contains("\"actionId\":\"action-fixed\"", command.Json);
        StringAssert.Contains("\"combatId\":\"combat-1\"", command.Json);
        StringAssert.Contains("\"expectedRevision\":\"42\"", command.Json);
        StringAssert.Contains("\"type\":\"END_TURN\"", command.Json);
        StringAssert.Contains("\"reason\":\"PLAYER\"", command.Json);
    }

    [TestCase("SELECT_CHOICE")]
    [TestCase("SURRENDER")]
    public void UnsupportedBackendCommandsAreRejectedLocally(string commandType)
    {
        var core = new ReactCombatBridgeCore(() => "action-fixed");
        core.Connect("combat-1");
        core.HandleCombatEvent(Snapshot);

        Assert.Throws<System.ArgumentException>(() => core.CreateCommand(commandType, new { }));
    }

    [Test]
    public async Task CausationActionConfirmsPendingCommand()
    {
        var core = new ReactCombatBridgeCore(() => "action-1");
        core.Connect("combat-1");
        core.HandleCombatEvent(Snapshot);
        ReactCombatCommand command = core.CreateCommand("END_TURN", new { });

        Task<ReactCombatCommandOutcome> pending = core.WaitForCommandAsync(command.ActionId, 1000);
        core.HandleCombatEvent(Event("43", command.ActionId));

        Assert.That(await pending, Is.EqualTo(ReactCombatCommandOutcome.Confirmed));
    }

    [Test]
    public async Task AuthoritativeStateUpdateMayAdvanceAcrossAiRevisions()
    {
        var core = new ReactCombatBridgeCore(() => "action-1");
        core.Connect("combat-1");
        core.HandleCombatEvent(Snapshot);
        ReactCombatCommand command = core.CreateCommand("END_TURN", new { });
        Task<ReactCombatCommandOutcome> pending = core.WaitForCommandAsync(command.ActionId, 1000);

        Assert.That(core.HandleCombatEvent(Event("46", command.ActionId, "STATE_UPDATED")), Is.True);
        Assert.That(core.CurrentRevision, Is.EqualTo("46"));
        Assert.That(await pending, Is.EqualTo(ReactCombatCommandOutcome.Confirmed));
    }

    [Test]
    public async Task CommandRejectionDoesNotAdvanceRevisionOrConfirmCommand()
    {
        var core = new ReactCombatBridgeCore(() => "action-1");
        core.Connect("combat-1");
        core.HandleCombatEvent(Snapshot);
        ReactCombatCommand command = core.CreateCommand("END_TURN", new { });
        Task<ReactCombatCommandOutcome> pending = core.WaitForCommandAsync(command.ActionId, 1000);

        Assert.That(core.HandleCombatEvent(Event("42", command.ActionId, "COMMAND_REJECTED")), Is.True);
        Assert.That(core.CurrentRevision, Is.EqualTo("42"));
        Assert.That(await pending, Is.EqualTo(ReactCombatCommandOutcome.Rejected));
    }

    [Test]
    public async Task ResultingRevisionCombatEventConfirmsCommandBeforeStateUpdate()
    {
        var core = new ReactCombatBridgeCore(() => "action-1");
        int delivered = 0;
        core.CombatEventReceived += _ => delivered++;
        core.Connect("combat-1");
        core.HandleCombatEvent(Snapshot); // revision = 42, delivered = 1
        ReactCombatCommand command = core.CreateCommand("PLAY_CARD", new { });
        Task<ReactCombatCommandOutcome> pending = core.WaitForCommandAsync(command.ActionId, 1000);

        Assert.That(core.HandleCombatEvent(Event("43", command.ActionId, "COMBAT_EVENT")), Is.True);
        Assert.That(core.CurrentRevision, Is.EqualTo("42"), "COMBAT_EVENT must not advance revision");
        Assert.That(delivered, Is.EqualTo(2), "COMBAT_EVENT must be delivered to listeners");
        Assert.That(await pending, Is.EqualTo(ReactCombatCommandOutcome.Confirmed));
        Assert.That(core.HandleCombatEvent(Event("43", type: "STATE_UPDATED")), Is.True);
        Assert.That(core.CurrentRevision, Is.EqualTo("43"));
    }

    [Test]
    public void CombatEventsOfAMultiRevisionCommandAreDelivered()
    {
        var core = new ReactCombatBridgeCore(() => "action-1");
        int delivered = 0;
        core.CombatEventReceived += _ => delivered++;
        core.Connect("combat-1");
        core.HandleCombatEvent(Snapshot); // revision = 42, delivered = 1

        // One END_TURN resolves the player discard, the whole AI chain and the next draw;
        // the server stamps every event it produced with the revision the command lands on,
        // which is several revisions ahead of the one the client still knows.
        Assert.That(core.HandleCombatEvent(Event("45", type: "COMBAT_EVENT")), Is.True);
        Assert.That(core.HandleCombatEvent(Event("45", type: "COMBAT_EVENT")), Is.True);
        Assert.That(core.CurrentRevision, Is.EqualTo("42"), "COMBAT_EVENT must not advance revision");
        Assert.That(delivered, Is.EqualTo(3), "every event of the command must reach the presentation layer");
    }

    [Test]
    public void CombatEventsOlderThanTheKnownRevisionAreNotDelivered()
    {
        var core = new ReactCombatBridgeCore(() => "action-1");
        int delivered = 0;
        core.CombatEventReceived += _ => delivered++;
        core.Connect("combat-1");
        core.HandleCombatEvent(Snapshot); // revision = 42, delivered = 1

        Assert.That(core.HandleCombatEvent(Event("41", type: "COMBAT_EVENT")), Is.False);
        Assert.That(delivered, Is.EqualTo(1));
    }

    [Test]
    public void CommandWaitCompletesInlineWhenUnityReceivesConfirmation()
    {
        var core = new ReactCombatBridgeCore(() => "action-1");
        core.Connect("combat-1");
        core.HandleCombatEvent(Snapshot);
        ReactCombatCommand command = core.CreateCommand("PLAY_CARD", new { });
        Task<ReactCombatCommandOutcome> pending = core.WaitForCommandAsync(command.ActionId, 1000);

        core.HandleCombatEvent(Event("43", command.ActionId, "COMBAT_EVENT"));

        Assert.That(pending.IsCompleted, Is.True,
            "WebGL has no worker thread available to resume an asynchronous continuation");
        Assert.That(pending.Result, Is.EqualTo(ReactCombatCommandOutcome.Confirmed));
    }

    /// <summary>
    /// A command the server could not process at all answers COMMAND_FAILED. The player
    /// who hit this waited on a deadline instead and softlocked, because the message was
    /// dropped before it could answer anything.
    /// </summary>
    [Test]
    public async Task CommandFailureAnswersThePendingCommand()
    {
        var core = new ReactCombatBridgeCore(() => "action-1");
        core.Connect("combat-1");
        core.HandleCombatEvent(Snapshot);
        ReactCombatCommand command = core.CreateCommand("END_TURN", new { });
        Task<ReactCombatCommandOutcome> pending = core.WaitForCommandAsync(command.ActionId, 1000);

        Assert.That(core.HandleCombatEvent(Event("42", command.ActionId, "COMMAND_FAILED")), Is.True);
        Assert.That(await pending, Is.EqualTo(ReactCombatCommandOutcome.Failed));
    }

    /// <summary>
    /// A failure rolled the server's transaction back, so nothing advanced. Taking the
    /// echoed revision as progress would leave the client a revision ahead of the server.
    /// </summary>
    [Test]
    public void CommandFailureDoesNotAdvanceTheRevision()
    {
        var core = new ReactCombatBridgeCore(() => "action-1");
        core.Connect("combat-1");
        core.HandleCombatEvent(Snapshot);
        ReactCombatCommand command = core.CreateCommand("END_TURN", new { });
        core.WaitForCommandAsync(command.ActionId, 1000);

        core.HandleCombatEvent(Event("42", command.ActionId, "COMMAND_FAILED"));

        Assert.That(core.CurrentRevision, Is.EqualTo("42"));
    }

    /// <summary>
    /// The failure that softlocked a real game was a message type this client did not
    /// know. Whatever the server invents next, a message naming our command must answer
    /// it rather than be dropped — Unknown is honest, and it makes the caller resync.
    /// </summary>
    [Test]
    public async Task AMessageOfAnUnknownTypeStillAnswersTheCommandItNames()
    {
        var core = new ReactCombatBridgeCore(() => "action-1");
        core.Connect("combat-1");
        core.HandleCombatEvent(Snapshot);
        ReactCombatCommand command = core.CreateCommand("END_TURN", new { });
        Task<ReactCombatCommandOutcome> pending = core.WaitForCommandAsync(command.ActionId, 1000);

        // Not a type this client knows, and not a revision it can place either.
        Assert.That(
            core.HandleCombatEvent(Event("99", command.ActionId, "SOMETHING_NEW")),
            Is.False);
        Assert.That(await pending, Is.EqualTo(ReactCombatCommandOutcome.Unknown));
        Assert.That(core.CurrentRevision, Is.EqualTo("42"));
    }

    [Test]
    public async Task TimeoutReturnsUnknownRatherThanRejected()
    {
        var core = new ReactCombatBridgeCore(() => "action-1");
        core.Connect("combat-1");
        core.HandleCombatEvent(Snapshot);
        ReactCombatCommand command = core.CreateCommand("END_TURN", new { });

        Assert.That(
            await core.WaitForCommandAsync(command.ActionId, 1),
            Is.EqualTo(ReactCombatCommandOutcome.Unknown));
    }

    [Test]
    public void DisconnectClearsCombatRevisionAndPendingCommands()
    {
        var core = new ReactCombatBridgeCore(() => "action-1");
        core.Connect("combat-1");
        core.HandleCombatEvent(Snapshot);
        ReactCombatCommand command = core.CreateCommand("END_TURN", new { });

        core.Disconnect();

        Assert.That(core.CombatId, Is.Null);
        Assert.That(core.CurrentRevision, Is.Null);
        Assert.That(core.HasPendingCommand(command.ActionId), Is.False);
    }

    [Test]
    public void MatchingTransportStatusIsRaised()
    {
        var core = new ReactCombatBridgeCore(() => "action-1");
        string received = null;
        core.CombatStatusChanged += value => received = value;
        core.Connect("combat-1");

        Assert.That(core.HandleCombatStatus(
            "{\"combatId\":\"combat-1\",\"status\":\"CONNECTED\"}"), Is.True);
        Assert.That(received, Is.EqualTo("CONNECTED"));
        Assert.That(core.HandleCombatStatus(
            "{\"combatId\":\"combat-2\",\"status\":\"DISCONNECTED\"}"), Is.False);
    }

    [Test]
    public void MonoBehaviourAdapterExposesNativeAndReactEntryPoints()
    {
        string source = File.ReadAllText(Path.Combine(
            "Assets", "Scripts", "Scene", "STS", "Api", "ReactCombatBridge.cs"));

        StringAssert.Contains("Insastral_CombatConnect", source);
        StringAssert.Contains("Insastral_CombatDisconnect", source);
        StringAssert.Contains("Insastral_CombatCommand", source);
        StringAssert.Contains("ConnectAsync", source);
        StringAssert.Contains("SendCommandAsync", source);
        StringAssert.Contains("HandleCombatEvent", source);
        StringAssert.Contains("HandleCombatStatus", source);
    }

    [Test]
    public void SharedProtocolFixtureMatchesTheFrontContract()
    {
        string unityPath = Path.Combine(
            "Assets", "Tests", "Fixtures", "combat-protocol-v1.json");
        string frontPath = Path.GetFullPath(Path.Combine(
            "..", "..", "insastral", "tests", "fixtures", "combat-protocol-v1.json"));

        Assert.That(File.Exists(unityPath), Is.True, "Unity protocol fixture must exist");
        Assert.That(File.Exists(frontPath), Is.True, "Front protocol fixture must exist");
        string fixture = File.ReadAllText(unityPath);
        Assert.That(fixture, Is.EqualTo(File.ReadAllText(frontPath)));
        StringAssert.Contains("valid-large-revision-event", fixture);
        StringAssert.Contains("invalid-leading-zero-revision", fixture);
        StringAssert.Contains("valid-end-turn-command", fixture);
    }

    [Test]
    public void ConnectPayloadNamesTheCombatAndItsMode()
    {
        string payload = ReactCombatBridgeCore.CreateConnectPayload("battle-77", "PVP");

        StringAssert.Contains("\"combatId\":\"battle-77\"", payload);
        StringAssert.Contains("\"mode\":\"PVP\"", payload);
    }

    /// Sans mode, la couche React retombe sur ses routes PvE sans le dire : la socket
    /// s'ouvre, la file reste vide et les commandes disparaissent. Refuser ici est le
    /// seul endroit ou cette panne peut encore faire du bruit.
    [Test]
    public void ConnectPayloadRefusesAModelessConnection()
    {
        Assert.Throws<System.ArgumentException>(
            () => ReactCombatBridgeCore.CreateConnectPayload("battle-77", null));
        Assert.Throws<System.ArgumentException>(
            () => ReactCombatBridgeCore.CreateConnectPayload("battle-77", "   "));
    }

    [Test]
    public void ConnectPayloadRefusesACombatlessConnection()
    {
        Assert.Throws<System.ArgumentException>(
            () => ReactCombatBridgeCore.CreateConnectPayload("", "PVP"));
    }

    /// Le PvE s'adresse par son runId, le PvP par son battleId. Deux methodes plutot
    /// qu'une parametree : la premiere leve sur un runId vide, ce qui est exactement ce
    /// qu'un PvP lui presenterait.
    [Test]
    public void APvpConnectionUsesTheBattleIdAsTransportId()
    {
        Assert.That(AuthoritativeCombatIdentity.GetPvpTransportId("battle-77"),
            Is.EqualTo("battle-77"));
        Assert.Throws<System.ArgumentException>(
            () => AuthoritativeCombatIdentity.GetPvpTransportId(" "));
    }
}
