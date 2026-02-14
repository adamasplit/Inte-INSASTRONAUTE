using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI loadingText;
    public Image loadingBarFill;
    public TextMeshProUGUI progressText;

    [Header("Settings")]
    [SerializeField] private bool animateProgress = true;
    [SerializeField] private float animationSpeed = 2f;
    [SerializeField] private bool showPercentage = true;
    [SerializeField] private bool showStepCount = false;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float completionDelay = 0.3f;

    private int totalSteps = 1;
    private int currentStep = 0;
    private float targetProgress = 0f;
    private float currentProgress = 0f;

    private void Start()
    {
        if (loadingText)
            loadingText.text = "Chargement...";
        
        UpdateProgressBar();
    }

    private void Update()
    {
        // Animate loading text dots
        if (loadingText && currentStep < totalSteps)
        {
            float t = Time.time % 1f;
            int dots = (int)(t * 4);
            loadingText.text = "Chargement" + new string('.', dots);
        }

        // Smooth progress bar animation
        if (animateProgress && Mathf.Abs(currentProgress - targetProgress) > 0.001f)
        {
            currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * animationSpeed);
            UpdateProgressBar();
        }
    }

    /// <summary>
    /// Initialize the loading bar with the total number of steps
    /// </summary>
    public void Initialize(int steps)
    {
        totalSteps = Mathf.Max(1, steps);
        currentStep = 0;
        targetProgress = 0f;
        currentProgress = 0f;
        
        UpdateProgressBar();
        
        if (loadingText)
            loadingText.text = "Chargement...";
    }

    /// <summary>
    /// Increment the loading progress by one step
    /// </summary>
    public void IncrementStep()
    {
        if (currentStep < totalSteps)
        {
            currentStep++;
            targetProgress = (float)currentStep / totalSteps;
            
            if (!animateProgress)
            {
                currentProgress = targetProgress;
            }
            
            UpdateProgressBar();

            // Update text when complete
            if (currentStep >= totalSteps && loadingText)
            {
                loadingText.text = "";
            }
        }
    }

    /// <summary>
    /// Set progress to a specific step
    /// </summary>
    public void SetStep(int step)
    {
        currentStep = Mathf.Clamp(step, 0, totalSteps);
        targetProgress = (float)currentStep / totalSteps;
        
        if (!animateProgress)
        {
            currentProgress = targetProgress;
        }
        
        UpdateProgressBar();
    }

    /// <summary>
    /// Set progress directly with a 0-1 value
    /// </summary>
    public void SetProgress(float progress)
    {
        targetProgress = Mathf.Clamp01(progress);
        
        if (!animateProgress)
        {
            currentProgress = targetProgress;
        }
        
        UpdateProgressBar();
    }

    /// <summary>
    /// Complete the loading instantly
    /// </summary>
    public void CompleteLoading()
    {
        currentStep = totalSteps;
        targetProgress = 1f;
        currentProgress = 1f;
        
        UpdateProgressBar();
        
        if (loadingText)
            loadingText.text = "";
    }

    private void UpdateProgressBar()
    {
        // Update fill amount
        if (loadingBarFill)
        {
            loadingBarFill.fillAmount = currentProgress;
        }

        // Update progress text
        if (progressText)
        {
            if (showPercentage && showStepCount)
            {
                progressText.text = $"{Mathf.RoundToInt(currentProgress * 100)}% ({currentStep}/{totalSteps})";
            }
            else if (showPercentage)
            {
                progressText.text = $"{Mathf.RoundToInt(currentProgress * 100)}%";
            }
            else if (showStepCount)
            {
                progressText.text = $"{currentStep}/{totalSteps}";
            }
        }
    }

    /// <summary>
    /// Get current progress as 0-1 value
    /// </summary>
    public float GetProgress()
    {
        return currentProgress;
    }

    /// <summary>
    /// Check if loading is complete
    /// </summary>
    public bool IsComplete()
    {
        return currentStep >= totalSteps;
    }

    /// <summary>
    /// Smoothly fade out and hide the loading screen
    /// </summary>
    public void HideWithFade()
    {
        var canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        LeanTween.delayedCall(completionDelay, () =>
        {
            LeanTween.alphaCanvas(canvasGroup, 0f, fadeOutDuration)
                .setEaseInCubic()
                .setOnComplete(() =>
                {
                    gameObject.SetActive(false);
                    canvasGroup.alpha = 1f; // Reset for next time
                });
        });
    }
}