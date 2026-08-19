using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public sealed class ReactCombatBridge : MonoBehaviour
{
    private const string WebBridgeGameObjectName = "WebBridge";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int Insastral_CombatConnect(string json);

    [DllImport("__Internal")]
    private static extern int Insastral_CombatDisconnect(string json);

    [DllImport("__Internal")]
    private static extern int Insastral_CombatCommand(string json);
#endif

    private static ReactCombatBridge instance;
    private ReactCombatBridgeCore core;

    public static event Action<string> CombatEventReceived;
    public static event Action<string> CombatStatusChanged;

    public static string CombatId => instance != null ? instance.core.CombatId : null;
    public static string CurrentRevision => instance != null ? instance.core.CurrentRevision : null;

    private void Awake()
    {
        gameObject.name = WebBridgeGameObjectName;
        instance = this;
        core = new ReactCombatBridgeCore(() => Guid.NewGuid().ToString());
        core.CombatEventReceived += json => CombatEventReceived?.Invoke(json);
        core.CombatStatusChanged += status => CombatStatusChanged?.Invoke(status);
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        core?.Disconnect();
        instance = null;
    }

    public static Task<bool> ConnectAsync(string combatId)
    {
        ReactCombatBridge bridge = EnsureInstance();
        bridge.core.Connect(combatId);
        string json = JsonConvert.SerializeObject(new { combatId });
        return Task.FromResult(InvokeConnect(json) != 0);
    }

    public static void Disconnect()
    {
        if (instance == null || instance.core.CombatId == null)
            return;

        string json = JsonConvert.SerializeObject(new { combatId = instance.core.CombatId });
        InvokeDisconnect(json);
        instance.core.Disconnect();
    }

    public static async Task<ReactCombatCommandOutcome> SendCommandAsync(
        string type,
        object payload,
        string expectedRevision,
        int timeoutMs = 5000)
    {
        ReactCombatBridge bridge = EnsureInstance();
        if (!string.Equals(bridge.core.CurrentRevision, expectedRevision, StringComparison.Ordinal))
            return ReactCombatCommandOutcome.Unknown;

        ReactCombatCommand command = bridge.core.CreateCommand(type, payload);
        if (InvokeCommand(command.Json) == 0)
            return await bridge.core.WaitForCommandAsync(command.ActionId, 0);

        return await bridge.core.WaitForCommandAsync(command.ActionId, timeoutMs);
    }

    public void HandleCombatEvent(string json)
    {
        core.HandleCombatEvent(json);
    }

    public void HandleCombatStatus(string json)
    {
        core.HandleCombatStatus(json);
    }

    private static ReactCombatBridge EnsureInstance()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<ReactCombatBridge>();
        if (instance != null)
            return instance;

        GameObject bridgeObject = GameObject.Find(WebBridgeGameObjectName);
        if (bridgeObject == null)
        {
            bridgeObject = new GameObject(WebBridgeGameObjectName);
            DontDestroyOnLoad(bridgeObject);
        }

        instance = bridgeObject.AddComponent<ReactCombatBridge>();
        return instance;
    }

    private static int InvokeConnect(string json)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return Insastral_CombatConnect(json);
#else
        Debug.LogWarning("Combat bridge connect skipped outside a WebGL player.");
        return 0;
#endif
    }

    private static int InvokeDisconnect(string json)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return Insastral_CombatDisconnect(json);
#else
        Debug.LogWarning("Combat bridge disconnect skipped outside a WebGL player.");
        return 0;
#endif
    }

    private static int InvokeCommand(string json)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return Insastral_CombatCommand(json);
#else
        Debug.LogWarning("Combat bridge command skipped outside a WebGL player.");
        return 0;
#endif
    }
}
