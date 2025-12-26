using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;

public static class PlayerProfileStore
{
    //Static Data
    public static long TOKEN = 0;
    public static long PACK = 0;
    public static string DISPLAY_NAME = "Guest";
    //Key for the display name in Cloud Save
    public const string DisplayNameKey = "displayName";

    public static async Task SaveDisplayNameAsync(string displayName)
    {
        var data = new Dictionary<string, object>
        {
            { DisplayNameKey, displayName }
        };

        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
    }

    public static async Task<string> LoadDisplayNameAsync()
    {
        var keys = new HashSet<string> { DisplayNameKey };
        var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

        if (result.TryGetValue(DisplayNameKey, out var item))
            return item.Value.GetAs<string>();

        return null;
    }
}
