using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// UI presentation layer for the tutorial system.
/// Handles overlay, highlighting, text display, and user interaction.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class TutorialUI : MonoBehaviour
{
    public static TutorialUI Instance { get; private set; }

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
    [SerializeField] private MainUIBinder mainUIBinder;

    [Header("Skip Confirmation")]
    [SerializeField] private GameObject skipConfirmPanel;
    [SerializeField] private TMP_Text skipConfirmMessageText;
    [SerializeField] private string skipConfirmMessage = "Passer le tutoriel ?";
    [SerializeField] private Button skipConfirmYesButton;
    [SerializeField] private Button skipConfirmNoButton;
    
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
    private readonly List<RectTransform> currentTargets = new List<RectTransform>();
    private readonly List<Button> currentTargetButtons = new List<Button>();
    private readonly Dictionary<Selectable, bool> lockedSelectables = new Dictionary<Selectable, bool>();
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private bool isVisible = false;
    private bool waitingForTargetClick = false;
    private bool lockInputToTarget = false;
    private bool allowParentTagClick = true;
    
    // Events
    public event Action OnAdvanceRequested;
    public event Action OnSkipRequested;
    public event Action OnTargetClicked;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

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
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }

        if (skipConfirmYesButton != null)
        {
            skipConfirmYesButton.onClick.AddListener(OnSkipConfirmed);
        }

        if (skipConfirmNoButton != null)
        {
            skipConfirmNoButton.onClick.AddListener(HideSkipConfirmation);
        }

        if (skipConfirmPanel != null)
        {
            skipConfirmPanel.SetActive(false);
        }
        
        // Start hidden
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    public IEnumerator ShowStep(TutorialStep step, int currentStepNumber, int totalSteps, bool deferTargetSearch = false)
    {
        currentStep = step;
        gameObject.SetActive(true);

        SetStepContentVisible(true);
        
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

        UpdateSkipButtonVisibility();
        HideSkipConfirmation();
        
        // Setup target click detection
        waitingForTargetClick = (step.advanceType == AdvanceType.TargetClick || step.waitForTargetClick);
        allowParentTagClick = step.allowParentTagClick;
        lockInputToTarget = waitingForTargetClick && step.lockInputToTarget;
        
        // Update overlay
        if (overlayImage != null)
        {
            var color = overlayColor;
            color.a = step.overlayAlpha;
            overlayImage.color = color;
        }
        
        // Find and setup target (defer if requested to allow animations/spawning)
        if (!deferTargetSearch)
        {
            yield return StartCoroutine(SetupHighlightCoroutine(step));
        }
        
        // Animate in
        yield return StartCoroutine(AnimateInCoroutine());
    }

    public IEnumerator RefreshTargets()
    {
        if (currentStep != null)
        {
            yield return StartCoroutine(SetupHighlightCoroutine(currentStep));
        }
    }
    
    private IEnumerator SetupHighlightCoroutine(TutorialStep step)
    {
        // Remove previous button listener if any
        RemoveTargetButtonListener();
        RestoreLockedSelectables();
        currentTargets.Clear();
        
        // Hide all highlights first
        if (highlightCircle != null)
            highlightCircle.gameObject.SetActive(false);
        if (highlightRect != null)
            highlightRect.gameObject.SetActive(false);
        
        if (step.highlightType == HighlightType.None)
        {
            currentTarget = null;
            currentTargetButton = null;
            yield break;
        }
        
        currentTargets.AddRange(FindTargets(step));

        if (currentTargets.Count == 0)
        {
            Debug.LogWarning($"[TutorialUI] Target not found for step: {step.title}");
            yield break;
        }

        currentTarget = currentTargets[0];

        foreach (var target in currentTargets)
        {
            var targetButton = ResolveTargetButton(target, allowParentTagClick);
            if (targetButton != null && !currentTargetButtons.Contains(targetButton))
            {
                currentTargetButtons.Add(targetButton);
            }
        }

        currentTargetButton = currentTargetButtons.Count > 0 ? currentTargetButtons[0] : null;

        Debug.Log($"[TutorialUI] Found {currentTargets.Count} target(s), clickable buttons: {currentTargetButtons.Count}");

        if (waitingForTargetClick)
        {
            foreach (var button in currentTargetButtons)
            {
                button.onClick.AddListener(OnTargetButtonClicked);
            }

            if (currentTargetButtons.Count == 0)
            {
                Debug.LogWarning($"[TutorialUI] Step '{step.title}' expects target clicks but no Button was found on target or parent.");
            }
        }

        if (lockInputToTarget)
        {
            LockInputToTargets();
        }
        
        // Position and size the highlight
        RectTransform highlightTransform = null;
        
        switch (step.highlightType)
        {
            case HighlightType.Circle:
                if (highlightCircle != null)
                    highlightTransform = highlightCircle;
                break;
                
            case HighlightType.Rectangle:
                if (highlightRect != null)
                    highlightTransform = highlightRect;
                break;
        }
        
        if (highlightTransform != null)
        {
            // Keep hidden while we compute position/size to avoid a one-frame visual glitch
            highlightTransform.gameObject.SetActive(false);

            // Wait for end of frame — guarantees the target canvas has completed its layout pass
            // This also fixes the bottom-left position bug after scene transitions
            yield return new WaitForEndOfFrame();
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();

            // Position highlight at target
            Vector3 targetWorldPos = currentTarget.position;
            highlightTransform.position = targetWorldPos;
            
            // Size highlight to match target (with padding)
            var targetSize = currentTarget.rect.size;
            var scaleFactor = Mathf.Max(0.01f, step.highlightScaleFactor);
            highlightTransform.sizeDelta = (targetSize + step.highlightPadding) * scaleFactor;

            // Apply color
            if (highlightImage != null)
            {
                highlightImage.color = highlightColor;
            }

            // Show only after position and size are correct
            highlightTransform.gameObject.SetActive(true);
            
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

        // Hide highlights immediately on click
        HideHighlights();
        
        // Notify that target was clicked
        OnTargetClicked?.Invoke();
        
        // Advance the tutorial if this step requires target click
        if (currentStep != null && currentStep.advanceType == AdvanceType.TargetClick)
        {
            Debug.Log($"[TutorialUI] Advancing tutorial after target click");
            OnAdvanceRequested?.Invoke();
        }
    }

    private void HideHighlights()
    {
        if (highlightCircle != null)
        {
            LeanTween.cancel(highlightCircle.gameObject);
            highlightCircle.gameObject.SetActive(false);
        }
        if (highlightRect != null)
        {
            LeanTween.cancel(highlightRect.gameObject);
            highlightRect.gameObject.SetActive(false);
        }
    }
    
    private void RemoveTargetButtonListener()
    {
        foreach (var targetButton in currentTargetButtons)
        {
            if (targetButton != null)
            {
                targetButton.onClick.RemoveListener(OnTargetButtonClicked);
            }
        }

        currentTargetButtons.Clear();
        currentTargetButton = null;
    }

    private List<RectTransform> FindTargets(TutorialStep step)
    {
        var results = new List<RectTransform>();

        if (step.targetTransform != null)
        {
            results.Add(step.targetTransform);
            return results;
        }

        if (string.IsNullOrEmpty(step.targetTag))
        {
            return results;
        }

        GameObject[] foundByTag = Array.Empty<GameObject>();
        try
        {
            foundByTag = GameObject.FindGameObjectsWithTag(step.targetTag);
        }
        catch (UnityException)
        {
            // Ignore invalid tag and fallback to name search below
        }

        foreach (var obj in foundByTag)
        {
            if (obj == null || !obj.activeInHierarchy)
                continue;

            // If parentTag is specified, check if any parent matches
            if (!string.IsNullOrEmpty(step.parentTag))
            {
                if (!HasParentWithTag(obj.transform, step.parentTag))
                    continue;
            }

            var rect = obj.GetComponent<RectTransform>() ?? obj.GetComponentInParent<RectTransform>();
            if (rect != null && !results.Contains(rect))
            {
                results.Add(rect);
            }
        }

        if (results.Count == 0)
        {
            var byName = GameObject.Find(step.targetTag);
            if (byName != null && byName.activeInHierarchy)
            {
                // Check parent tag filter if specified
                if (!string.IsNullOrEmpty(step.parentTag))
                {
                    if (!HasParentWithTag(byName.transform, step.parentTag))
                        return results;
                }

                var rect = byName.GetComponent<RectTransform>() ?? byName.GetComponentInParent<RectTransform>();
                if (rect != null)
                {
                    results.Add(rect);
                }
            }
        }

        if (!step.allowAnyMatchingTarget && results.Count > 1)
        {
            return new List<RectTransform> { results[0] };
        }

        if (step.highlightFirstAvailable && results.Count > 0)
        {
            currentTarget = results[0];
        }

        return results;
    }

    private bool HasParentWithTag(Transform target, string tag)
    {
        if (target == null || string.IsNullOrEmpty(tag))
            return false;

        var current = target.parent;
        while (current != null)
        {
            if (current.CompareTag(tag))
                return true;
            current = current.parent;
        }

        return false;
    }

    private Button ResolveTargetButton(RectTransform target, bool allowParent)
    {
        if (target == null) return null;

        var ownButton = target.GetComponent<Button>();
        if (ownButton != null)
            return ownButton;

        if (allowParent)
        {
            return target.GetComponentInParent<Button>();
        }

        return null;
    }

    private void LockInputToTargets()
    {
        var allowed = new HashSet<Selectable>();

        if (nextButton != null)
            allowed.Add(nextButton);
        if (skipButton != null)
            allowed.Add(skipButton);

        foreach (var button in currentTargetButtons)
        {
            if (button != null)
                allowed.Add(button);
        }

        if (waitingForTargetClick && currentTargetButtons.Count == 0)
        {
            Debug.LogWarning("[TutorialUI] Input lock skipped: no clickable target buttons found.");
            return;
        }

        var allSelectables = FindObjectsByType<Selectable>(FindObjectsSortMode.None);
        foreach (var selectable in allSelectables)
        {
            if (selectable == null || selectable == nextButton || selectable == skipButton)
                continue;

            if (!lockedSelectables.ContainsKey(selectable))
            {
                lockedSelectables.Add(selectable, selectable.interactable);
            }

            selectable.interactable = allowed.Contains(selectable);
        }
    }

    private void RestoreLockedSelectables()
    {
        foreach (var pair in lockedSelectables)
        {
            if (pair.Key != null)
            {
                pair.Key.interactable = pair.Value;
            }
        }

        lockedSelectables.Clear();
    }

    public IEnumerator FadeOverlayTo(float targetAlpha, float duration)
    {
        if (overlayImage == null)
            yield break;

        var startColor = overlayImage.color;
        var endColor = startColor;
        endColor.a = Mathf.Clamp01(targetAlpha);

        if (duration <= 0f)
        {
            overlayImage.color = endColor;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            overlayImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        overlayImage.color = endColor;
    }

    public void SetStepContentVisible(bool visible)
    {
        if (contentPanel != null)
        {
            contentPanel.SetActive(visible);
        }
    }

    private bool IsInMainScene()
    {
        return SceneManager.GetActiveScene().name == "Main - Copie";
    }

    private MainUIBinder ResolveMainUIBinder()
    {
        if (mainUIBinder == null)
        {
            mainUIBinder = FindFirstObjectByType<MainUIBinder>();
        }

        return mainUIBinder;
    }

    private void UpdateSkipButtonVisibility()
    {
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(IsInMainScene());
        }
    }

    private void OnSkipButtonClicked()
    {
        if (!IsInMainScene())
            return;

        var binder = ResolveMainUIBinder();
        if (binder != null)
        {
            binder.ShowConfirmation(
                "Passer le tutoriel",
                skipConfirmMessage,
                OnSkipConfirmed,
                null
            );
            return;
        }

        if (skipConfirmPanel == null)
        {
            OnSkipRequested?.Invoke();
            return;
        }

        if (skipConfirmMessageText != null)
        {
            skipConfirmMessageText.text = skipConfirmMessage;
        }

        skipConfirmPanel.SetActive(true);
    }

    private void OnSkipConfirmed()
    {
        HideSkipConfirmation();
        OnSkipRequested?.Invoke();
    }

    private void HideSkipConfirmation()
    {
        if (skipConfirmPanel != null)
        {
            skipConfirmPanel.SetActive(false);
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
    
    private IEnumerator AnimateInCoroutine()
    {
        if (canvasGroup == null) yield break;
        
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
        yield return new WaitForSecondsRealtime(fadeInDuration * 0.5f);
        
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
    
    public void Hide()
    {
        if (!isVisible) return;
        
        isVisible = false;
        waitingForTargetClick = false;
        HideSkipConfirmation();
        
        // Remove button listener before clearing references
        RemoveTargetButtonListener();
        RestoreLockedSelectables();
        currentTargets.Clear();
        
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
                // Safety check: ensure target still exists and is active
                if (currentTarget.gameObject.activeInHierarchy)
                {
                    Vector3 targetWorldPos = currentTarget.position;
                    Vector3 highlightCurrentPos = highlightTransform.position;
                    
                    // Update position if target has moved or highlight is at origin
                    float distanceSqr = Vector3.SqrMagnitude(targetWorldPos - highlightCurrentPos);
                    if (distanceSqr > 0.01f)
                    {
                        highlightTransform.position = targetWorldPos;
                    }
                }
            }
        }
    }
}
