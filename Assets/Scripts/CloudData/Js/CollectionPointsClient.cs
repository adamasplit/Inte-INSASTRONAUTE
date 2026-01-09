using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode;
using UnityEngine;

[System.Serializable]
public class UpdatePCResult
{
    public bool ok;
    public long newPC;
    public string message;
}

public static class CollectionPointsClient
{
    /// <summary>
    /// Met à jour les PC du joueur sur le serveur avec la valeur calculée localement
    /// </summary>
    public static async Task<UpdatePCResult> UpdatePCAsync(int computedPC)
    {
        try
        {
            Debug.Log($"[CollectionPointsClient] Updating PC to {computedPC}...");
            
            var result = await CloudCodeService.Instance.CallEndpointAsync<UpdatePCResult>(
                "SetCollectionPoints",
                new Dictionary<string, object>
                {
                    { "pc", computedPC }
                }
            );
            
            if (result.ok)
            {
                Debug.Log($"[CollectionPointsClient] PC updated to {computedPC}");
                PlayerProfileStore.PC = computedPC;
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
            return new UpdatePCResult
            {
                ok = false,
                message = e.Message
            };
        }
    }
}
