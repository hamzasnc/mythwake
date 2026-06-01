# Mythwake Tester Release Process

Last updated: 2026-06-01

This is the reproducible handoff path for small Android tester builds. It is meant for local APK tests now and for a later Google Play Internal Testing track once signing, Play Console setup, and policy text are ready.

## Current Android Build Identity

- App name: `Mythwake`
- Company name in Unity PlayerSettings: `xMiepsen`
- Android package name: `com.xmiepsen.mythwake`
- Prototype / Android Version Name: `0.2.174`
- Android Version Code: `2174`
- Local save version: `2`
- Backend default API version: `0.2.61`
- Main scene: `Assets/Scenes/SampleScene.unity`
- Launcher icon: `Assets/_Mythwake/Branding/Mythwake_icon_launcher.png`
- Orientation: portrait only, fullscreen, inside Android safe viewport for MuMu/input stability.

Important: builds before `0.2.170` may have used Unity's default package identity. Android treats a package-name change as a different app, so local PlayerPrefs from older APKs may not carry over. Email accounts can still recover server progress through Login when the PostgreSQL backend is available.

## Version Rule

The visible tester version starts in `Assets/_Mythwake/Scripts/IdlePrototypeController.cs`:

```csharp
public const string PrototypeVersion = "0.2.174";
```

For tester builds:

- Bump `PrototypeVersion` for every handed-off APK/AAB.
- Keep `ProjectSettings/ProjectSettings.asset` `bundleVersion` equal to `PrototypeVersion`.
- Derive Android Version Code as `major * 1000000 + minor * 1000 + patch`.
- Example: `0.2.174` -> `2174`.
- Keep `CurrentSaveVersion` unchanged unless the local save schema changes.
- Mention the backend version separately when backend contracts changed.

`Mythwake/Validate Mobile UX` checks that the Android Version Name and Version Code match this rule.

## Build Artifacts

Local build artifacts live under ignored workspace paths:

- APK: `Builds/Android/*.apk`
- AAB: `Builds/Android/*.aab`
- Build log: `Temp/android-build.log`
- Local screenshots/logcat captures: usually `Builds/Android/` or `docs/screenshots/android/<date>/`

Do not commit APK/AAB files unless a release policy explicitly changes. Keep release docs and screenshots that explain a pass; keep raw local artifacts ignored.

## Build Commands

APK for local MuMuPlayer, emulator, or USB install:

```powershell
.\scripts\build-android.cmd -OutputPath Builds\Android\Mythwake-0.2.174-hero-progression.apk
```

AAB for Play Console preparation:

```powershell
.\scripts\build-android.cmd -AppBundle -OutputPath Builds\Android\Mythwake-0.2.174-play-internal.aab
```

The same helper runs Unity batchmode through `AndroidBuildAutomation.BuildAndroidApk`. Passing `-AppBundle` switches the Unity build to Android App Bundle output. The Unity menu also exposes:

- `Mythwake/Build Android APK`
- `Mythwake/Build Android AAB`

## Install And Launch Locally

Use Unity's embedded Android tools or any available Android SDK `adb`:

```powershell
adb devices -l
adb install -r Builds\Android\Mythwake-0.2.174-hero-progression.apk
adb shell monkey -p com.xmiepsen.mythwake 1
```

For a Windows-hosted backend with MuMuPlayer, Android emulator, or USB device:

```powershell
.\scripts\start-backend.cmd
adb reverse tcp:8080 tcp:8080
```

The Android client uses `http://127.0.0.1:8080`, so `adb reverse` is the expected local bridge.

## Signing Status

Current local tester builds use Unity/Android default signing because:

- `androidUseCustomKeystore: 0`
- No release/upload keystore is stored in the repository.
- No keystore passwords are committed or documented.

Use this split:

- Debug/local APK: good for direct MuMuPlayer, emulator, and USB smoke tests.
- AAB/release candidate: buildable now for process testing, but Play Console Internal Testing should use a real upload key / Play App Signing setup before handing to external testers.

Do not add keystore files, aliases, or passwords to git. Store them outside the repo and configure them through local Unity settings or CI secrets when the Play Console path starts.

## Required Checks Before Handoff

Run these before sharing an APK/AAB:

```powershell
.\scripts\check-unity-csharp.cmd
.\scripts\check-unity-current-slice.cmd
git diff --check
```

Run backend checks if backend code or API contracts changed:

```powershell
cd backend
go test ./...
cd ..
.\scripts\check-postgres-e2e.cmd
```

Run an Android smoke whenever possible:

- App starts.
- Account Start renders.
- Continue works with an existing session/save.
- Email Register/Login works against PostgreSQL backend.
- Guest flow still enters play.
- Home opens.
- Fight starts and ends.
- Hero/Gear opens.
- Village/Fast Rewards opens.
- Summon opens.
- Restart keeps the intended save/session.
- Filtered Logcat has no Mythwake/Unity crash, ANR, `NullReference`, or missing-asset blocker.

## Latest Local Verification

Source candidate: Prototype `0.2.174` / Backend `0.2.61`.

- `go test ./...` in `backend`: passed after the Hero Awakening backend rule change.
- `scripts/check-unity-csharp.cmd`: passed, with existing serialized-field warnings only.
- `scripts/check-unity-current-slice.cmd`: passed, including Hero Progression validation for Lv. 100 cap, Awakening lockout, shard spend, and stat growth.
- APK built: `Builds/Android/Mythwake-0.2.174-hero-progression.apk`, `165,314,419` bytes.
- APK metadata: package `com.xmiepsen.mythwake`, versionCode `2174`, versionName `0.2.174`, label `Mythwake`, minSdk `25`, targetSdk `36`.
- MuMuPlayer `emulator-5554` installed/launched the APK on Android `12` at `1080x1920`; cold launch reported `TotalTime 750 ms` / `WaitTime 754 ms`.
- Filtered process Logcat found no Mythwake/Unity `FATAL EXCEPTION`, `ANR`, `NullReference`, missing-file, `libunity`, or generic exception blocker after launch.
- AAB packaging was not rerun in this Hero progression pass.

Latest fully packaged APK+AAB candidate remains Prototype `0.2.170`.

- `scripts/check-unity-csharp.cmd`: passed, with existing serialized-field warnings only.
- `scripts/check-unity-current-slice.cmd`: passed.
- `scripts/check-postgres-e2e.cmd`: passed for Email register/login/logout/restart persistence and Guest auth.
- APK built: `Builds/Android/Mythwake-0.2.170-tester-release.apk`, `164,818,571` bytes.
- AAB built: `Builds/Android/Mythwake-0.2.170-play-internal.aab`, `164,692,854` bytes.
- APK metadata: package `com.xmiepsen.mythwake`, versionCode `2170`, versionName `0.2.170`, label `Mythwake`, minSdk `25`, targetSdk `36`.
- MuMuPlayer installed/launched the APK and covered Startscreen, Continue, Guest fallback, Home, Formation/Fight/Result, Heroes, Village, Fast Rewards, Dungeons, Summon, restart/Continue, and filtered Logcat.
- Latest local screenshots live under ignored `Builds/Android/` files named `Mythwake-0.2.170-*.png`.

## Feedback Template

Ask testers to include:

- Build version / Prototype version.
- Android Version Code if visible in build notes.
- Device/emulator and Android version.
- Screen name.
- Local/Server mode.
- Session kind: Email Account, Guest, or none.
- Player ID from Account Start or Management -> Account.
- Steps to reproduce.
- What they expected.
- What happened.
- Screenshot or short video.
- Logcat snippet if the app crashed, froze, or lost account/save state.

## Play Internal Testing Prep

Official Play Console notes to keep in mind:

- Google Play testing tracks include Internal Testing for quick QA with up to 100 testers, and testers need a Google or Google Workspace account.
- New apps published on Google Play are expected to use Android App Bundles.
- Internal App Sharing can upload APK or AAB files for quick links, can use any signing key, and Google re-signs those artifacts for sharing; links expire after 60 days.

Sources:

- https://support.google.com/googleplay/android-developer/answer/9845334
- https://support.google.com/googleplay/android-developer/answer/9844679

Open Play Console requirements for Mythwake:

- Confirm final package name before the first Play upload; package names are effectively permanent once published.
- Create or configure Play Console app entry for `Mythwake`.
- Prepare square icon, feature graphic, screenshots, short/full descriptions, and content-rating material.
- Configure Play App Signing/upload key outside the repo.
- Build an AAB with a new Version Code for every track release.
- Prepare tester email list.
- Add release notes from `docs/TESTER_BUILD_NOTES.md`.
- Add privacy/account disclosure notes for Email, session, Player ID, and gameplay progress storage.
- Keep Google Login as a later Play Services integration task, not part of the current Email MVP.

## Technical Account/Privacy Notes

Current tester-account data:

- Email address for Email accounts.
- Salted password hash in PostgreSQL, never raw password.
- Bearer session token on device; token hash in PostgreSQL when DB is enabled.
- Backend `player_id`.
- Server player state, currencies, campaign/dungeon progress, heroes, gear, village, daily/AFK state.
- Local PlayerPrefs save for Local Mode.

Not final yet:

- Password reset.
- Email verification.
- Account deletion/export flow.
- Production privacy policy.
- Production-grade abuse, monitoring, and incident response.
- Google Play Services login.

These are product/technical notes only, not legal advice.
