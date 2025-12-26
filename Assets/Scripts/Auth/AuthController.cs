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
        SceneManager.LoadScene("Main");
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

        SceneManager.LoadScene("Main");
    }

    public async Task SignIn(string username, string password)
    {
        await Init();

        await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
        Debug.Log($"Signed in OK. PlayerId={AuthenticationService.Instance.PlayerId}");

        // Lire pseudo (si tu veux l'afficher dans Main ou ailleurs)
        // Tu peux le faire ici ou dans Main, mais évite les appels cloud lourds au chargement de Main.
        PlayerProfileStore.DISPLAY_NAME = username;

        SceneManager.LoadScene("Main");
    }

    public void SignOut()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
            Debug.Log("✅ Déconnecté");
            SceneManager.LoadScene("LoginScreen");
        }
        else
        {
            Debug.Log("⚠️ Pas de session active");
        }
    }
}
