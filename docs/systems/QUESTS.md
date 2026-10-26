# Quest System – concept

Source: design doc, "Quest System" (Main, Side, Daily, Weekly, Village, Faction, Hunt and Event quests). This
concept covers **Daily, Weekly and Main/Village quests**. Side, Faction and Event quests need content the doc does
not have yet (local stories, faction reputation, Event Portals); Hunt quests are the Hunt Board (`HUNT_BOARD.md`).

## Goals

- Give a clear next step at every moment (doc: "Quests guide the player through the game").
- Daily/weekly goals that reward coming back, without forcing long sessions (idle game: 5–10 minutes a day).
- Every quest is checked by the rules, not by the client, so a server can validate it later.

## Tracking

Commands count what the player does in lifetime **stats** (`PlayerState.Stats`):

| Stat | Counted by |
|---|---|
| `battleWon` | every won battle (campaign, hunts) |
| `stageCleared` | first clear of a campaign stage |
| `afkClaimed` | claiming idle rewards (when there was something to claim) |
| `villageCollected` | collecting Dawnhollow storage |
| `heroLevelUps` | hero levels gained |
| `summons` | heroes summoned (1 per pull) |
| `gearEnhanced`, `gearCrafted` | enhancing / crafting gear |
| `buildingUpgraded` | building or upgrading a building |
| `huntCompleted` | finished hunts (Hunt Board) |
| `dailyCompleted` | days on which all daily points were earned |

Daily and weekly progress = stat now − stat at the start of the day/week (a snapshot taken at the first action
after the reset). No per-quest bookkeeping, so new quests can be added to content without migrations.

## Reset

Daily reset at a fixed hour (UTC, content: `resetHourUtc`, default 04:00). Weekly reset on Monday at the same hour.
Unclaimed rewards of the old day are lost (doc, Boss Fragments: entries "reset at a fixed time and would not stack").

## Daily quests

Six short tasks, each worth 20 activity points (120 possible). Activity chests at 20 / 40 / 60 / 80 / 100 points.
Completing 100 points counts as one `dailyCompleted`.

| Task | Target |
|---|---|
| Claim idle rewards | 2 |
| Win battles | 3 |
| Gain hero levels | 3 |
| Collect the Dawnhollow storage | 1 |
| Enhance gear | 1 |
| Summon a hero | 1 |

Chests: 2,000 Gold · 1,500 Hero EXP · 50 Gems · 30 Gear materials · 1 Summon Scroll.

## Weekly quests

| Task | Target | Points |
|---|---|---|
| Finish all daily quests on 5 days | 5 `dailyCompleted` | 40 |
| Win battles | 30 | 20 |
| Claim idle rewards | 10 | 20 |
| Summon heroes | 10 | 20 |

Chests at 40 / 70 / 100 points: 200 Gems · 3 Summon Scrolls · 300 Gems + 5 Summon Scrolls.

## Main and Village quests

One ordered chain; only the current quest is shown, so the player always has exactly one main goal. Conditions are
checked against the state (not counters), so goals reached earlier complete at once:

- `stageCleared` ≥ N, `buildingLevel` (building, N), `heroLevel` (any hero ≥ N), `heroesOwned` ≥ N,
  `fullyEquipped` (heroes wearing all four gear slots) ≥ N.

The chain walks through the systems in the order they unlock: first stages → hero levels → Summoning Gate → more
heroes → Healer's Tent → Survivor Camp upgrades → Broken Forge → full gear → chapter boss → chapter clear; after
that one quest per 10 stages up to the end of the campaign.

## Rules

- `ClaimQuest(kind, id)`: daily/weekly task reached its target, not claimed this period → grants its points.
- `ClaimQuestChest(kind, points)`: enough points this period, chest not claimed → grants the chest.
- `ClaimMainQuest()`: current main quest's condition holds → grants rewards, moves to the next.

## UI

A quest screen reachable from the home screen with three tabs (Main, Daily, Weekly): the current main quest at the
top with its reward, a list of tasks with progress bars and "Abholen" buttons, the activity bar with chests, and the
time to the next reset. A badge on the home button shows claimable rewards.
