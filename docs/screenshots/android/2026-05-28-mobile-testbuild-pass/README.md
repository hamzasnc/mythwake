# Android Mobile Testbuild Pass - 2026-05-28

APK:
- `Builds\Android\Mythwake-0.2.155-mumu.apk`

Target:
- MuMuPlayer as `emulator-5554` / `127.0.0.1:16384`
- Android `12`, model `SM_F946B`
- Display `1080x1920`, `280 dpi`

Launch and runtime:
- Cold launch through `adb shell am start -W`: `TotalTime 781 ms`, `WaitTime 784 ms`
- Host stopwatch around the launch command: `809 ms`
- Runtime FPS overlay observed around `FPS 30 | 33.3 ms`
- The overlay is a small non-raycast debug label under the top bar and does not block touches.
- `dumpsys window` showed portrait `mBounds=Rect(0, 0 - 1080, 1920)`, `mAppBounds=Rect(0, 0 - 1080, 1920)`, `mRotation=ROTATION_0`, and hidden status bar.

Logcat:
- Filtered app Logcat found no app crash, ANR, Unity exception, `NullReference`, `MissingReference`, or missing-asset error.
- One MuMu renderer line remained: `EGL_BAD_ATTRIBUTE`; this appears to be emulator EGL noise, not a game failure.

Touch checks:
- Visible-coordinate ADB taps opened Heroes, Summon, Home, Hero Detail gear list, Summon Result, Formation, Fight, and Result Continue.
- Result Continue responded when tapping the visible button center and returned to Home immediately.

Screenshots:
- `01-home-idle-combat.png`
- `02-home-stage-detail.png`
- `03-patrol-info.png`
- `04-village.png`
- `05-village-detail.png`
- `06-fast-rewards.png`
- `07-hero-detail.png`
- `08-hero-detail-gear-list.png`
- `09-gear-screen.png`
- `10-summon.png`
- `11-summon-result.png`
- `12-formation.png`
- `13-fight.png`
- `14-result-popup.png`
- `result-continue-home.png`

Support captures:
- `debug-plus-state.png`
- `tap-check-heroes.png`
- `tap-check-home.png`
- `tap-check-summon.png`

UI fixes from this pass:
- Summon Result popup was compacted: smaller result cards, clearer repeat/close controls, moved auto-toggle, and validator coverage for mobile bounds/overlaps.
- Formation is less dense: smaller slot cards/art, clearer 3+4 grid, moved enemy preview, brighter auto-next checkbox, and slot overlap validation.
- Hero Detail gear columns moved inward, Previous/Next stays clear, and a non-blocking right-edge scrim softens the old right line while preserving clickable slots.
- Result Continue now disables/hides immediately before cleanup/navigation so the response feels instant.

Open:
- No physical Android phone was attached. Real notch/gesture safe-area behavior is still open.
- MuMu `gfxinfo` does not expose useful Unity frame buckets here, so the runtime FPS overlay is the current lightweight profiler signal.
- Recheck Summon Result x10/x300 and Formation density on a cleaner tester save with enough summon currency.
