using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode;

/// <summary>Représente une récompense effectivement attribuée par ClaimDailyReward.</summary>
[System.Serializable]
public class GrantedReward
{
    /// <summary>"TOKEN", "PC" ou "PACK"</summary>
    public string type;
    /// <summary>Identifiant du pack (uniquement si type == "PACK").</summary>
    public string packId;
    public int amount;
    /// <summary>Libellé affiché à l'utilisateur.</summary>
    public string label;
}

/// <summary>
/// Résultat retourné par le Cloud Code <c>ClaimDailyReward</c>.
/// errorCode possibles : "ALREADY_CLAIMED", "CONFIG_NOT_FOUND", "CONFIG_ERROR".
/// </summary>
[System.Serializable]
public class DailyRewardResult
{
    public bool ok;
    public string errorCode;
    public GrantedReward[] grantedRewards;
    public int cooldownSecondsRemaining;
    public string message;
}

public readonly struct DailyRewardStatus
{
    public bool CanClaim { get; }
    public int CooldownSecondsRemaining { get; }

    public DailyRewardStatus(bool canClaim, int cooldownSecondsRemaining)
    {
        CanClaim = canClaim;
        CooldownSecondsRemaining = cooldownSecondsRemaining;
    }
}

public static class DailyRewardClient
{
    public static async Task<DailyRewardResult> ClaimAsync()
    {
        // On lit Remote Config côté C# (fonctionne) et on passe la config en paramètre
        // plutôt que de la relire côté Cloud Code (SDK JS peu fiable).
        UnityEngine.Debug.Log("[DailyRewardClient] ClaimAsync start");

        var config = await DailyRewardRemoteConfig.GetConfigAsync();

        if (config == null)
            UnityEngine.Debug.LogError("[DailyRewardClient] GetConfigAsync returned null — clé Remote Config absente ou JSON invalide.");
        else
            UnityEngine.Debug.Log($"[DailyRewardClient] Config OK : cooldownHours={config.cooldownHours}, rewards={(config.rewards?.Length ?? 0)}");

        var args = BuildArgs(config);

        return await CloudCodeService.Instance.CallEndpointAsync<DailyRewardResult>(
            "ClaimDailyReward",
            args
        );
    }

    public static async Task<DailyRewardStatus> GetStatusAsync()
    {
        var config = await DailyRewardRemoteConfig.GetConfigAsync();
        var args = BuildArgs(config);
        args["checkOnly"] = true;

        var res = await CloudCodeService.Instance.CallEndpointAsync<DailyRewardResult>(
            "ClaimDailyReward",
            args
        );

        if (res.ok)
            return new DailyRewardStatus(true, 0);

        if (res.errorCode == "ALREADY_CLAIMED")
            return new DailyRewardStatus(false, res.cooldownSecondsRemaining);

        return new DailyRewardStatus(false, 0);
    }

    private static Dictionary<string, object> BuildArgs(DailyRewardConfig config)
    {
        var args = new Dictionary<string, object>();

        if (config == null)
            return args;

        // Le SDK Cloud Code ne sérialise pas bien les List<Dictionary>,
        // on passe rewards en JSON string que le JS parsera.
        args["rewardsJson"] = UnityEngine.JsonUtility.ToJson(
            new DailyRewardConfig { cooldownHours = config.cooldownHours, rewards = config.rewards }
        );

        return args;
    }
}
