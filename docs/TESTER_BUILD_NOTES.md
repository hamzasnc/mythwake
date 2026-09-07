# Mythwake Tester Build Notes

Last updated: 2026-09-07

## Current Candidate

- Tester build label: `internal alpha UI overlap fix`
- Prototype / Android Version Name: `0.2.178`
- Android Version Code: `2178`
- Package: `com.xmiepsen.mythwake`
- Backend: `0.2.63`
- Save version: `2`
- APK target: `Builds/Android/Mythwake-0.2.178-ui-fix.apk`
- AAB target: `Builds/Android/Mythwake-0.2.178-ui-fix.aab`

## What Is New

- Hero details replace the visible roster; equipment opens an opaque page with larger rows, readable details and a separate close button. Decorative, nonfunctional Story/Skills labels were removed from this page.
- Regression check: open a hero, tap each of the eight equipment slots, select/equip an available item, close equipment, switch hero, close details and switch main tabs. Check empty slots and long German labels on a narrow portrait screen. Existing account and economy behavior is unchanged.
- Android PlayerSettings now have an explicit package name and no longer use the Unity `DefaultCompany` placeholder.
- Android Version Name follows the visible Prototype version.
- Android Version Code is derived from the Prototype version and is validator-checked.
- The Android build helper can build either APK or AAB.
- Release process documentation now covers build commands, signing status, Play Internal Testing prep, tester feedback fields, and account/privacy MVP notes.
- Current release notes and known issues are centralized here for small tester handoffs.
- Hero progression now has a server-persistent Shard Rift loop on top of the Lv. 100 cap: Awakening uses Awakening Shards, Hero Shards upgrade Star levels 0-5, Hero Shard Chests can be opened from Hero Detail, and Shard Rift keeps per-enemy rewards even after defeat/manual end.
- Tower Server Mode is included: the active floor, rewards, defeats, duplicate requests, flush, restart, and logout/login all use the server snapshot/action ledger path. A manual client stop ends only the local visual playback; it must not duplicate or remove the server result.
- Shop and Bag are internal-alpha functional surfaces. Shop tabs must reveal their matching content; Bag quantity selection must clamp to owned Hero Shard Chests and show the server/local reward summary.

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
- Follow `docs/ALPHA_TEST_PLAN.md` for the current compact account-to-Tower route and failure cases.

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

- `0.2.178` UI fix: APK and AAB builds passed; package/version metadata is `com.xmiepsen.mythwake` / `0.2.178` / `2178`. Both signatures verified with the existing local signing certificate. Unity C# and Current Slice passed. Core-screen rendering passed, including hero/equipment captures at 540x864, 540x960 and 540x1170. Actual emulator touch testing remains a manual tester check.
- Build helper now ignores only the known Unity internal pipe-shutdown warning, matching the Current Slice helper; real build exceptions still fail the gate.

- Previous candidate `0.2.177` packaging passed on 2026-09-07 (superseded by the UI fix above):
  - APK `Builds/Android/Mythwake-0.2.177-internal-alpha.apk` (`203,617,797` bytes).
  - AAB `Builds/Android/Mythwake-0.2.177-internal-alpha.aab` (`203,500,756` bytes).
  - APK metadata: package `com.xmiepsen.mythwake`, versionCode `2177`, versionName `0.2.177`, label `Mythwake`, minSdk `25`, targetSdk `36`.
  - APK signature: verified with APK Signature Scheme v2; AAB JAR signature verified with the local/self-signed debug-style certificate. Production upload-key/Play App Signing is still an external release step.
  - Automated gates passed: `go test ./...`, `go vet ./...`, Unity C# validation, Current Slice validation, and whitespace validation.
  - Manual device install/EN-DE account/Tower smoke remains the explicit tester execution in `docs/ALPHA_TEST_PLAN.md`; no external Play Console action was performed.

- `go test ./...` in `backend`: passed for the Hero Awakening backend rule/test update.
- `scripts/check-unity-csharp.cmd`: passed, with the existing serialized-field warnings only.
- `scripts/check-unity-current-slice.cmd`: passed, including Hero Progression validation and the Android package/version/icon/orientation validator.
- APK build passed: `Builds/Android/Mythwake-0.2.174-hero-progression.apk` (`165,314,419` bytes).
- APK metadata via `aapt`: package `com.xmiepsen.mythwake`, versionCode `2174`, versionName `0.2.174`, label `Mythwake`, minSdk `25`, targetSdk `36`.
- MuMuPlayer installed and cold-launched the APK on `emulator-5554` / Android `12` / `1080x1920`; `am start -W` reported `TotalTime 750 ms`, `WaitTime 754 ms`.
- Filtered process Logcat found no Mythwake/Unity crash, ANR, `NullReference`, missing-file, `libunity`, or generic exception blocker after launch.
- AAB build was not rerun in the Hero progression source pass.

Historical previous APK+AAB verification (superseded by `0.2.177` above):

- `scripts/check-postgres-e2e.cmd`: passed for Email register/login/logout/restart progress recovery and Guest auth.
- APK build passed: `Builds/Android/Mythwake-0.2.170-tester-release.apk` (`164,818,571` bytes).
- AAB build passed: `Builds/Android/Mythwake-0.2.170-play-internal.aab` (`164,692,854` bytes).
- APK metadata via `aapt`: package `com.xmiepsen.mythwake`, versionCode `2170`, versionName `0.2.170`, label `Mythwake`, minSdk `25`, targetSdk `36`.
- MuMuPlayer installed and launched package `com.xmiepsen.mythwake`; cold launch after install reported `TotalTime 1494 ms`, restart launch reported `TotalTime 983 ms`, and fresh Guest launch reported `TotalTime 1355 ms`.
- MuMu smoke covered Startscreen version, Continue, Guest fallback, Home, Formation, Fight Result, Heroes, Village, Fast Rewards, Dungeons, Summon, and restart/Continue returning to Stage 1-2 after local fight progress.
- Filtered Logcat found no Mythwake/Unity crash, ANR, `NullReference`, missing asset, or `libunity` blocker; remaining errors were MuMu/Android system noise.
- Local ignored screenshots are under `Builds/Android/` with names beginning `Mythwake-0.2.170-`.
