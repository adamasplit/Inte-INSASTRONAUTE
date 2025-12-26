using System.Threading.Tasks;
using Unity.Services.RemoteConfig;

public struct UserAttributes {}
public struct AppAttributes {}

public static class EventConfig
{
    public static async Task FetchAsync()
    {
        await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes());
    }

    public static bool Enabled =>
        RemoteConfigService.Instance.appConfig.GetBool("event_enabled", false);

    public static float BonusMultiplier =>
        (float)RemoteConfigService.Instance.appConfig.GetFloat("event_bonus_multiplier", 1f);
}
