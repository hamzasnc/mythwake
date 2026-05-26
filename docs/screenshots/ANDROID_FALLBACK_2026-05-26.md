# Android Fallback Pass 2026-05-26

## Target Status

- Real Android target: blocked.
- `adb`: available through Unity embedded SDK at `C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`.
- `adb devices -l`: no emulator or physical device listed.
- `adb get-state`: `error: no devices/emulators found`.
- Emulator binary: not found in Unity embedded SDK or `%LOCALAPPDATA%\Android\Sdk\emulator`.
- Android version, launch time, logcat, real touch, real safe-area, and FPS: unavailable until a device/emulator is connected.

## Build

- Prototype: `0.2.138`.
- APK command: `scripts\build-android.cmd -OutputPath Builds\Android\Mythwake-0.2.138.apk`.
- APK artifact: `Builds\Android\Mythwake-0.2.138.apk`.
- Artifact size: `164,143,730` bytes.
- Unity cached build report time: about `00:01:30`.
- Build artifacts are ignored by git under `Builds/`.

## Fallback Screenshots

Fallback command:

```powershell
scripts\capture-portrait-screenshots.cmd -OutputDirectory Builds\Android\portrait-screenshots
```

Captured ignored local artifacts:

- `01-home.png`
- `02-home-stage-detail.png`
- `03-home-patrol-info.png`
- `04-village.png`
- `05-fast-rewards.png`
- `06-hero-detail.png`
- `07-gear.png`
- `08-summon.png`
- `09-summon-result.png`
- `10-fight-formation.png`
- `11-fight-visible.png`

## Observations

- Home, Village/Fast Rewards, Hero Detail, Summon, Formation, and visible Fight remain readable in the 1080x1920 editor-batch fallback.
- Gear was the roughest fallback screen before this pass because the oversized parchment left a large empty block and weak visual hierarchy.
- Gear now uses compact dark Training and Accessory cards, readable Slot/Rarity labels, stronger text contrast, and cohesive brown navigation/action buttons.
- True device-only items remain open: notch/status-bar safe area, Android gesture navigation, touch feel, launch time, logcat, and FPS/stutter checks.
- Later the same day, MuMuPlayer became available as a real Android emulator target; see `docs/screenshots/android/2026-05-26-mumu/README.md` for the install/start/screenshot/logcat pass.
