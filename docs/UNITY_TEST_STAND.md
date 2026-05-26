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

Latest Android/device availability check:

- Project has an Android Build Profile at `Assets/Settings/Build Profiles/Android™.asset`.
- The repo now has `scripts/build-android.cmd` / `.ps1` for reproducible APK builds and `scripts/capture-portrait-screenshots.cmd` / `.ps1` for batch portrait screenshot fallback capture.
- `adb` is not available on PATH, but Unity's embedded Android SDK includes `C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`.
- Unity's embedded `adb devices -l` found no attached Android emulator or physical device.
- No emulator executable was found in the Unity embedded SDK or `%LOCALAPPDATA%\Android\Sdk\emulator\emulator.exe`.
- Unity Android Build Support, SDK, NDK, and OpenJDK are installed under the Unity editor path.
- `scripts/build-android.cmd -OutputPath Builds\Android\Mythwake-0.2.137.apk` succeeds. The ignored local artifact is `Builds/Android/Mythwake-0.2.137.apk`, 164,140,446 bytes. The cached Unity build report logged about 00:01:35.
- A real Android install/start/logcat/touch/performance pass remains blocked by missing device/emulator access.

Mobile UX issues addressed in Prototype `0.2.135`:

- Project PlayerSettings now default to portrait `1080x1920` instead of landscape `1920x1080`.
- Android landscape and upside-down autorotation are disabled for the tester baseline.
- OS autorotation override is disabled so the portrait test layout remains stable.
- Android render-outside-safe-area is disabled until runtime safe-area padding is implemented.
- Current Slice now includes `Mythwake/Validate Mobile UX`, covering portrait settings, CanvasScaler reference shape, version label fit, top/bottom chrome bounds, bottom-nav and side-nav touch target sizes, and navigation to Home, Village, Dungeons, Heroes, Gear, Summon, Shop, and Battle.

Current validation state for the mobile-portrait slice:

- `scripts/check-unity-csharp.cmd` passes.
- `git diff --check` passes with only LF-to-CRLF working-copy warnings for touched Markdown files.
- `scripts/check-unity-current-slice.cmd` passes after the Mobile UX validator was narrowed to the actual runtime `Prototype UI` canvas.
- `scripts/build-android.cmd -OutputPath Builds\Android\Mythwake-0.2.137.apk` passes.
- `scripts/capture-portrait-screenshots.cmd -OutputDirectory Builds\Android\portrait-screenshots` passes.
- Current Slice coverage includes Home map/idle combat touch targets, reward strip fit, unit/reward separation, popup exclusivity, Village scroll/build/detail flows, Dungeons map zoom/marker spacing, Fast Rewards copy/progress/close flows, Summon result slots and repeat buttons, Hero Detail/Gear spacing and localized text fit, Gear action labels, combat result summary shape, Fight/Formation controls and result flow, and Paladin formation/fight handoff checks.

Mobile UX issues addressed in this continuation:

- Added repeatable Android APK and portrait screenshot fallback helpers so the next tester build does not require manual Unity menu setup.
- Captured 1080x1920 fallback PNGs under ignored local artifact path `Builds/Android/portrait-screenshots/` for Home, Home stage detail, Home patrol info, Village, Fast Rewards, Hero Detail, Gear, Summon, Summon result, Formation, and visible Fight.
- Fixed Ravik/Paladin preview rigs so `ShowPreview` applies the first pose immediately, which makes batch screenshots and the first visible UI frame use the intended scale instead of oversized default rig transforms.
- Reduced Ravik/Paladin Formation/Fight rig scale for the portrait battle layout. The fallback screenshots show Formation and visible Fight are substantially clearer after the fix.
- Home idle patrol middle lane was moved above the reward strip and guarded by validation.
- Gear selected-rarity copy now distinguishes bag/equipped copies, and local Gear action result copy is localized.
- Combat result bodies now show server-like HP/ATK/enemy damage/result fields.
- Home Next Goal now auto-sizes and points through the early loop with Power and resource gaps.
- Fight/Formation now has a dedicated validator for campaign Formation swap, auto-next, visible Fight controls, AUTO/x2, HP/mana skill cards, ultimate queueing, result Continue flow, and dungeon focus chrome hiding.
- Mobile UX validation now checks the runtime portrait canvas used by Topbar/Bottom Nav instead of failing on the old zero-scale legacy `Canvas` that remains in the scene.

Not yet run in this pass:

- Emulator install or physical-device run.
- Real emulator/device screenshots for Home, Village/Fast Rewards, Hero Detail, Gear, Summon, and Fight. Editor-batch fallback screenshots exist, but they are not a substitute for real safe-area/touch/device rendering.
- Manual safe-area checks for notches, Android gesture navigation, and status/navigation bar cutouts.
- Device performance/load-time sampling on Home Map, Hero Detail, Gear, Village, Summon, and Fight.

Next Android pass should attach an emulator/device, install `Builds/Android/Mythwake-0.2.137.apk` or rerun `scripts/build-android.cmd`, then record screenshots, logcat, touch behavior, safe-area behavior, load time, and rough FPS/performance for Home, Hero Detail, Gear, Village/Fast Rewards, Summon, and Fight.

## Future Account Login Need

The current guest/dev flow is useful for local server testing, but internal testers need durable accounts so each build does not feel like starting from zero again.

- First durable login slice: Email + Password registration/login.
- Later platform login slice: Google Login through Play Store / Google Play Services.
- Google Login should wait until the simpler Email + Password path and backend account linking are stable.

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
