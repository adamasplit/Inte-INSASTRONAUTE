using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class AuthController : MonoBehaviour
{
    public static AuthController Instance { get; private set; }
    public bool IsReady { get; private set; }

    private const string PREF_USERNAME = "SavedUsername";

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        await Init();
        await TryAutoLogin();
    }

        private void SaveUsername(string username)
    {
        PlayerPrefs.SetString(PREF_USERNAME, username);
        PlayerPrefs.Save();
    }

    private void ClearSavedCredentials()
    {
        PlayerPrefs.DeleteKey(PREF_USERNAME);
        PlayerPrefs.Save();
        Debug.Log("Saved credentials cleared");
    }

    private async Task TryAutoLogin()
    {
        // On WebGL, Unity Auth SDK restores the cached session token from IndexedDB on
        // page reload. If the session is already active, navigate directly to Main.
        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("[AuthController] Session already active (restored from cache). Navigating to Main.");
            if (PlayerPrefs.HasKey(PREF_USERNAME))
                PlayerProfileStore.DISPLAY_NAME = PlayerPrefs.GetString(PREF_USERNAME);
            SceneManager.LoadScene("Main - Copie");
            return;
        }

        // UGS stores its own encrypted session token — no need to store the password locally.
        // SignInAnonymouslyAsync restores the existing session when SessionTokenExists is true.
        if (AuthenticationService.Instance.SessionTokenExists)
        {
            try
            {
                Debug.Log("[AuthController] Session token found. Restoring session...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                if (PlayerPrefs.HasKey(PREF_USERNAME))
                    PlayerProfileStore.DISPLAY_NAME = PlayerPrefs.GetString(PREF_USERNAME);
                SceneManager.LoadScene("Main - Copie");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AuthController] Session restore failed: {ex.Message}");
                ClearSavedCredentials();
            }
        }
    }

    public async Task Init()
    {
        if (IsReady) return;

        await UnityServices.InitializeAsync();
        IsReady = true;
    }

    public async Task SignInGuest()
    {
        await Init();

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log($"Guest signed in: {AuthenticationService.Instance.PlayerId}");
        PlayerProfileStore.DISPLAY_NAME = "Guest";
        SceneManager.LoadScene("Main - Copie");
    }

    public async Task SignUp(string username, string password)
    {
        await Init();

        await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
        Debug.Log($"Signed up OK. PlayerId={AuthenticationService.Instance.PlayerId}");

        // Persist pseudo côté cloud (optionnel mais tu le veux)
        await PlayerProfileStore.SaveDisplayNameAsync(username);
        Debug.Log("displayName saved.");
        PlayerProfileStore.DISPLAY_NAME = username;

        // Save username only (session token is managed securely by UGS SDK)
        SaveUsername(username);

        SceneManager.LoadScene("Main - Copie");
    }

    public async Task SignIn(string username, string password)
    {
        await Init();

        await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
        Debug.Log($"Signed in OK. PlayerId={AuthenticationService.Instance.PlayerId}");

        PlayerProfileStore.DISPLAY_NAME = username;

        // Save username only (session token is managed securely by UGS SDK)
        SaveUsername(username);

        SceneManager.LoadScene("Main - Copie");
    }

    public void SignOut()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
            ClearSavedCredentials();
            Debug.Log("Déconnecté");
            SceneManager.LoadScene("LoginScreen");
        }
        else
        {
            Debug.Log("Pas de session active");
        }
    }

    /// <summary>
    /// Supprime définitivement le compte utilisateur et toutes ses données (RGPD Art. 17).
    /// </summary>
    public async Task DeleteAccount()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("Aucun utilisateur connecté pour effectuer la suppression");
            return;
        }

        try
        {
            Debug.Log("[AuthController] Début de la suppression du compte...");

            // 1. Effacer toutes les données Cloud Save
            await PlayerProfileStore.ClearAllDataAsync();
            Debug.Log("[AuthController] Données Cloud Save supprimées");

            // 2. Effacer les données locales
            ClearSavedCredentials();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[AuthController] Données locales supprimées");

            // 3. Suppression définitive du compte UGS (RGPD Art. 17)
            await AuthenticationService.Instance.DeleteAccountAsync();
            Debug.Log("[AuthController] Compte UGS supprimé définitivement");

            // 4. Retour à l'écran de connexion
            SceneManager.LoadScene("LoginScreen");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthController] Erreur lors de la suppression du compte: {ex.Message}");
            throw;
        }
    }

}

