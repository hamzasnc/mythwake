# MuMuPlayer Touch Alignment Pass - 2026-05-27

- Device: MuMuPlayer `emulator-5554`, Android `12`, model `SM_F946B`, `1080x1920`, `280 dpi`.
- APKs: `Builds/Android/Mythwake-0.2.141-mumu.apk` for the original touch-alignment pass, `Builds/Android/Mythwake-0.2.143-mumu.apk` for the launcher-start regression fix, `Builds/Android/Mythwake-0.2.144-mumu.apk` for the safe-viewport fix, `Builds/Android/Mythwake-0.2.145-mumu.apk` for the Android legacy UI input fix, and `Builds/Android/Mythwake-0.2.146-mumu.apk` for the MuMu inverted-mouse-Y correction.
- Cold launch: Android `am start -W` reported `TotalTime 795 ms`.
- Root cause: MuMu/Android system bars could reserve a top inset, and MuMu desktop mouse clicks reached Unity with an inverted Y coordinate path, so visible UI and button raycasts could drift apart.
- Fix path: Android fullscreen manifest/theme, explicit `MAIN`/`LAUNCHER` activity intent filter, native `MythwakeFullscreen` helper, bounded fullscreen reapply on launch/resume/focus, safe-viewport rendering, and a MuMu-corrected Android UI input module that flips desktop mouse Y before Unity button raycasts.
- Verification: `dumpsys window` reported `ITYPE_STATUS_BAR ... visible=false` with app bounds `1080x1920`. The 0.2.144 safe-viewport screenshot shows the top inset as a black non-rendered area instead of drawing Unity UI behind it.
- Tap checks: visible coordinates opened Village, Summon, and Battle Formation. The 0.2.143 APK was launched via the Android launcher intent and verified with a Summon tap. The 0.2.144 APK kept a high tap on Home and opened Summon only from the visible bottom-nav position. The 0.2.145 APK repeats the visible Summon tap after switching Android UI input to the legacy module for MuMu desktop mouse stability. The 0.2.146 APK keeps normal Android touch input working while desktop mouse Y is corrected before Unity UI raycasts.
- Logcat: no Mythwake/Unity `Exception`, `NullReference`, `FATAL`, `ANR`, or InputSystem errors were found in the pass. Remaining errors were MuMu/Android environment noise such as missing telephony service.

Final evidence screenshots:

- `home-0141-installed-full-viewport.png`
- `tap-village-0141.png`
- `tap-summon-0141.png`
- `tap-battle-0141.png`
- `home-0143-launcher-open.png`
- `tap-summon-0143.png`
- `home-0144-safe-viewport.png`
- `home-0144-after-high-tap.png`
- `tap-summon-0144-visible-position.png`
- `home-0145-legacy-input.png`
- `tap-summon-0145-visible-position.png`
- `home-0146-mumu-input.png`
- `tap-summon-0146-touch.png`
