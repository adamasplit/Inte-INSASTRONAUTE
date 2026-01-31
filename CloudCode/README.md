# Cloud Code Scripts - Betting System

## Files Overview

### PlaceBet.js
Allows players to place YES/NO bets on events.

**Parameters:**
- `eventId` (string): Event identifier
- `amount` (number): Bet amount in TOKEN
- `side` (boolean): true = YES, false = NO
- `odds` (number): Event odds
- `deadlineIso` (string): ISO deadline for betting
- `status` (string): Event status (must be "OPEN")

**Returns:**
```json
{
  "ok": true/false,
  "message": "string",
  "bet": {
    "eventId": "string",
    "amount": 100,
    "odds": 1.5,
    "side": true,
    "placedIso": "2026-01-24T...",
    "resolved": false
  },
  "eventId": "string"
}
```

**Features:**
- Validates event is OPEN
- Checks deadline hasn't passed
- Prevents duplicate bets on same event
- Debits TOKEN from player balance
- Saves bet to Cloud Save with key `bet_{eventId}`

---

### ResolveBets.js
Resolves bets for closed events and credits winnings.

**Parameters:**
- `events` (array): Array of closed events with outcomes
  ```json
  [
    {
      "id": "event1",
      "status": "CLOSED",
      "outcome": true
    }
  ]
  ```

**Returns:**
```json
{
  "ok": true/false,
  "resolved": [
    {
      "eventId": "event1",
      "win": true,
      "refund": 150
    }
  ],
  "message": "string"
}
```

**Features:**
- Only processes CLOSED events with outcomes
- Determines win: `bet.side === event.outcome`
- Calculates refund: `amount * odds` if win, 0 if loss
- Credits TOKEN directly via Economy API
- Marks bet as resolved in Cloud Save
- Returns summary for each resolved bet

---

## Deployment Instructions

1. Go to Unity Dashboard → Cloud Code
2. Create two new scripts:
   - Name: `PlaceBet`, paste content from PlaceBet.js
   - Name: `ResolveBets`, paste content from ResolveBets.js
3. Save and publish both scripts
4. Ensure your project has:
   - Economy currency "TOKEN" configured
   - Cloud Save enabled

## Betting Flow

1. **Place Bet**: Player opens event page → chooses YES/NO → confirms → PlaceBet.js executes
2. **TOKEN Debit**: Immediate debit from player's TOKEN balance
3. **Store Bet**: Saved in Cloud Save with key `bet_{eventId}`
4. **Resolve**: On login, client fetches events → sends CLOSED events with outcomes to ResolveBets.js
5. **TOKEN Credit**: ResolveBets.js calculates and credits winnings
6. **Notification**: Client displays win/loss notification with refund amount

## Testing

Example Remote Config for events_json:
```json
{
  "events": [
    {
      "id": "event1",
      "type": "PARI",
      "enabled": true,
      "priority": 100,
      "title": "Test Event",
      "body": "Will it rain tomorrow?",
      "bannerUrl": "",
      "odds": 1.5,
      "deadlineIso": "2026-02-01T00:00:00Z",
      "status": "OPEN",
      "outcome": ""
    }
  ]
}
```

To close and resolve:
- Change `status` to `"CLOSED"`
- Set `outcome` to `"true"` or `"false"`
- Player logs in → ResolveBets runs → notification shown
