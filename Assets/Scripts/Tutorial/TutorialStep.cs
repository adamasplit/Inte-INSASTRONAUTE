using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ScriptableObject defining a single tutorial step.
/// Can be reused and configured easily in the editor.
/// </summary>
[CreateAssetMenu(fileName = "TutorialStep", menuName = "Tutorial/Tutorial Step", order = 1)]
public class TutorialStep : ScriptableObject
{
    [Header("Step Information")]
    [Tooltip("Internal ID for this step")]
    public string stepId;
    
    [Tooltip("Title displayed in the tutorial UI")]
    public string title;
    
    [Tooltip("Description/instruction text")]
    [TextArea(3, 6)]
    public string description;
    
    [Header("Target Configuration")]
    [Tooltip("Type of UI element to highlight")]
    public HighlightType highlightType = HighlightType.None;
    
    [Tooltip("Tag or name to find the target GameObject")]
    public string targetTag;

    [Tooltip("Optional: Only match targets whose parent (or ancestor) has this tag")]
    public string parentTag;

    [Tooltip("When searching by tag, allow any matching object to satisfy the click")]
    public bool allowAnyMatchingTarget = true;

    [Tooltip("If target object is not directly clickable, allow parent button to be used")]
    public bool allowParentTagClick = true;

    [Tooltip("Highlight only the first available matching target")]
    public bool highlightFirstAvailable = true;
    
    [Tooltip("Direct reference to target (optional, overrides tag)")]
    public RectTransform targetTransform;
    
    [Header("Visual Settings")]
    [Tooltip("Overlay opacity (0-1)")]
    [Range(0f, 1f)]
    public float overlayAlpha = 0.7f;
    
    [Tooltip("Highlight padding around target")]
    public Vector2 highlightPadding = new Vector2(10f, 10f);

    [Tooltip("Scale factor applied to highlight size (1 = same size as target)")]
    public float highlightScaleFactor = 1.2f;
    
    [Tooltip("Should the highlight pulse?")]
    public bool pulseHighlight = true;
    
    [Header("Interaction")]
    [Tooltip("How does user proceed to next step?")]
    public AdvanceType advanceType = AdvanceType.Button;
    
    [Tooltip("Custom button text (if using button advance)")]
    public string buttonText = "Suivant";
    
    [Tooltip("Should we wait for user to click the highlighted element?")]
    public bool waitForTargetClick = false;

    [Tooltip("Block non-tutorial UI interactions while this step is active")]
    public bool lockInputToTarget = true;
    
    [Tooltip("Automatic advance delay (seconds, 0 = disabled)")]
    public float autoAdvanceDelay = 0f;
    
    [Header("Actions")]
    [Tooltip("Only execute this step if in the Main scene (Main - Copie)")]
    public bool isInMainScene = false;
    
    [Tooltip("Should we open the top menu before this step?")]
    public bool openTopMenu = false;
    
    [Tooltip("Should we navigate to a specific screen? (-1 = no navigation)")]
    public int navigateToScreen = -1;
    
    [Tooltip("Delay before showing this step (seconds)")]
    public float delayBeforeShow = 0f;

    [Tooltip("Delay after successful completion before next step (seconds)")]
    public float delayAfterSuccess = 0f;

    [Tooltip("Overlay alpha to use between successful step and next step")]
    [Range(0f, 1f)]
    public float successOverlayAlpha = 0.08f;

    [Tooltip("Fade duration for overlay reduction after success")]
    public float successOverlayFadeTime = 0.15f;
    
    [Header("Optional Icon")]
    [Tooltip("Icon to display next to the title")]
    public Sprite icon;
}

public enum HighlightType
{
    None,           // No highlight, just show text
    Circle,         // Circular highlight
    Rectangle,      // Rectangular highlight
    Custom          // Custom shape (advanced)
}

public enum AdvanceType
{
    Button,         // User clicks "Next" button
    TargetClick,    // User clicks the highlighted element
    Automatic       // Advances automatically after delay
}
