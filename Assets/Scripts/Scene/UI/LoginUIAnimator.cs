using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginUIAnimator : MonoBehaviour
{
    [Header("Tabs")]
    public Toggle loginToggle;
    public Toggle registerToggle;
    public RectTransform indicator;
    public TMP_Text loginTabText;
    public TMP_Text registerTabText;
    public Color selectedColor = new Color(0.2f, 0.6f, 1f);
    public Color unselectedColor = new Color(0.2f, 1f, 0.6f);

    [Header("Groups")]
    public CanvasGroup loginGroup;
    public CanvasGroup registerGroup;

    [Header("Form Fields")]
    public CanvasGroup signInFields;
    public CanvasGroup signUpFields;

    [Header("CTA")]
    public Button ctaButton;
    public TMP_Text ctaText;
    public AuthUIBinder authBinder;

    [Header("Card")]
    public RectTransform card;

    bool isAnimating = false;

    void Start()
    {
        // Init state
        loginToggle.isOn = true;
        registerGroup.alpha = 1;
        signUpFields.alpha = 0;

        AnimateCardEntry();

        loginToggle.onValueChanged.AddListener(isOn =>
        {
            if (isOn) SwitchToLogin();
        });

        registerToggle.onValueChanged.AddListener(isOn =>
        {
            if (isOn) SwitchToRegister();
        });
    }

    void AnimateCardEntry()
    {
        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        cg.alpha = 0;

        card.anchoredPosition += Vector2.down * 80;

        LeanTween.moveY(card, card.anchoredPosition.y + 80, 0.6f)
            .setEaseOutCubic();

        LeanTween.alphaCanvas(cg, 1f, 0.6f);
    }

    void SwitchToLogin()
    {
        if (isAnimating) return;
        isAnimating = true;

        AnimateIndicator(loginToggle.transform as RectTransform);
        SwitchGroups(signUpFields, signInFields);

        ctaText.text = "Se connecter";
        AnimateTextColor(loginTabText, selectedColor, 0.3f);
        AnimateTextColor(registerTabText, unselectedColor, 0.3f);

        isAnimating = false;
    }

    void SwitchToRegister()
    {
        if (isAnimating) return;
        isAnimating = true;

        AnimateIndicator(registerToggle.transform as RectTransform);
        SwitchGroups(signInFields, signUpFields);

        ctaText.text = "S'inscrire";
        AnimateTextColor(loginTabText, unselectedColor, 0.3f);
        AnimateTextColor(registerTabText, selectedColor, 0.3f);

        isAnimating = false;
    }

    void ShowGroup(CanvasGroup show, CanvasGroup hide)
    {
        // Hide (mais reste actif)
        hide.interactable = false;
        hide.blocksRaycasts = false;
        LeanTween.alphaCanvas(hide, 0f, 0.2f);

        // Show
        show.interactable = true;
        show.blocksRaycasts = true;
        LeanTween.alphaCanvas(show, 1f, 0.25f);
    }


    void AnimateIndicator(RectTransform target)
    {
        LeanTween.moveX(indicator, target.anchoredPosition.x, 0.25f)
            .setEaseOutQuad();
    }

    void AnimateTextColor(TMP_Text text, Color targetColor, float duration)
    {
        Color startColor = text.color;
        LeanTween.value(gameObject, startColor, targetColor, duration)
            .setOnUpdate((Color c) => {
                if (text != null) text.color = c;
            })
            .setEaseOutQuad();
    }

    void SwitchGroups(CanvasGroup hide, CanvasGroup show)
    {
        // Hide
        LeanTween.alphaCanvas(hide, 0f, 0.2f);
        LeanTween.moveLocalY(hide.gameObject, -15, 0.2f);

        // Show
        show.transform.localPosition = new Vector3(0, -15, 0);
        LeanTween.alphaCanvas(show, 1f, 0.25f)
            .setDelay(0.15f);
        LeanTween.moveLocalY(show.gameObject, 0, 0.25f)
            .setDelay(0.15f)
            .setEaseOutCubic();

        LeanTween.delayedCall(0.25f, () =>
        {
            ShowGroup(show, hide);
        });
    }

    public void OnCTAClick()
    {
        if (authBinder == null)
        {
            Debug.LogWarning("AuthUIBinder not assigned!");
            return;
        }

        if (loginToggle.isOn)
        {
            authBinder.OnClick_SignIn();
        }
        else
        {
            authBinder.OnClick_SignUp();
        }
    }

    public void OnCTAPress()
    {
        LeanTween.scale(ctaButton.gameObject, Vector3.one * 0.97f, 0.1f);
    }

    public void OnCTARelease()
    {
        LeanTween.scale(ctaButton.gameObject, Vector3.one, 0.15f)
            .setEaseOutBack();
    }
}