using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BottomMenuController : MonoBehaviour
{
    [Serializable]
    public struct TabDefinition
    {
        public string sceneName;
    }

    [Header("Tabs (ordre des boutons du menu)")]
    public TabDefinition[] tabs;

    private string _activeScene;
    private bool _isTransitioning;

    /// <summary>
    /// Appelé par les boutons du menu via OnClick(int index).
    /// Charge la scène de l'onglet en mode Additive et décharge la précédente.
    /// </summary>
    public void GoToScreen(int index)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning("[BottomMenu] Transition déjà en cours, ignore.");
            return;
        }
        if (index < 0 || index >= tabs.Length)
        {
            Debug.LogError($"[BottomMenu] Index onglet invalide : {index}");
            return;
        }
        var target = tabs[index].sceneName;
        if (target == _activeScene) return;
        _ = TransitionAsync(target);
    }

    private async Task TransitionAsync(string target)
    {
        _isTransitioning = true;
        try
        {
            // Décharger la scène active
            if (!string.IsNullOrEmpty(_activeScene))
            {
                var unload = SceneManager.UnloadSceneAsync(_activeScene);
                if (unload != null)
                    await WaitAsync(unload);
            }

            // Charger la nouvelle scène en Additive
            if (!IsSceneValid(target))
            {
                Debug.LogError($"[BottomMenu] Scène inconnue : '{target}'. " +
                               "Vérifiez que la scène est ajoutée dans Build Settings.");
                return;
            }

            var load = SceneManager.LoadSceneAsync(target, LoadSceneMode.Additive);
            load.allowSceneActivation = true;
            await WaitAsync(load);

            _activeScene = target;
            NotifySceneLoaded(target);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BottomMenu] Échec du chargement de '{target}' : {ex.Message}");
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    /// <summary>
    /// Déclenche le rafraîchissement du contrôleur de la scène qui vient d'être chargée.
    /// </summary>
    private void NotifySceneLoaded(string sceneName)
    {
        if (sceneName == SceneNames.Collection)
        {
            var c = FindFirstObjectByType<CardCollectionController>();
            if (c != null) c.RefreshCollection();
            else Debug.LogError($"[BottomMenu] CardCollectionController introuvable dans {sceneName}.");
        }
        else if (sceneName == SceneNames.Store)
        {
            var s = FindFirstObjectByType<ShopRemoteLoader>();
            if (s != null) s.UpdateShopFromRemote();
            else Debug.LogError($"[BottomMenu] ShopRemoteLoader introuvable dans {sceneName}.");
        }
        else if (sceneName == SceneNames.Packs)
        {
            // PackCollectionController se rafraîchit via OnEnable + PlayerProfileStore.OnPackCollectionChanged
        }
        else if (sceneName == SceneNames.Leaderboard)
        {
            var l = FindFirstObjectByType<LeaderboardBinder>();
            if (l != null) _ = l.RefreshLeaderboardAsync();
            else Debug.LogError($"[BottomMenu] LeaderboardBinder introuvable dans {sceneName}.");
        }
        else if (sceneName == SceneNames.Events)
        {
            var e = FindFirstObjectByType<EventsMenuController>();
            if (e != null) _ = e.RefreshEventsAsync();
            else Debug.LogError($"[BottomMenu] EventsMenuController introuvable dans {sceneName}.");
        }
    }

    private static bool IsSceneValid(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path.Contains(sceneName)) return true;
        }
        return false;
    }

    private static Task WaitAsync(AsyncOperation op)
    {
        var tcs = new TaskCompletionSource<bool>();
        op.completed += _ => tcs.TrySetResult(true);
        return tcs.Task;
    }
}
