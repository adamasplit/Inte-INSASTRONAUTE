using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode;

[System.Serializable]
public class DailyRewardResult
{
    public bool ok;
    public int grantedTokens;
    public int grantedPacks;
    public int cooldownSecondsRemaining;
    public string message;
}

public static class DailyRewardClient
{
    public static Task<DailyRewardResult> ClaimAsync()
    {
        // Aucun param nécessaire ici, tout est codé côté serveur (TOKEN + PACK)
        return CloudCodeService.Instance.CallEndpointAsync<DailyRewardResult>(
            "ClaimDailyReward",
            new Dictionary<string, object>()
        );
    }
}
