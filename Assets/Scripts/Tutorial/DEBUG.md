# Tutorial Debug Guide

## Debugging Tutorial Issues

### Common Issues

#### 1. Tutorial Starts Every Time in Editor
**Cause:** `resetTutorialOnStart` is checked in TutorialManager inspector.
**Fix:** Uncheck `resetTutorialOnStart` in the TutorialManager component (should only be used during development).

#### 2. Tutorial Doesn't Start in WebGL Build
**Possible causes:**
- PlayerPrefs not saving correctly in browser
- Tutorial auto-start timing issues
- PlayerPrefs were saved from a previous session

### Reading Console Logs

The TutorialManager now logs detailed state information at every critical point. Look for these log patterns:

### Checking PlayerPrefs in WebGL

#### WebGL Console Logs
In WebGL builds, open browser Developer Tools (F12) → Console tab.
All Unity Debug.Log messages appear here.

Look for:
- `[TutorialManager] === Tutorial State ===` - shows all PlayerPrefs values
- `[TutorialManager] IsFirstTime check:` - shows if user is first-time
- `[PlayerStatusController] Starting first-time tutorial` - player controller trigger
- `[TutorialManager] Starting first-time tutorial:` - actual start confirmation

#### Checking PlayerPrefs in Browser
WebGL PlayerPrefs are stored in browser LocalStorage. In browser console:

```javascript
// Check if player has seen tutorial
localStorage.getItem('Unity.UnityPlayer.HasSeenAnyTutorial')

// Check active tutorial sequence
localStorage.getItem('Unity.UnityPlayer.Tutorial_Active_Sequence')

// Clear specific tutorial PlayerPrefs
localStorage.removeItem('Unity.UnityPlayer.HasSeenAnyTutorial');
localStorage.removeItem('Unity.UnityPlayer.Tutorial_Active_Sequence');
localStorage.removeItem('Unity.UnityPlayer.Tutorial_Active_Step');
localStorage.removeItem('Unity.UnityPlayer.Tutorial_Completed_FirstTime');

// Then reload
location.reload();
```

#### Calling Unity Methods from Browser Console (WebGL)
You can trigger Unity debug methods directly from browser console:

```javascript
// Log current tutorial state (shows in console)
unityInstance.SendMessage('TutorialManager', 'DebugLogState');

// Reset all tutorial progress
unityInstance.SendMessage('TutorialManager', 'DebugResetTutorial');
```

**Note:** The `unityInstance` variable name depends on your WebGL template. Check your index.html or try `gameInstance` if `unityInstance` doesn't work.

### Manual Testing Steps

#### Test in Editor
1. **First run (should show tutorial):**
   - Ensure `resetTutorialOnStart` is UNCHECKED
   - Clear all PlayerPrefs: Unity menu → Edit → Clear All PlayerPrefs
   - Run the game
   - Tutorial should start automatically
   - Complete the tutorial

2. **Second run (should NOT show tutorial):**
   - Run the game again (without clearing PlayerPrefs)
   - Tutorial should NOT start
   - Check console: should say "Not first time, skipping tutorial"

#### Test in WebGL B with Developer Tools (F12) → Console tab
   - Tutorial should start after loading completes
   - Watch console for `[TutorialManager]` logs
   - Should see: `[TutorialManager] Starting first-time tutorial: FirstTime`

2. **Debug if tutorial doesn't start:**
   - Check console for state logs showing "resetTutorialOnStart: False"
   - Check for "HasSeenAnyTutorial: 0" (should be 0 for first time)
   - Look for any error messages
   - In console, run: `localStorage.getItem('Unity.UnityPlayer.HasSeenAnyTutorial')`
   - Should return `null` or `"0"` for first-time users

3. **Second run:**
   - Reload the page (or close and reopen)
   - Tutorial should NOT start
   - Console should show: `[TutorialManager] Not first time, skipping tutorial`
   - In console: `localStorage.getItem('Unity.UnityPlayer.HasSeenAnyTutorial')` should return `"1"`

4. **Reset tutorial:**
   - In browser console, run the clear commands above (see "Checking PlayerPrefs in Browser")
4. **Reset tutorial:**
   - Press F12 in game
   - Click "Reset" button
   - Reload the page
   - Tutorial should start again

### Understanding the Logs

Key log messages to look for:

```
[TutorialManager] === Tutorial State (Awake) ===
  resetTutorialOnStart: False  ✓ Good - won't reset every time
  HasSeenAnyTutorial: 0        ✓ First time user
  
[TutorialManager] Starting first-time tutorial: FirstTime
  → Tutorial starting successfully

[TutorialManager] Setting HasSeenAnyTutorial = 1 and saving PlayerPrefs
  → Tutorial completed and saved

[TutorialManager] IsFirstTime check: False (HasSeenAnyTutorial=1)
  → Next run will skip tutorial
```

### Production Checklist

Before deploying:
- [ ] `resetTutorialOnStart` is UNCHECKED in TutorialManager
- [ ] `est in WebGL: fresh browser session shows tutorial
- [ ] Test in WebGL: second session doesn't show tutorial
- [ ] Test in WebGL: browser console shows correct state logs
- [ ] Test incognito/private browsing mode (fresh PlayerPrefs)
- [ ] Test in WebGL: F12 console shows correct PlayerPrefs state

### Troubleshooting Specific Issues

**Tutorial starts every time in editor:**
→ Check `resetTutorialOnStart` in inspector

**Tutorial doesn't start at all in WebGL:**
→ Look for `[TutorialManager]` state logs
→ Verify localStorage has `HasSeenAnyTutorial` = null or "0"
→ Ensure TutorialManager and TutorialUI are in scene

**Tutorial starts but doesn't save progress:**
→ Check browser console for `Setting HasSeenAnyTutorial = 1` log
→ Browser might be blocking LocalStorage (check privacy settings)
→ Try incognito mode or different browser
→ Clear browser data and try again

**How to force tutorial to show again:**
→ Open browser console (F12)
→ Run: `localStorage.clear(); location.reload();`
→ Or use the PlayerPrefs clear commands from abovescene
→ Check debugPanel reference is assigned
