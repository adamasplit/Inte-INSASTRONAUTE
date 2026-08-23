using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public enum ReactCombatCommandOutcome
{
    Confirmed,

    /// <summary>The rules evaluated the command and refused it. The state is unchanged
    /// and the server recorded the refusal, so replaying the same action id returns it
    /// again rather than re-running anything.</summary>
    Rejected,

    /// <summary>The server could not process the command at all and rolled back. Nothing
    /// was recorded, so unlike a rejection this says nothing about the resulting state —
    /// the caller has to resynchronise.</summary>
    Failed,

    Unknown
}

public sealed class ReactCombatCommand
{
    public ReactCombatCommand(string actionId, string json)
    {
        ActionId = actionId;
        Json = json;
    }

    public string ActionId { get; }
    public string Json { get; }
}

public sealed class ReactCombatBridgeCore
{
    private static readonly HashSet<string> CommandTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "PLAY_CARD",
        "END_TURN"
    };

    private static readonly HashSet<string> StatusTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "CONNECTING",
        "CONNECTED",
        "RESYNCHRONIZING",
        "DISCONNECTED"
    };

    private readonly Func<string> actionIdFactory;
    private readonly Dictionary<string, TaskCompletionSource<ReactCombatCommandOutcome>> pendingCommands =
        new Dictionary<string, TaskCompletionSource<ReactCombatCommandOutcome>>(StringComparer.Ordinal);

    public ReactCombatBridgeCore(Func<string> actionIdFactory)
    {
        this.actionIdFactory = actionIdFactory ?? throw new ArgumentNullException(nameof(actionIdFactory));
    }

    public event Action<string> CombatEventReceived;
    public event Action<string> CombatStatusChanged;

    public string CombatId { get; private set; }
    public string CurrentRevision { get; private set; }

    public void Connect(string combatId)
    {
        if (string.IsNullOrWhiteSpace(combatId))
            throw new ArgumentException("Combat identifier is required", nameof(combatId));

        Disconnect();
        CombatId = combatId;
    }

    public void Disconnect()
    {
        foreach (TaskCompletionSource<ReactCombatCommandOutcome> pending in pendingCommands.Values)
            pending.TrySetResult(ReactCombatCommandOutcome.Unknown);

        pendingCommands.Clear();
        CombatId = null;
        CurrentRevision = null;
    }

    public ReactCombatCommand CreateCommand(string type, object payload)
    {
        if (CombatId == null || CurrentRevision == null)
            throw new InvalidOperationException("A synchronized combat is required");
        if (!CommandTypes.Contains(type))
            throw new ArgumentException("Unsupported combat command", nameof(type));

        string actionId = actionIdFactory();
        if (string.IsNullOrWhiteSpace(actionId))
            throw new InvalidOperationException("Action identifier factory returned an empty value");

        var body = new
        {
            protocolVersion = 1,
            actionId,
            combatId = CombatId,
            expectedRevision = CurrentRevision,
            type,
            payload = payload ?? new { }
        };

        pendingCommands[actionId] = NewPendingCommand();
        return new ReactCombatCommand(actionId, JsonConvert.SerializeObject(body));
    }

    public async Task<ReactCombatCommandOutcome> WaitForCommandAsync(string actionId, int timeoutMs)
    {
        if (!pendingCommands.TryGetValue(actionId, out TaskCompletionSource<ReactCombatCommandOutcome> pending))
            return ReactCombatCommandOutcome.Unknown;

        Task completed = await Task.WhenAny(pending.Task, Task.Delay(Math.Max(0, timeoutMs)));
        ReactCombatCommandOutcome outcome = completed == pending.Task
            ? await pending.Task
            : ReactCombatCommandOutcome.Unknown;
        pendingCommands.Remove(actionId);
        return outcome;
    }

    public bool HasPendingCommand(string actionId)
    {
        return pendingCommands.ContainsKey(actionId);
    }

    public bool HandleCombatEvent(string json)
    {
        JObject message;
        try
        {
            message = JObject.Parse(json);
        }
        catch
        {
            return false;
        }

        if (message.Value<int?>("protocolVersion") != 1
            || !string.Equals(message.Value<string>("combatId"), CombatId, StringComparison.Ordinal))
            return false;

        string revision = message.Value<string>("revision");
        string type = message.Value<string>("type");
        if (!IsCanonicalRevision(revision) || string.IsNullOrWhiteSpace(type))
            return false;

        bool isSnapshot = string.Equals(type, "COMBAT_SNAPSHOT", StringComparison.Ordinal);
        bool isStateUpdate = string.Equals(type, "STATE_UPDATED", StringComparison.Ordinal);
        bool isCommandRejected = string.Equals(type, "COMMAND_REJECTED", StringComparison.Ordinal);
        bool isCommandFailed = string.Equals(type, "COMMAND_FAILED", StringComparison.Ordinal);
        bool isCombatEvent = string.Equals(type, "COMBAT_EVENT", StringComparison.Ordinal);
        if (isSnapshot)
        {
            CurrentRevision = revision;
        }
        else if (isCommandRejected || isCommandFailed)
        {
            // Neither answer moves the combat on: a rejection was evaluated and refused, a
            // failure was rolled back. Both merely echo the revision we sent, so taking it
            // as progress would put the client a revision ahead of the server.
            if (CurrentRevision == null || !string.Equals(revision, CurrentRevision, StringComparison.Ordinal))
            {
                AnswerPendingCommand(message, ReactCombatCommandOutcome.Unknown);
                return false;
            }
        }
        else if (isCombatEvent)
        {
            // A single command can make the server advance several internal AI steps at once
            // (End Turn resolving multiple enemy turns), so every resulting COMBAT_EVENT is
            // tagged with the same final resultingRevision rather than incrementing per event.
            // Requiring an exact +1 match silently dropped every event from that whole chain.
            if (CurrentRevision == null || CompareRevisions(revision, CurrentRevision) < 0)
                return false;
        }
        else if (isStateUpdate)
        {
            if (CurrentRevision == null || CompareRevisions(revision, CurrentRevision) <= 0)
                return false;
            CurrentRevision = revision;
        }
        else
        {
            if (CurrentRevision == null || !string.Equals(revision, IncrementRevision(CurrentRevision), StringComparison.Ordinal))
            {
                // A message we cannot place must still answer the command it names.
                // Dropping it leaves the sender waiting on a deadline that teaches it
                // nothing, which is how one unhandled server error softlocked a combat.
                AnswerPendingCommand(message, ReactCombatCommandOutcome.Unknown);
                return false;
            }
            CurrentRevision = revision;
        }

        AnswerPendingCommand(message,
            isCommandRejected ? ReactCombatCommandOutcome.Rejected
            : isCommandFailed ? ReactCombatCommandOutcome.Failed
            : ReactCombatCommandOutcome.Confirmed);

        CombatEventReceived?.Invoke(json);
        return true;
    }

    /// <summary>
    /// Settles the command a message names, if we are still waiting on it.
    ///
    /// <para>Every message carrying our causationActionId is an answer to our command,
    /// whether or not we understand its type or can place its revision. Answering is
    /// therefore separate from applying: the command stops waiting, while the local state
    /// only moves when the message is one we know how to apply.</para>
    /// </summary>
    private void AnswerPendingCommand(JObject message, ReactCombatCommandOutcome outcome)
    {
        string actionId = message.Value<string>("causationActionId");
        if (!string.IsNullOrEmpty(actionId)
            && pendingCommands.TryGetValue(actionId, out TaskCompletionSource<ReactCombatCommandOutcome> pending))
        {
            pending.TrySetResult(outcome);
        }
    }

    public bool HandleCombatStatus(string json)
    {
        JObject message;
        try
        {
            message = JObject.Parse(json);
        }
        catch
        {
            return false;
        }

        string status = message.Value<string>("status");
        if (!string.Equals(message.Value<string>("combatId"), CombatId, StringComparison.Ordinal)
            || !StatusTypes.Contains(status))
            return false;

        CombatStatusChanged?.Invoke(status);
        return true;
    }

    private static TaskCompletionSource<ReactCombatCommandOutcome> NewPendingCommand()
    {
        return new TaskCompletionSource<ReactCombatCommandOutcome>();
    }

    private static bool IsCanonicalRevision(string revision)
    {
        if (string.IsNullOrEmpty(revision) || (revision.Length > 1 && revision[0] == '0'))
            return false;

        for (int index = 0; index < revision.Length; index++)
        {
            if (revision[index] < '0' || revision[index] > '9')
                return false;
        }
        return true;
    }

    private static string IncrementRevision(string revision)
    {
        char[] digits = revision.ToCharArray();
        for (int index = digits.Length - 1; index >= 0; index--)
        {
            if (digits[index] != '9')
            {
                digits[index]++;
                return new string(digits);
            }
            digits[index] = '0';
        }
        return "1" + new string(digits);
    }

    private static int CompareRevisions(string left, string right)
    {
        int lengthComparison = left.Length.CompareTo(right.Length);
        return lengthComparison != 0
            ? lengthComparison
            : string.CompareOrdinal(left, right);
    }
}
