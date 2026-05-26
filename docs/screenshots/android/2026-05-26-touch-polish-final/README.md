# Android Touch Polish Pass - 2026-05-26

Environment:
- Target: MuMuPlayer `emulator-5554`
- Device model: `SM_F946B`
- Android: `12`
- Resolution/DPI: `1080x1920`, `280 dpi`
- APK: `Builds\Android\Mythwake-0.2.139-mumu.apk`
- Cold launch: Android `am start -W` `TotalTime 881 ms`; host stopwatch about `0.92s` until launch command returned, then an extra UI settle wait was used before screenshots.

Result:
- App installed, launched, stayed focused, and remained touchable in portrait.
- Runtime FPS overlay was visible and non-blocking. MuMu stayed around `29-30 FPS` / `33-35 ms` on the checked screens.
- Filtered Logcat found no Mythwake/Unity `Exception`, `NullReference`, `FATAL`, `ANR`, EventSystem/InputSystem error, or missing-asset error.
- Remaining Logcat noise was emulator-side renderer spam: `TimeStats: RenderEngineTimes are already at its maximum size[64]`.

Screenshots:
- `01-home-fps.png`: Home with runtime FPS overlay.
- `02-home-stage-preview.png`: Home campaign stage preview/selection state.
- `03-fast-rewards-popup.png`: Fast Rewards local popup.
- `04-fast-rewards-blocks-battle-tap.png`: Fast Rewards stayed open after a Battle tap, confirming the modal blocker prevents click-through.
- `05-formation.png`: Formation after Battle.
- `06-fight-result.png`: Result popup after Confirm/fight.
- `07-after-result-continue-home.png`: Continue returned immediately to Home.
- `08-village.png`: Village map.
- `18-village-main-building-detail.png`: Built Rathaus detail panel.
- `19-village-plot-detail.png`: Free plot build panel.
- `10-heroes.png`: Heroes screen.
- `11-hero-detail.png`: Hero Detail with gear slots.
- `12-hero-accessory-gear-list.png`: Accessory gear list from Hero Detail.
- `13-hero-weapon-open-gear-list.png`: Starter Weapon track list with Open Gear row.
- `14-runtime-gear-showcase.png`: Gear screen.
- `15-summon.png`: Summon screen.
- `16-summon-insufficient-currency.png`: Summon remained on-screen because this emulator save had only `20` summon currency and the single-pull cost was `35`.

Notes:
- Physical Android device/notch/gesture safe-area pass is still open; only MuMu was attached.
- Stage-detail full popup was not captured in this coordinate pass; the stage preview and Battle/Formation flow were verified instead.
