using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode;
using UnityEngine;

[System.Serializable]
public class UpdateCPResult
{
    public bool ok;
    public long newCP;
    public string message;
}

public static class CollectionPointsClient
{
    /// <summary>
    /// Met à jour les CP du joueur sur le serveur avec la valeur calculée localement
    /// </summary>
    public static async Task<UpdateCPResult> UpdateCPAsync(int computedCP)
    {
        try
        {
            Debug.Log($"[CollectionPointsClient] Updating PC to {computedCP}...");
            
            var result = await CloudCodeService.Instance.CallEndpointAsync<UpdateCPResult>(
                "SetCollectionPoints",
                new Dictionary<string, object>
                {
                    { "pc", computedCP }
                }
            );
            
            if (result.ok)
            {
                Debug.Log($"[CollectionPointsClient] CP updated to {result.newCP}");
                PlayerProfileStore.PC = result.newCP;
            }
            else
            {
                Debug.LogWarning($"[CollectionPointsClient] Update failed: {result.message}");
            }
            
            return result;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CollectionPointsClient] Error: {e.Message}");
            return new UpdateCPResult
            {
                ok = false,
                message = e.Message
            };
        }
    }
}
