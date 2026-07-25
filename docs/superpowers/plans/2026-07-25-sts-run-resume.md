# STS Run Resume Consistency Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resume remote STS runs on the correct gameplay scene and make boss continue/retire decisions safe across retries and network failures.

**Architecture:** The backend remains authoritative and returns every field required to derive a resume phase. Unity applies the full remote state, resolves one deterministic scene from that state, and only leaves the boss choice after a confirmed backend transition.

**Tech Stack:** Java 25, Spring Boot 4, JUnit 5, Mockito, Unity 6/C#, Newtonsoft JSON, React/Vite bridge regression tests.

## Global Constraints

- Backend and PanelAdmin work branches start from `dev`.
- Unity and insastral work branches start from `main`.
- Mid-combat snapshots remain out of scope; an interrupted combat restarts the same encounter.
- Local Unity saves never override a successfully loaded remote run.
- No database migration or new dependency is introduced.

---

### Task 1: Complete the backend current-run contract

**Files:**
- Modify: `/home/brehan/IdeaProjects/webAPI/src/main/java/fr/insastronaute/webapi/sts/dto/StsRunStateDto.java`
- Modify: `/home/brehan/IdeaProjects/webAPI/src/main/java/fr/insastronaute/webapi/sts/service/StsRunService.java`
- Test: `/home/brehan/IdeaProjects/webAPI/src/test/java/fr/insastronaute/webapi/sts/controller/StsRunControllerTest.java`

**Interfaces:**
- Produces: `StsRunStateDto.activeEncounter()` serialized as `activeEncounter`.
- Consumes: existing `StsRun.activeEncounter` JSON state.

- [ ] **Step 1: Write the failing controller test**

Extend `currentRunSerializesJsonStateAsPayload` so its `StsRunStateDto` fixture
contains both an active encounter and active event, then assert:

```java
.andExpect(jsonPath("$.run.activeEncounter.encounterInstanceId").value("encinst_1"))
.andExpect(jsonPath("$.run.activeEvent.id").value("event_a"));
```

- [ ] **Step 2: Run the focused test and verify RED**

Run:

```bash
./gradlew test --tests 'fr.insastronaute.webapi.sts.controller.StsRunControllerTest.currentRunSerializesJsonStateAsPayload' --console=plain
```

Expected: compilation or assertion failure because `StsRunStateDto` does not
expose `activeEncounter`.

- [ ] **Step 3: Add `activeEncounter` to the DTO mapping**

Add `Object activeEncounter` before `Object activeEvent` in
`StsRunStateDto`, and pass `apiJson(run.getActiveEncounter())` from
`StsRunService.toDto`.

- [ ] **Step 4: Run the focused test and verify GREEN**

Run the command from Step 2. Expected: PASS.

- [ ] **Step 5: Commit the backend contract**

```bash
git add src/main/java/fr/insastronaute/webapi/sts/dto/StsRunStateDto.java \
  src/main/java/fr/insastronaute/webapi/sts/service/StsRunService.java \
  src/test/java/fr/insastronaute/webapi/sts/controller/StsRunControllerTest.java
git commit -m "fix: expose active STS encounter on resume"
```

### Task 2: Make boss decisions retry-safe on the backend

**Files:**
- Modify: `/home/brehan/IdeaProjects/webAPI/src/main/java/fr/insastronaute/webapi/sts/service/StsRunService.java`
- Test: `/home/brehan/IdeaProjects/webAPI/src/test/java/fr/insastronaute/webapi/sts/service/StsRunServiceTest.java`

**Interfaces:**
- Produces: retry-safe `continueAfterRetreat(AppUser, UUID)`.
- Preserves: retry-safe `retireRun(AppUser, UUID)` with no duplicate tokens.

- [ ] **Step 1: Write failing continuation tests**

Add a test proving continuation rejects a run unless its current node is a
completed boss, and a test proving a retry after successful advancement returns
the same act/map without advancing twice.

The retry fixture must represent an active run at the generated start node with
`act` already incremented and no active/entered node.

- [ ] **Step 2: Run the continuation tests and verify RED**

Run:

```bash
./gradlew test --tests 'fr.insastronaute.webapi.sts.service.StsRunServiceTest.*continue*Retreat*' --console=plain
```

Expected: the invalid-state test fails because continuation currently advances
any inactive-node run, and the retry test advances the act twice.

- [ ] **Step 3: Implement the minimal state guard and idempotency**

Before advancing, inspect the current map node:

```java
ObjectNode currentNode = node(run.getMapState(), run.getCurrentNodeId());
boolean completedBoss = "Boss".equals(currentNode.path("type").asText())
        && currentNode.path("completed").asBoolean(false);
```

Advance only for `completedBoss`. Recognize an already-continued state by the
canonical generated start node (`currentNodeId == 0`, floor `0`, start node
completed, no entered node) and return `toDto(run)` unchanged.

- [ ] **Step 4: Re-run continuation and retirement tests**

Run:

```bash
./gradlew test --tests 'fr.insastronaute.webapi.sts.service.StsRunServiceTest' --console=plain
```

Expected: PASS, including the existing duplicate-retirement-credit test.

- [ ] **Step 5: Commit retry-safe transitions**

```bash
git add src/main/java/fr/insastronaute/webapi/sts/service/StsRunService.java \
  src/test/java/fr/insastronaute/webapi/sts/service/StsRunServiceTest.java
git commit -m "fix: make STS boss decisions retry-safe"
```

### Task 3: Add a deterministic Unity resume-phase resolver

**Files:**
- Create: `Assets/Scripts/Scene/STS/Core/STSRunResumeResolver.cs`
- Create: `Assets/Scripts/Scene/STS/Core/STSRunResumeResolver.cs.meta`
- Create: `Assets/Tests/EditMode/STSRunResumeResolverTests.cs`
- Create: `Assets/Tests/EditMode/STSRunResumeResolverTests.cs.meta`
- Create: `Assets/Tests/EditMode/STS.EditModeTests.asmdef`
- Create: `Assets/Tests/EditMode/STS.EditModeTests.asmdef.meta`

**Interfaces:**
- Produces: `STSRunResumeResolver.Resolve(STSApiRunState state, IReadOnlyList<JToken> pendingRewards)`.
- Returns: scene names `STS_Combat`, `STS_Event`, `STS_Rest`, `STS_Reward`, `STS_Retreat`, or `STS_Map`.

- [ ] **Step 1: Write failing resolver tests**

Create NUnit edit-mode tests for these exact cases:

```text
entered node + activeEncounter -> STS_Combat
entered Event node + activeEvent -> STS_Event
entered Rest node -> STS_Rest
unclaimed pending reward -> STS_Reward
completed current Boss -> STS_Retreat
ordinary active run -> STS_Map
```

- [ ] **Step 2: Run the edit-mode tests and verify RED**

Run the repository’s available Unity test runner, or compile the test assembly
in batch mode. Expected: failure because `STSRunResumeResolver` does not exist.

- [ ] **Step 3: Implement the pure resolver**

Implement a static resolver with priority:

```text
entered encounter, entered event/rest, pending rewards, completed boss, map
```

Only rewards whose `claimed` property is not `true` count as pending.

- [ ] **Step 4: Re-run edit-mode tests and verify GREEN**

Run the command from Step 2. Expected: all six cases PASS.

- [ ] **Step 5: Commit the resolver**

```bash
git add Assets/Scripts/Scene/STS/Core/STSRunResumeResolver.cs* \
  Assets/Tests/EditMode
git commit -m "feat: resolve STS resume scene from server state"
```

### Task 4: Apply complete remote state and enter the resolved scene

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Api/STSApiClient.cs`
- Modify: `Assets/Scripts/Scene/STS/Core/RunManager.cs`
- Modify: `Assets/Scripts/Scene/STS/UI/STSMainMenuController.cs`
- Test: `Assets/Tests/EditMode/STSRunResumeResolverTests.cs`

**Interfaces:**
- Consumes: `STSApiRunCreateResponse.activeEncounter`, `activeEvent`,
  `enteredNodeId`, and pending rewards.
- Produces: `RunManager.ResolveRemoteResumeScene()`.

- [ ] **Step 1: Extend the test fixture to parse and preserve active state**

Add a JSON conversion test proving `STSApiClient.ConvertToRunState` retains
`enteredNodeId`, `activeEncounter`, and `activeEvent`.

- [ ] **Step 2: Run the test and verify RED**

Expected: failure because `ConvertToRunState` currently omits both active
fields.

- [ ] **Step 3: Map and apply the missing fields**

Copy `response.activeEncounter` and `response.activeEvent` in
`ConvertToRunState`. In both RunManager remote-state application paths, choose
the local current node from `enteredNodeId ?? currentNodeId`, retain active
state, and expose the resolver result.

- [ ] **Step 4: Use the resolved scene in the main menu**

Replace the unconditional `STS_Map` load in
`TryContinueExistingRunAsync` with `RunManager.Instance.ResolveRemoteResumeScene()`.
Use the same resolver after create/recovery in `StartRunAsync`.

- [ ] **Step 5: Run Unity tests and compile**

Expected: resolver/conversion tests PASS and Unity scripts compile.

- [ ] **Step 6: Commit remote resume orchestration**

```bash
git add Assets/Scripts/Scene/STS/Api/STSApiClient.cs \
  Assets/Scripts/Scene/STS/Core/RunManager.cs \
  Assets/Scripts/Scene/STS/UI/STSMainMenuController.cs \
  Assets/Tests/EditMode/STSRunResumeResolverTests.cs
git commit -m "fix: resume STS run on authoritative scene"
```

### Task 5: Keep the boss choice open until confirmation

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Retreat/RetreatManager.cs`
- Test: `Assets/Tests/EditMode/STSRunResumeResolverTests.cs`

**Interfaces:**
- Consumes: retry-safe retire and retreat-continue API calls.
- Produces: a successful transition result before scene navigation.

- [ ] **Step 1: Add failing transition-result tests**

Extract or expose pure predicates that accept only:

- retirement response with `accepted == true` and status `Retired`;
- continuation state with the same run ID, `Active` status, and a later act.

Test null, rejected, stale, and valid responses.

- [ ] **Step 2: Run tests and verify RED**

Expected: failure because the predicates do not exist and final retirement
currently returns early whenever `leavingRetreat` is true.

- [ ] **Step 3: Implement confirmed navigation**

Make final retirement return `Task<bool>`. Do not skip response validation
because the screen is leaving. On failure, restore controls and stay on
`STS_Retreat`. Apply the same rule to continuation. Clear local run state only
after confirmed retirement.

- [ ] **Step 4: Run Unity tests and compile**

Expected: all transition and resolver tests PASS.

- [ ] **Step 5: Commit guarded boss navigation**

```bash
git add Assets/Scripts/Scene/STS/Retreat/RetreatManager.cs \
  Assets/Tests/EditMode/STSRunResumeResolverTests.cs
git commit -m "fix: wait for STS boss decision confirmation"
```

### Task 6: Verify bridge and complete regression suite

**Files:**
- Modify only if required: `insastral/src/lib/unityApiBridge.ts`
- Modify only if required: `insastral/tests/jeu-regression.test.mjs`
- No planned PanelAdmin production change.

**Interfaces:**
- Preserves existing Unity bridge request names and HTTP endpoints.

- [ ] **Step 1: Run frontend bridge regression**

```bash
node --test tests/jeu-regression.test.mjs
npm run build
```

Expected: PASS. If contract assertions expose a missing bridge field, first add
a failing regression assertion, then make the smallest bridge change.

- [ ] **Step 2: Run the full backend suite**

```bash
./gradlew test --console=plain
```

Expected: PASS.

- [ ] **Step 3: Run Unity edit-mode tests and batch compilation**

Use the installed Unity editor in batch mode when available. Expected: zero
compiler errors and all STS edit-mode tests PASS.

- [ ] **Step 4: Check every worktree**

```bash
git status --short
git diff --check
```

Expected: only intentional changes, with no whitespace errors. PanelAdmin must
remain unchanged unless a discovered shared-contract requirement was first
covered by a failing test.
