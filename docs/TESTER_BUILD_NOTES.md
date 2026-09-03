# Mythwake Tester Build Notes

Last updated: 2026-09-03

## Current Candidate

- Tester build label: `0.4 tower-server internal-alpha candidate`
- Prototype / Android Version Name: `0.2.177`
- Android Version Code: `2177`
- Package: `com.xmiepsen.mythwake`
- Backend: `0.2.63`
- Save version: `2`
- APK target: `Builds/Android/Mythwake-0.2.177-tower-server.apk`
- AAB target: `Builds/Android/Mythwake-0.2.177-play-internal.aab`

## What Is New

- Android PlayerSettings now have an explicit package name and no longer use the Unity `DefaultCompany` placeholder.
- Android Version Name follows the visible Prototype version.
- Android Version Code is derived from the Prototype version and is validator-checked.
- The Android build helper can build either APK or AAB.
- Release process documentation now covers build commands, signing status, Play Internal Testing prep, tester feedback fields, and account/privacy MVP notes.
- Current release notes and known issues are centralized here for small tester handoffs.
- Hero progression now has a server-persistent Shard Rift loop on top of the Lv. 100 cap: Awakening uses Awakening Shards, Hero Shards upgrade Star levels 0-5, Hero Shard Chests can be opened from Hero Detail, and Shard Rift keeps per-enemy rewards even after defeat/manual end.
- Tower Dungeon now runs in Server Mode through `POST /dungeons/tower_dungeon/run?floor=N`; Tower progress, authoritative combat/rewards, Hero Shards, and idempotent action replay are account-bound. The Dungeons UI shows the server snapshot and the existing visible combat.

## What To Test

- App starts from a fresh install.
- Account Start appears and shows the current version.
- `Continue` works with existing local save or cached backend session.
- `Create Account` and `Login with Email` work against the PostgreSQL backend.
- `Play as Guest` still enters play, with local fallback if the backend is down.
- Home opens and navigation works.
- Fight starts and reaches Result.
- Result `Continue` returns to Home.
- Local Mode: Dungeons -> Shard Rift opens Formation, starts an endless fight, and keeps per-enemy rewards after End/failure.
- Server Mode: Dungeons -> Shard Rift runs through the backend and keeps Awakening Shards, Hero Shard Chests, best kills, and total kills after restart/login.
- Server Mode: Dungeons -> Tower Dungeon -> Formation starts the active unlocked floor, shows the backend combat result, advances `Highest Cleared`/`Highest Unlocked`, and rejects a second run of the already-cleared floor.
- Server Mode: repeat the same Tower request with the same `Idempotency-Key` and confirm the response is marked replay without granting rewards twice.
- Hero Detail can open Hero Shard Chests and spend hero-specific shards on Star Up in Local Mode and Server Mode.
- Hero Detail opens.
- Gear opens.
- Dungeons opens.
- Village opens.
- Fast Rewards opens.
- Summon opens.
- App restart keeps the intended local save or server player state.
- A hero at Lv. 100 shows `Awaken` instead of Level Up when enough shards exist.
- A hero below Lv. 100 explains that Lv. 100 is required before Awakening.

## Known Issues

- Google Login is not implemented yet.
- Password reset, email verification, account deletion, and account recovery are not implemented.
- Guest-to-Email linking is not implemented; testers should create Email accounts before meaningful Server Mode progress.
- Local-only saves can disappear after uninstall, app-data clear, or package-name changes.
- This package-name stabilization may make older local APK installs appear as a separate app; Email Login can recover server progress.
- Startup splash still uses Unity branding; custom splash was previously unstable in MuMu and remains a later safe task.
- UI is still runtime-built and visually uneven in places.
- Physical REDMAGIC/tall-phone safe-area testing remains a required follow-up.
- Current signing is local/debug-style; Play Internal Testing still needs upload-key/Play App Signing setup.
- No final privacy policy exists yet.
- This source candidate has not been packaged in the current environment because Unity reports no valid Editor license; do not treat the APK/AAB target names above as built artifacts.

## Do Not Report As New

- Missing Google Login button/functionality.
- Missing password reset or verification email.
- Missing payments/monetization.
- Placeholder art, placeholder popups, or non-final splash branding.
- Guest progress not automatically becoming Email progress.
- Local save loss after uninstall/clear-data when no Email account was used.

## Report Immediately

- Crash, ANR, or black screen on launch.
- Login succeeds but Player ID/progress changes unexpectedly.
- Email account progress disappears after restart or Logout/Login.
- Buttons do not react on visible tap targets.
- Fight hangs and never reaches Result.
- Result `Continue` does not return to Home.
- Massive UI overlaps on core screens.
- Missing launcher icon or missing main UI icons.
- Backend-down state causes endless loading instead of a readable error.

## Feedback Questions

- Was the version/build visible enough to report?
- Did the tester know whether they were in Local Mode or Server Mode?
- Did they know whether they were using Email Account, Guest, or no session?
- Did Login/Register/Continue/Logout behave as expected?
- Did progress survive restart?
- Which screen felt most confusing in the first 10 minutes?
- Which button label was unclear?
- What exact Player ID and build version were visible when the issue happened?

## Latest Verification

Source candidate: Prototype `0.2.177` / Backend `0.2.63`.

- `go test ./...` in `backend`: passed, including Tower balance, player progression/idempotency, and HTTP endpoint coverage.
- `go vet ./...` in `backend`: passed.
- Direct Runtime C# compilation against the installed Unity 6 runtime/module references: passed with 0 errors; the official Unity C# project validator remains blocked because `Assembly-CSharp.csproj` cannot be generated without a valid Editor license.
- `git diff --check`: passed for the completed working-tree validation runs.
- `scripts/check-unity-current-slice.ps1`: blocked before project validation by Unity license error 198 (`No valid Unity Editor license found`).
- PostgreSQL restart/re-login E2E passed against the available local PostgreSQL instance, including Tower definition loading, floor progression, cleared-floor rejection, restart recovery, and idempotent replay. Android APK/AAB packaging remains pending because Unity has no valid Editor license; Docker and `adb` are not on `PATH` here.

- `go test ./...` in `backend`: passed for the Hero Awakening backend rule/test update.
- `scripts/check-unity-csharp.cmd`: passed, with the existing serialized-field warnings only.
- `scripts/check-unity-current-slice.cmd`: passed, including Hero Progression validation and the Android package/version/icon/orientation validator.
- APK build passed: `Builds/Android/Mythwake-0.2.174-hero-progression.apk` (`165,314,419` bytes).
- APK metadata via `aapt`: package `com.xmiepsen.mythwake`, versionCode `2174`, versionName `0.2.174`, label `Mythwake`, minSdk `25`, targetSdk `36`.
- MuMuPlayer installed and cold-launched the APK on `emulator-5554` / Android `12` / `1080x1920`; `am start -W` reported `TotalTime 750 ms`, `WaitTime 754 ms`.
- Filtered process Logcat found no Mythwake/Unity crash, ANR, `NullReference`, missing-file, `libunity`, or generic exception blocker after launch.
- AAB build was not rerun in the Hero progression source pass.

Latest fully packaged APK+AAB candidate remains Prototype `0.2.170`:

- `scripts/check-postgres-e2e.cmd`: passed for Email register/login/logout/restart progress recovery and Guest auth.
- APK build passed: `Builds/Android/Mythwake-0.2.170-tester-release.apk` (`164,818,571` bytes).
- AAB build passed: `Builds/Android/Mythwake-0.2.170-play-internal.aab` (`164,692,854` bytes).
- APK metadata via `aapt`: package `com.xmiepsen.mythwake`, versionCode `2170`, versionName `0.2.170`, label `Mythwake`, minSdk `25`, targetSdk `36`.
- MuMuPlayer installed and launched package `com.xmiepsen.mythwake`; cold launch after install reported `TotalTime 1494 ms`, restart launch reported `TotalTime 983 ms`, and fresh Guest launch reported `TotalTime 1355 ms`.
- MuMu smoke covered Startscreen version, Continue, Guest fallback, Home, Formation, Fight Result, Heroes, Village, Fast Rewards, Dungeons, Summon, and restart/Continue returning to Stage 1-2 after local fight progress.
- Filtered Logcat found no Mythwake/Unity crash, ANR, `NullReference`, missing asset, or `libunity` blocker; remaining errors were MuMu/Android system noise.
- Local ignored screenshots are under `Builds/Android/` with names beginning `Mythwake-0.2.170-`.
