# Android Summon / Formation / Fight Polish - 2026-05-28

Target: MuMuPlayer Android portrait pass for Prototype `0.2.157`.

APK:
- `Builds\Android\Mythwake-0.2.157-mumu.apk`
- Final install: success on `emulator-5554`
- Final cold launch: Android `am start -W` `TotalTime 882 ms`, `WaitTime 883 ms`; host stopwatch `940 ms`

Screenshots:
- `00-home-after-fresh-launch.png` - home after fresh launch
- `01-summon-main.png` - Summon main screen
- `02-summon-result-polished.png` - single-pull Summon Result with summary, repeat buttons, and disabled x10/x300 state
- `03-after-result-close-home.png` - home after closing Summon Result
- `04-formation-polished.png` - Formation layout after spacing/preview polish
- `05-formation-swap-selected.png` - Formation selected-slot swap state
- `06-fight-polished.png` - quick fight/result transition coverage
- `07-result-continue-returned.png` - Home after Result Continue
- `08-fight-active.png` - active Fight UI with skill cards and controls
- `09-fight-auto-x2-toggled.png` - follow-up Fight/Result capture after AUTO/x2 tap attempt; early fight ended quickly

Observations:
- Summon main, single Summon Result, result Close, Formation entry, Formation selected-slot swap, Confirm, Fight entry, and Result Continue all responded to visible-coordinate taps.
- Runtime FPS overlay was visible around `30 FPS | 33.3 ms`, matching the emulator cap.
- Filtered Logcat showed no app crash, ANR, Unity exception, `NullReference`, or missing-asset error. Remaining noise was MuMu/Android environment output plus one non-fatal Unity `APP_CMD_LOW_MEMORY` signal.
- Portrait bounds stayed at 1080x1920 in MuMuPlayer. Physical Android notch/gesture safe-area behavior was not available in this pass.

Follow-ups:
- Recheck x10/x300 repeat behavior with a high-gem clean tester save.
- Capture a longer fight where AUTO/x2 can remain visibly toggled before victory.
- Run the same pass on a physical Android device for notch/gesture safe-area behavior.
