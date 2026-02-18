using UnityEngine;

/// <summary>
/// Simple component to trigger a specific tutorial when clicked.
/// Useful for buttons that should start feature-specific tutorials.
/// </summary>
public class TutorialTrigger : MonoBehaviour
{
    [Header("Tutorial Configuration")]
    [Tooltip("ID of the tutorial sequence to start")]
    public string tutorialSequenceId = "FirstTime";
    
    [Tooltip("Should this tutorial only trigger once?")]
    public bool triggerOnce = true;
    
    [Tooltip("Trigger automatically on Start?")]
    public bool triggerOnStart = false;
    
    [Tooltip("Delay before triggering (seconds)")]
    public float triggerDelay = 0f;
    
    private bool hasTriggered = false;
    
    private void Start()
    {
        if (triggerOnStart)
        {
            if (triggerDelay > 0)
            {
                Invoke(nameof(TriggerTutorial), triggerDelay);
            }
            else
            {
                TriggerTutorial();
            }
        }
    }
    
    public void TriggerTutorial()
    {
        if (triggerOnce && hasTriggered)
        {
            Debug.Log($"[TutorialTrigger] Tutorial '{tutorialSequenceId}' already triggered (triggerOnce=true)");
            return;
        }
        
        var tutorialManager = TutorialManager.Instance;
        if (tutorialManager == null)
        {
            tutorialManager = FindFirstObjectByType<TutorialManager>();
        }
        
        if (tutorialManager != null)
        {
            Debug.Log($"[TutorialTrigger] Starting tutorial: {tutorialSequenceId}");
            tutorialManager.StartTutorial(tutorialSequenceId);
            hasTriggered = true;
        }
        else
        {
            Debug.LogError("[TutorialTrigger] TutorialManager not found in scene!");
        }
    }
    
    /// <summary>
    /// Reset the trigger so it can be used again
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
