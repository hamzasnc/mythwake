# Android Mobile Polish Follow-up - 2026-05-28

APK:
- `Builds\Android\Mythwake-0.2.156-mumu.apk`

Target:
- MuMuPlayer as `emulator-5554`
- Android `12`, model `SM_F946B`
- Display `1080x1920`, `280 dpi`

Launch and runtime:
- Cold launch through `adb shell am start -W`: `TotalTime 949 ms`, `WaitTime 957 ms`
- Host stopwatch around the launch command: `1002 ms`
- Runtime FPS overlay observed around `FPS 29-30 | 33.3-33.7 ms`
- The FPS overlay is non-raycast and can now be toggled from Management -> Options.
- `dumpsys window` showed portrait `mBounds=Rect(0, 0 - 1080, 1920)`, `mAppBounds=Rect(0, 0 - 1080, 1920)`, `mRotation=ROTATION_0`, and hidden status bar.

Logcat:
- Filtered app Logcat found no app crash, ANR, Unity exception, `NullReference`, `MissingReference`, or missing-asset error.
- Remaining notable lines are emulator or Android environment noise: MuMu/Android graphics mapper property warnings, `EGL_BAD_ATTRIBUTE`, telephony service null, and a MuMu-side `10.0.2.2:32552` refused connection from a non-game process.

Touch checks:
- Visible-coordinate taps opened Management Options, toggled the FPS overlay off/on, opened Summon, completed a single pull, kept the result popup open when tapping behind it, closed the popup, entered Formation, completed a campaign fight, and returned Home through Result Continue.

Screenshots:
- `01-summon-result-centered-modal.png`
- `02-summon-result-blocker-check.png`
- `03-performance-overlay-toggle-on.png`
- `04-performance-overlay-toggle-off.png`
- `05-formation-label-polish.png`
- `06-result-popup.png`
- `07-result-continue-home.png`

Reference set:
- The full 14-screen MuMu pass remains under `docs/screenshots/android/2026-05-28-mobile-testbuild-pass/`.

UI fixes from this follow-up:
- Summon Result now uses a modal blocker that prevents taps from reaching the carousel/offer behind the popup.
- One-result Summon cards are centered instead of sitting in the left grid slot.
- Formation labels are kept above skeletal preview rigs, and Paladin's Formation preview scale is slightly smaller.
- Runtime FPS overlay is now an Options toggle instead of a permanently forced debug label.

Open:
- No physical Android phone was attached. Real notch/gesture safe-area behavior is still open.
- MuMu `gfxinfo` still does not expose useful Unity frame buckets, so the runtime FPS overlay remains the lightweight profiler signal.
