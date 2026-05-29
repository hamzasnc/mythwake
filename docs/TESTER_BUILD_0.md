# Mythwake Tester Build 0

Last updated: 2026-05-29

Superseded for handoff by `docs/TESTER_BUILD_0_1.md` and Prototype `0.2.163`. Keep this file as the original Build 0 evidence and tester-flow baseline.

## Build

- APK: `Builds/Android/Mythwake-0.2.162-tester-build-0.apk`
- Prototype: `0.2.162`
- Save version: `2`
- Branch: `codex/batch-1-stabilize-prototype`
- Target: small internal Android tester run for 2-5 people.

## Technical Smoke

- Built with `scripts/build-android.cmd -OutputPath Builds\Android\Mythwake-0.2.162-tester-build-0.apk`.
- Installed and launched on MuMuPlayer `emulator-5554`, Android 12, `1080x1920`.
- Cold start reported `TotalTime 847 ms`, `WaitTime 849 ms` after reinstall.
- A fresh-save launch for Summon capture reported `TotalTime 950 ms`, `WaitTime 958 ms`.
- Runtime FPS overlay was visible around `29-30 FPS | 33.3-33.5 ms`.
- Filtered Logcat found no app crash, ANR, Unity exception, `NullReferenceException`, `MissingReferenceException`, or missing-asset error.
- Save/load was smoke-checked by clearing Campaign Stage 3, force-restarting the app, and confirming Home reloaded at Stage 4.
- Fixed during this pass: Campaign Result text no longer leaks onto the Dungeons screen after `Continue`.

## Screenshot Set

Screenshots are under `docs/screenshots/android/2026-05-28-tester-build-0/`.

- `01-home.png`
- `02-stage-detail.png`
- `03-formation.png`
- `04-fight.png`
- `05-result.png`
- `06-home-after-result.png`
- `07-hero-detail.png`
- `08-hero-detail.png`
- `09-gear.png`
- `10-dungeons.png`
- `11-home-reload-0.2.162.png`
- `12-dungeons-clean-0.2.162.png`
- `13-village-0.2.162.png`
- `14-fast-rewards-0.2.162.png`
- `15-summon-0.2.162.png`
- `16-summon-result-0.2.162.png`

## Tester Flow

Use a fresh local save unless the tester is explicitly checking save persistence.

1. Install the APK and start the app.
2. Stay on Home for a moment. Read the current stage, power, resources, and FPS overlay.
3. Tap the current campaign node and read Stage Detail.
4. Tap `Zur Formation` / `Battle` and inspect Formation.
5. Check whether deployed heroes are visible, slots look tappable, presets `1-5` are understandable, and `Begin Battle` versus `Auto Battle` is clear.
6. Start one fight with `Begin Battle`.
7. Let the fight finish or tap `End Fight`, then press `Continue`.
8. Confirm Home updates to the next stage or gives a clear next action.
9. Open Heroes, inspect one Hero Detail, and try a level-up if resources are available.
10. Open Gear from Hero Detail and check whether equipment/accessory actions are understandable.
11. Open Dungeons and inspect Gold, Essence, and Gear dungeon markers.
12. Open Village, tap empty plots, and build or inspect a building when affordable.
13. Open Fast Rewards and check stored time, rate, ready rewards, and Redeem/Close.
14. Open Summon, inspect banner/cost/rates, do one starter pull, and close the result popup.
15. Start another campaign fight and write down where the loop feels unclear.

## Feedback Questions

- Hast du verstanden, was dein naechstes Ziel ist?
- Wo warst du verwirrt?
- Welche Buttons hast du nicht verstanden?
- War Text zu klein oder abgeschnitten?
- Hat ein Screen kaputt, leer oder zu voll gewirkt?
- Hat der Kampf Spass gemacht oder war er unklar?
- Waren Rewards und Upgrades nachvollziehbar?
- Hat `Continue` sofort reagiert?
- Ist die App haengen geblieben oder abgestuerzt?
- Hat dein Fortschritt nach App-Neustart weiter existiert?

## Known Issues

- Physical Android safe-area and notch behavior still needs one more pass on the REDMAGIC/real-phone class after this `0.2.162` build.
- Tester accounts do not exist yet. Testers currently use local device saves and may restart from zero after reinstall/clear-data.
- Email + Password login is the next durable tester-account slice; Google Login through Play Store / Google Play Services comes later.
- UI is still runtime-built and visually uneven. Hero/Gear/Village/Summon/Fight are testable, but not final.
- Formation is closer to the requested mockup, but the top power/FPS area can still feel dense.
- Fight uses placeholder/free hero and enemy visuals, and skill-card spacing is still tight on mobile.
- Gear is understandable enough for a test loop, but the path from Hero Detail to Gear still needs clearer product UI.
- Dungeons screen is functional but still visually rough and map labels can feel busy.
- FPS overlay is useful for internal testing but not a final player-facing profiler.
- Backend/server mode is not the target of Tester Build 0; local mode is the expected feedback path.

## Pass Criteria

Tester Build 0 is successful if 2-5 internal testers can play 20-30 minutes, understand the basic loop, and report confusion points without hitting a crash, dead button, broken save, or unreadable core screen.
