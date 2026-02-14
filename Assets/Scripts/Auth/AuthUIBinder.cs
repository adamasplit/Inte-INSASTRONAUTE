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
    [SerializeField] private TMP_Text signInUsernameHint;
    [SerializeField] private TMP_Text signInPasswordHint;

    [Header("Sign Up")]
    [SerializeField] private TMP_InputField signUpUsername;
    [SerializeField] private TMP_InputField signUpPassword;
    [SerializeField] private TMP_InputField signUpPasswordConfirm;
    [SerializeField] private TMP_Text signUpUsernameHint;
    [SerializeField] private TMP_Text signUpPasswordHint;

    [Header("Feedback")]
    [SerializeField] private TMP_Text statusText;

    [Header("Validation Colors")]
    [SerializeField] private Color validColor = new Color(0.2f, 0.8f, 0.4f);
    [SerializeField] private Color invalidColor = new Color(0.9f, 0.3f, 0.3f);
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(0.4f, 0.7f, 1.0f, 1f); // Bright blue highlight
    [SerializeField] private Color caretColor = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark caret for visibility
    [SerializeField] private int caretWidth = 2; // Wider caret for mobile
    [SerializeField] private Color hintTextColor = new Color(0.6f, 0.6f, 0.6f); // Gray hint text
    [SerializeField] private Color warningTextColor = new Color(0.9f, 0.6f, 0.3f); // Orange warning

    [Header("Haptic Feedback")]
    [SerializeField] private bool enableHaptics = true;

    private Vector3 statusTextInitialPos;
    private TMP_InputField currentSelectedField;
    private Color currentFieldOriginalColor;

    private void Start()
    {
        if (statusText)
            statusTextInitialPos = statusText.transform.localPosition;

        // Add real-time validation listeners
        if (signInUsername)
        {
            signInUsername.onValueChanged.AddListener(_ => ValidateSignInFields());
            signInUsername.onSelect.AddListener(_ => OnFieldSelected(signInUsername));
            signInUsername.onDeselect.AddListener(_ => OnFieldDeselected(signInUsername));
        }
        if (signInPassword)
        {
            signInPassword.onValueChanged.AddListener(_ => ValidateSignInFields());
            signInPassword.onSelect.AddListener(_ => OnFieldSelected(signInPassword));
            signInPassword.onDeselect.AddListener(_ => OnFieldDeselected(signInPassword));
        }
        
        if (signUpUsername)
        {
            signUpUsername.onValueChanged.AddListener(_ => ValidateSignUpFields());
            signUpUsername.onSelect.AddListener(_ => OnFieldSelected(signUpUsername));
            signUpUsername.onDeselect.AddListener(_ => OnFieldDeselected(signUpUsername));
        }
        if (signUpPassword)
        {
            signUpPassword.onValueChanged.AddListener(_ => ValidateSignUpFields());
            signUpPassword.onSelect.AddListener(_ => OnFieldSelected(signUpPassword));
            signUpPassword.onDeselect.AddListener(_ => OnFieldDeselected(signUpPassword));
        }
        if (signUpPasswordConfirm)
        {
            signUpPasswordConfirm.onValueChanged.AddListener(_ => ValidateSignUpFields());
            signUpPasswordConfirm.onSelect.AddListener(_ => OnFieldSelected(signUpPasswordConfirm));
            signUpPasswordConfirm.onDeselect.AddListener(_ => OnFieldDeselected(signUpPasswordConfirm));
        }

        // Mobile optimization: Configure input fields
        ConfigureForMobile();
    }

    public async void OnClick_Guest()
    {
        TriggerHaptic();
        await Run(async () =>
        {
            await AuthController.Instance.SignInGuest();
            SetStatus("Connecté en mode invité");
        });
    }

    public async void OnClick_SignIn()
    {
        TriggerHaptic();
        await Run(async () =>
        {
            var u = (signInUsername ? signInUsername.text : "").Trim();
            var p = signInPassword ? signInPassword.text : "";

            ValidateNotEmpty(u, "Veuillez entrer votre nom d'utilisateur");
            ValidateNotEmpty(p, "Veuillez entrer votre mot de passe");

            await AuthController.Instance.SignIn(u, p);
            SetStatus("Vous êtes connecté !");
        });
    }

    public async void OnClick_SignUp()
    {
        TriggerHaptic();
        await Run(async () =>
        {
            var u = (signUpUsername ? signUpUsername.text : "").Trim();
            var p = signUpPassword ? signUpPassword.text : "";
            var c = signUpPasswordConfirm ? signUpPasswordConfirm.text : "";

            ValidateNotEmpty(u, "Veuillez choisir un nom d'utilisateur");
            ValidateNotEmpty(p, "Veuillez choisir un mot de passe");

            if (p.Length < 8)
                throw new Exception("Le mot de passe doit contenir au moins 8 caractères");

            if (signUpPasswordConfirm != null && p != c)
                throw new Exception("Les mots de passe ne correspondent pas");

            await AuthController.Instance.SignUp(u, p);
            SetStatus("Compte créé avec succès !");
        });
    }

    // Bonus : pour conserver la progression quand on passe d’invité à compte
    // Nécessite d'être déjà connecté en invité.
    public async void OnClick_LinkGuestToAccount()
    {
        TriggerHaptic();
        await Run(async () =>
        {
            var u = (signUpUsername ? signUpUsername.text : "").Trim();
            var p = signUpPassword ? signUpPassword.text : "";

            ValidateNotEmpty(u, "Veuillez choisir un nom d'utilisateur");
            ValidateNotEmpty(p, "Veuillez choisir un mot de passe");

            if (p.Length < 8)
                throw new Exception("Le mot de passe doit contenir au moins 8 caractères");

            if (!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn)
                throw new Exception("Vous devez être connecté en mode invité pour créer un compte");

            // Lien officiel UGS : link username/password à l'identité actuelle (invité)
            await Unity.Services.Authentication.AuthenticationService.Instance.AddUsernamePasswordAsync(u, p);

            SetStatus("Compte créé ! Votre progression a été conservée");
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
            TriggerHaptic(30); // Lighter vibration for errors
            SetStatus(MapAuthError(ae));
        }
        catch (RequestFailedException rfe)
        {
            TriggerHaptic(30);
            SetStatus(MapRequestError(rfe));
        }
        catch (Exception e)
        {
            TriggerHaptic(30);
            SetStatus(e.Message);
        }
    }

    private void SetStatus(string msg)
    {
        if (!statusText) return;
        
        statusText.text = msg;
        Debug.Log($"[AuthUI] {msg}");
        
        // Annuler toutes les animations en cours pour éviter les conflits
        LeanTween.cancel(statusText.gameObject);
        statusText.transform.localPosition = statusTextInitialPos;
        statusText.transform.localScale = Vector3.one;
        
        // Détecter si c'est une erreur (ne contient pas de mots de succès)
        bool isError = !msg.Contains("Connecté") && !msg.Contains("créé") && !msg.Contains("...") && msg.Length > 5;
        
        if (isError)
        {
            // Animation de shake horizontal
            LeanTween.moveLocalX(statusText.gameObject, statusTextInitialPos.x - 5f, 0.05f)
                .setLoopPingPong(3)
                .setEaseShake();
            
            // Pulse de scale pour attirer l'attention
            statusText.transform.localScale = Vector3.one * 0.8f;
            LeanTween.scale(statusText.gameObject, Vector3.one, 0.3f)
                .setEaseOutBack();
        }
        else if (msg != "...")
        {
            // Animation douce pour les messages de succès
            statusText.transform.localScale = Vector3.one * 0.9f;
            LeanTween.scale(statusText.gameObject, Vector3.one, 0.2f)
                .setEaseOutCubic();
        }
    }

    private static void ValidateNotEmpty(string v, string msg)
    {
        if (string.IsNullOrWhiteSpace(v)) throw new Exception(msg);
    }

    private static string MapAuthError(AuthenticationException ae)
    {
        var msg = ae.Message.ToLower();
        
        if (msg.Contains("player not found") || msg.Contains("user not found"))
            return "Nom d'utilisateur inconnu";
        
        if (msg.Contains("invalid password") || msg.Contains("wrong password"))
            return "Mot de passe incorrect";
        
        if (msg.Contains("already exists") || msg.Contains("username taken"))
            return "Ce nom d'utilisateur est déjà pris";
        
        if (msg.Contains("invalid username"))
            return "Nom d'utilisateur invalide (3-20 caractères)";
        
        if (msg.Contains("password too short"))
            return "Le mot de passe doit contenir au moins 8 caractères, 1 majuscule, 1 chiffre, 1 minuscule et 1 caractère spécial";
        
        if (msg.Contains("network") || msg.Contains("timeout"))
            return "Problème de connexion. Réessayez dans quelques instants";
        
        if (msg.Contains("rate limit") || msg.Contains("too many"))
            return "Trop de tentatives. Patientez quelques minutes";
        
        return "Une erreur est survenue. Réessayez plus tard";
    }

    private static string MapRequestError(RequestFailedException rfe)
    {
        // Vérifier d'abord le message d'erreur pour les erreurs d'authentification spécifiques
        var message = rfe.Message.ToLower();
        
        if (message.Contains("wrong_username_password") || message.Contains("invalid username or password"))
            return "Nom d'utilisateur ou mot de passe incorrect";
        
        if (message.Contains("username taken") || message.Contains("already exists"))
            return "Ce nom d'utilisateur est déjà pris";
        
        if (message.Contains("invalid username"))
            return "Nom d'utilisateur invalide (3-20 caractères)";
        
        if (message.Contains("invalid_password") || message.Contains("password does not match requirements"))
            return "Le mot de passe doit contenir au moins 8 caractères, 1 majuscule, 1 chiffre, 1 minuscule et 1 caractère spécial";
        
        if (message.Contains("password too short"))
            return "Le mot de passe doit contenir au moins 8 caractères, 1 majuscule, 1 chiffre, 1 minuscule et 1 caractère spécial";
        
        // Erreurs HTTP/Réseau détaillées
        var errorCode = rfe.ErrorCode;
        
        if (errorCode >= 500 && errorCode < 600)
            return $"Le serveur rencontre des difficultés (erreur {errorCode}). Réessayez plus tard";
        
        if (errorCode == 408 || errorCode == 504)
            return "Le serveur met trop de temps à répondre. Vérifiez votre connexion";
        
        if (errorCode == 429)
            return "Trop de requêtes. Patientez quelques minutes";
        
        if (errorCode == 401 || errorCode == 403)
            return "Accès refusé. Vérifiez vos identifiants";
        
        if (errorCode == 404)
            return "Service introuvable. L'application nécessite une mise à jour";
        
        if (errorCode >= 400 && errorCode < 500)
            return "Erreur de requête. Vérifiez vos informations";
        
        if (errorCode == 0 || errorCode == -1)
            return "Pas de connexion internet. Vérifiez votre réseau";
        
        // Message par défaut avec le code d'erreur
        return $"Erreur réseau ({errorCode}). Vérifiez votre connexion";
    }

    private void ValidateSignInFields()
    {
        // Username validation
        if (signInUsername)
        {
            var username = signInUsername.text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                SetFieldColor(signInUsername, neutralColor);
                SetHint(signInUsernameHint, "");
            }
            else if (username.Length >= 3 && username.Length <= 20)
            {
                SetFieldColor(signInUsername, validColor);
                SetHint(signInUsernameHint, "Valide !", validColor);
            }
            else
            {
                SetFieldColor(signInUsername, invalidColor);
                if (username.Length < 3)
                    SetHint(signInUsernameHint, $"Trop court ({username.Length}/3)", warningTextColor);
                else
                    SetHint(signInUsernameHint, $"Trop long ({username.Length}/20)", warningTextColor);
            }
        }

        // Password validation
        if (signInPassword)
        {
            var password = signInPassword.text;
            if (string.IsNullOrEmpty(password))
            {
                SetFieldColor(signInPassword, neutralColor);
                SetHint(signInPasswordHint, "");
            }
            else if (password.Length >= 8)
            {
                SetFieldColor(signInPassword, validColor);
                SetHint(signInPasswordHint, " OK", validColor);
            }
            else
            {
                SetFieldColor(signInPassword, invalidColor);
                SetHint(signInPasswordHint, $"Minimum 8 caractères ({password.Length}/8)", warningTextColor);
            }
        }
    }

    private void ValidateSignUpFields()
    {
        // Username validation
        if (signUpUsername)
        {
            var username = signUpUsername.text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                SetFieldColor(signUpUsername, neutralColor);
                SetHint(signUpUsernameHint, "");
            }
            else if (username.Length >= 3 && username.Length <= 20)
            {
                SetFieldColor(signUpUsername, validColor);
                SetHint(signUpUsernameHint, " Nom d'utilisateur valide", validColor);
            }
            else
            {
                SetFieldColor(signUpUsername, invalidColor);
                if (username.Length < 3)
                    SetHint(signUpUsernameHint, $"Trop court ({username.Length}/3)", warningTextColor);
                else
                    SetHint(signUpUsernameHint, $"Trop long ({username.Length}/20)", warningTextColor);
            }
        }

        // Password validation
        if (signUpPassword)
        {
            var password = signUpPassword.text;
            if (string.IsNullOrEmpty(password))
            {
                SetFieldColor(signUpPassword, neutralColor);
                SetHint(signUpPasswordHint, "Min 8 caractères, 1 majuscule, 1 minuscule, 1 chiffre, 1 symbole");
            }
            else if (IsPasswordValid(password))
            {
                SetFieldColor(signUpPassword, validColor);
                SetHint(signUpPasswordHint, " Mot de passe fort", validColor);
            }
            else
            {
                SetFieldColor(signUpPassword, invalidColor);
                SetHint(signUpPasswordHint, GetPasswordRequirementsHint(password), warningTextColor);
            }
        }

        // Password confirmation validation
        if (signUpPasswordConfirm)
        {
            var password = signUpPassword ? signUpPassword.text : "";
            var confirm = signUpPasswordConfirm.text;
            
            if (string.IsNullOrEmpty(confirm))
            {
                SetFieldColor(signUpPasswordConfirm, neutralColor);
            }
            else if (confirm == password && IsPasswordValid(password))
            {
                SetFieldColor(signUpPasswordConfirm, validColor);
            }
            else
            {
                SetFieldColor(signUpPasswordConfirm, invalidColor);
            }
        }
    }

    private bool IsPasswordValid(string password)
    {
        if (password.Length < 8 || password.Length > 30) return false;
        
        bool hasUpper = false, hasLower = false, hasDigit = false, hasSymbol = false;
        
        foreach (char c in password)
        {
            if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsLower(c)) hasLower = true;
            else if (char.IsDigit(c)) hasDigit = true;
            else if (!char.IsLetterOrDigit(c)) hasSymbol = true;
        }
        
        return hasUpper && hasLower && hasDigit && hasSymbol;
    }

    private void SetFieldColor(TMP_InputField field, Color color)
    {
        if (field && field.image)
        {
            field.image.color = color;
        }
    }

    private void SetHint(TMP_Text hintText, string text, Color? color = null)
    {
        if (!hintText) return;
        
        hintText.text = text;
        hintText.color = color ?? hintTextColor;
    }

    private string GetPasswordRequirementsHint(string password)
    {
        var missing = new System.Collections.Generic.List<string>();
        
        if (password.Length < 8)
            missing.Add($"{password.Length}/8 caractères");
        
        bool hasUpper = false, hasLower = false, hasDigit = false, hasSymbol = false;
        
        foreach (char c in password)
        {
            if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsLower(c)) hasLower = true;
            else if (char.IsDigit(c)) hasDigit = true;
            else if (!char.IsLetterOrDigit(c)) hasSymbol = true;
        }
        
        if (!hasUpper) missing.Add("majuscule");
        if (!hasLower) missing.Add("minuscule");
        if (!hasDigit) missing.Add("chiffre");
        if (!hasSymbol) missing.Add("symbole (!@#$%)");
        
        if (missing.Count == 0)
            return " Mot de passe fort";
        
        return "Manque: " + string.Join(" • ", missing);
    }

    private void OnFieldSelected(TMP_InputField field)
    {
        if (!field || !field.image) return;
        
        TriggerHaptic(8); // Very light haptic feedback
        
        currentSelectedField = field;
        currentFieldOriginalColor = field.image.color;
        
        // Apply selection highlight
        field.image.color = selectedColor;
    }

    private void OnFieldDeselected(TMP_InputField field)
    {
        if (!field || !field.image) return;
        
        currentSelectedField = null;
        
        // Restore validation color based on current content
        if (field == signInUsername || field == signInPassword)
            ValidateSignInFields();
        else
            ValidateSignUpFields();
    }

    private void ConfigureForMobile()
    {
        // Configure username fields for mobile
        if (signInUsername)
        {
            signInUsername.contentType = TMP_InputField.ContentType.Standard;
            signInUsername.keyboardType = TouchScreenKeyboardType.Default;
            signInUsername.inputType = TMP_InputField.InputType.Standard;
            signInUsername.characterValidation = TMP_InputField.CharacterValidation.None;
            signInUsername.lineType = TMP_InputField.LineType.SingleLine;
            ConfigureCaret(signInUsername);
        }
        
        if (signUpUsername)
        {
            signUpUsername.contentType = TMP_InputField.ContentType.Standard;
            signUpUsername.keyboardType = TouchScreenKeyboardType.Default;
            signUpUsername.inputType = TMP_InputField.InputType.Standard;
            signUpUsername.characterValidation = TMP_InputField.CharacterValidation.None;
            signUpUsername.lineType = TMP_InputField.LineType.SingleLine;
            ConfigureCaret(signUpUsername);
        }

        // Configure password fields for mobile
        if (signInPassword)
        {
            signInPassword.contentType = TMP_InputField.ContentType.Password;
            signInPassword.inputType = TMP_InputField.InputType.Password;
            signInPassword.lineType = TMP_InputField.LineType.SingleLine;
            ConfigureCaret(signInPassword);
        }
        
        if (signUpPassword)
        {
            signUpPassword.contentType = TMP_InputField.ContentType.Password;
            signUpPassword.inputType = TMP_InputField.InputType.Password;
            signUpPassword.lineType = TMP_InputField.LineType.SingleLine;
            ConfigureCaret(signUpPassword);
        }
        
        if (signUpPasswordConfirm)
        {
            signUpPasswordConfirm.contentType = TMP_InputField.ContentType.Password;
            signUpPasswordConfirm.inputType = TMP_InputField.InputType.Password;
            signUpPasswordConfirm.lineType = TMP_InputField.LineType.SingleLine;
            ConfigureCaret(signUpPasswordConfirm);
        }
    }

    private void ConfigureCaret(TMP_InputField field)
    {
        if (!field) return;
        
        // Make caret visible and wider for mobile
        field.customCaretColor = true;
        field.caretColor = caretColor;
        field.caretWidth = caretWidth;
        
        // Ensure caret blink rate is visible
        field.caretBlinkRate = 0.85f;
        
        // Set selection color for better visibility
        field.selectionColor = new Color(0.65f, 0.8f, 1f, 0.5f);
    }

    private void TriggerHaptic(int durationMs = 15)
    {
        if (enableHaptics)
        {
            HapticFeedback.Trigger(durationMs);
        }
    }
}
