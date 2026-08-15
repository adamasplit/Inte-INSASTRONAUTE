# STS Run Resume Consistency Design

## Goal

Make the backend the canonical source for STS run resumption so that an
interrupted run returns to the correct gameplay phase and a completed boss
choice is never lost.

## Scope

This change covers:

- resuming an entered combat at the beginning of the same encounter;
- resuming an entered event or rest node on its matching scene;
- reopening pending rewards before allowing further map progress;
- reopening the boss retreat choice after a completed boss;
- waiting for backend confirmation before leaving the boss choice screen;
- making continue and retire decisions safe to retry.

Persisting mid-combat state is explicitly out of scope. Combat is not
turn-based, so interruption restarts the same encounter from its initial
server state.

## Canonical State

The backend run remains authoritative. Unity local save data is a fallback for
offline/unrestricted play and must not override a successfully loaded remote
run.

The resume phase is derived from the existing run DTO:

1. `enteredNodeId` with `activeEncounter` opens `STS_Combat`.
2. `enteredNodeId` with `activeEvent` opens the event scene.
3. `enteredNodeId` for a rest node opens the rest scene.
4. Unclaimed `pendingRewards` open `STS_Reward`.
5. A completed current boss node with no entered node opens `STS_Retreat`.
6. Otherwise the run opens `STS_Map`.

Unity must use `enteredNodeId` to select the entered map node. It must only use
`currentNodeId` as the last completed position when there is no entered node.

## Boss Decision Model

Completing a boss does not terminate the run. It leaves the run `Active` on a
completed boss node until the player chooses:

- Continue: the existing retreat-continue transaction advances the act,
  creates the next map state, and returns the updated run.
- Finish: the existing retire transaction calculates the score and token
  reward and changes the status to `Retired`.

Both operations must be idempotent:

- retrying continue after it succeeded returns the already-advanced active run;
- retrying retire returns the stored retirement result without granting tokens
  twice.

Unity stays on `STS_Retreat` if either operation fails or returns an unusable
state. It clears local state and leaves for the menu only after retirement is
confirmed. It opens the map only after the continued run is confirmed.

## Unity Responsibilities

Add one focused resolver that maps a remote run state to a resume scene. The
main menu and run startup paths must both use it instead of always loading
`STS_Map`.

Applying a remote run must preserve:

- `enteredNodeId`;
- the matching entered `MapNode`;
- `activeEncounter` and `activeEvent`;
- pending rewards;
- the completed-boss state needed by the retreat screen.

Starting a resumed combat reconstructs combat from `activeEncounter`. No local
combat snapshot is loaded.

The retreat screen must not treat API failure as a successful finalization.
Errors remain visible through logs and the choice stays retryable.

## Backend Responsibilities

Keep `currentNodeId` as the last completed node and `enteredNodeId` as the node
currently in progress. Return both consistently in current-run responses.

Make retreat continuation retry-safe by recognizing a request that already
advanced the boss state. Preserve the existing retirement retry behavior and
add coverage proving tokens are not credited twice.

No database migration is required because the necessary state already exists.

## Error Handling

- A failed current-run request does not erase a local save.
- A malformed remote state does not silently switch to a random map position.
- A failed resume leaves the player on the menu with the run still available.
- A failed continue or retire keeps the retreat controls available for retry.
- API conflicts trigger a current-run refresh before Unity decides whether the
  requested transition already succeeded.

## Testing

Backend service tests will cover:

- current-run state retaining an entered encounter;
- retrying retreat continuation without advancing twice;
- retrying retirement without duplicate token credit;
- boss completion remaining active until a decision.

Unity edit-mode or source-level regression tests will cover the pure resume
scene resolver:

- active encounter -> combat;
- active event -> event;
- entered rest -> rest;
- pending rewards -> reward;
- completed boss -> retreat;
- ordinary run -> map.

Existing backend STS tests, Unity compilation, the React Unity bridge regression
test, and the frontend production build must remain green.
