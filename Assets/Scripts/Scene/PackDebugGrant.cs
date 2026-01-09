using UnityEngine;

public class PackDebugGrant : MonoBehaviour
{
    [Header("Debug packs (Editor only)")]
    public bool grantOnStart = true;
    public PackData[] packsToGrant;
    public int amountPerPack = 3;

    private async void Start()
    {
    #if UNITY_EDITOR
        if (!grantOnStart) return;

        foreach (var pack in packsToGrant)
        {
            if (pack != null)
                await PlayerProfileStore.AddPackAsync(pack, amountPerPack);
        }

        Debug.Log("[PackDebugGrant]DEBUG: Packs granted on start");
    #endif
    }
}
