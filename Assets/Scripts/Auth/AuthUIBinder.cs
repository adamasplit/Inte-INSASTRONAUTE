using System;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;

public class AuthUIBinder : MonoBehaviour
{

    [Header("Sign In")]
    [SerializeField] private TMP_InputField signInUsername;
    [SerializeField] private TMP_InputField signInPassword;

    [Header("Sign Up")]
    [SerializeField] private TMP_InputField signUpUsername;
    [SerializeField] private TMP_InputField signUpPassword;
    [SerializeField] private TMP_InputField signUpPasswordConfirm;

    [Header("Feedback")]
    [SerializeField] private TMP_Text statusText;

    public async void OnClick_Guest()
    {
        await Run(async () =>
        {
            await AuthController.Instance.SignInGuest();
            SetStatus("Connecté en invité");
        });
    }

    public async void OnClick_SignIn()
    {
        await Run(async () =>
        {
            var u = (signInUsername ? signInUsername.text : "").Trim();
            var p = signInPassword ? signInPassword.text : "";

            ValidateNotEmpty(u, "Username requis");
            ValidateNotEmpty(p, "Mot de passe requis");

            await AuthController.Instance.SignIn(u, p);
            SetStatus("Connexion réussie");
        });
    }

    public async void OnClick_SignUp()
    {
        await Run(async () =>
        {
            var u = (signUpUsername ? signUpUsername.text : "").Trim();
            var p = signUpPassword ? signUpPassword.text : "";
            var c = signUpPasswordConfirm ? signUpPasswordConfirm.text : "";

            ValidateNotEmpty(u, "Username requis");
            ValidateNotEmpty(p, "Mot de passe requis");

            if (signUpPasswordConfirm != null && p != c)
                throw new Exception("Les mots de passe ne correspondent pas.");

            await AuthController.Instance.SignUp(u, p);
            SetStatus("Compte créé");
        });
    }

    // Bonus : pour conserver la progression quand on passe d’invité à compte
    // Nécessite d'être déjà connecté en invité.
    public async void OnClick_LinkGuestToAccount()
    {
        await Run(async () =>
        {
            var u = (signUpUsername ? signUpUsername.text : "").Trim();
            var p = signUpPassword ? signUpPassword.text : "";

            ValidateNotEmpty(u, "Username requis");
            ValidateNotEmpty(p, "Mot de passe requis");

            if (!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn)
                throw new Exception("Tu dois être connecté en invité avant de lier un compte.");

            // Lien officiel UGS : link username/password à l'identité actuelle (invité)
            await Unity.Services.Authentication.AuthenticationService.Instance.AddUsernamePasswordAsync(u, p);

            SetStatus("Compte lié ✅ (progression conservée)");
        });
    }

    private async Task Run(Func<Task> op)
    {
        try
        {
            SetStatus("...");
            await op();
        }
        catch (AuthenticationException ae)
        {
            SetStatus(MapAuthError(ae));
        }
        catch (RequestFailedException rfe)
        {
            SetStatus($"Erreur réseau/serveur: {rfe.ErrorCode}");
        }
        catch (Exception e)
        {
            SetStatus(e.Message);
        }
    }

    private void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
        Debug.Log($"[AuthUI] {msg}");
    }

    private static void ValidateNotEmpty(string v, string msg)
    {
        if (string.IsNullOrWhiteSpace(v)) throw new Exception(msg);
    }

    private static string MapAuthError(AuthenticationException ae)
    {
        // Messages “user friendly” basiques
        // Tu peux enrichir selon tes retours de test.
        return ae.Message switch
        {
            _ => $"Erreur d’auth: {ae.Message}"
        };
    }
}
