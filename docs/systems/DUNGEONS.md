# Portal Chamber and Dungeons – concept

Source: design doc, "Building System" (Portal Chamber: dungeons, events, Shattered Realm access) and "Dungeon System"
(story, material, gear, Fragment, Relic and endless dungeons; special rules; limited tries linked to daily goals;
bosses end a dungeon).

## Portal Chamber

Rebuilt after stage 20 (end of chapter 1). Its level opens dungeon types:

| Level | Opens |
|---|---|
| 1 | Material Dungeon (Gold, Hero EXP) |
| 2 | Gear Dungeon (Gear materials, gear) |
| later | Fragment Dungeon, Relic Dungeon (with Fragments / Relics), Endless Dungeon |

## How a dungeon works

- **Difficulty levels 1–100.** Each level is one fight: the dungeon's boss with an escort. Clearing level N opens
  N+1. Enemy level = base + 2 per difficulty level.
- **Special rule per dungeon** (doc examples):
  - Material Dungeon: enemies regenerate over time (1.5 % max HP per second).
  - Gear Dungeon: healing is reduced by 50 %.
- **Attempts:** 3 per dungeon per day, reset at the daily reset (04:00 UTC), unused attempts do not stack (doc).
  Only a won fight uses an attempt.
- **Rewards:** every clear pays the level's reward; the first clear of a level pays a bigger one-time reward.
- **Sweep:** the highest cleared level can be cleared instantly for its normal reward (one attempt, no fight).
- Wins count for `battleWon`; dungeon clears add the stat `dungeonCleared` for future quests.

## Bosses

| Dungeon | Boss (doc) | Escort |
|---|---|---|
| Material Dungeon | Obsidian Vault Sentinel (guards a vault) | Fragment Construct, Riftborn Wisp |
| Gear Dungeon | Hollow Spire Warden | Hollowed Soldier, Hollowed Archer |

## UI

Dawnhollow → Portal Chamber card → **Dungeons** screen: one card per dungeon (boss art, special rule, attempts
left), difficulty selector (◀ level ▶), enemy vs team power, normal and first-clear rewards, "Kampf" and "Sweep".
