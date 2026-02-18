using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Threading.Tasks;

/// <summary>
/// UI presentation layer for the tutorial system.
/// Handles overlay, highlighting, text display, and user interaction.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class TutorialUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image overlayImage;
    [SerializeField] private GameObject contentPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text stepCounterText;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextButtonText;
    [SerializeField] private Button skipButton;
    [SerializeField] private Image iconImage;
    
    [Header("Highlight")]
    [SerializeField] private RectTransform highlightCircle;
    [SerializeField] private RectTransform highlightRect;
    [SerializeField] private Image highlightImage;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private float pulseDuration = 1f;
    [SerializeField] private float pulseScale = 1.1f;
    
    [Header("Colors")]
    [SerializeField] private Color overlayColor = new Color(0, 0, 0, 0.7f);
    [SerializeField] private Color highlightColor = new Color(1, 1, 1, 0.2f);
    
    // State
    private TutorialStep currentStep;
    private RectTransform currentTarget;
    private Button currentTargetButton;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private bool isVisible = false;
    private bool waitingForTargetClick = false;
    
    // Events
    public event Action OnAdvanceRequested;
    public event Action OnSkipRequested;
    public event Action OnTargetClicked;
    
    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Ensure this canvas renders on top
        canvas.sortingOrder = 1600;
        
        // Setup button listeners
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(() => OnAdvanceRequested?.Invoke());
        }
        
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(() => OnSkipRequested?.Invoke());
        }
        
        // Start hidden
        gameObject.SetActive(false);
    }
    
    public async Task ShowStep(TutorialStep step, int currentStepNumber, int totalSteps)
    {
        currentStep = step;
        gameObject.SetActive(true);
        
        // Update text content
        if (titleText != null)
            titleText.text = step.title;
        
        if (descriptionText != null)
            descriptionText.text = step.description;
        
        if (stepCounterText != null)
            stepCounterText.text = $"{currentStepNumber}/{totalSteps}";
        
        if (nextButtonText != null)
            nextButtonText.text = step.buttonText;
        
        // Update icon
        if (iconImage != null)
        {
            if (step.icon != null)
            {
                iconImage.sprite = step.icon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }
        
        // Configure button visibility
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(step.advanceType == AdvanceType.Button);
        }
        
        // Setup target click detection
        waitingForTargetClick = (step.advanceType == AdvanceType.TargetClick || step.waitForTargetClick);
        
        // Update overlay
        if (overlayImage != null)
        {
            var color = overlayColor;
            color.a = step.overlayAlpha;
            overlayImage.color = color;
        }
        
        // Find and setup target
        await SetupHighlight(step);
        
        // Animate in
        await AnimateIn();
    }
    
    private async Task SetupHighlight(TutorialStep step)
    {
        // Remove previous button listener if any
        RemoveTargetButtonListener();
        
        // Hide all highlights first
        if (highlightCircle != null)
            highlightCircle.gameObject.SetActive(false);
        if (highlightRect != null)
            highlightRect.gameObject.SetActive(false);
        
        if (step.highlightType == HighlightType.None)
        {
            currentTarget = null;
            currentTargetButton = null;
            return;
        }
        
        // Find target
        RectTransform target = step.targetTransform;
        
        if (target == null && !string.IsNullOrEmpty(step.targetTag))
        {
            // Try to find by tag
            var targetObj = GameObject.FindGameObjectWithTag(step.targetTag);
            
            // If not found by tag, try by name
            if (targetObj == null)
            {
                targetObj = GameObject.Find(step.targetTag);
            }
            
            if (targetObj != null)
            {
                target = targetObj.GetComponent<RectTransform>();
            }
        }
        
        if (target == null)
        {
            Debug.LogWarning($"[TutorialUI] Target not found for step: {step.title}");
            return;
        }
        
        currentTarget = target;
        currentTargetButton = target.GetComponent<Button>();
        
        // Log what we found
        Debug.Log($"[TutorialUI] Found target: {target.name}, Has Button: {currentTargetButton != null}");
        
        // Add listener to the actual button if we're waiting for clicks
        if (waitingForTargetClick && currentTargetButton != null)
        {
            currentTargetButton.onClick.AddListener(OnTargetButtonClicked);
            Debug.Log($"[TutorialUI] Added click listener to button: {target.name}");
        }
        
        // Position and size the highlight
        RectTransform highlightTransform = null;
        
        switch (step.highlightType)
        {
            case HighlightType.Circle:
                if (highlightCircle != null)
                {
                    highlightCircle.gameObject.SetActive(true);
                    highlightTransform = highlightCircle;
                }
                break;
                
            case HighlightType.Rectangle:
                if (highlightRect != null)
                {
                    highlightRect.gameObject.SetActive(true);
                    highlightTransform = highlightRect;
                }
                break;
        }
        
        if (highlightTransform != null)
        {
            // Wait a frame to ensure layout is updated
            await Task.Yield();
            
            // Position highlight at target
            highlightTransform.position = target.position;
            
            // Size highlight to match target (with padding)
            var targetSize = target.rect.size;
            highlightTransform.sizeDelta = targetSize + step.highlightPadding;
            
            // Apply color
            if (highlightImage != null)
            {
                highlightImage.color = highlightColor;
            }
            
            // Start pulse animation if enabled
            if (step.pulseHighlight)
            {
                StartPulseAnimation(highlightTransform);
            }
        }
    }
    
    private void OnTargetButtonClicked()
    {
        Debug.Log($"[TutorialUI] Target button clicked!");
        
        // Notify that target was clicked
        OnTargetClicked?.Invoke();
        
        // Advance the tutorial if this step requires target click
        if (currentStep != null && currentStep.advanceType == AdvanceType.TargetClick)
        {
            Debug.Log($"[TutorialUI] Advancing tutorial after target click");
            OnAdvanceRequested?.Invoke();
        }
    }
    
    private void RemoveTargetButtonListener()
    {
        if (currentTargetButton != null)
        {
            currentTargetButton.onClick.RemoveListener(OnTargetButtonClicked);
            Debug.Log($"[TutorialUI] Removed click listener from button");
        }
    }
    
    private void StartPulseAnimation(RectTransform target)
    {
        if (target == null) return;
        
        LeanTween.cancel(target.gameObject);
        
        // Scale pulse
        LeanTween.scale(target.gameObject, Vector3.one * pulseScale, pulseDuration / 2f)
            .setEaseInOutSine()
            .setLoopPingPong();
        
        // Alpha pulse
        var image = target.GetComponent<Image>();
        if (image != null)
        {
            Color fromColor = image.color;
            Color toColor = fromColor;
            toColor.a = fromColor.a * 0.5f;
            
            LeanTween.value(target.gameObject, fromColor, toColor, pulseDuration / 2f)
                .setOnUpdate((Color color) => {
                    if (image != null)
                        image.color = color;
                })
                .setEaseInOutSine()
                .setLoopPingPong();
        }
    }
    
    private async Task AnimateIn()
    {
        if (canvasGroup == null) return;
        
        canvasGroup.alpha = 0f;
        isVisible = true;
        
        // Fade in overlay
        if (overlayImage != null)
        {
            var overlayCanvasGroup = overlayImage.GetComponent<CanvasGroup>();
            if (overlayCanvasGroup == null)
                overlayCanvasGroup = overlayImage.gameObject.AddComponent<CanvasGroup>();
            
            overlayCanvasGroup.alpha = 0f;
            LeanTween.alphaCanvas(overlayCanvasGroup, 1f, fadeInDuration)
                .setEaseOutCubic();
        }
        
        // Wait a bit for overlay
        await Task.Delay((int)(fadeInDuration * 0.5f * 1000));
        
        // Fade in content
        LeanTween.alphaCanvas(canvasGroup, 1f, fadeInDuration)
            .setEaseOutCubic();
        
        // Animate content panel
        if (contentPanel != null)
        {
            var panelTransform = contentPanel.GetComponent<RectTransform>();
            if (panelTransform != null)
            {
                Vector3 originalPos = panelTransform.localPosition;
                panelTransform.localPosition = originalPos + Vector3.down * 50f;
                
                LeanTween.moveLocal(contentPanel, originalPos, fadeInDuration)
                    .setEaseOutBack();
            }
        }
    }
    
    public async void Hide()
    {
        if (!isVisible) return;
        
        isVisible = false;
        waitingForTargetClick = false;
        
        // Remove button listener before clearing references
        RemoveTargetButtonListener();
        
        currentTarget = null;
        currentTargetButton = null;
        
        // Stop any ongoing animations
        if (highlightCircle != null)
            LeanTween.cancel(highlightCircle.gameObject);
        if (highlightRect != null)
            LeanTween.cancel(highlightRect.gameObject);
        
        // Fade out
        if (canvasGroup != null)
        {
            LeanTween.alphaCanvas(canvasGroup, 0f, fadeOutDuration)
                .setEaseInCubic()
                .setOnComplete(() => {
                    gameObject.SetActive(false);
                });
        }
        else
        {
            gameObject.SetActive(false);
        }
        
        await Task.Delay((int)(fadeOutDuration * 1000));
    }
    
    /// <summary>
    /// Update highlight position if target moves
    /// </summary>
    private void LateUpdate()
    {
        if (currentTarget != null && currentStep != null)
        {
            RectTransform highlightTransform = null;
            
            if (currentStep.highlightType == HighlightType.Circle && highlightCircle != null)
                highlightTransform = highlightCircle;
            else if (currentStep.highlightType == HighlightType.Rectangle && highlightRect != null)
                highlightTransform = highlightRect;
            
            if (highlightTransform != null && highlightTransform.gameObject.activeSelf)
            {
                // Update position to follow target
                highlightTransform.position = currentTarget.position;
            }
        }
    }
}
