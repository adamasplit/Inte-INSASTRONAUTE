# STS Deck Responsive Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Display STS deck cards in a centered horizontal row on desktop while preserving a vertical mobile layout.

**Architecture:** A pure resolver converts viewport dimensions and card count into a desktop/mobile layout description. `DeckGridPanel` applies that description to its `GridLayoutGroup`, `ScrollRect`, and content `RectTransform` whenever the panel opens or its dimensions change.

**Tech Stack:** Unity 6.0, C#, UGUI `GridLayoutGroup`/`ScrollRect`, NUnit edit-mode tests.

## Global Constraints

- Work directly on the existing Unity `main` worktree.
- Do not modify the pre-existing untracked `ServerModeling/` directory.
- Desktop mode requires a landscape viewport at least 900 pixels wide.
- Desktop uses one centered horizontal row and horizontal overflow.
- Mobile keeps the serialized vertical behavior and vertical overflow.
- Card prefabs, selection animation, and game rules remain unchanged.

---

### Task 1: Define and test responsive deck layout calculations

**Files:**
- Create: `Assets/Scripts/Scene/STS/UI/RunManagerUI/STSDeckResponsiveLayout.cs`
- Create: `Assets/Tests/EditMode/STSDeckResponsiveLayoutTests.cs`

**Interfaces:**
- Produces: `STSDeckLayout STSDeckResponsiveLayout.Resolve(float viewportWidth, float viewportHeight, int itemCount, Vector2 cellSize, Vector2 spacing, RectOffset padding, float contentPadding)`
- `STSDeckLayout` exposes `isHorizontal`, `constraint`, `constraintCount`, `contentWidth`, and `contentHeight`.

- [ ] **Step 1: Write failing edit-mode tests**

Cover these cases with direct resolver calls:

```csharp
[Test]
public void DesktopLandscapeUsesOneHorizontalRow()
{
    STSDeckLayout layout = Resolve(width: 1600f, height: 900f, itemCount: 3);

    Assert.That(layout.isHorizontal, Is.True);
    Assert.That(layout.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedRowCount));
    Assert.That(layout.constraintCount, Is.EqualTo(1));
}

[Test]
public void ThreeDesktopCardsFitInsideWideViewport()
{
    STSDeckLayout layout = Resolve(width: 1600f, height: 900f, itemCount: 3);

    Assert.That(layout.contentWidth, Is.LessThanOrEqualTo(1600f));
}

[Test]
public void LargerDesktopDeckProducesHorizontalOverflow()
{
    STSDeckLayout layout = Resolve(width: 1000f, height: 700f, itemCount: 5);

    Assert.That(layout.contentWidth, Is.GreaterThan(1000f));
}

[TestCase(800f, 1200f)]
[TestCase(899f, 500f)]
public void PortraitOrNarrowViewportUsesVerticalLayout(float width, float height)
{
    STSDeckLayout layout = Resolve(width, height, itemCount: 3);

    Assert.That(layout.isHorizontal, Is.False);
}
```

Use cell size `200x300`, spacing `48x48`, padding `48` on every side, and
content padding `24` in the test helper.

- [ ] **Step 2: Run tests and verify RED**

Run:

```bash
/home/brehan/Unity/Hub/Editor/6000.3.2f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /home/brehan/Documents/Insastronaute/UnityPanel/Inte-INSASTRONAUTE \
  -runTests -testPlatform EditMode \
  -testFilter STSDeckResponsiveLayoutTests \
  -testResults /tmp/sts-deck-layout-red.xml \
  -logFile /tmp/sts-deck-layout-red.log
```

Expected: compilation fails because the resolver types do not exist.

- [ ] **Step 3: Implement the pure resolver**

Implement desktop detection as:

```csharp
bool isHorizontal = viewportWidth >= 900f && viewportWidth > viewportHeight;
```

For horizontal mode, calculate one row:

```csharp
float width = padding.horizontal
    + itemCount * cellSize.x
    + Mathf.Max(0, itemCount - 1) * spacing.x
    + contentPadding * 2f;
float height = padding.vertical + cellSize.y + contentPadding * 2f;
```

For mobile mode, return `FixedColumnCount` with one column and calculate one
vertical item per row using the same padding and spacing rules.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run the same Unity command with output paths ending in `-green`.

Expected XML: all `STSDeckResponsiveLayoutTests` cases pass. Unity may still
return code 3 only because existing card filenames differ by case; verify the
XML separately.

---

### Task 2: Apply the responsive layout in DeckGridPanel

**Files:**
- Modify: `Assets/Scripts/Scene/STS/UI/RunManagerUI/DeckGridPanel.cs`
- Modify: `Assets/Scenes/STS_Boot.unity`

**Interfaces:**
- Consumes: `STSDeckResponsiveLayout.Resolve(...)`
- Produces: runtime configuration of the existing deck `GridLayoutGroup`,
  `ScrollRect`, and content `RectTransform`.

- [ ] **Step 1: Preserve serialized mobile settings**

During `Awake`, retain the existing constraint, constraint count, content
anchors/pivot, and scroll directions. These values are the mobile fallback and
must be restored when the WebGL canvas becomes narrow.

- [ ] **Step 2: Apply the layout before measuring content**

Inside `RefreshGridContentSize`, resolve the layout from the actual viewport
rectangle. In desktop mode:

```csharp
gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
gridLayout.constraintCount = 1;
scrollRect.horizontal = true;
scrollRect.vertical = false;
```

Set content width and height from the resolver, clamp the content width to at
least the viewport width so one to three cards can be centered, and set the
content pivot/anchors so horizontal scrolling starts at the left edge only
when overflow exists.

In mobile mode, restore the serialized constraint and vertical scrolling, then
use the resolver’s vertical dimensions.

- [ ] **Step 3: Keep the scene viewport full-width**

Verify the `Panel_DeckList`, `ScrollView`, and `Viewport` anchors remain
stretched from `(0,0)` to `(1,1)`. Serialize explicit horizontal scrolling on
the existing `ScrollRect`; runtime code still switches it off for mobile.

- [ ] **Step 4: Run all relevant edit-mode tests**

Run both fixtures:

```bash
/home/brehan/Unity/Hub/Editor/6000.3.2f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /home/brehan/Documents/Insastronaute/UnityPanel/Inte-INSASTRONAUTE \
  -runTests -testPlatform EditMode \
  -testFilter 'STSDeckResponsiveLayoutTests|STSRunResumeResolverTests' \
  -testResults /tmp/sts-deck-layout-final.xml \
  -logFile /tmp/sts-deck-layout-final.log
```

Expected XML: all selected tests pass.

- [ ] **Step 5: Restore unrelated Unity-generated files and inspect**

```bash
git restore -- Assets/StreamingAssets/STSCardData Inte-INSASTRONAUTE.slnx
git diff --check
git status --short --branch
```

Expected: only the resolver, tests, `DeckGridPanel`, intended scene change,
and Unity-generated `.meta` files for the two new files remain, plus the
pre-existing untracked `ServerModeling/`.

- [ ] **Step 6: Commit Unity changes**

```bash
git add Assets/Scripts/Scene/STS/UI/RunManagerUI/STSDeckResponsiveLayout.cs \
  Assets/Scripts/Scene/STS/UI/RunManagerUI/STSDeckResponsiveLayout.cs.meta \
  Assets/Scripts/Scene/STS/UI/RunManagerUI/DeckGridPanel.cs \
  Assets/Tests/EditMode/STSDeckResponsiveLayoutTests.cs \
  Assets/Tests/EditMode/STSDeckResponsiveLayoutTests.cs.meta \
  Assets/Scenes/STS_Boot.unity
git commit -m "fix: use horizontal STS deck layout on desktop"
```

---

### Task 3: Final verification

**Files:**
- Verify only: Unity repository

**Interfaces:**
- Produces: test and Git evidence for delivery.

- [ ] **Step 1: Re-run selected tests from the committed state**

Use the Task 2 Unity command with `/tmp/sts-deck-layout-committed.xml`.

Expected XML: all selected tests pass.

- [ ] **Step 2: Verify repository state**

Run:

```bash
git restore -- Assets/StreamingAssets/STSCardData Inte-INSASTRONAUTE.slnx
git diff --check
git status --short --branch
git log -3 --oneline
```

Expected: Unity remains on `main`; only `ServerModeling/` is untracked; the
responsive layout commit is at `HEAD`.
