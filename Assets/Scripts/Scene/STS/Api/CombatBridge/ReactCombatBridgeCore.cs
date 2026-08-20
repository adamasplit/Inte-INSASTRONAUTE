using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public enum ReactCombatCommandOutcome
{
    Confirmed,
    Rejected,
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
        if (isSnapshot)
        {
            CurrentRevision = revision;
        }
        else if (isCommandRejected)
        {
            if (CurrentRevision == null || !string.Equals(revision, CurrentRevision, StringComparison.Ordinal))
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
                return false;
            CurrentRevision = revision;
        }

        string actionId = message.Value<string>("causationActionId");
        if (!string.IsNullOrEmpty(actionId)
            && pendingCommands.TryGetValue(actionId, out TaskCompletionSource<ReactCombatCommandOutcome> pending))
        {
            pending.TrySetResult(isCommandRejected
                ? ReactCombatCommandOutcome.Rejected
                : ReactCombatCommandOutcome.Confirmed);
        }

        CombatEventReceived?.Invoke(json);
        return true;
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
        return new TaskCompletionSource<ReactCombatCommandOutcome>(
            TaskCreationOptions.RunContinuationsAsynchronously);
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
