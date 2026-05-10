# Unity Test Stand

This file describes the current practical test target for Mythwake before real character assets, proper UI art, monetization, or public testing.

## Current Test Target

The current internal test stand should prove that the core loop works with the Go backend and PostgreSQL:

- Launch Unity in Editor or Android emulator.
- Start the local backend with PostgreSQL enabled.
- Use the Shop tab Backend panel.
- Press `Ping` to confirm backend, PostgreSQL, catalog, cache, lock, and hot-player status.
- Press `Server` to enter Server Mode.
- Press `Smoke` to run a compact server-backed sequence across Campaign, Dungeons, Hero Level, Weapon Level, Summon, AFK, and Flush.
- Run Campaign, Gold Dungeon, Essence Dungeon, Gear Dungeon, Summon, Hero upgrade, Equipment upgrade, Accessory equip/level/fuse, Daily Mission claim, Mission Track claim, AFK claim, Backend Reset, and app restart checks.
- Inspect PostgreSQL in Navicat after actions.
- Confirm state survives backend restart and Unity restart.

## Server Mode Rules

Server Mode should behave like the first real mobile-client path:

- Server Mode preference persists across app/editor restarts.
- Server Mode restores through `/client/bootstrap`.
- Gameplay actions use authenticated backend endpoints.
- Gameplay actions send idempotency keys and known player state revisions.
- Local debug grants are blocked while Server Mode is active.
- Local reset is blocked while Server Mode is active.
- Backend Reset is the allowed reset path for the active dev player.
- Backend Smoke is the allowed one-click flow for broad local server checks.
- Gameplay buttons are disabled while a backend request is in flight.
- Auto Attack stays local-only and paused in Server Mode until server-side auto/AFK behavior is designed.

## Smoke Test Checklist

Use this whenever a build feels "ready enough" for a bigger test pass:

- `scripts/start-backend.cmd` starts the API.
- `scripts/check-backend.cmd` returns healthy backend status.
- `scripts/check-postgres-e2e.cmd` passes.
- Unity `Ping` shows `DB connected`, expected catalog source, cache counters, lock store, and version.
- Unity `Server` loads a player snapshot and definitions.
- Unity `Smoke` finishes with `Server smoke complete` or shows the first transport failure.
- Campaign fight updates stage or returns a combat loss without breaking UI.
- Gold/Essence/Gear dungeons update floors or return a combat loss without breaking UI.
- Gear Dungeon can drop an accessory copy.
- Accessory equip, level, and fuse update UI from the server snapshot.
- Hero level and starter equipment level update team stats.
- Summon updates shards, summon count, and daily mission progress.
- Daily and Mission Track claims update claim state and currencies.
- AFK claim grants Gold and Myth Essence when enough server time has passed.
- Backend Reset returns a clean server player.
- Closing and reopening Unity keeps Server Mode and reloads from the backend.
- Restarting the API does not lose accepted actions.

## Useful Next Batches

Do these before asking for real assets or character model input:

1. Build a cleaner Unity test surface for server-backed progression, with fewer prototype debug labels and clearer action feedback.
2. Add a compact player/account display that shows player ID, revision, server mode, definition hash, and last request result.
3. Add better client-side error presentation for `player_busy`, stale revision, auth failure, rate limit, backend offline, and validation errors.
4. Add server-side integration coverage for AFK timing edge cases, stale revision conflicts, concurrent actions, and reset/session boundaries.
5. Start replacing placeholder UI with a real mobile layout while keeping the same backend action flow.

## Input Needed Later

We do not need final assets yet. We should ask for user input when one of these becomes the blocker:

- Visual direction for the first real UI pass.
- Free asset/model pack choices for the first 5 starter heroes.
- Character names, factions, and basic fantasy theme direction.
- Whether combat should be fully visualized or mostly result-card driven for the first playable build.
- Final currency names and monetization boundaries before store-facing builds.
- Login policy details for email, Google, and Apple before account linking leaves dev mode.
