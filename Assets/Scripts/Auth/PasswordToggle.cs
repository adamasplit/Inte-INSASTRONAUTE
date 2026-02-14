using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PasswordToggle : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private TMP_InputField passwordField;
    
    [Header("Toggle Button")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private Image toggleIcon;
    
    [Header("Icons")]
    [SerializeField] private Sprite showIcon; // Eye open icon
    [SerializeField] private Sprite hideIcon; // Eye closed/crossed icon
    
    [Header("Colors")]
    [SerializeField] private Color visibleColor = new Color(0.4f, 0.7f, 1.0f);
    [SerializeField] private Color hiddenColor = new Color(0.5f, 0.5f, 0.5f);
    
    [Header("Settings")]
    [SerializeField] private bool startHidden = true;
    [SerializeField] private bool enableHaptics = true;
    
    private bool isPasswordVisible;

    private void Start()
    {
        if (!passwordField)
        {
            Debug.LogError("[PasswordToggle] Password field not assigned!");
            return;
        }

        if (toggleButton)
        {
            toggleButton.onClick.AddListener(TogglePasswordVisibility);
        }

        // Set initial state
        isPasswordVisible = !startHidden;
        UpdatePasswordVisibility(startHidden);
    }

    public void TogglePasswordVisibility()
    {
        isPasswordVisible = !isPasswordVisible;
        UpdatePasswordVisibility(!isPasswordVisible);
        
        // Haptic feedback
        TriggerHaptic();
    }

    private void UpdatePasswordVisibility(bool hide)
    {
        if (!passwordField) return;

        if (hide)
        {
            // Hide password
            passwordField.contentType = TMP_InputField.ContentType.Password;
            passwordField.inputType = TMP_InputField.InputType.Password;
            
            if (toggleIcon)
            {
                toggleIcon.sprite = hideIcon ? hideIcon : toggleIcon.sprite;
                toggleIcon.color = hiddenColor;
            }
        }
        else
        {
            // Show password as plain text
            passwordField.contentType = TMP_InputField.ContentType.Standard;
            passwordField.inputType = TMP_InputField.InputType.Standard;
            
            if (toggleIcon)
            {
                toggleIcon.sprite = showIcon ? showIcon : toggleIcon.sprite;
                toggleIcon.color = visibleColor;
            }
        }

        // Force update the input field display
        passwordField.ForceLabelUpdate();
        
        // Restore caret position to end
        passwordField.caretPosition = passwordField.text.Length;
    }

    private void TriggerHaptic()
    {
        if (!enableHaptics) return;
        
        HapticFeedback.Light();
    }

    // Public method to set password field from external scripts
    public void SetPasswordField(TMP_InputField field)
    {
        passwordField = field;
    }

    // Public method for button click alternative
    public void OnToggleButtonClick()
    {
        TogglePasswordVisibility();
    }
}
