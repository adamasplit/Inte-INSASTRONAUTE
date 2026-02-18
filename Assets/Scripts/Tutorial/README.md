# Tutorial System Documentation

## Overview
This modular tutorial system allows you to create guided onboarding experiences for first-time users and feature-specific tutorials. The system is:

- **Modular**: Each tutorial step is a reusable ScriptableObject
- **Flexible**: Supports multiple tutorial sequences
- **Persistent**: Tracks completion status via PlayerPrefs
- **Animated**: Uses LeanTween for smooth transitions
- **Mobile-friendly**: Works with touch inputs

## Components

### 1. TutorialManager
**Location**: `Assets/Scripts/Tutorial/TutorialManager.cs`

The main controller that manages tutorial flow and state.

**Key Features:**
- Manages multiple tutorial sequences
- Tracks completion status
- Auto-starts first-time tutorial
- Events for step/sequence completion

**Public Methods:**
```csharp
void StartTutorial(string sequenceId)          // Start a specific tutorial
void StartFirstTimeTutorial()                  // Start the first-time tutorial
bool HasCompletedTutorial(string sequenceId)   // Check if tutorial is complete
bool IsFirstTime()                             // Check if user is new
void ResetAllTutorials()                       // Reset all progress (testing)
```

### 2. TutorialStep (ScriptableObject)
**Location**: `Assets/Scripts/Tutorial/TutorialStep.cs`

Defines a single tutorial step with configuration options.

**Key Properties:**
- **stepId**: Unique identifier
- **title**: Step title displayed to user
- **description**: Instruction text
- **highlightType**: None, Circle, Rectangle
- **targetTag**: Name/tag of UI element to highlight
- **advanceType**: Button, TargetClick, or Automatic
- **openTopMenu**: Opens the hamburger menu before this step
- **navigateToScreen**: Navigate to a specific screen (0-based index)

### 3. TutorialUI
**Location**: `Assets/Scripts/Tutorial/TutorialUI.cs`

Handles the visual presentation of tutorial steps.

**Features:**
- Overlay with configurable opacity
- Circle or rectangle highlights
- Pulse animations
- Smooth fade in/out transitions
- Step counter display

### 4. TutorialTrigger
**Location**: `Assets/Scripts/Tutorial/TutorialTrigger.cs`

Component to trigger tutorials from buttons or automatically.

**Use Cases:**
- Button to start feature tutorials
- Auto-trigger on scene load
- Delayed tutorial start

## Setup Instructions

### Step 1: Create Tutorial Steps

1. In Unity, right-click in Project window
2. Select `Create > Tutorial > Tutorial Step`
3. Configure the step properties:
   - Set `stepId`, `title`, `description`
   - Choose `highlightType` (None/Circle/Rectangle)
   - Set `targetTag` to the name of the UI element to highlight
   - Choose `advanceType` (Button/TargetClick/Automatic)
   - Configure actions (openTopMenu, navigateToScreen)

**Example Configuration:**

**Step 1 - Welcome**
```
stepId: "welcome"
title: "Bienvenue !"
description: "Bienvenue dans INSASTRONAUTE ! Ce tutoriel va vous guider."
highlightType: None
advanceType: Button
buttonText: "Commencer"
```

**Step 2 - Menu**
```
stepId: "menu_intro"
title: "Menu Principal"
description: "Cliquez sur ce bouton pour ouvrir le menu."
highlightType: Circle
targetTag: "MenuButton"  // Must match the GameObject name
advanceType: TargetClick
waitForTargetClick: true
openTopMenu: false
```

**Step 3 - Collection**
```
stepId: "collection_screen"
title: "Votre Collection"
description: "Ici vous pouvez voir toutes vos cartes collectées."
highlightType: Rectangle
targetTag: "CollectionButton"
advanceType: Button
openTopMenu: true
navigateToScreen: 0
delayBeforeShow: 0.5
```

### Step 2: Create TutorialUI GameObject

1. Create a new Canvas in your Main scene (or use existing)
2. Add a GameObject named "TutorialUI" as child
3. Add `TutorialUI.cs` component
4. Setup the UI hierarchy:

```
Canvas
└── TutorialUI
    ├── Overlay (Image - full screen, black, alpha 0.7)
    ├── HighlightCircle (Image - circular sprite)
    ├── HighlightRect (Image - rounded rectangle)
    └── ContentPanel
        ├── Icon (Image)
        ├── TitleText (TextMeshPro)
        ├── DescriptionText (TextMeshPro)
        ├── StepCounter (TextMeshPro - "1/5")
        ├── NextButton
        │   └── ButtonText (TextMeshPro - "Suivant")
        └── SkipButton
            └── ButtonText (TextMeshPro - "Passer")
```

5. Assign references in TutorialUI component:
   - Drag UI elements to their respective fields
   - Configure colors and animation settings

**Recommended Settings:**
- Fade In Duration: 0.3s
- Fade Out Duration: 0.2s
- Pulse Duration: 1s
- Overlay Color: Black, Alpha 0.7
- Highlight Color: White, Alpha 0.2

### Step 3: Setup TutorialManager

1. Create an empty GameObject named "TutorialManager" in Main scene
2. Add `TutorialManager.cs` component
3. Configure sequences:
   - Set array size for `Tutorial Sequences`
   - For each sequence:
     - Set `sequenceId` (e.g., "FirstTime", "CollectionFeature")
     - Set `displayName`
     - Check `isFirstTimeTutorial` for the main tutorial
     - Drag your TutorialStep ScriptableObjects into the `steps` array
     - Set `completionMessage`

4. Assign references:
   - TutorialUI: Drag the TutorialUI GameObject
   - Top Menu Controller: Usually auto-found
   - Main UI Binder: Usually auto-found

5. Test mode:
   - Check `resetTutorialOnStart` during development
   - Uncheck for production builds

### Step 4: Tag UI Elements

For tutorial highlights to work, your UI elements need identifiable names:

1. Select the UI element you want to highlight
2. Set its GameObject name to match the `targetTag` in your TutorialStep
3. Example:
   - Menu button → Name: "MenuButton"
   - Collection button → Name: "CollectionButton"
   - Shop button → Name: "ShopButton"

**Alternative:** Use Unity tags (assign in Inspector)

### Step 5: Test

1. Enter Play mode
2. Sign in with a new account or guest
3. Tutorial should auto-start after loading screen
4. Follow through all steps

**Testing Tips:**
- Enable `resetTutorialOnStart` in TutorialManager to replay tutorial
- Check console for debug logs: `[TutorialManager]`, `[TutorialUI]`
- Use `TutorialManager.Instance.ResetAllTutorials()` in code

## Usage Examples

### Example 1: First-Time Tutorial
Create a sequence of steps introducing main features:
1. Welcome message
2. Explain top menu
3. Show collection screen
4. Show shop
5. Show events
6. Completion reward

### Example 2: Feature-Specific Tutorial
When user opens shop for the first time:
1. Add `TutorialTrigger` to a button
2. Set `tutorialSequenceId = "ShopIntro"`
3. Check `triggerOnce`
4. Create ShopIntro tutorial steps

### Example 3: Conditional Tutorial
```csharp
// In your code
if (player.level >= 5 && !TutorialManager.Instance.HasCompletedTutorial("AdvancedFeatures"))
{
    TutorialManager.Instance.StartTutorial("AdvancedFeatures");
}
```

## Extending the System

### Adding New Advance Types
Edit `AdvanceType` enum in TutorialStep.cs:
```csharp
public enum AdvanceType
{
    Button,
    TargetClick,
    Automatic,
    Swipe,          // New: Wait for swipe gesture
    DoubleTap       // New: Wait for double tap
}
```

Then handle in TutorialManager.ShowCurrentStep().

### Adding New Highlight Shapes
1. Create new shape prefab in TutorialUI
2. Add to `HighlightType` enum
3. Handle in `TutorialUI.SetupHighlight()`

### Custom Animations
Modify animation settings in TutorialUI Inspector or edit the AnimateIn() method.

### Tutorial Events
Subscribe to events for analytics or custom behavior:
```csharp
void Start()
{
    var manager = TutorialManager.Instance;
    manager.OnStepStarted += (step) => Debug.Log($"Step: {step.title}");
    manager.OnStepCompleted += (step) => Debug.Log($"Completed: {step.title}");
    manager.OnSequenceCompleted += (id) => Debug.Log($"Tutorial done: {id}");
    manager.OnAllTutorialsCompleted += () => Debug.Log("All tutorials done!");
}
```

## Troubleshooting

### Tutorial doesn't start
- Check `TutorialManager` exists in scene
- Verify `autoStartOnFirstLogin` is enabled
- Check PlayerPrefs hasn't marked as completed
- Look for errors in Console

### Highlight not appearing
- Verify target GameObject name matches `targetTag`
- Ensure target has RectTransform component
- Check target is active in hierarchy
- Try using direct reference instead of tag

### Animation issues
- Ensure LeanTween is working (test with other animations)
- Check CanvasGroup component exists on TutorialUI
- Verify animation duration settings

### Tutorial gets stuck
- Check step's `advanceType` is configured correctly
- For TargetClick, ensure clickable element is interactable
- For Automatic, verify `autoAdvanceDelay` is set
- Add debug logs in TutorialManager.ShowCurrentStep()

## Best Practices

1. **Keep steps short**: 5-7 steps max per sequence
2. **Clear instructions**: Use simple, action-oriented text
3. **Visual feedback**: Always highlight the element user should interact with
4. **Test thoroughly**: Test on actual device with touch input
5. **Localization**: Store text in localization system if supporting multiple languages
6. **Analytics**: Track tutorial completion rates
7. **Skip option**: Always provide a way to skip
8. **Resumable**: Consider saving current step for interrupted tutorials

## Integration with Existing Systems

### PlayerStatusController
Already integrated! Tutorial starts automatically after loading.

### MainUIBinder
Use notifications for completion messages:
```csharp
mainUIBinder.ShowNotification("Tutoriel terminé ! +100 TOKENS");
```

### TopMenuController
Tutorial can control menu:
- Set `openTopMenu = true` to open menu
- Set `navigateToScreen` to show specific screen

## Performance Considerations

- Tutorial system is lightweight (~5-10 KB per step)
- Uses object pooling for highlights
- LeanTween animations are GPU-accelerated
- ScriptableObjects loaded once, reused across scenes
- PlayerPrefs checks are fast (cached)

## Future Enhancements

Consider adding:
- [ ] Hand/pointer graphic pointing at targets
- [ ] Voice-over support
- [ ] Video clips in tutorial steps
- [ ] Branching tutorials (different paths)
- [ ] Tutorial replay from settings
- [ ] Tutorial progress indicator
- [ ] Reward system for completing tutorials
- [ ] Multi-language support
- [ ] Context-sensitive help system

## Support

For issues or questions:
1. Check Unity Console for errors
2. Review this documentation
3. Test with `resetTutorialOnStart` enabled
4. Check that all UI names match targetTags

---

**Created by:** GitHub Copilot
**Last Updated:** February 2026
**Version:** 1.0
