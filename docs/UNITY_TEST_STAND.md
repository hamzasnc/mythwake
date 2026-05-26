# Unity Test Stand

This file describes the current practical test target for Mythwake before real character assets, proper UI art, monetization, or public testing.

## Current Test Target

The current internal test stand should prove that the core loop works with the Go backend and PostgreSQL:

- Launch Unity in Editor or Android emulator.
- Start the local backend with PostgreSQL enabled.
- Use the Shop tab Backend panel.
- Press `Ping` to confirm backend, PostgreSQL, catalog, cache, lock, and hot-player status.
- Press `Server` to enter Server Mode.
- Press `Smoke` to run a compact server-backed sequence across Campaign, Dungeons, Accessory equip/level/fuse candidates, Hero Level, Weapon Level, Summon, Daily Summon claim, Mission Track claim, AFK, and Flush.
- Run Campaign, Gold Dungeon, Essence Dungeon, Gear Dungeon, Summon, Hero upgrade, Equipment upgrade, Accessory equip/level/fuse, Daily Mission claim, Mission Track claim, AFK claim, Backend Reset, and app restart checks.
- Inspect PostgreSQL in Navicat after actions.
- Confirm state survives backend restart and Unity restart.

## Mobile UX Pass 2026-05-26

Batchmode validation is green for the current mobile-portrait slice:

- `scripts/check-unity-csharp.cmd` passes.
- `scripts/check-unity-current-slice.cmd` passes.
- `git diff --check` passes, with the usual LF/CRLF warnings on touched Markdown files.
- Current Slice coverage includes Home map/idle combat touch targets, reward strip fit, unit/reward separation, popup exclusivity, Village scroll/build/detail flows, Dungeons map zoom/marker spacing, Fast Rewards copy/progress/close flows, Summon result slots and repeat buttons, Hero Detail/Gear spacing and localized text fit, Gear action labels, combat result summary shape, and Paladin formation/fight handoff checks.

Mobile UX issues addressed in this continuation:

- Home idle patrol middle lane was moved above the reward strip and guarded by validation.
- Gear selected-rarity copy now distinguishes bag/equipped copies, and local Gear action result copy is localized.
- Combat result bodies now show server-like HP/ATK/enemy damage/result fields.
- Home Next Goal now auto-sizes and points through the early loop with Power and resource gaps.

Not yet run in this pass:

- A real Android APK/AAB build, emulator install, or physical-device run. The repo currently has an Android Build Profile, but no checked-in build/install helper script.
- Manual safe-area checks for notches, Android gesture navigation, and status/navigation bar cutouts.
- Device performance/load-time sampling on Home Map, Hero Detail, Gear, Village, Summon, and Fight.

Next Android pass should either add a reproducible Unity Android build script or run the Android Build Profile manually, then record emulator/device screenshots for Home, Hero Detail, Gear, Village, Summon, and Fight.

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
- Backend requests carry client request IDs, and structured server errors should be readable without opening server logs.
- Normal Fight/Dungeon spam should not show HTTP 429; gameplay spam is handled by request gating, idempotency, and player locks.
- Auto Attack stays local-only and paused in Server Mode until server-side auto/AFK behavior is designed.

## Smoke Test Checklist

Use this whenever a build feels "ready enough" for a bigger test pass:

- `scripts/start-backend.cmd` starts the API.
- `scripts/check-backend.cmd` returns healthy backend status.
- `scripts/check-postgres-e2e.cmd` passes.
- Unity `Ping` shows `DB connected`, expected catalog source, cache counters, lock store, and version.
- Unity `Server` loads a player snapshot and definitions.
- Unity `Smoke` finishes with `Server smoke complete`, shows rejected gameplay outcomes inline, or shows the first transport failure with request diagnostics.
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
2. Add a compact player/account display that shows player ID, revision, server mode, definition hash, and last request result outside the Shop tab.
3. Add server-side integration coverage for AFK timing edge cases, concurrent actions, and reset/session boundaries.
4. Start replacing placeholder UI with a real mobile layout while keeping the same backend action flow.

## Input Needed Later

We do not need final assets yet. We should ask for user input when one of these becomes the blocker:

- Visual direction for the first real UI pass.
- Free asset/model pack choices for the first 5 starter heroes.
- Character names, factions, and basic fantasy theme direction.
- Whether combat should be fully visualized or mostly result-card driven for the first playable build.
- Final currency names and monetization boundaries before store-facing builds.
- Login policy details for email, Google, and Apple before account linking leaves dev mode.
