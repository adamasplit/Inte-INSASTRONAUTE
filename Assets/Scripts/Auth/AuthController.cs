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
    private const string PREF_PASSWORD = "SavedPassword";

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

        private void SaveCredentials(string username, string password)
    {
        PlayerPrefs.SetString(PREF_USERNAME, username);
        PlayerPrefs.SetString(PREF_PASSWORD, password);
        PlayerPrefs.Save();
        Debug.Log("Credentials saved for auto-login");
    }

    private void ClearSavedCredentials()
    {
        PlayerPrefs.DeleteKey(PREF_USERNAME);
        PlayerPrefs.DeleteKey(PREF_PASSWORD);
        PlayerPrefs.Save();
        Debug.Log("Saved credentials cleared");
    }

    private async Task TryAutoLogin()
    {
        if (PlayerPrefs.HasKey(PREF_USERNAME) && PlayerPrefs.HasKey(PREF_PASSWORD))
        {
            string savedUsername = PlayerPrefs.GetString(PREF_USERNAME);
            string savedPassword = PlayerPrefs.GetString(PREF_PASSWORD);

            if (!string.IsNullOrEmpty(savedUsername) && !string.IsNullOrEmpty(savedPassword))
            {
                try
                {
                    Debug.Log("Attempting auto-login with saved credentials...");
                    await SignIn(savedUsername, savedPassword);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Auto-login failed: {ex.Message}");
                    // Clear invalid credentials
                    ClearSavedCredentials();
                }
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

        // Save credentials for auto-login
        SaveCredentials(username, password);

        SceneManager.LoadScene("Main - Copie");
    }

    public async Task SignIn(string username, string password)
    {
        await Init();

        await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
        Debug.Log($"Signed in OK. PlayerId={AuthenticationService.Instance.PlayerId}");

        // Lire pseudo (si tu veux l'afficher dans Main ou ailleurs)
        // Tu peux le faire ici ou dans Main, mais évite les appels cloud lourds au chargement de Main.
        PlayerProfileStore.DISPLAY_NAME = username;

        // Save credentials for auto-login
        SaveCredentials(username, password);

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
    /// Supprime le compte utilisateur et toutes les données associées
    /// Note: Unity Authentication ne permet pas de supprimer directement un compte.
    /// Cette méthode efface toutes les données locales et cloud, puis déconnecte l'utilisateur.
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

            // 3. Déconnexion
            AuthenticationService.Instance.SignOut();
            Debug.Log("[AuthController] Compte supprimé et déconnecté");

            // 4. Retour à l'écran de connexion
            SceneManager.LoadScene("LoginScreen");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthController] Erreur lors de la suppression du compte: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// URL pour demander la suppression définitive du compte Unity Gaming Services
    /// </summary>
    public const string ACCOUNT_DELETION_REQUEST_URL = "https://forms.gle/XHAcg1pbsvjDoK6D6";
}

