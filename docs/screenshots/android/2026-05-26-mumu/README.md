# MuMuPlayer Android Pass 2026-05-26

## Target

- Emulator: MuMuPlayer, detected as `emulator-5554`.
- Device model from Android: `SM-F946B` / manufacturer `Samsung`.
- Android version: `12`.
- Resolution / density: `1080x1920`, `280 dpi`.
- Display mode: `60 Hz`.
- ADB path: `C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`.
- Package: `com.DefaultCompany.Mythwake`.

## Build And Launch

- APK: `Builds\Android\Mythwake-0.2.138-mumu.apk`.
- APK size after final rebuild: `164,143,722` bytes.
- Unity batch build time measured by wrapper after the final metadata fix: about `60.2s`.
- Install: `adb -s emulator-5554 install -r -d Builds\Android\Mythwake-0.2.138-mumu.apk` succeeded.
- Launch: `am start -W -n com.DefaultCompany.Mythwake/com.unity3d.player.UnityPlayerGameActivity` succeeded.
- Cold start after final reinstall: Android `TotalTime 715 ms`, host-side stopwatch about `0.76s` until the activity command returned.
- Focus: `com.DefaultCompany.Mythwake/com.unity3d.player.UnityPlayerGameActivity`.
- Portrait: app bounds stayed at `1080x1920`; Unity surface started below the Android status bar.

## Screenshots

- `01-home.png`
- `02-home-stage-detail.png`
- `03-home-patrol-info.png`
- `04-village.png`
- `05-village-build-panel.png`
- `06-village-building-detail.png`
- `07-fast-rewards-popup.png`
- `08-hero-detail.png`
- `09-hero-detail-gear-list.png`
- `10-gear-screen.png`
- `11-summon.png`
- `12-summon-result.png`
- `13-formation.png`
- `14-fight.png`
- `15-result-popup.png`

## Logcat And Performance Notes

- No Mythwake/Unity `Exception`, `NullReference`, `FATAL`, `ANR`, or missing-asset errors were found in the filtered logcat pass. See `logcat-relevant.txt`.
- Logged warnings were emulator/Android-environment noise: `vold` media-directory attributes, Google Play Services background-start warnings, MuMu `opengl-gc` checks, and a telephony-service-null warning.
- `dumpsys gfxinfo` does not expose detailed Unity SurfaceView frame buckets here, so FPS is based on the MuMu 60 Hz display mode plus visual observation.
- Home map scrolling/taps, Village map, Gear, Summon, and Fight were responsive through ADB tap input.
- No obvious long screen-transition stalls were observed.
- Rough memory snapshot after the pass: `TOTAL PSS` about `396 MB`.

## UX Observations

- Home: topbar stays below the status bar, map/checkpoints and bottom navigation are readable, idle combat does not block checkpoint use.
- Stage detail and Patrol Info popups are readable on MuMu. Stage selection state remains clear.
- Village: map is readable, plots are tappable, build panel and building detail panel fit in portrait.
- Fast Rewards: popup is readable and touchable; local Village HP bonus does not incorrectly appear as an AFK-rate bonus.
- Hero Detail: slots and buttons are usable, but the right-side vertical guide/edge line is still visually rough.
- Gear: the new compact Training/Accessory card layout is much cleaner on a real Android target than the old parchment layout.
- Summon: main screen works; one-pull result popup is functional but still has a large dark lower area dominated by repeat controls.
- Formation: usable, though the slot grid and character art remain visually dense.
- Fight: starts and animates on MuMu; early stages are extremely short, so result popups can appear within about one second.
- Result: popup is readable. Continue advanced after repeated taps once the result state was settled; this should be watched on future device runs.

## Open Follow-Up

- Repeat on a physical Android phone for real notch/gesture safe-area behavior.
- Add a better runtime FPS overlay or profiler capture; MuMu `gfxinfo` is too thin for Unity frame timing.
- Polish Summon result spacing and Formation density in a later UI pass.
- Verify Server Mode over MuMu networking once the backend is running for the emulator target.
