# STS Rest Completion Idempotency Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prevent a duplicated rest-exit action from disabling remote STS progression saves.

**Architecture:** Unity uses a small single-flight gate around rest completion and remains in the rest scene after genuine failures. The backend treats a retry for the authoritative current, already-completed node as a read-only success response, while preserving conflicts for unrelated nodes.

**Tech Stack:** Unity 6.0/C#, NUnit edit-mode tests, Java/Spring Boot, Jackson, AssertJ, Gradle.

## Global Constraints

- Work directly in the existing Unity `main` and backend `dev` working trees.
- Do not create isolated worktrees or modify the pre-existing untracked `ServerModeling/` directory.
- Never enable Unity unrestricted mode because a rest completion failed.
- Do not grant rewards, tokens, or inventory changes twice on an idempotent retry.

---

### Task 1: Make backend node completion retries idempotent

**Files:**
- Modify: `/home/brehan/IdeaProjects/webAPI/src/main/java/fr/insastronaute/webapi/sts/service/StsRunService.java`
- Test: `/home/brehan/IdeaProjects/webAPI/src/test/java/fr/insastronaute/webapi/sts/service/StsRunServiceTest.java`

**Interfaces:**
- Consumes: `completeNode(AppUser owner, UUID runId, int nodeId, StsCompleteNodeRequest request)`
- Produces: an accepted `StsCompleteNodeResponse` for the authoritative current node when it is already completed and no node is entered.

- [ ] **Step 1: Write the failing backend test**

Add a test that prepares a run whose node 1 is `visited=true`,
`completed=true`, whose `currentNodeId` is 1, and whose `enteredNodeId` is
`null`. Preserve the existing pending rewards, then repeat:

```java
StsCompleteNodeResponse response = stsRunService.completeNode(owner, runId, 1, null);

assertThat(response.accepted()).isTrue();
assertThat(response.currentNodeId()).isEqualTo(1);
assertThat(json(response.pendingRewards())).isEqualTo(run.getPendingRewards());
verifyNoInteractions(userRepository);
```

- [ ] **Step 2: Run the focused test and verify RED**

Run:

```bash
./gradlew test --tests 'fr.insastronaute.webapi.sts.service.StsRunServiceTest.completeNodeRetryReturnsAuthoritativeCompletedState' --console=plain
```

Expected: FAIL with `ConflictException: Ce node STS n'est pas en cours.`

- [ ] **Step 3: Implement the minimal idempotent branch**

Resolve the map node before validating `enteredNodeId`. When no node is
entered, return a read-only completion response only if the requested node is
both completed and equal to `currentNodeId`:

```java
ObjectNode node = node(run.getMapState(), nodeId);
if (run.getEnteredNodeId() == null) {
    if (node.path("completed").asBoolean(false) && run.getCurrentNodeId() == nodeId) {
        return completionResponse(
                run,
                objectMapper.createObjectNode(),
                objectMapper.createObjectNode()
        );
    }
    throw new ConflictException("Ce node STS n'est pas en cours.");
}
if (run.getEnteredNodeId() != nodeId) {
    throw new ConflictException("Ce node STS n'est pas en cours.");
}
```

Keep the existing completed-node conflict after this branch so an inconsistent
state with an entered, already-completed node is not silently accepted.

- [ ] **Step 4: Verify backend GREEN**

Run the focused test, then:

```bash
./gradlew test --console=plain
git diff --check
```

Expected: focused and complete suites pass; `git diff --check` emits nothing.

- [ ] **Step 5: Commit backend changes**

```bash
git add src/main/java/fr/insastronaute/webapi/sts/service/StsRunService.java \
  src/test/java/fr/insastronaute/webapi/sts/service/StsRunServiceTest.java
git commit -m "fix: make STS node completion idempotent"
```

---

### Task 2: Make Unity rest exit single-flight and retryable

**Files:**
- Modify: `Assets/Scripts/Scene/STS/Resume/STSRunResumeResolver.cs`
- Modify: `Assets/Scripts/Scene/STS/Rest/RestManager.cs`
- Test: `Assets/Tests/EditMode/STSRunResumeResolverTests.cs`

**Interfaces:**
- Produces: `STSCompletionGate.TryBegin(): bool`
- Produces: `STSCompletionGate.Reset(): void`
- Consumes: the gate from `RestManager.ReturnToMap()`.

- [ ] **Step 1: Write failing Unity gate tests**

Add two edit-mode tests:

```csharp
[Test]
public void CompletionGateRejectsASecondConcurrentAttempt()
{
    var gate = new STSCompletionGate();

    Assert.That(gate.TryBegin(), Is.True);
    Assert.That(gate.TryBegin(), Is.False);
}

[Test]
public void CompletionGateAllowsRetryAfterReset()
{
    var gate = new STSCompletionGate();

    Assert.That(gate.TryBegin(), Is.True);
    gate.Reset();
    Assert.That(gate.TryBegin(), Is.True);
}
```

- [ ] **Step 2: Run Unity edit-mode tests and verify RED**

Run Unity 6000.3.2f1 in batch mode with the existing edit-mode test selection
and an XML result under `/tmp`.

Expected: compilation fails because `STSCompletionGate` does not exist.

- [ ] **Step 3: Implement the minimal gate**

Add the following focused type beside the existing resume helpers:

```csharp
public sealed class STSCompletionGate
{
    private bool isRunning;

    public bool TryBegin()
    {
        if (isRunning)
            return false;

        isRunning = true;
        return true;
    }

    public void Reset()
    {
        isRunning = false;
    }
}
```

- [ ] **Step 4: Integrate the gate into rest exit**

Create one gate per `RestManager`. At the start of `ReturnToMap`, return when
`TryBegin()` is false. Load the map only after an accepted completion. When the
completion returns false, reset the gate and remain in the scene.

Change `TryCompleteCurrentNodeAsync` so a null/rejected response or exception
logs the failure and returns `false`. Remove both calls to
`EnableUnrestrictedMode`. Successful and explicitly local/unrestricted paths
continue to return `true`.

- [ ] **Step 5: Verify Unity GREEN**

Run the same edit-mode test command and inspect the XML result.

Expected: all edit-mode tests pass. If Unity still exits 3 solely because of
the pre-existing card filenames that differ only by case, report that
separately and restore only Unity-generated tracked changes:

```bash
git restore -- Assets/StreamingAssets/STSCardData Inte-INSASTRONAUTE.slnx
git diff --check
```

- [ ] **Step 6: Commit Unity changes**

```bash
git add Assets/Scripts/Scene/STS/Resume/STSRunResumeResolver.cs \
  Assets/Scripts/Scene/STS/Rest/RestManager.cs \
  Assets/Tests/EditMode/STSRunResumeResolverTests.cs
git commit -m "fix: keep STS rest completion retryable"
```

---

### Task 3: Verify integration boundaries

**Files:**
- Verify only: `/home/brehan/Documents/Insastronaute/insastral`
- Verify only: both modified repositories

**Interfaces:**
- Consumes: existing React bridge routing for `sts.runs.*.nodes.*.complete`
- Produces: evidence that the bridge build and repository states remain clean.

- [ ] **Step 1: Run the web bridge regression and build**

```bash
node --test tests/jeu-regression.test.mjs
npm run build
```

Expected: regression test passes and Vite build succeeds.

- [ ] **Step 2: Inspect final repository state**

Run `git status --short --branch`, `git diff --check`, and `git log -1
--oneline` in backend, Unity, insastral, and PanelAdmin.

Expected: backend is on `dev`; Unity and insastral are on `main`; PanelAdmin is
on `dev`; only the pre-existing untracked Unity `ServerModeling/` directory
remains.
