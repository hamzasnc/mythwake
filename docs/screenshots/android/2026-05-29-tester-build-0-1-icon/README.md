# Android Tester Build 0.1 Icon Smoke

Date: 2026-05-29
Device: MuMuPlayer `emulator-5554`, Android 12, model `SM_F946B`, portrait `1080x1920`
APK: `Builds/Android/Mythwake-0.2.164-tester-build-0.1.apk`
Prototype: `0.2.164`
Save version: `2`

## Result

- APK build passed and installed with `adb install -r`.
- Cold launch after final install passed with `TotalTime 964 ms` / `WaitTime 967 ms`.
- Save/reload launch passed with `TotalTime 697 ms` / `WaitTime 703 ms`.
- Launcher shows the Mythwake blue/gold portal icon from `Assets/_Mythwake/Branding/Mythwake_icon_launcher.png`, not the Unity default launcher icon.
- Home, Formation, Fight Result, Result Continue, Hero Detail, Gear popup, Summon, Summon Result, and save/reload were checked with visible-coordinate taps.
- Summon Result was reached through the local tester/debug Gems path in Shop because the existing emulator save had insufficient Gems for a pull.
- App-focused filtered Logcat found no Mythwake fatal crash, ANR, Unity exception, `NullReference`, `libunity` crash, or missing-asset blocker.

## Files

- `01-launcher-icon-0.2.164.png` - launcher/home screen with Mythwake app icon.
- `02-home-0.2.164.png` - Home after final launch.
- `03-formation-0.2.164.png` - Formation readability and button access.
- `04-fight-result-0.2.164.png` - Fight result popup with Continue.
- `05-after-continue-0.2.164.png` - Home after Result Continue.
- `06-hero-detail-0.2.164.png` - Hero Detail slots.
- `07-gear-0.2.164.png` - Gear/accessory popup state.
- `08-summon-0.2.164.png` - Summon screen.
- `09-summon-attempt-0.2.164.png` - no-result state with insufficient Gems.
- `10-shop-debug-gems-0.2.164.png` - local tester/debug Gems path.
- `11-summon-result-0.2.164.png` - Summon Result popup.
- `12-reload-save-0.2.164.png` - app after force-stop/restart, showing saved progress.
- `logcat-filtered-0.2.164.txt` - app-focused filtered Logcat.

## Open

- Physical Android phone safe-area/notch/gesture retest is still open.
- Replacing/disabling the Unity startup splash logo caused a MuMu launch crash during the experiment and was rolled back. The launcher icon is fixed in this build; startup splash branding needs a separate Android-safe follow-up.
