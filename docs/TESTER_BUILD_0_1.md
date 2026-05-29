# Mythwake Tester Build 0.1

Last updated: 2026-05-29

## Build

- APK: `Builds/Android/Mythwake-0.2.163-tester-build-0.1.apk`
- Prototype: `0.2.163`
- Save version: `2`
- Branch: `codex/batch-1-stabilize-prototype`
- Target: improved internal Android tester build after the first Build 0 smoke pass.

## Feedback Sources

- `docs/TESTER_BUILD_0.md`
- `docs/INTERNAL_TESTBUILD_CHECKLIST.md`
- `docs/CURRENT_STATUS.md`
- `docs/NEXT_CHAT_CONTEXT.md`
- Android screenshots under `docs/screenshots/android/2026-05-28-tester-build-0/`
- MuMuPlayer install/start/touch/save/logcat observations from Build 0

## Priority List

### 1. Blocker

- Campaign Result text could remain visible after `Continue` and leak onto Dungeons. Fixed in `0.2.162` and kept under Fight Formation validation.
- Build 0 did not show open hard blockers for app start, visible-coordinate touch, bottom navigation, save/reload, fight result, Summon, Gear, Village, missing assets, or crash/ANR/Unity exception in MuMuPlayer.
- Build 0.1 still needs a fresh APK install/start/logcat pass before handoff.

### 2. Schwer Verstaendlich

- Gear/Hero Detail selected rarity, bag/equipped copy counts, and disabled action states were too easy to misread. Build 0.1 clarifies `No copy`, `No item`, `Equipped`, and needed fuse-copy labels, and validates those states.
- Hero Detail `Remove Gear` on empty/non-removable slots looked actionable. Build 0.1 now shows the localized no-item state instead.
- Dungeons had no compact route hint after the Build 0 tester path. Build 0.1 adds localized guidance for selecting a dungeon, entering Formation, clearing a floor, and spending rewards.
- Home stage preview now says `Ziel:` before the current-stage route so fresh testers see it as the next objective, not just status text.

### 3. UI/UX Stoerend

- Formation top power plus runtime FPS overlay felt dense. Build 0.1 hides the runtime performance overlay on the Formation screen and validates that it does not cover Formation power copy.
- Dungeons old/default labels could reappear during language refresh or after returning from Result. Build 0.1 localizes the default result, future-card labels, and flow hint with validation.
- Result Continue responsiveness remains a watchpoint, but Build 0 already smoke-checked Continue back to Home and the previous leak fix.
- Fight skill-card spacing is still tight on small portrait screens; keep it under tester observation for Build 0.2.

### 4. Balance/Progression

- First-wall placement is not final. Do not do a numeric balance pass until 2-5 testers have played 20-30 minutes and reported where they stalled.
- Summon x10/x300 with high Gems was not rechecked in the first Build 0 pass. Keep one targeted high-currency Summon check for Build 0.2.
- Longer campaign/dungeon fights are still needed to judge `AUTO`, `x2`, mana readability, and auto-next pacing.

### 5. Spaeter

- Physical Android safe-area/notch/gesture behavior still needs a real REDMAGIC-class phone pass.
- Durable tester accounts are still missing. Email + Password is the first account slice; Google Play login comes later.
- Runtime UI is still generated and visually uneven. Larger art/design replacement should wait until the test loop is proven.
- Backend/server mode is not part of Build 0.1 tester flow; local mode remains the expected path.

## Fixed For Build 0.1

- Hidden the non-blocking FPS/performance overlay on Battle Formation so it no longer competes with enemy power and stage text.
- Clarified Gear accessory action labels for empty, equipped, level, and fuse states.
- Clarified Hero Detail remove-action disabled copy when the selected slot has no removable item.
- Added localized Dungeons flow/default/future labels and German language-refresh validation.
- Added a clearer Home current-stage objective prefix.
- Extended editor validators for Formation performance overlay, Dungeons language refresh, Home objective copy, and Gear/Hero disabled-state text fit.

## Tester Flow Delta

- Use the Build 0 tester flow, but pay special attention to Formation top text, Dungeons route copy, Hero Detail empty slots, Gear empty/equipped/fuse disabled states, and Result Continue.
- Testers should still report every dead button, clipped label, unreadable popup, or moment where they do not know where to get Gold, Essence, Gear, or Power.

## Android Smoke

- Built with `scripts/build-android.cmd -OutputPath Builds\Android\Mythwake-0.2.163-tester-build-0.1.apk`.
- Installed and launched on MuMuPlayer `emulator-5554`, Android 12, `1080x1920`.
- Cold launch reported `TotalTime 995 ms`, `WaitTime 999 ms`.
- Smoke-covered Home, Dungeons, Heroes, Hero Detail, Gear list, Summon, Formation, Fight, Result, and Result Continue.
- Screenshots and filtered Logcat are under `docs/screenshots/android/2026-05-29-tester-build-0-1/`.
- Filtered Logcat found no critical crash, ANR, `UnityException`, `NullReferenceException`, `MissingReferenceException`, or missing-asset line. Remaining notable line was MuMu/Android renderer/environment noise.

## Checks

- `scripts/check-unity-csharp.cmd`: passed.
- `scripts/check-unity-current-slice.cmd`: passed.
- Android APK/Emulator retest: passed on MuMuPlayer.
- `git diff --check`: passed with only LF-to-CRLF working-copy warnings for touched Markdown files.

## Open For Tester Build 0.2

- Real phone safe-area/notch/gesture pass.
- More structured Result Continue and auto-next stress pass across longer fights.
- High-currency Summon x10/x300 pass.
- First balance tuning after tester feedback identifies the first intentional wall.
- Durable tester account slice planning: Email + Password first, Google Play login later.
