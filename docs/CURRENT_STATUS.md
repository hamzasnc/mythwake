# Mythwake Current Status

Last updated: 2026-05-26

## Where We Are

- Current branch: `codex/batch-1-stabilize-prototype`.
- Unity client code is at Prototype `0.2.138`, save version `2`.
- Backend API default version is `0.2.58`.
- Backend core tests for balance, player, and HTTP routes are green.
- Server-authoritative core is already broad: guest auth, sessions, idempotent gameplay actions, PostgreSQL state, definition snapshots, AFK, daily progress, combat results, dungeons, summons, gear, and village building state.
- Client has moved beyond the older roadmap notes: Dungeons have their own map screen, Village has a scrollable map with 12 build plots, building art is imported, and Paladin/Ravik art plus combat presentation hooks exist.
- Local Fast Rewards already stores continuous AFK time up to 24h. The backend AFK definition has now been aligned to the same 24h cap.

## What Was Behind The Notes

- `README.md` and `docs/NEXT_CHAT_CONTEXT.md` were refreshed earlier for the Home idle combat/Village/Dungeons/Paladin state; the latest Home idle map layout is now tracked here and in `docs/NEXT_CHAT_CONTEXT.md`.
- Parts of `docs/ROADMAP.md` still describe older batch goals and can be cleaned up later.
- The note "split Dungeons into a real screen" is now first-pass done.
- The note "make Fast Rewards real enough for testing" is now closer: local accumulation, 24h cap, Village rate bonuses, Server Mode/backend-authoritative popup copy, server-side AFK Village bonus claims, and editor-validator coverage for local/server Village bonus lines are in place. A reproducible Android APK build now succeeds locally, but real install/logcat/touch/performance checks are still open because no device/emulator is attached on this machine.
- The next account-system gap is now explicit: testers need durable accounts so they do not restart from zero every pass. The planned path is Email + Password first, then Google Login through Play Store / Google Play Services later.

## Started This Pass

- Converted the built-village-plot panel from a debug demolish menu into a building detail panel.
- Added a Village building upgrade button in the Unity client.
- Wired the button to local Myth Essence spending and to the existing backend `/village/upgrade` action in Server Mode.
- Added an editor validation entry point for the Village UI so map, build panel, building detail, upgrade, demolish, close controls, bonus detail categories, and max-level upgrade lockout can be checked in Unity.
- Added visible placeholder Village bonuses; local mode applies small Team ATK/HP or Fast Rewards rate boosts from built building type and level.
- Village building detail now shows both the current bonus and either the next upgrade's bonus or the max bonus, with validator coverage for the extra detail line.
- Village now shows a compact hint-line summary for local Team ATK/HP and Fast Rewards rate bonuses, or a Server Mode note that local Village bonuses are paused while backend state stays authoritative.
- Village building definitions now exist in backend/common/PostgreSQL shape and are exposed through `/definitions`; backend Village build/upgrade actions use the injected catalog for costs, IDs, max levels, and server-side Team ATK/HP bonuses.
- Village balance definitions now carry editable admin-facing fields for bonus labels, bonus curve, upgrade cost formula, and local/server compatibility. PostgreSQL migration `0029_village_balance_admin_fields.sql` adds those columns plus `debug.v_common_village_building_balance` for Navicat/debug inspection.
- The Unity local fallback now matches backend Village values: Team ATK +3/+4/+5 per level, Team HP +24/+28/+32 per level, Gold/s Fast Rewards +0.08/+0.11/+0.14 per level, and Essence/s Fast Rewards +0.05/+0.07/+0.09 per level.
- Server Mode keeps client-side local Village bonuses paused so server-authoritative team stats and AFK rewards are not double-counted locally.
- Backend AFK claims now apply catalog-driven Village `afk_gold_rate` and `afk_essence_rate` bonuses from built building definitions, using floor-per-claim additive rewards so the server owns the actual reward mutation.
- Polished the Fast Rewards popup so local mode shows stored time, rate, Village bonus, and ready rewards, while Server Mode shows backend min/cap/rate/ready estimate.
- Added an editor validation entry point for the Fast Rewards popup so local copy, 0s/capped 24h states, Server Mode fallback copy, redeem/claim labels, button state, text fit, and control bounds can be checked in Unity.
- Fast Rewards now shows the remaining time before the 24h storage cap, gives the popup more text room, and the Fast Rewards validator checks normal, empty, and capped cap-left states.
- Server Mode Fast Rewards now shows an explicit claim status and keeps the Claim button gated until the backend min claim time is reached and a backend session exists; the Fast Rewards validator checks waiting and ready server AFK states.
- Server Mode Fast Rewards now shows the server-snapshot Village bonus line and includes that rate in the ready estimate; the Fast Rewards validator checks that copy.
- Local Fast Rewards redeem now explicitly refreshes the open popup after claiming, and the Fast Rewards validator checks the claim/reset/button-disable flow.
- Fast Rewards now has a compact progress bar/status line: local mode shows stored-percent plus cap-left state, Server Mode shows synced timer progress plus wait/ready state, and the validator checks fill percent and progress text fit.
- Added Paladin to the local `Vanguard Oath` summon banner so the frontline banner actually features and rolls the Paladin.
- Added a Paladin integration editor validator that checks the client hero definition, local summon banners, formation/fight hook anchors, backend definition/migration anchors, EN/DE localization keys, runtime portrait, combat sheets, skeletal part textures, Paladin runtime rig part loading, and Formation/Fight runtime rig visibility.
- Added an editor validation entry point for the Summon UI so the Vanguard Oath banner, Paladin feature art, rates, carousel center card, Paladin result popup, Auto-Summon label, repeat costs, and x10/x300 gem-gated repeat states can be checked in Unity.
- Added an editor validation entry point for upgrade clutter so legacy Battle/Hero upgrade buttons stay hidden, Gear upgrade controls stay on Gear, and debug shortcuts stay in Shop/tools.
- Extended the upgrade clutter validator so the 8-slot Hero Detail gear layout also has non-overlap checks against the portrait, stats, resources, and action buttons.
- Added a `Mythwake/Validate Current Slice` editor validation entry point that runs Village UI, Dungeons UI, Fast Rewards UI, Summon UI, Upgrade Clutter, Home Idle Combat, Paladin integration, and Paladin Spine handoff checks in one pass.
- Added a first Home idle combat slice: the campaign map stays in the background, stage nodes still open a compact info preview, and a foreground patrol fight animates three formation heroes against current-stage monsters for small active local Gold/Myth Essence ticks without changing `enemyLevel`.
- Added `Mythwake/Validate Home Idle Combat` and included it in `Mythwake/Validate Current Slice`; it checks map art, clickable preview info, visible patrol units, one active reward tick, and that idle combat does not auto-clear campaign stages.
- Home campaign nodes now show a visible halo only on the actual current unlocked stage, and the Home idle validator checks that locked nodes do not inherit that marker.
- Home campaign nodes now also show a separate selected-stage halo for the tapped checkpoint, and the Home idle validator checks that selecting a locked future node does not move the true current-stage halo.
- Home campaign cleared nodes now show a small OK badge, and the Home idle validator checks that current and locked nodes do not inherit the cleared marker.
- Home campaign locked nodes now show a small LOCK badge, and the Home idle validator checks that cleared and current nodes do not inherit the locked marker.
- Home campaign current nodes now show a small ZIEL badge in addition to the current halo, and the Home idle validator checks that cleared and locked nodes do not inherit the target marker.
- Home campaign stage previews now tint the panel and show a non-intercepting state accent for cleared/current/locked selections, with Home idle validator coverage.
- Home campaign stage-detail popups now mirror that status treatment with a tinted panel, a non-intercepting side accent, and an OK/ZIEL/LOCK badge, with Home idle validator coverage for cleared/current/locked popups.
- Home campaign stage-detail action buttons now switch label and color by state: `Zur Formation` for the current target, `Erledigt` for cleared stages, and `Gesperrt` for locked stages, with Home idle validator coverage.
- Home campaign stage-detail reward labels and the connected main-map-to-idle transition now have stricter fit/layout coverage so reward copy, Battle button coverage, and the lower idle strip stay inside the generated Home root.
- Home campaign boss nodes now show a visible Boss badge, and the Home idle validator checks boss and non-boss badge state.
- Home campaign non-boss milestone nodes now show a visible Bonus badge while boss milestones keep the Boss badge, with Home idle validator coverage.
- Home campaign stage previews now include Boss/Bonus/Normal tags plus compact special-reward hints, with Home idle validator coverage for preview copy and fit.
- Home campaign stage-detail popups now mirror those Boss/Bonus/Normal tags and special-reward hints, with Home idle validator coverage for detail copy and fit.
- Home campaign stage-detail reward rows now distinguish normal next-bonus hints, Bonus Gems/Pass XP, and Boss Gems/Boss XP, with validator coverage for all reward labels fitting.
- Home campaign path segments now color reached paths brighter and keep future locked paths dim, with Home idle validator coverage that future-path selection does not fake progress.
- Moved Home idle patrol combat below the map so it no longer covers checkpoint interaction, switched the campaign map to `area_map_scorched_plains`, enlarged it into a vertical scroll viewport, and anchored stage checkpoints to the scrollable map content.
- Lowered the Home idle patrol farther into the bottom free lane below the Battle button and extended the visible campaign-map viewport downward so the current-stage map occupies more of the screen.
- Expanded the Home campaign map viewport to fill the full marked play area behind side controls and the Battle button, leaving only the idle patrol in the lower lane.
- Pulled the Home campaign map up to the top edge under the resource bar and enlarged the lower idle patrol heroes and monsters so the fight reads better.
- Imported the remaining `area_map_*` region images into Runtime resources so Home progress maps can swap by stage region.
- Reworked the Home idle combat lane so its mini-map background connects directly to the main campaign map, spans the same width, and continues downward behind the fighting heroes/monsters.
- Updated `Mythwake/Validate Home Idle Combat` so it guards the connected Home map layout, the idle mini-map texture, the Battle button coverage, active reward ticks, and no automatic stage clear.
- Extended `Mythwake/Validate Home Idle Combat` so it also checks progress-map region texture sync across the main campaign map, stage-detail preview, and lower idle mini-map, plus reward progress fill behavior and Server Mode local-reward blocking.
- Home idle combat now crops the lower mini-map background by current stage progress instead of using one static slice, and the Home idle validator checks main/detail/idle map texture and UV sync.
- Home idle Server Mode now clears stale local loot popups and shows an empty reward progress bar while rewards are server-side; the Home idle validator guards the progress bar, popup clearing, and Server Mode Patrol Info copy.
- Home idle local reward summaries now show both the last Gold and last Essence tick; the Home idle validator checks the local tick summary after a reward fires.
- Home idle local reward summaries now split the last and next tick lines into a taller label area so the lower patrol reward copy remains readable; the Home idle validator guards the line break and fit.
- Home idle patrol now keeps the middle hero/enemy lane above the reward strip, and the Home idle validator guards mobile touch target size, reward shelf fit, unit/reward separation, and loot-popup separation from the reward strip.
- Home idle Patrol Info now shows last reward, next reward countdown, and tick cadence details after tapping the patrol fight area; the Home idle validator checks the popup before and after a local reward tick.
- Added a short floating Home idle loot popup for local active Gold/Myth Essence ticks, with validator coverage that confirms the popup appears and fits.
- Added a tappable Home patrol info popup on the idle combat area so players can inspect the current stage, enemy, active tick rewards, and the no-auto-clear rule; the Home idle validator now clicks and closes it.
- Added a Home campaign checkpoint detail popup so tapping a stage node now opens a larger AFK-style panel with a map preview, enemy formation, completion reward row, and Battle/Close controls; the Home idle validator checks and closes it.
- Fixed local campaign clear rewards so winning a stage grants the displayed Myth Essence reward and returns it in the action result payload; the Home idle validator now forces a won clear and checks the currency increase.
- Guarded the stage-detail Battle action so only the current unlocked checkpoint can start Formation, and extended the Home idle validator to click the current checkpoint through the detail popup into the Formation screen.
- Extended the Home idle validator again so locked checkpoint detail Battle listeners stay guarded even under direct invocation, and local campaign clear action results must include a non-empty Myth Essence reward payload.
- Extended the Village validator to cover the scrollable map/content wiring, all 12 build plot buttons, loaded village map/building art, free-plot build panel close flow, built-plot detail close flow, and hidden build marks on built plots.
- Extended the Fast Rewards validator to cover Home popup exclusivity, close-button behavior, and disabled Server Mode fallback claims when no backend session is available.
- Extended the Home idle validator to cover popup exclusivity between Fast Rewards, Patrol Info, and checkpoint details.
- Extended the Summon validator to cover visible result-slot text fit/art, hidden unused result cards, Auto-Summon toggle mark state, and result close flow.
- Summon result popup buttons now register through the runtime result-popup setup path, keeping repeat pulls, close, and Auto-Summon checkbox refresh valid even when the popup already exists.
- Added a dedicated Dungeons UI validator and included it in Current Slice; it checks the world-map viewport, map art, pan/scroll handlers, zoom controls and clamps, all three dungeon markers, marker spacing/text/art fit, and Gold/Essence/Gear Formation entry plus back-navigation flows.
- Dungeons map zoom buttons now register their listeners through the runtime zoom-control setup path so the Dungeons validator can exercise zoom in/out and clamps reliably.
- Dungeons map markers now normalize and immediately refresh their title/progress/detail labels even when reusing existing scene text fields, and Gear dungeon details now explicitly label accessory drops.
- Dungeons run buttons now use idempotent runtime listener registration so Gold/Essence/Gear marker clicks keep their Formation routes and back-flow coverage after the map setup path runs.
- Polished the Hero Detail gear slice: it now exposes all 2 equipment tracks plus all 6 accessory slots, `Equip Gear` opens the selected gear list for equipment and accessories, and `Remove Gear` can unequip accessories locally or through the Server Mode backend action.
- Added validator coverage for the Hero Detail gear list popup so equipment slots open the inline list first and only the list's `Open Gear` row navigates to the Gear screen.
- Added contextual Hero Detail gear action labels so equipment slots show `Open Gear` while accessory slots keep `Equip Gear`.
- Routed Hero Detail action labels through localization keys so Level Up, Open/Equip Gear, and Remove Gear follow the active language.
- Extended the upgrade clutter validator to switch Hero Detail to German during validation and confirm the open gear, equip gear, and remove gear labels refresh live.
- Added Hero Detail action-label overflow checks so localized Level Up, Open/Equip Gear, and Remove Gear labels must fit inside their buttons.
- Added Hero Detail gear-list text overflow checks so localized list titles and option rows must fit in equipment/accessory popups.
- Added a Hero Detail gear-list state guard so equipment lists expose only summary/open rows while accessory lists restore every rarity row after switching slots.
- Empty Hero Detail accessory slots now stay visually empty until gear is equipped, even when bag copies exist; owned copies remain prioritized inside the accessory list, and the validator checks empty-slot labels/icons including after German language refresh.
- Hero Detail accessory option lists now put owned, equippable copies above empty rows and keep higher rarity first inside those groups, so empty slots stay clean while the picker still surfaces useful copies; the validator locks that ordering and the copy/tap-to-equip text.
- Hero Detail now renders the new armory background plus visible starter Weapon/Armor training icons and equipped accessory slot icon art, and the upgrade clutter validator checks that active art loads through normal and German-refresh paths, empty accessory slots stay blank, everything fits, and icons do not intercept slot input.
- Hero Detail armory art is opaque and non-intercepting, and accessory option rows now keep owned copies before empty candidate rows while preserving higher-rarity ordering inside each group.
- Hero Detail now labels starter Weapon/Armor slots and rows as training tracks instead of claiming they are equipped item instances; the validator guards both slot and row wording in the normal and German refresh paths.
- Hero Detail and Gear runtime equipment icons now use an Editor asset-path fallback when `Resources.Load` is stale, hide missing-texture RawImage placeholders instead of showing white blocks, and the validator rejects hidden/white-placeholder icon art.
- Hero Detail previous/next buttons now sit outside the gear-slot columns, and the upgrade clutter validator checks they do not overlap any of the 8 gear slots.
- Gear screen runtime showcase art now uses the new equipment icon set, and the upgrade clutter validator checks the hero, weapon, armor, headgear, gloves, boots, and accessory images for loaded textures, fit, and input passthrough.
- Gear opening now ensures the runtime showcase is created before validation refreshes it, and multi-line Gear action labels auto-size so localized controls do not overflow their buttons.
- Gear screen runtime showcase copy now names all visible equipment slots from the title area, and the upgrade clutter validator checks that the full label fits without overlapping the icon rows.
- Gear screen summary text and upgrade/accessory controls now have clearer vertical spacing below the showcase, and the upgrade clutter validator checks those controls do not overlap the showcase or each other.
- Gear screen accessory action copy is now localized for selected rarity, equip, level, empty, fuse, target tier, and floor labels, with German refresh coverage in the upgrade clutter validator.
- Gear screen accessory selection now shows the selected rarity and current copy count inline, uses selected-rarity localization keys, and has Upgrade Clutter coverage in English and German refresh paths.
- Gear screen selected-rarity and equip action copy now distinguish bag copies from equipped copies, with Upgrade Clutter validation covering the combined count in English and German refresh paths.
- Local Gear accessory equip, level, unequip, and fuse action results now use EN/DE localization keys; the Upgrade Clutter validator runs the German action flow and restores temporary inventory/equipped state afterwards.
- Gear screen accessory inventory copy counts now render as a compact two-line summary with auto-sized Gear text blocks, keeping the control stack readable in localized layouts.
- The prototype UI builder now creates the Gear equipment/accessory controls in the current localized runtime layout with localized equipment names, runtime Gear navigation uses compact arrow labels, and the upgrade clutter validator rejects stale builder placeholder copy.
- Added backend `accessory_unequip` support, including the action catalog entry, `/gear/accessories/unequip` route, state mutation, and Player/HTTP tests.
- Backend definition snapshots and PostgreSQL seeds now include the sixth `headgear`/Helm accessory slot plus R0-R4 headgear definitions, matching the Hero Detail and Gear UI.
- Added `scripts/check-unity-current-slice.cmd` / `.ps1` so the current slice validator can be run from PowerShell or CI-style local checks once this Unity project is not already open.
- Fixed the `scripts/check-unity-current-slice.cmd` wrapper so PowerShell validation failures now propagate as non-zero exit codes instead of being hidden by `endlocal`.
- Added `scripts/check-unity-csharp.cmd` / `.ps1` for reproducible Unity runtime/editor C# MSBuild checks through Unity's bundled .NET Framework references, and standardized the backend/PostgreSQL/start wrappers to propagate PowerShell exit codes.
- Added backend tests for service-level and HTTP-level Village upgrades.
- Added migration `0026_afk_reward_24h_cap.sql` so existing PostgreSQL dev databases pick up the 24h AFK cap.
- Refreshed `README.md` and `docs/NEXT_CHAT_CONTEXT.md` so the main handoff notes match the current pass.
- Fixed the Prototype Builder Gear selected-rarity default so it no longer references the stale selected fuse-tier localization key.
- Moved local Village building names, textures, build costs, max levels, upgrade scaling, stable IDs, and placeholder bonus values into client-side building definitions as a bridge toward backend-owned Village balance.
- Extended `Validate Village UI` so every 12x3 Village definition is checked for stable ID, build cost, max level, loaded texture, and expected bonus category.
- Moved the same Village definition shape into backend/static definitions, `/definitions`, PostgreSQL migration `0028_village_building_definitions.sql`, and the injected backend balance catalog.
- Extended the Village definition shape again with editable labels/curves/formulas/mode compatibility, exposed the fields through `/definitions`, and added the PostgreSQL debug view for balance/admin inspection.
- Backend Village build/upgrade actions now read cost, stable building ID, max level, and server-side Team ATK/HP bonus contribution through the catalog.
- Backend Village/AFK tests now cover valid build, invalid building option, insufficient Essence, upgrade cost, max-level lockout, demolish, snapshot state, and server-side AFK Gold/Essence rate bonuses from Village definitions.
- Local Campaign/Dungeon combat result bodies now use a server-style summary with Team HP, Enemy HP, Team ATK, Enemy DMG, damage dealt/taken, healing, crits, misses, and execute flags; Upgrade Clutter validates that summary shape.
- Home Next Goal now follows the early loop more explicitly: push Campaign when Power is ready, otherwise suggest Gear drops/equip, Weapon/Armor/accessory/Hero upgrades, affordable Village build/upgrade, Gear Dungeon drops, Summon shards, or concrete Gold/Essence/Power farm gaps.
- `docs/UNITY_TEST_STAND.md` and `docs/screenshots/ANDROID_FALLBACK_2026-05-26.md` now record the 2026-05-26 Mobile UX pass, the successful Android APK build helper pass, the portrait screenshot fallback pass, Gear polish, and the remaining real emulator/device install/performance blocker.
- Added `Mythwake/Validate Fight Formation UI` and included it in `Mythwake/Validate Current Slice`; it checks campaign Formation swap, auto-next, visible Fight controls, AUTO/x2 toggles, HP/mana skill cards, ultimate queueing, result popup Continue flow, and dungeon fight focus chrome hiding.
- Prototype `0.2.135` tightens the Android/mobile baseline: PlayerSettings now launch portrait at 1080x1920, disable landscape/upside-down autorotation, disable OS autorotation override, and stop Android rendering outside the safe area until runtime safe-area padding is added.
- Added `Mythwake/Validate Mobile UX` and included it in `Mythwake/Validate Current Slice`; it checks Android portrait/safe-area settings, portrait CanvasScaler setup, version label fit, top/bottom chrome bounds, bottom-nav and side-nav touch target sizes, and navigation into Home, Village, Dungeons, Heroes, Gear, Summon, Shop, and Battle.
- Android availability check on 2026-05-26 found Unity's embedded `adb`, Android SDK/NDK/OpenJDK, and a working Android APK batch build path, but no attached device and no available emulator executable. An earlier Unity batchmode attempt was blocked by an open editor, but later batchmode checks and screenshot capture run successfully.
- Prototype `0.2.136` fixes the Mobile UX Current Slice guard so it validates the real runtime `Prototype UI` canvas, not the old zero-scale legacy `Canvas` in the scene.
- `scripts/check-unity-current-slice.cmd` passes again after the Mobile UX validator fix. The pass covers Home, Village, Dungeons, Fast Rewards, Mobile UX, Summon, Upgrade Clutter, Home Idle Combat, Fight Formation, Paladin integration, and Paladin Spine handoff.
- Prototype `0.2.137` adds `scripts/build-android.cmd` / `.ps1` and `scripts/capture-portrait-screenshots.cmd` / `.ps1` for repeatable Android APK creation and editor-batch portrait screenshot fallback capture.
- Android Build Support, Unity embedded SDK, NDK, OpenJDK, and `adb` are present under `C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Data\PlaybackEngines\AndroidPlayer`.
- `scripts/build-android.cmd -OutputPath Builds\Android\Mythwake-0.2.137.apk` succeeds. The ignored local artifact is `Builds/Android/Mythwake-0.2.137.apk` at 164,140,446 bytes; Unity's cached build report logged about 00:01:35.
- Unity embedded `adb devices -l` still lists no attached emulator/physical device, and no `emulator.exe` was found in the Unity embedded SDK or `%LOCALAPPDATA%\Android\Sdk\emulator`.
- Because no Android target is attached, install/start/logcat/touch/safe-area/FPS checks are still blocked. The fallback screenshot pass captured 1080x1920 PNGs under ignored local artifact path `Builds/Android/portrait-screenshots/` for Home, Home stage detail, Home patrol info, Village, Fast Rewards, Hero Detail, Gear, Summon, Summon result, Formation, and visible Fight.
- The fallback screenshot pass found that Ravik/Paladin skeletal previews could render oversized on the first visible Formation/Fight frame. `ShowPreview` now applies the pose immediately, and the Formation/Fight rig scales were reduced for the portrait layout so those heroes no longer swallow the enemy preview or fight readability.
- Fallback visual observations before Prototype `0.2.138`: Home, Village/Fast Rewards, Hero Detail, Summon, Formation, and visible Fight were readable enough for the next device build, while Gear still looked rough because of an oversized empty parchment/dead-space block. Prototype `0.2.138` addresses that Gear issue in the editor-batch fallback.
- Latest client checks for Prototype `0.2.137`: `scripts/check-unity-csharp.cmd`, `scripts/check-unity-current-slice.cmd`, `scripts/build-android.cmd -OutputPath Builds\Android\Mythwake-0.2.137.apk`, `scripts/capture-portrait-screenshots.cmd -OutputDirectory Builds\Android\portrait-screenshots`, and `git diff --check` pass. `git diff --check` only reports LF-to-CRLF working-copy warnings for touched Markdown files, not whitespace errors.
- Backend checks were not rerun in this Android pass because no backend files changed. Earlier `go test ./...` and `scripts/check-backend.cmd -BaseUrl http://localhost:18081 -CheckUnauthorized` against a temporary no-DB API passed after the Village balance/admin field pass.
- Prototype `0.2.138` rechecks the Android target state: Unity embedded `adb` starts, but `adb devices -l` still lists no attached emulator/physical device and `adb get-state` returns `error: no devices/emulators found`. Android version, launch time, logcat, real touch, real safe-area, and FPS numbers therefore remain unavailable on this machine.
- `scripts/build-android.cmd -OutputPath Builds\Android\Mythwake-0.2.138.apk` succeeds. The ignored local artifact is `Builds/Android/Mythwake-0.2.138.apk` at 164,143,730 bytes; Unity's cached build report logged about 00:01:30.
- `scripts/capture-portrait-screenshots.cmd -OutputDirectory Builds\Android\portrait-screenshots` captured the fallback screen set again after Gear polish. The current set covers Home, Home stage detail, Home patrol info, Village, Fast Rewards, Hero Detail, Gear, Summon, Summon result, Formation, and visible Fight.
- Gear is no longer the roughest visual screen in the fallback pass: the old oversized empty parchment is hidden, Training and Accessory controls now sit in compact dark cards, Slot/Rarity navigation has readable labels, nav buttons use the same brown button style as other actions, and EN/DE layout/text-fit is covered by Upgrade Clutter validation.
- Latest client checks for Prototype `0.2.138`: `scripts/check-unity-csharp.cmd`, `scripts/check-unity-current-slice.cmd`, `scripts/build-android.cmd -OutputPath Builds\Android\Mythwake-0.2.138.apk`, `scripts/capture-portrait-screenshots.cmd -OutputDirectory Builds\Android\portrait-screenshots`, and `git diff --check` pass. `git diff --check` only reports LF-to-CRLF working-copy warnings for touched Markdown files, not whitespace errors. Backend checks were not required because no backend files changed in this Gear/Android pass.

## Next Small Steps

1. Attach an Android emulator/device, install `Builds/Android/Mythwake-0.2.138.apk` or rebuild with `scripts/build-android.cmd`, then capture real safe-area, gesture navigation, load time, logcat, screenshot, and performance checks on Home, Hero Detail/Gear List, Gear, Village/Fast Rewards, Summon, Formation, Fight, and Result.
2. Plan the durable tester account slice: Email + Password registration/login first, Google Login through Play Store / Google Play Services later.
3. Continue Village backend-readiness by deciding whether AFK-rate bonuses need caps/diminishing returns and whether the editable PostgreSQL rows should get a small admin UI or import/export workflow.
4. Continue the visible Hero/Gear polish pass behind `Mythwake/Validate Upgrade Clutter` once the real item/inventory direction is selected.
