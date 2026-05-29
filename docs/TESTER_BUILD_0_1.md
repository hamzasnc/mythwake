# Mythwake Tester Build 0.1

Last updated: 2026-05-29

## Build

- APK: `Builds/Android/Mythwake-0.2.164-tester-build-0.1.apk`
- Prototype: `0.2.164`
- Save version: `2`
- Branch: `codex/batch-1-stabilize-prototype`
- Current tester scope: solo tester first.
- Later tester scope: keep this same structure so more internal testers can be added without losing build notes, known issues, and feedback context.

## What Changed Since Build 0

- Campaign Result text leaking onto Dungeons after `Continue` was fixed in `0.2.162` and remains validator-covered.
- Formation hides the runtime FPS overlay so enemy power/stage copy is readable.
- Gear/Hero empty/equipped/fuse disabled states are clearer (`No item`, `No copy`, `Equipped`, needed fuse copies).
- Dungeons default/future/flow labels are localized and validated.
- Home current-stage preview starts with `Ziel:` so the next route reads more clearly.
- Android app icon is now the Mythwake icon, not the Unity default icon.

## App Icon Decision

- Source assets checked:
  - `Mythwake_logo_transparent.png`: `1774x887`, aspect `2:1`, content fills about `90.7%`. This is a strong logo/splash/about asset, but too wide and text-heavy for small launcher icons.
  - `Mythwake_icon_transparent.png`: `1254x1254`, aspect `1:1`, content fills about `71.7%`. This stays recognizable at `48x48`, `72x72`, and `96x96`, and has enough transparent padding for Android launchers.
- Selected launcher icon: `Mythwake_icon_transparent.png`.
- Unity asset used by PlayerSettings: `Assets/_Mythwake/Branding/Mythwake_icon_launcher.png`.
- The logo image remains better suited for a future splash/about/brand panel, not for the phone launcher.
- Custom startup splash branding was tested and rolled back because replacing/disabling the Unity startup logo caused a MuMu launch crash. Build `0.2.164` only changes the launcher/app-switcher icon.

## Install And Start

1. Build or use `Builds/Android/Mythwake-0.2.164-tester-build-0.1.apk`.
2. Install on MuMuPlayer or Android device.
3. Confirm the launcher/app switcher shows the Mythwake blue/gold portal icon instead of Unity's standard icon.
4. Start the app and confirm the visible version text shows Prototype `0.2.164`, Save `2`.
5. Use local mode for this test pass. Backend/server mode is not the target for Build 0.1.

## Solo Test Checklist

- Start app and confirm Home opens cleanly.
- Check Home Idle Combat, current stage, `Ziel:` preview, resources, and version label.
- Open Stage Detail from the current Home map node.
- Enter Formation and confirm the top power/stage area is readable without FPS overlay overlap.
- Start a fight with `Begin Battle`.
- Let the fight end or tap `End Fight`.
- Press Result `Continue` and confirm Home updates immediately with no result-text leak.
- Open Hero Detail and inspect empty gear slots, equipped icons, and `No item` remove state.
- Open Gear or Hero Gear list and inspect copy counts, disabled labels, Equip/Level/Fuse clarity.
- Open Village and check empty plot/building detail readability.
- Open Fast Rewards and check stored time, rate, ready rewards, Claim/Redeem/Close.
- Open Summon, inspect banner/cost/rates, and if Gems are available do one Summon Result close flow.
- Close/restart the app and confirm progress/save reloaded.
- Note every dead button, clipped label, confusing reward, unclear next goal, or screen that feels too dense.

## Feedback Questions For Solo Test

- What is my next goal in one sentence?
- Do I know where to get Gold, Myth Essence, Gear, and Power?
- Did Formation, `Begin Battle`, `Auto Battle`, `End Fight`, `Continue`, and popup close buttons react immediately?
- Did any text become too small, overlap, or become unreadable?
- Did Gear/Hero explain why an action is disabled?
- Did Summon make it clear that shards add small stats now and duplicates matter later?
- Did the app icon look like Mythwake on the launcher?
- Did progress survive an app restart?

## Known Open Issues

- Physical Android safe-area/notch/gesture behavior still needs a real REDMAGIC-class phone pass.
- Tester accounts do not exist yet. Current tests use local device saves and can reset after reinstall/clear-data.
- Gear/Hero is clearer than Build 0, but still product-UI rough and should stay in feedback questions.
- Fight skill-card spacing and longer `AUTO`/`x2` readability still need longer fights.
- High-currency Summon x10/x300 has not been rechecked in this build.
- Startup splash branding still shows Unity branding; the launcher icon is fixed, but the first custom splash attempt was not stable enough for Tester Build 0.1.
- First intentional progression wall is not final; balance should wait for real tester feedback.
- Runtime UI is still generated and visually uneven.

## Later Multi-Tester Prep

Do not build the full login system in Build 0.1, but keep the requirement explicit:

- Durable tester accounts are needed so testers do not restart from zero every pass.
- Planned first account slice: Email + Password login/registration.
- Later account slice: Google Login through Play Store / Google Play Services.
- Multi-tester builds need:
  - Unique tester/player ID.
  - Build version visible in-game and included in feedback.
  - Known bugs list per build.
  - Simple reset or test-save flow.
  - A repeatable APK/install note or Play Internal Track style flow.
  - Feedback template that captures device, Android version, build version, save age, and reproduction steps.

## Priority List

### 1. Blocker

- No open hard blocker is known for app start, visible-coordinate touch, bottom navigation, save/reload, fight result, Summon, Gear, Village, missing assets, or crash/ANR/Unity exception in MuMuPlayer.
- Unity default launcher icon was still a professionalism blocker; Build `0.2.164` replaces it with the Mythwake icon.

### 2. Schwer Verstaendlich

- Gear/Hero disabled and copy states remain a tester focus.
- Dungeons route hints and Home `Ziel:` route should be checked by the solo tester.

### 3. UI/UX Stoerend

- Formation density is improved by hiding the FPS overlay there, but Formation is still dense.
- Summon Result can still feel tight on small portrait screens.

### 4. Balance/Progression

- Defer numeric balance changes until the solo pass records where the first wall feels right or wrong.

### 5. Spaeter

- Account/login, Google Play login, Play/Internal Track flow, physical safe-area matrix, richer profiler path.

## Android Smoke

- Prototype `0.2.164` APK built: `Builds/Android/Mythwake-0.2.164-tester-build-0.1.apk`.
- MuMuPlayer install passed with `adb install -r`.
- Cold launch after final install passed: `TotalTime 964 ms`, `WaitTime 967 ms`; save/reload launch later passed with `TotalTime 697 ms`, `WaitTime 703 ms`.
- Launcher shows the Mythwake blue/gold portal app icon, not Unity's default launcher icon.
- Visible-coordinate smoke covered Home, Formation, Fight Result, Result `Continue`, Hero Detail, Gear popup, Summon, Summon Result, and save/reload.
- Summon Result was checked by using the existing local debug Gems path in Shop to create enough Gems for a single pull.
- Filtered app-focused Logcat found no Mythwake fatal crash, ANR, Unity exception, `NullReference`, `libunity` crash, or missing-asset blocker. One Google Play Services background crash appeared in broader emulator logs and is treated as MuMu/system noise, not a Mythwake process crash.
- Screenshots and Logcat are under `docs/screenshots/android/2026-05-29-tester-build-0-1-icon/`.

## Checks

- `scripts/check-unity-csharp.cmd`: passed after the launcher-icon and validator changes.
- `scripts/check-unity-current-slice.cmd`: passed.
- Android APK/Emulator retest: passed in MuMuPlayer for the listed smoke path.
- `git diff --check`: passed; Git only printed LF-to-CRLF working-copy warnings for touched Markdown files.
