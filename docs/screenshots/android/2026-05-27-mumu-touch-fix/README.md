# MuMuPlayer Touch Alignment Pass - 2026-05-27

- Device: MuMuPlayer `emulator-5554`, Android `12`, model `SM_F946B`, `1080x1920`, `280 dpi`.
- APKs: `Builds/Android/Mythwake-0.2.141-mumu.apk` for the original touch-alignment pass, then `Builds/Android/Mythwake-0.2.143-mumu.apk` for the launcher-start regression fix.
- Cold launch: Android `am start -W` reported `TotalTime 795 ms`.
- Root cause: MuMu/Android system bars could reserve a top inset, so visible UI and pointer coordinates could drift apart.
- Fix path: Android fullscreen manifest/theme, explicit `MAIN`/`LAUNCHER` activity intent filter, native `MythwakeFullscreen` helper, bounded fullscreen reapply on launch/resume/focus, and full viewport rendering via `androidRenderOutsideSafeArea`.
- Verification: `dumpsys window` reported `ITYPE_STATUS_BAR ... visible=false` with app bounds `1080x1920`.
- Tap checks: visible coordinates opened Village, Summon, and Battle Formation. The 0.2.143 APK was also launched via the Android launcher intent and verified with a Summon tap.
- Logcat: no Mythwake/Unity `Exception`, `NullReference`, `FATAL`, `ANR`, or InputSystem errors were found in the pass. Remaining errors were MuMu/Android environment noise such as missing telephony service.

Final evidence screenshots:

- `home-0141-installed-full-viewport.png`
- `tap-village-0141.png`
- `tap-summon-0141.png`
- `tap-battle-0141.png`
- `home-0143-launcher-open.png`
- `tap-summon-0143.png`
