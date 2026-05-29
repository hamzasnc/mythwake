# Android Tester Build 0.1 Smoke - 2026-05-29

Prototype `0.2.163`, APK `Builds/Android/Mythwake-0.2.163-tester-build-0.1.apk`.

## Target

- MuMuPlayer `emulator-5554`
- Android 12
- Model `SM_F946B`
- Portrait `1080x1920`

## Build And Launch

- Built with `scripts/build-android.cmd -OutputPath Builds\Android\Mythwake-0.2.163-tester-build-0.1.apk`.
- Installed with Unity embedded `adb install -r`.
- Cold launch via `am start -W` succeeded:
  - `TotalTime 995 ms`
  - `WaitTime 999 ms`
- Filtered Logcat file: `logcat-filtered-0.2.163.txt`.
- No critical crash, ANR, `UnityException`, `NullReferenceException`, `MissingReferenceException`, or missing-asset line was found in the filtered log.
- Remaining notable noise is MuMu/Android environment noise, including one `EGL_BAD_ATTRIBUTE` renderer line seen in earlier MuMu passes too.

## Screenshots

- `01-home-0.2.163.png`: Home current-stage preview includes the clearer `Ziel:` route line.
- `02-formation-0.2.163.png`: Formation enemy power/stage area is readable and the runtime FPS overlay is hidden on this screen.
- `03-dungeons-0.2.163.png`: Dungeons shows the localized flow hint and clean default result copy.
- `04-hero-detail-0.2.163.png`: Hero Detail empty accessory slots stay visually empty; empty remove action reads `No item`.
- `05-hero-gear-list-0.2.163.png`: Hero Gear list shows `No copy` rows for unavailable rarity copies.
- `06-summon-0.2.163.png`: Summon main screen remains reachable/readable; this emulator save had `0` Gems, so no new result pull was made.
- `07-fight-result-0.2.163.png`: Fight starts from Formation and reaches Result; Continue is visible and tappable.
- `08-home-after-continue-0.2.163.png`: Result Continue returns to Home immediately and the next stage is visible with no result-text leak.

## Observations

- App start, install, Home, Dungeons, Heroes, Hero Detail, Gear list, Summon, Formation, Fight, Result, and Continue were smoke-checked.
- Formation no longer has the FPS overlay competing with top enemy power.
- Dungeons guidance is clearer for the early resource loop.
- Hero/Gear empty states are clearer, but the Gear path is still product-UI rough and should remain in tester questions.
- Physical REDMAGIC/notch/gesture safe-area behavior remains unverified in this pass.
