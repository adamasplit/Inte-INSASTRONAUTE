# STS Rest Completion Idempotency Design

## Problem

Leaving rest node 17 emitted two concurrent completion requests. One request
completed the node successfully; the other received HTTP 409 because the node
was no longer entered. Unity treated that second response as a remote-save
failure, enabled unrestricted mode, and stopped sending authoritative
progression updates for the remainder of the run.

## Expected behavior

- Only one rest completion request may be in flight from a Unity client.
- A repeated completion of the same already-completed node is safe and returns
  the authoritative completed state.
- A genuine completion failure keeps the player in the rest scene so the
  operation can be retried.
- A completion failure must never enable unrestricted mode or silently continue
  with local-only progression.
- The successful completion response remains authoritative for player HP and
  map state.

## Design

Unity owns interaction-level concurrency. `RestManager` records that a return
is in progress before awaiting the API call and ignores subsequent return
actions until the first operation finishes. It loads the map only after an
accepted response. A null response, rejection, or exception clears the guard
and leaves the rest scene active.

The backend owns transport-level idempotency. When `completeNode` receives a
retry for a node that is already present in `completedNodeIds`, with no other
node currently entered, it returns the normal authoritative completion
response without applying rewards or inventory effects again. Requests for a
different node or a conflicting active node remain rejected.

## Tests

- Unity edit-mode tests cover the single-flight return guard and its reset
  after failure.
- Backend service tests cover a retry of an already-completed rest node and
  verify that the response is accepted without duplicating effects.
- Existing backend, Unity edit-mode, and web bridge regression suites remain
  green.
