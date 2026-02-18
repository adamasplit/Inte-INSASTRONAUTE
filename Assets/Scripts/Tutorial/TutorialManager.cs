using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

/// <summary>
/// Main manager for the tutorial system.
/// Handles tutorial flow, state persistence, and step progression.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }
    
    [Header("Tutorial Configuration")]
    [Tooltip("List of tutorial sequences available")]
    public TutorialSequence[] tutorialSequences;
    
    [Tooltip("Should tutorial auto-start on first login?")]
    public bool autoStartOnFirstLogin = true;
    
    [Header("References")]
    [SerializeField] private TutorialUI tutorialUI;
    [SerializeField] private TopMenuController topMenuController;
    [SerializeField] private MainUIBinder mainUIBinder;
    
    [Header("Debug")]
    [SerializeField] private bool resetTutorialOnStart = false;
    
    // State
    private TutorialSequence currentSequence;
    private int currentStepIndex = 0;
    private bool isTutorialActive = false;
    private bool isStepInProgress = false;
    
    // PlayerPrefs keys
    private const string PREF_TUTORIAL_COMPLETED = "Tutorial_Completed_";
    private const string PREF_HAS_SEEN_ANY_TUTORIAL = "HasSeenAnyTutorial";
    
    // Events
    public event Action<TutorialStep> OnStepStarted;
    public event Action<TutorialStep> OnStepCompleted;
    public event Action<string> OnSequenceCompleted;
    public event Action OnAllTutorialsCompleted;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        if (resetTutorialOnStart)
        {
            ResetAllTutorials();
        }
    }
    
    private void Start()
    {
        // Auto-find references if not set
        if (tutorialUI == null)
            tutorialUI = FindFirstObjectByType<TutorialUI>();
        if (topMenuController == null)
            topMenuController = FindFirstObjectByType<TopMenuController>();
        if (mainUIBinder == null)
            mainUIBinder = FindFirstObjectByType<MainUIBinder>();
        
        if (tutorialUI != null)
        {
            tutorialUI.OnAdvanceRequested += AdvanceToNextStep;
            tutorialUI.OnSkipRequested += SkipTutorial;
            tutorialUI.OnTargetClicked += OnTargetClicked;
        }
    }
    
    private void OnDestroy()
    {
        if (tutorialUI != null)
        {
            tutorialUI.OnAdvanceRequested -= AdvanceToNextStep;
            tutorialUI.OnSkipRequested -= SkipTutorial;
            tutorialUI.OnTargetClicked -= OnTargetClicked;
        }
    }
    
    private void OnTargetClicked()
    {
        Debug.Log("[TutorialManager] Target element was clicked!");
        // You can add additional logic here if needed
        // The advancement is already handled in TutorialUI for TargetClick type
    }
    
    /// <summary>
    /// Check if user has completed a specific tutorial
    /// </summary>
    public bool HasCompletedTutorial(string sequenceId)
    {
        return PlayerPrefs.GetInt(PREF_TUTORIAL_COMPLETED + sequenceId, 0) == 1;
    }
    
    /// <summary>
    /// Check if this is the user's first time
    /// </summary>
    public bool IsFirstTime()
    {
        return PlayerPrefs.GetInt(PREF_HAS_SEEN_ANY_TUTORIAL, 0) == 0;
    }
    
    /// <summary>
    /// Start a specific tutorial sequence by ID
    /// </summary>
    public async void StartTutorial(string sequenceId)
    {
        if (isTutorialActive)
        {
            Debug.LogWarning($"[TutorialManager] Cannot start tutorial '{sequenceId}' - tutorial already active");
            return;
        }
        
        var sequence = Array.Find(tutorialSequences, s => s.sequenceId == sequenceId);
        if (sequence == null)
        {
            Debug.LogError($"[TutorialManager] Tutorial sequence '{sequenceId}' not found!");
            return;
        }
        
        if (HasCompletedTutorial(sequenceId) && !resetTutorialOnStart)
        {
            Debug.Log($"[TutorialManager] Tutorial '{sequenceId}' already completed.");
            return;
        }
        
        await StartTutorialSequence(sequence);
    }
    
    /// <summary>
    /// Start the first-time user tutorial
    /// </summary>
    public async void StartFirstTimeTutorial()
    {
        if (!IsFirstTime() && !resetTutorialOnStart)
        {
            Debug.Log("[TutorialManager] Not first time, skipping tutorial");
            return;
        }
        
        // Find the first-time tutorial
        var firstTimeSequence = Array.Find(tutorialSequences, s => s.isFirstTimeTutorial);
        if (firstTimeSequence != null)
        {
            await StartTutorialSequence(firstTimeSequence);
        }
        else
        {
            Debug.LogWarning("[TutorialManager] No first-time tutorial configured!");
        }
    }
    
    private async Task StartTutorialSequence(TutorialSequence sequence)
    {
        currentSequence = sequence;
        currentStepIndex = 0;
        isTutorialActive = true;
        
        Debug.Log($"[TutorialManager] Starting tutorial: {sequence.sequenceId}");
        
        if (tutorialUI != null)
        {
            tutorialUI.gameObject.SetActive(true);
        }
        
        await ShowCurrentStep();
    }
    
    private async Task ShowCurrentStep()
    {
        if (currentSequence == null || currentStepIndex >= currentSequence.steps.Length)
        {
            CompleteTutorial();
            return;
        }
        
        isStepInProgress = true;
        var step = currentSequence.steps[currentStepIndex];
        
        Debug.Log($"[TutorialManager] Showing step {currentStepIndex + 1}/{currentSequence.steps.Length}: {step.title}");
        
        OnStepStarted?.Invoke(step);
        
        // Wait for delay if specified
        if (step.delayBeforeShow > 0)
        {
            await Task.Delay((int)(step.delayBeforeShow * 1000));
        }
        
        // Perform step actions
        await PerformStepActions(step);
        
        // Show the step UI
        if (tutorialUI != null)
        {
            await tutorialUI.ShowStep(step, currentStepIndex + 1, currentSequence.steps.Length);
        }
        
        // Handle automatic advance
        if (step.advanceType == AdvanceType.Automatic && step.autoAdvanceDelay > 0)
        {
            await Task.Delay((int)(step.autoAdvanceDelay * 1000));
            if (isTutorialActive && isStepInProgress) // Check still active
            {
                AdvanceToNextStep();
            }
        }
    }
    
    private async Task PerformStepActions(TutorialStep step)
    {
        // Open top menu if needed
        if (step.openTopMenu && topMenuController != null)
        {
            topMenuController.ToggleMenu();
            await Task.Delay(400); // Wait for animation
        }
        
        // Navigate to screen if specified
        if (step.navigateToScreen >= 0 && topMenuController != null)
        {
            topMenuController.GoToScreen(step.navigateToScreen);
            await Task.Delay(500); // Wait for screen transition
        }
    }
    
    public void AdvanceToNextStep()
    {
        if (!isTutorialActive || !isStepInProgress) return;
        
        var completedStep = currentSequence.steps[currentStepIndex];
        OnStepCompleted?.Invoke(completedStep);
        
        isStepInProgress = false;
        currentStepIndex++;
        
        _ = ShowCurrentStep();
    }
    
    public void SkipTutorial()
    {
        if (!isTutorialActive) return;
        
        Debug.Log($"[TutorialManager] Skipping tutorial: {currentSequence.sequenceId}");
        
        // Mark as completed even if skipped
        MarkTutorialComplete(currentSequence.sequenceId);
        
        EndTutorial();
    }
    
    private void CompleteTutorial()
    {
        if (currentSequence == null) return;
        
        Debug.Log($"[TutorialManager] Completed tutorial: {currentSequence.sequenceId}");
        
        MarkTutorialComplete(currentSequence.sequenceId);
        
        OnSequenceCompleted?.Invoke(currentSequence.sequenceId);
        
        // Mark that user has seen a tutorial
        PlayerPrefs.SetInt(PREF_HAS_SEEN_ANY_TUTORIAL, 1);
        PlayerPrefs.Save();
        
        // Show completion message if configured
        if (!string.IsNullOrEmpty(currentSequence.completionMessage) && mainUIBinder != null)
        {
            mainUIBinder.ShowNotification(currentSequence.completionMessage);
        }
        
        // Check if all tutorials are complete
        bool allComplete = true;
        foreach (var seq in tutorialSequences)
        {
            if (!HasCompletedTutorial(seq.sequenceId))
            {
                allComplete = false;
                break;
            }
        }
        
        if (allComplete)
        {
            OnAllTutorialsCompleted?.Invoke();
        }
        
        EndTutorial();
    }
    
    private void MarkTutorialComplete(string sequenceId)
    {
        PlayerPrefs.SetInt(PREF_TUTORIAL_COMPLETED + sequenceId, 1);
        PlayerPrefs.Save();
    }
    
    private void EndTutorial()
    {
        isTutorialActive = false;
        isStepInProgress = false;
        currentSequence = null;
        currentStepIndex = 0;
        
        if (tutorialUI != null)
        {
            tutorialUI.Hide();
        }
    }
    
    /// <summary>
    /// Reset all tutorial progress (for testing)
    /// </summary>
    public void ResetAllTutorials()
    {
        foreach (var seq in tutorialSequences)
        {
            PlayerPrefs.DeleteKey(PREF_TUTORIAL_COMPLETED + seq.sequenceId);
        }
        PlayerPrefs.DeleteKey(PREF_HAS_SEEN_ANY_TUTORIAL);
        PlayerPrefs.Save();
        
        Debug.Log("[TutorialManager] All tutorials reset");
    }
    
    /// <summary>
    /// Get progress for a specific tutorial (0-1)
    /// </summary>
    public float GetTutorialProgress(string sequenceId)
    {
        if (HasCompletedTutorial(sequenceId))
            return 1f;
        
        if (currentSequence != null && currentSequence.sequenceId == sequenceId)
        {
            return (float)currentStepIndex / currentSequence.steps.Length;
        }
        
        return 0f;
    }
}

/// <summary>
/// Container for a sequence of tutorial steps
/// </summary>
[System.Serializable]
public class TutorialSequence
{
    [Tooltip("Unique identifier for this tutorial")]
    public string sequenceId = "FirstTime";
    
    [Tooltip("Display name for this tutorial")]
    public string displayName = "Introduction";
    
    [Tooltip("Is this the first-time user tutorial?")]
    public bool isFirstTimeTutorial = true;
    
    [Tooltip("Tutorial steps in order")]
    public TutorialStep[] steps;
    
    [Tooltip("Message to show when tutorial is completed")]
    public string completionMessage = "Tutoriel terminé !";
}
