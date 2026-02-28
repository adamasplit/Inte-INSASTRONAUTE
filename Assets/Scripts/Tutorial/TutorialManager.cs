using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;

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
    private TutorialUI subscribedTutorialUI;
    private bool hasAttemptedResume = false;
    
    // PlayerPrefs keys
    private const string PREF_TUTORIAL_COMPLETED = "Tutorial_Completed_";
    private const string PREF_HAS_SEEN_ANY_TUTORIAL = "HasSeenAnyTutorial";
    private const string PREF_ACTIVE_SEQUENCE = "Tutorial_Active_Sequence";
    private const string PREF_ACTIVE_STEP = "Tutorial_Active_Step";
    
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
        DontDestroyOnLoad(gameObject);
        
        if (resetTutorialOnStart)
        {
            ResetAllTutorials();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void Start()
    {
        ResolveSceneDependencies();
        BindTutorialUI();

        TryResumeTutorialState();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindTutorialUI();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveTutorialState();
        }
    }

    private void OnApplicationQuit()
    {
        SaveTutorialState();
    }
    
    private void OnDestroy()
    {
        UnbindTutorialUI();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[TutorialManager] Scene loaded: {scene.name}, Tutorial active: {isTutorialActive}");
        ResolveSceneDependencies();
        BindTutorialUI();
        
        // If tutorial is active, refresh highlight targets after scene load
        // This ensures highlights are repositioned correctly after scene transitions
        if (isTutorialActive && tutorialUI != null)
        {
            _ = RefreshHighlightsAfterSceneLoad();
        }
    }

    private async Task RefreshHighlightsAfterSceneLoad()
    {
        // Wait a frame for scene to fully initialize
        await Task.Yield();
        
        // Wait for canvas to calculate layout
        Canvas.ForceUpdateCanvases();
        await Task.Delay(50);
        
        // Refresh highlight targets
        if (tutorialUI != null)
        {
            Debug.Log("[TutorialManager] Refreshing highlight targets after scene load");
            await tutorialUI.RefreshTargets();
        }
    }

    private void ResolveSceneDependencies()
    {
        if (tutorialUI == null)
            tutorialUI = FindFirstObjectByType<TutorialUI>();

        if (topMenuController == null)
            topMenuController = FindFirstObjectByType<TopMenuController>();

        if (mainUIBinder == null)
            mainUIBinder = FindFirstObjectByType<MainUIBinder>();
    }

    private void BindTutorialUI()
    {
        if (tutorialUI == null)
            tutorialUI = FindFirstObjectByType<TutorialUI>();

        if (tutorialUI == null)
            return;

        if (subscribedTutorialUI == tutorialUI)
            return;

        UnbindTutorialUI();

        tutorialUI.OnAdvanceRequested += AdvanceToNextStep;
        tutorialUI.OnSkipRequested += SkipTutorial;
        tutorialUI.OnTargetClicked += OnTargetClicked;
        subscribedTutorialUI = tutorialUI;
    }

    private void UnbindTutorialUI()
    {
        if (subscribedTutorialUI == null)
            return;

        subscribedTutorialUI.OnAdvanceRequested -= AdvanceToNextStep;
        subscribedTutorialUI.OnSkipRequested -= SkipTutorial;
        subscribedTutorialUI.OnTargetClicked -= OnTargetClicked;
        subscribedTutorialUI = null;
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
        if (isTutorialActive)
        {
            Debug.LogWarning($"[TutorialManager] Cannot start tutorial '{sequence.sequenceId}' - tutorial already active");
            return;
        }

        currentSequence = sequence;
        currentStepIndex = 0;
        isTutorialActive = true;
        hasAttemptedResume = true; // Prevent resume from interfering
        
        Debug.Log($"[TutorialManager] Starting tutorial: {sequence.sequenceId}");
        
        if (tutorialUI != null)
        {
            tutorialUI.gameObject.SetActive(true);
        }

        SaveTutorialState();
        
        await ShowCurrentStep();
    }

    private async Task ResumeTutorialSequence(TutorialSequence sequence, int stepIndex)
    {
        currentSequence = sequence;
        currentStepIndex = Mathf.Clamp(stepIndex, 0, Mathf.Max(0, sequence.steps.Length - 1));
        isTutorialActive = true;
        isStepInProgress = false;
        hasAttemptedResume = true; // Mark as resumed to prevent re-resuming

        Debug.Log($"[TutorialManager] Resuming tutorial: {sequence.sequenceId} at step {currentStepIndex + 1}");

        if (tutorialUI != null)
        {
            tutorialUI.gameObject.SetActive(true);
        }

        SaveTutorialState();
        await ShowCurrentStep();
    }
    
    private async Task ShowCurrentStep()
    {
        ResolveSceneDependencies();
        BindTutorialUI();

        if (currentSequence == null || currentStepIndex >= currentSequence.steps.Length)
        {
            CompleteTutorial();
            return;
        }
        
        isStepInProgress = true;
        var step = currentSequence.steps[currentStepIndex];

        // Wait for main scene if step requires it
        if (step.isInMainScene)
        {
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (currentScene.name != "Main - Copie")
            {
                Debug.Log($"[TutorialManager] Step '{step.title}' requires Main scene. Waiting for scene change...");
                
                // Wait until we're in the main scene
                while (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Main - Copie")
                {
                    await Task.Delay(100); // Check every 100ms
                    
                    // Safety check: if tutorial was cancelled, exit
                    if (!isTutorialActive)
                        return;
                }
                
                Debug.Log($"[TutorialManager] Now in Main scene, continuing step '{step.title}'");
                
                // Re-resolve dependencies after scene change
                ResolveSceneDependencies();
                BindTutorialUI();
            }
        }

        SaveTutorialState();
        
        Debug.Log($"[TutorialManager] Showing step {currentStepIndex + 1}/{currentSequence.steps.Length}: {step.title}");
        
        OnStepStarted?.Invoke(step);
        
        // Wait for delay if specified (allows animations/spawning to complete)
        bool hasDelay = step.delayBeforeShow > 0;
        if (hasDelay)
        {
            await Task.Delay((int)(step.delayBeforeShow * 1000));
        }
        
        // Perform step actions
        await PerformStepActions(step);
        
        // Show the step UI (defer target search if we had a delay/actions that might spawn targets)
        bool deferTargets = hasDelay || step.openTopMenu || step.navigateToScreen >= 0;
        if (tutorialUI != null)
        {
            await tutorialUI.ShowStep(step, currentStepIndex + 1, currentSequence.steps.Length, deferTargets);
            
            // If we deferred target search, do it now after delay/actions completed
            if (deferTargets)
            {
                await Task.Delay(100); // Small additional delay for layout to settle
                await tutorialUI.RefreshTargets();
            }
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
        if (topMenuController == null)
        {
            topMenuController = FindFirstObjectByType<TopMenuController>();
        }

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
        
        _ = AdvanceToNextStepAsync();
    }

    private async Task AdvanceToNextStepAsync()
    {
        if (!isTutorialActive || !isStepInProgress) return;

        var completedStep = currentSequence.steps[currentStepIndex];
        OnStepCompleted?.Invoke(completedStep);

        if (tutorialUI != null)
        {
            tutorialUI.SetStepContentVisible(false);
            await tutorialUI.FadeOverlayTo(completedStep.successOverlayAlpha, completedStep.successOverlayFadeTime);
        }
        
        isStepInProgress = false;
        currentStepIndex++;

        SaveTutorialState();

        if (completedStep.delayAfterSuccess > 0f)
        {
            await Task.Delay((int)(completedStep.delayAfterSuccess * 1000f));
        }
        
        _ = ShowCurrentStep();
    }
    
    public void SkipTutorial()
    {
        if (!isTutorialActive) return;
        
        Debug.Log($"[TutorialManager] Skipping tutorial: {currentSequence.sequenceId}");
        
        // Mark as completed even if skipped
        MarkTutorialComplete(currentSequence.sequenceId);

        ClearSavedTutorialState();
        
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
        if (mainUIBinder == null)
        {
            mainUIBinder = FindFirstObjectByType<MainUIBinder>();
        }

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

        ClearSavedTutorialState();
        
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
        hasAttemptedResume = false; // Reset for next tutorial
        
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
        ClearSavedTutorialState();
        PlayerPrefs.Save();
        
        hasAttemptedResume = false; // Allow resume after reset
        
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

    private void SaveTutorialState()
    {
        if (!isTutorialActive || currentSequence == null)
            return;

        PlayerPrefs.SetString(PREF_ACTIVE_SEQUENCE, currentSequence.sequenceId);
        PlayerPrefs.SetInt(PREF_ACTIVE_STEP, currentStepIndex);
        PlayerPrefs.Save();
    }

    private void ClearSavedTutorialState()
    {
        PlayerPrefs.DeleteKey(PREF_ACTIVE_SEQUENCE);
        PlayerPrefs.DeleteKey(PREF_ACTIVE_STEP);
    }

    private async void TryResumeTutorialState()
    {
        if (isTutorialActive)
        {
            Debug.Log("[TutorialManager] Tutorial already active, not resuming from saved state");
            return;
        }

        if (hasAttemptedResume)
        {
            Debug.Log("[TutorialManager] Already attempted resume, skipping");
            return;
        }

        hasAttemptedResume = true;

        var savedSequenceId = PlayerPrefs.GetString(PREF_ACTIVE_SEQUENCE, string.Empty);
        var savedStep = PlayerPrefs.GetInt(PREF_ACTIVE_STEP, -1);

        if (string.IsNullOrEmpty(savedSequenceId) || savedStep < 0)
        {
            Debug.Log("[TutorialManager] No saved tutorial state found");
            return;
        }

        var sequence = Array.Find(tutorialSequences, s => s.sequenceId == savedSequenceId);
        if (sequence == null)
        {
            Debug.LogWarning($"[TutorialManager] Saved sequence '{savedSequenceId}' not found, clearing state");
            ClearSavedTutorialState();
            return;
        }

        Debug.Log($"[TutorialManager] Resuming from saved state: {savedSequenceId} at step {savedStep}");
        await ResumeTutorialSequence(sequence, savedStep);
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
