# Quick Setup Guide - Tutorial System

## 🚀 Quick Start (5 minutes)

### Option 1: Automatic Template (Recommended)

1. **Create Tutorial Steps Template**
   - In Unity menu: `Tools > Tutorial > Create First-Time Tutorial Template`
   - This creates 5 pre-configured tutorial steps in `Assets/Resources/Tutorials/`

2. **Setup TutorialUI GameObject**
   - In your Main scene, create: `Canvas > TutorialUI`
   - Add component: `TutorialUI.cs`
   - Create child objects:
     ```
     TutorialUI
     ├── Overlay (Image - Black, Alpha 0.7, Stretch to fill)
     ├── HighlightCircle (Image - White circle sprite)
     ├── HighlightRect (Image - White rounded rect)
     └── ContentPanel (Vertical Layout Group)
         ├── Icon (Image - Optional)
         ├── TitleText (TextMeshPro - Bold, 32pt)
         ├── DescriptionText (TextMeshPro - Regular, 20pt)
         ├── StepCounter (TextMeshPro - "1/5")
         ├── NextButton > ButtonText ("Suivant")
         └── SkipButton > ButtonText ("Passer")
     ```
   - Drag references in TutorialUI component
   - Position ContentPanel at bottom of screen

3. **Setup TutorialManager**
   - Create empty GameObject: "TutorialManager"
   - Add component: `TutorialManager.cs`
   - Configure:
     - Tutorial Sequences: Size = 1
     - Sequence 0:
       - Sequence ID: "FirstTime"
       - Display Name: "Introduction"
       - Is First Time Tutorial: ✓
       - Steps: Drag your 5 tutorial steps
       - Completion Message: "Tutoriel terminé ! Amusez-vous bien !"
   - Assign references:
     - Tutorial UI: Drag TutorialUI GameObject
     - Top Menu Controller: Auto-found (leave empty)
     - Main UI Binder: Auto-found (leave empty)
   - For testing: Check "Reset Tutorial On Start"

4. **Tag Your UI Elements**
   - Find your menu button → Rename to "MenuButton"
   - Find collection menu item → Rename to "CollectionButton"
   - Find shop menu item → Rename to "ShopButton"
   - (Names must match the targetTag in tutorial steps)

5. **Test**
   - Play the scene
   - Tutorial should start automatically after loading
   - Follow through all steps
   - Check console for logs

### Option 2: Manual Setup

If you prefer to create steps manually:

1. Right-click in Project: `Create > Tutorial > Tutorial Step`
2. Configure each step (see README.md for details)
3. Follow steps 2-5 from Option 1

## 📝 Customizing Tutorial Steps

After creating the template, customize each step:

### How Click Detection Works

The system is simple:
1. **targetTag** is used to FIND the button by name (e.g., "MenuButton")
2. The system adds a temporary onClick listener to that button
3. When the user clicks the button, we detect it and advance the tutorial
4. The listener is removed when the step completes

**No complex overlays needed!** The user clicks the actual button.

### Step 1 (Welcome) - Already good!
Just update the text if needed.

### Step 2 (Menu)
- Open the asset
- Change `targetTag` to match your actual menu button's name
- Test that the name matches exactly

### Step 3 (Collection)
- Update `targetTag` to your collection button name
- Update `navigateToScreen` to the correct index (count from 0)
- Test screen navigation

### Step 4 (Shop)
- Update `targetTag` to your shop button name
- Update `navigateToScreen` to the correct index

### Step 5 (Done) - Already good!
Final encouraging message.

## 🎨 Styling the Tutorial UI

### ContentPanel Position
Bottom screen (mobile friendly):
- Anchor: Bottom-Center
- Pivot: (0.5, 0)
- Position Y: 100
- Width: 90% of screen
- Height: Auto (use Vertical Layout Group)

### Colors
Match your app theme:
```
Overlay: Black with 70% opacity
Highlight: White with 20% opacity
Title: Your primary color
Description: White or light grey
```

### Fonts
Use your app's fonts:
- Title: Bold, 28-36pt
- Description: Regular, 18-22pt
- Button: Bold, 20-24pt

### Animations
Default settings work well, but you can adjust:
- Fade In: 0.3s (smooth intro)
- Fade Out: 0.2s (quick exit)
- Pulse: 1s (attention grabbing)

## 🧪 Testing

### During Development
```csharp
// In TutorialManager Inspector:
☑ Reset Tutorial On Start

// Or in code:
TutorialManager.Instance.ResetAllTutorials();
```

### Test Checklist
- [ ] Tutorial starts automatically on first login
- [ ] All UI elements are highlighted correctly
- [ ] Menu opens/closes as expected
- [ ] Screen navigation works
- [ ] "Skip" button works
- [ ] "Next" button works
- [ ] Tutorial completes successfully
- [ ] Tutorial doesn't show on second login
- [ ] Completion message displays

### Common Issues

**Tutorial doesn't start**
- Check PlayerStatusController has the integration code
- Verify TutorialManager is in scene
- Check Console for errors

**Highlights don't appear**
- UI element names must match exactly (case-sensitive!)
- Check GameObject is active
- Verify it has RectTransform
- Try using direct reference instead of tag

**Clicks not detected**
- Check Console logs: "[TutorialUI] Added click listener to button: MenuButton"
- Verify the target button has a Button component
- Make sure the button is interactable (not disabled)
- Check tutorial step has advanceType = TargetClick
- The targetTag is used to FIND the button, then we add a listener to it

**Animation glitches**
- Ensure LeanTween is imported
- Check Canvas render mode
- Verify all UI references are assigned

## 🎯 Next Steps

1. **Test thoroughly** on actual device
2. **Adjust timing** (step delays, animation speeds)
3. **Customize text** for your app's tone
4. **Add more tutorials** for other features
5. **Track analytics** (add events in TutorialManager)

## 🔄 Update TopMenuController Integration

Make sure your menu items have accessible names:

```csharp
// In TopMenuController, make sure menu items are named properly
// Example hierarchy:
TopMenu
└── Panel
    └── MenuItems
        ├── CollectionButton (This is what tutorial targets)
        ├── ShopButton
        ├── LeaderboardButton
        └── EventsButton
```

## 🌟 Pro Tips

1. **Keep it short**: 5-7 steps maximum for first-time tutorial
2. **One concept per step**: Don't overwhelm users
3. **Clear actions**: Always tell user what to do
4. **Visual feedback**: Highlight makes huge difference
5. **Test on device**: Touch interactions differ from mouse
6. **Skip option**: Some users don't want tutorials
7. **Reward completion**: Give tokens or bonus after tutorial

## 📞 Need Help?

- Check the full [README.md](README.md) for detailed documentation
- Review Console logs for errors
- Use menu item: `Tools > Tutorial > Reset All Tutorial Progress`
- Test step by step using debug mode

---

**Setup Time**: ~5 minutes
**Customization**: ~15 minutes
**Total**: ~20 minutes to full working tutorial! 🎉
