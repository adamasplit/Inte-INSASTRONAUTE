using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

/// <summary>
/// Main manager for the tutorial system.
/// Handles tutorial flow, state persistence, and step progression.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }
    private static bool diagnosticLoggingConfigured;

    private static void ConfigureDiagnosticLogging()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (diagnosticLoggingConfigured)
            return;

        // WebGL warnings can include very large stacktraces that hide useful startup diagnostics.
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        diagnosticLoggingConfigured = true;
#endif
    }

    // In WebGL builds, emit diagnostics as warnings so they stay visible with stricter console filters.
    private static void LogDiag(string message)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.LogWarning(message);
#else
        Debug.Log(message);
#endif
    }
    
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
        ConfigureDiagnosticLogging();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        LogTutorialState("Awake");
        
        if (resetTutorialOnStart)
        {
            Debug.LogWarning("[TutorialManager] resetTutorialOnStart is TRUE - tutorial will restart every session! Disable this for production.");
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

        StartCoroutine(TryResumeTutorialState());
        StartCoroutine(EnsureAutoStartFirstTimeTutorial());
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
        LogDiag($"[TutorialManager] Scene loaded: {scene.name}, Tutorial active: {isTutorialActive}");
        ResolveSceneDependencies();
        BindTutorialUI();
        
        // If tutorial is active, refresh highlight targets after scene load
        // This ensures highlights are repositioned correctly after scene transitions
        if (isTutorialActive && tutorialUI != null)
        {
            StartCoroutine(RefreshHighlightsAfterSceneLoad());
        }
    }

    private IEnumerator RefreshHighlightsAfterSceneLoad()
    {
        // Wait a frame for scene to fully initialize
        yield return null;
        
        // Wait for canvas to calculate layout
        Canvas.ForceUpdateCanvases();
        yield return new WaitForSecondsRealtime(0.05f);
        
        // Refresh highlight targets
        if (tutorialUI != null)
        {
            LogDiag("[TutorialManager] Refreshing highlight targets after scene load");
            yield return StartCoroutine(tutorialUI.RefreshTargets());
        }
    }

    private TutorialUI FindTutorialUIIncludingInactive()
    {
        if (tutorialUI != null)
            return tutorialUI;

        tutorialUI = FindFirstObjectByType<TutorialUI>();
        if (tutorialUI != null)
            return tutorialUI;

        var allTutorialUIs = FindObjectsByType<TutorialUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (allTutorialUIs != null && allTutorialUIs.Length > 0)
        {
            tutorialUI = allTutorialUIs[0];
            LogDiag($"[TutorialManager] Found TutorialUI via inactive search: '{tutorialUI.name}' (activeSelf={tutorialUI.gameObject.activeSelf})");
        }

        return tutorialUI;
    }

    private void ResolveSceneDependencies()
    {
        if (tutorialUI == null)
            tutorialUI = FindTutorialUIIncludingInactive();

        if (topMenuController == null)
            topMenuController = FindFirstObjectByType<TopMenuController>();

        if (mainUIBinder == null)
            mainUIBinder = FindFirstObjectByType<MainUIBinder>();
    }

    private void BindTutorialUI()
    {
        if (tutorialUI == null)
            tutorialUI = FindTutorialUIIncludingInactive();

        if (tutorialUI == null)
        {
            LogDiag("[TutorialManager] BindTutorialUI: no TutorialUI found yet.");
            return;
        }

        if (subscribedTutorialUI == tutorialUI)
            return;

        UnbindTutorialUI();

        tutorialUI.OnAdvanceRequested += AdvanceToNextStep;
        tutorialUI.OnSkipRequested += SkipTutorial;
        tutorialUI.OnTargetClicked += OnTargetClicked;
        subscribedTutorialUI = tutorialUI;

        LogDiag($"[TutorialManager] Bound TutorialUI: '{tutorialUI.name}' (activeSelf={tutorialUI.gameObject.activeSelf})");
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
        LogDiag("[TutorialManager] Target element was clicked!");
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
        int hasSeenTutorial = PlayerPrefs.GetInt(PREF_HAS_SEEN_ANY_TUTORIAL, 0);
        bool isFirstTime = hasSeenTutorial == 0;
        LogDiag($"[TutorialManager] IsFirstTime check: {isFirstTime} (HasSeenAnyTutorial={hasSeenTutorial})");
        return isFirstTime;
    }
    
    /// <summary>
    /// Start a specific tutorial sequence by ID
    /// </summary>
    public void StartTutorial(string sequenceId)
    {
        StartCoroutine(StartTutorialCoroutine(sequenceId));
    }
    
    private IEnumerator StartTutorialCoroutine(string sequenceId)
    {
        if (isTutorialActive)
        {
            Debug.LogWarning($"[TutorialManager] Cannot start tutorial '{sequenceId}' - tutorial already active");
            yield break;
        }
        
        var sequence = Array.Find(tutorialSequences, s => s.sequenceId == sequenceId);
        if (sequence == null)
        {
            Debug.LogError($"[TutorialManager] Tutorial sequence '{sequenceId}' not found!");
            yield break;
        }
        
        if (HasCompletedTutorial(sequenceId) && !resetTutorialOnStart)
        {
            LogDiag($"[TutorialManager] Tutorial '{sequenceId}' already completed.");
            yield break;
        }
        
        yield return StartCoroutine(StartTutorialSequenceCoroutine(sequence));
    }
    
    /// <summary>
    /// Start the first-time user tutorial
    /// </summary>
    public void StartFirstTimeTutorial()
    {
        StartCoroutine(StartFirstTimeTutorialCoroutine());
    }
    
    private IEnumerator StartFirstTimeTutorialCoroutine()
    {
        LogTutorialState("StartFirstTimeTutorial called");
        
        if (!IsFirstTime() && !resetTutorialOnStart)
        {
            LogDiag("[TutorialManager] Not first time, skipping tutorial");
            yield break;
        }
        
        // Find the first-time tutorial
        var firstTimeSequence = Array.Find(tutorialSequences, s => s.isFirstTimeTutorial);
        if (firstTimeSequence != null)
        {
            LogDiag($"[TutorialManager] First-time sequence selected: id='{firstTimeSequence.sequenceId}', steps={(firstTimeSequence.steps == null ? 0 : firstTimeSequence.steps.Length)}");

            if (firstTimeSequence.steps == null || firstTimeSequence.steps.Length == 0)
            {
                Debug.LogWarning($"[TutorialManager] First-time sequence '{firstTimeSequence.sequenceId}' has no steps. Tutorial cannot start.");
                yield break;
            }

            LogDiag($"[TutorialManager] Starting first-time tutorial: {firstTimeSequence.sequenceId}");
            yield return StartCoroutine(StartTutorialSequenceCoroutine(firstTimeSequence));
        }
        else
        {
            LogDiag("[TutorialManager] No sequence marked as first-time. Available sequences:");
            if (tutorialSequences != null)
            {
                foreach (var seq in tutorialSequences)
                {
                    if (seq != null)
                        LogDiag($"  sequenceId='{seq.sequenceId}', isFirstTimeTutorial={seq.isFirstTimeTutorial}, steps={(seq.steps == null ? 0 : seq.steps.Length)}");
                }
            }
            Debug.LogWarning("[TutorialManager] No first-time tutorial configured!");
        }
    }
    
    private IEnumerator StartTutorialSequenceCoroutine(TutorialSequence sequence)
    {
        if (isTutorialActive)
        {
            Debug.LogWarning($"[TutorialManager] Cannot start tutorial '{sequence.sequenceId}' - tutorial already active");
            yield break;
        }

        ResolveSceneDependencies();
        BindTutorialUI();

        if (tutorialUI == null)
        {
            LogDiag("[TutorialManager] TutorialUI not found before start, waiting briefly...");
            yield return new WaitForSecondsRealtime(0.5f);
            ResolveSceneDependencies();
            BindTutorialUI();
        }

        if (tutorialUI == null)
        {
            Debug.LogWarning("[TutorialManager] Cannot start tutorial: TutorialUI still missing after retry.");
            yield break;
        }

        currentSequence = sequence;
        currentStepIndex = 0;
        isTutorialActive = true;
        hasAttemptedResume = true; // Prevent resume from interfering
        
        LogDiag($"[TutorialManager] Starting tutorial: {sequence.sequenceId}");
        
        if (tutorialUI != null)
        {
            tutorialUI.gameObject.SetActive(true);
        }

        SaveTutorialState();
        
        yield return StartCoroutine(ShowCurrentStep());
    }

    private IEnumerator ResumeTutorialSequence(TutorialSequence sequence, int stepIndex)
    {
        currentSequence = sequence;
        currentStepIndex = Mathf.Clamp(stepIndex, 0, Mathf.Max(0, sequence.steps.Length - 1));
        isTutorialActive = true;
        isStepInProgress = false;
        hasAttemptedResume = true; // Mark as resumed to prevent re-resuming

        LogDiag($"[TutorialManager] Resuming tutorial: {sequence.sequenceId} at step {currentStepIndex + 1}");

        if (tutorialUI != null)
        {
            tutorialUI.gameObject.SetActive(true);
        }

        SaveTutorialState();
        yield return StartCoroutine(ShowCurrentStep());
    }
    
    private IEnumerator ShowCurrentStep()
    {
        ResolveSceneDependencies();
        BindTutorialUI();

        if (currentSequence == null || currentStepIndex >= currentSequence.steps.Length)
        {
            CompleteTutorial();
            yield break;
        }
        
        isStepInProgress = true;
        var step = currentSequence.steps[currentStepIndex];

        // Wait for main scene if step requires it
        if (step.isInMainScene)
        {
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (currentScene.name != "Main - Copie")
            {
                LogDiag($"[TutorialManager] Step '{step.title}' requires Main scene. Waiting for scene change...");
                
                // Wait until we're in the main scene
                while (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Main - Copie")
                {
                    yield return new WaitForSecondsRealtime(0.1f);
                    
                    // Safety check: if tutorial was cancelled, exit
                    if (!isTutorialActive)
                        yield break;
                }
                
                LogDiag($"[TutorialManager] Now in Main scene, continuing step '{step.title}'");
                
                // Re-resolve dependencies after scene change
                ResolveSceneDependencies();
                BindTutorialUI();
            }
        }

        SaveTutorialState();
        
        LogDiag($"[TutorialManager] Showing step {currentStepIndex + 1}/{currentSequence.steps.Length}: {step.title}");
        
        OnStepStarted?.Invoke(step);
        
        // Wait for delay if specified (allows animations/spawning to complete)
        bool hasDelay = step.delayBeforeShow > 0;
        if (hasDelay)
        {
            yield return new WaitForSecondsRealtime(step.delayBeforeShow);
        }
        
        // Perform step actions
        yield return StartCoroutine(PerformStepActions(step));
        
        // Show the step UI (defer target search if we had a delay/actions that might spawn targets)
        bool deferTargets = hasDelay || step.openTopMenu || step.navigateToScreen >= 0;
        if (tutorialUI != null)
        {
            yield return StartCoroutine(tutorialUI.ShowStep(step, currentStepIndex + 1, currentSequence.steps.Length, deferTargets));
            
            // If we deferred target search, do it now after delay/actions completed
            if (deferTargets)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                yield return StartCoroutine(tutorialUI.RefreshTargets());
            }
        }
        
        // Handle automatic advance
        if (step.advanceType == AdvanceType.Automatic && step.autoAdvanceDelay > 0)
        {
            yield return new WaitForSecondsRealtime(step.autoAdvanceDelay);
            if (isTutorialActive && isStepInProgress) // Check still active
            {
                AdvanceToNextStep();
            }
        }
    }
    
    private IEnumerator PerformStepActions(TutorialStep step)
    {
        if (topMenuController == null)
        {
            topMenuController = FindFirstObjectByType<TopMenuController>();
        }

        // Open top menu if needed
        if (step.openTopMenu && topMenuController != null)
        {
            topMenuController.ToggleMenu();
            yield return new WaitForSecondsRealtime(0.4f);
        }
        
        // Navigate to screen if specified
        if (step.navigateToScreen >= 0 && topMenuController != null)
        {
            topMenuController.GoToScreen(step.navigateToScreen);
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }
    
    public void AdvanceToNextStep()
    {
        if (!isTutorialActive || !isStepInProgress) return;
        
        StartCoroutine(AdvanceToNextStepAsync());
    }

    private IEnumerator AdvanceToNextStepAsync()
    {
        if (!isTutorialActive || !isStepInProgress) yield break;

        var completedStep = currentSequence.steps[currentStepIndex];
        OnStepCompleted?.Invoke(completedStep);

        if (tutorialUI != null)
        {
            tutorialUI.SetStepContentVisible(false);
            yield return StartCoroutine(tutorialUI.FadeOverlayTo(completedStep.successOverlayAlpha, completedStep.successOverlayFadeTime));
        }
        
        isStepInProgress = false;
        currentStepIndex++;

        SaveTutorialState();

        if (completedStep.delayAfterSuccess > 0f)
        {
            yield return new WaitForSecondsRealtime(completedStep.delayAfterSuccess);
        }
        
        yield return StartCoroutine(ShowCurrentStep());
    }
    
    public void SkipTutorial()
    {
        if (!isTutorialActive) return;
        
        LogDiag($"[TutorialManager] Skipping tutorial: {currentSequence.sequenceId}");
        
        // Mark as completed even if skipped
        MarkTutorialComplete(currentSequence.sequenceId);

        ClearSavedTutorialState();
        
        EndTutorial();
    }
    
    private void CompleteTutorial()
    {
        if (currentSequence == null) return;
        
        LogDiag($"[TutorialManager] Completed tutorial: {currentSequence.sequenceId}");
        
        MarkTutorialComplete(currentSequence.sequenceId);
        
        OnSequenceCompleted?.Invoke(currentSequence.sequenceId);
        
        // Mark that user has seen a tutorial
        LogDiag("[TutorialManager] Setting HasSeenAnyTutorial = 1 and saving PlayerPrefs");
        PlayerPrefs.SetInt(PREF_HAS_SEEN_ANY_TUTORIAL, 1);
        PlayerPrefs.Save();
        
        LogTutorialState("After completing tutorial");
        
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
        LogDiag("[TutorialManager] Resetting all tutorial progress...");
        
        foreach (var seq in tutorialSequences)
        {
            PlayerPrefs.DeleteKey(PREF_TUTORIAL_COMPLETED + seq.sequenceId);
        }
        PlayerPrefs.DeleteKey(PREF_HAS_SEEN_ANY_TUTORIAL);
        ClearSavedTutorialState();
        PlayerPrefs.Save();
        
        hasAttemptedResume = false; // Allow resume after reset
        
        LogDiag("[TutorialManager] All tutorials reset");
        LogTutorialState("After reset");
    }
    
    /// <summary>
    /// Log current tutorial state from PlayerPrefs (for debugging)
    /// </summary>
    public void LogTutorialState(string context = "")
    {
        if (!string.IsNullOrEmpty(context))
            LogDiag($"[TutorialManager] === Tutorial State ({context}) ===");
        else
            LogDiag("[TutorialManager] === Tutorial State ===");
        
        LogDiag($"  resetTutorialOnStart: {resetTutorialOnStart}");
        LogDiag($"  autoStartOnFirstLogin: {autoStartOnFirstLogin}");
        LogDiag($"  isTutorialActive: {isTutorialActive}");
        LogDiag($"  HasSeenAnyTutorial: {PlayerPrefs.GetInt(PREF_HAS_SEEN_ANY_TUTORIAL, 0)}");
        LogDiag($"  ActiveSequence: {PlayerPrefs.GetString(PREF_ACTIVE_SEQUENCE, "none")}");
        LogDiag($"  ActiveStep: {PlayerPrefs.GetInt(PREF_ACTIVE_STEP, -1)}");
        
        foreach (var seq in tutorialSequences)
        {
            bool completed = PlayerPrefs.GetInt(PREF_TUTORIAL_COMPLETED + seq.sequenceId, 0) == 1;
            LogDiag($"  Tutorial '{seq.sequenceId}': {(completed ? "COMPLETED" : "NOT COMPLETED")}");
        }
        
        LogDiag("[TutorialManager] ================================");
    }
    
    /// <summary>
    /// Get all tutorial PlayerPrefs as a formatted string (for WebGL debugging via console)
    /// </summary>
    public string GetTutorialStateString()
    {
        string state = "Tutorial State:\n";
        state += $"  resetTutorialOnStart: {resetTutorialOnStart}\n";
        state += $"  HasSeenAnyTutorial: {PlayerPrefs.GetInt(PREF_HAS_SEEN_ANY_TUTORIAL, 0)}\n";
        state += $"  ActiveSequence: {PlayerPrefs.GetString(PREF_ACTIVE_SEQUENCE, "none")}\n";
        state += $"  ActiveStep: {PlayerPrefs.GetInt(PREF_ACTIVE_STEP, -1)}\n";
        
        foreach (var seq in tutorialSequences)
        {
            bool completed = PlayerPrefs.GetInt(PREF_TUTORIAL_COMPLETED + seq.sequenceId, 0) == 1;
            state += $"  {seq.sequenceId}: {(completed ? "COMPLETED" : "NOT COMPLETED")}\n";
        }
        
        return state;
    }
    
    /// <summary>
    /// Call from browser console to check tutorial state in WebGL builds.
    /// Usage in browser console: unityInstance.SendMessage('TutorialManager', 'DebugLogState');
    /// </summary>
    public void DebugLogState()
    {
        LogTutorialState("Browser Console Request");
    }
    
    /// <summary>
    /// Call from browser console to reset tutorial in WebGL builds.
    /// Usage: unityInstance.SendMessage('TutorialManager', 'DebugResetTutorial');
    /// </summary>
    public void DebugResetTutorial()
    {
        Debug.LogWarning("[TutorialManager] Reset requested from browser console!");
        ResetAllTutorials();
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

    private IEnumerator TryResumeTutorialState()
    {
        if (isTutorialActive)
        {
            LogDiag("[TutorialManager] Tutorial already active, not resuming from saved state");
            yield break;
        }

        if (hasAttemptedResume)
        {
            LogDiag("[TutorialManager] Already attempted resume, skipping");
            yield break;
        }

        hasAttemptedResume = true;

        var savedSequenceId = PlayerPrefs.GetString(PREF_ACTIVE_SEQUENCE, string.Empty);
        var savedStep = PlayerPrefs.GetInt(PREF_ACTIVE_STEP, -1);

        if (string.IsNullOrEmpty(savedSequenceId) || savedStep < 0)
        {
            LogDiag("[TutorialManager] No saved tutorial state found");
            yield break;
        }

        var sequence = Array.Find(tutorialSequences, s => s.sequenceId == savedSequenceId);
        if (sequence == null)
        {
            Debug.LogWarning($"[TutorialManager] Saved sequence '{savedSequenceId}' not found, clearing state");
            ClearSavedTutorialState();
            yield break;
        }

        LogDiag($"[TutorialManager] Resuming from saved state: {savedSequenceId} at step {savedStep}");
        yield return StartCoroutine(ResumeTutorialSequence(sequence, savedStep));
    }

    private IEnumerator EnsureAutoStartFirstTimeTutorial()
    {
        LogDiag("[TutorialManager] EnsureAutoStartFirstTimeTutorial - Fallback check starting");
        
        if (!autoStartOnFirstLogin)
        {
            LogDiag("[TutorialManager] autoStartOnFirstLogin is FALSE, skipping fallback");
            yield break;
        }

        yield return new WaitForSecondsRealtime(1.5f);
        LogDiag("[TutorialManager] Fallback delay elapsed, evaluating first-time autostart");

        if (isTutorialActive)
        {
            LogDiag("[TutorialManager] Tutorial already active, fallback not needed");
            yield break;
        }

        var savedSequenceId = PlayerPrefs.GetString(PREF_ACTIVE_SEQUENCE, string.Empty);
        var savedStep = PlayerPrefs.GetInt(PREF_ACTIVE_STEP, -1);
        if (!string.IsNullOrEmpty(savedSequenceId) && savedStep >= 0)
        {
            LogDiag($"[TutorialManager] Found saved tutorial state ({savedSequenceId}, step {savedStep}), fallback not needed");
            yield break;
        }

        LogTutorialState("Before fallback auto-start check");

        if (!IsFirstTime() && !resetTutorialOnStart)
        {
            LogDiag("[TutorialManager] Not first time, fallback skipped");
            yield break;
        }

        ResolveSceneDependencies();
        BindTutorialUI();

        if (tutorialUI == null)
        {
            Debug.LogWarning("[TutorialManager] TutorialUI not found, waiting...");
            yield return new WaitForSecondsRealtime(0.5f);
            ResolveSceneDependencies();
            BindTutorialUI();
        }

        if (isTutorialActive)
        {
            LogDiag("[TutorialManager] Tutorial became active while waiting, fallback not needed");
            yield break;
        }

        LogDiag("[TutorialManager] Fallback: Auto-starting first-time tutorial");
        StartFirstTimeTutorial();
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

