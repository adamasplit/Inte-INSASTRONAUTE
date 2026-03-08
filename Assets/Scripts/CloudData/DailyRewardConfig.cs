using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.RemoteConfig;

/// <summary>
/// Représente un item de récompense journalière, tel que défini dans Remote Config.
/// </summary>
[Serializable]
public class DailyRewardItem
{
    /// <summary>"TOKEN", "PC" ou "PACK"</summary>
    public string type;
    /// <summary>Identifiant du pack (utilisé uniquement si type == "PACK").</summary>
    public string packId;
    public int amount;
    /// <summary>Libellé affiché dans l'UI (ex: "50 Tokens", "1 Pack Débutant").</summary>
    public string label;
    /// <summary>Nom de la sprite à charger depuis Resources/Icons/ (optionnel).</summary>
    public string iconName;
}

/// <summary>
/// Configuration complète des récompenses journalières, désérialisée depuis Remote Config.
/// Clé Remote Config : <c>daily_rewards_config</c>
/// Exemple JSON :
/// <code>
/// {
///   "cooldownHours": 24,
///   "rewards": [
///     { "type": "TOKEN", "amount": 50, "label": "50 Tokens", "iconName": "icon_token" },
///     { "type": "PACK",  "packId": "pack_starter", "amount": 1, "label": "1 Pack Débutant", "iconName": "icon_pack" }
///   ]
/// }
/// </code>
/// </summary>
[Serializable]
public class DailyRewardConfig
{
    public float cooldownHours = 24f;
    public DailyRewardItem[] rewards;
}

/// <summary>
/// Utilitaire statique pour charger <see cref="DailyRewardConfig"/> depuis Remote Config.
/// </summary>
public static class DailyRewardRemoteConfig
{
    public const string RC_KEY = "daily_rewards_config";

    private static DailyRewardConfig _cached;

    /// <summary>
    /// Récupère la configuration des récompenses journalières.
    /// Utilise le cache Remote Config déjà chargé si disponible ; sinon effectue un fetch.
    /// </summary>
    public static async Task<DailyRewardConfig> GetConfigAsync()
    {
        if (_cached != null)
        {
            Debug.Log("[DailyRewardRC] Config servie depuis le cache.");
            return _cached;
        }

        try
        {
            var allKeys = RemoteConfigService.Instance?.appConfig?.GetKeys();
            Debug.Log($"[DailyRewardRC] Clés RC actuelles : {(allKeys != null ? string.Join(", ", allKeys) : "(null appConfig)")} ");

            // Si Remote Config a déjà été chargé (ex. ShopRemoteLoader au démarrage), on lit directement.
            if (RemoteConfigService.Instance?.appConfig != null
                && RemoteConfigService.Instance.appConfig.HasKey(RC_KEY))
            {
                Debug.Log($"[DailyRewardRC] Clé '{RC_KEY}' trouvée dans appConfig existant.");
                return ParseAndCache(RemoteConfigService.Instance.appConfig.GetJson(RC_KEY));
            }

            Debug.Log($"[DailyRewardRC] Clé '{RC_KEY}' absente, on force un fetch RC.");

            // Sinon, on force un fetch.
            await RemoteConfigService.Instance.FetchConfigsAsync(
                new UserAttributes(),
                new AppAttributes()
            );

            var keysAfter = RemoteConfigService.Instance?.appConfig?.GetKeys();
            Debug.Log($"[DailyRewardRC] Clés après fetch : {(keysAfter != null ? string.Join(", ", keysAfter) : "(null)")} ");

            if (!RemoteConfigService.Instance.appConfig.HasKey(RC_KEY))
            {
                Debug.LogWarning($"[DailyRewardRC] Clé Remote Config '{RC_KEY}' introuvable même après fetch. Vérifie Unity Dashboard.");
                return null;
            }

            var json = RemoteConfigService.Instance.appConfig.GetJson(RC_KEY);
            Debug.Log($"[DailyRewardRC] JSON brut reçu : {json}");
            return ParseAndCache(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DailyRewardRC] Exception : {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    /// <summary>Invalide le cache local (utile après une mise à jour de config).</summary>
    public static void InvalidateCache() => _cached = null;

    private static DailyRewardConfig ParseAndCache(string json)
    {
        _cached = JsonUtility.FromJson<DailyRewardConfig>(json);
        return _cached;
    }
}
