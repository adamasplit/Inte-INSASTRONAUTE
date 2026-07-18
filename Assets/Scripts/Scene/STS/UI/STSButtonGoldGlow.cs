using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class STSButtonGoldGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    public TextMeshProUGUI buttonText;
    public Color baseFaceColor = new Color(0.96f, 0.92f, 0.82f, 1f);
    public Color hoverFaceColor = new Color(1f, 0.96f, 0.78f, 1f);
    public Color pressedFaceColor = new Color(1f, 0.99f, 0.88f, 1f);
    public Color hoverGlowColor = new Color(1f, 0.82f, 0.2f, 1f);
    public Color pressedGlowColor = new Color(1f, 0.92f, 0.4f, 1f);
    public float hoverGlowPower = 0.18f;
    public float pressedGlowPower = 0.26f;
    public float hoverOutlineWidth = 0.04f;
    public float pressedOutlineWidth = 0.07f;
    public float transitionSpeed = 12f;

    Material materialInstance;
    bool hasFaceColor;
    bool hasGlowColor;
    bool hasGlowPower;
    bool hasOutlineWidth;
    bool isHovered;
    bool isPressed;
    float currentIntensity;
    Color cachedTextColor = Color.white;

    void Awake()
    {
        if (buttonText == null)
        {
            buttonText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        CacheMaterialState();
        ApplyVisuals(0f);
    }

    void OnEnable()
    {
        if (buttonText == null)
        {
            buttonText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        CacheMaterialState();
        ApplyVisuals(currentIntensity);
    }

    void OnDisable()
    {
        isHovered = false;
        isPressed = false;
        currentIntensity = 0f;
        ApplyVisuals(0f);
    }

    void Update()
    {
        float targetIntensity = isPressed ? 1f : isHovered ? 0.7f : 0f;
        currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, transitionSpeed * Time.unscaledDeltaTime);
        ApplyVisuals(currentIntensity);
    }

    void CacheMaterialState()
    {
        if (buttonText == null)
        {
            return;
        }

        cachedTextColor = buttonText.color;

        Material shared = buttonText.fontSharedMaterial;
        if (shared != null && materialInstance == null)
        {
            materialInstance = new Material(shared);
            buttonText.fontMaterial = materialInstance;
        }

        if (materialInstance == null)
        {
            materialInstance = buttonText.fontMaterial;
        }

        if (materialInstance != null)
        {
            hasFaceColor = materialInstance.HasProperty("_FaceColor");
            hasGlowColor = materialInstance.HasProperty("_GlowColor");
            hasGlowPower = materialInstance.HasProperty("_GlowPower");
            hasOutlineWidth = materialInstance.HasProperty("_OutlineWidth");
        }
    }

    void ApplyVisuals(float intensity)
    {
        if (buttonText == null)
        {
            return;
        }

        Color faceColor = Color.Lerp(baseFaceColor, isPressed ? pressedFaceColor : hoverFaceColor, intensity);
        buttonText.color = Color.Lerp(cachedTextColor, faceColor, intensity > 0f ? 1f : 0f);

        if (materialInstance == null)
        {
            return;
        }

        if (hasFaceColor)
        {
            materialInstance.SetColor("_FaceColor", faceColor);
        }

        if (hasGlowColor)
        {
            Color glowColor = Color.Lerp(hoverGlowColor, pressedGlowColor, isPressed ? 1f : 0f);
            materialInstance.SetColor("_GlowColor", Color.Lerp(Color.clear, glowColor, intensity));
        }

        if (hasGlowPower)
        {
            float glowPower = Mathf.Lerp(hoverGlowPower, pressedGlowPower, isPressed ? 1f : 0f);
            materialInstance.SetFloat("_GlowPower", glowPower * intensity);
        }

        if (hasOutlineWidth)
        {
            float outlineWidth = Mathf.Lerp(hoverOutlineWidth, pressedOutlineWidth, isPressed ? 1f : 0f);
            materialInstance.SetFloat("_OutlineWidth", outlineWidth * intensity);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        isHovered = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        isHovered = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }
}