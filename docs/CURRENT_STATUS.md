# Mythwake Current Status

Last updated: 2026-05-25

## Where We Are

- Current branch: `codex/batch-1-stabilize-prototype`.
- Unity client code is at Prototype `0.2.92`, save version `2`.
- Backend API default version is `0.2.56`.
- Backend core tests for balance, player, and HTTP routes are green.
- Server-authoritative core is already broad: guest auth, sessions, idempotent gameplay actions, PostgreSQL state, definition snapshots, AFK, daily progress, combat results, dungeons, summons, gear, and village building state.
- Client has moved beyond the older roadmap notes: Dungeons have their own map screen, Village has a scrollable map with 12 build plots, building art is imported, and Paladin/Ravik art plus combat presentation hooks exist.
- Local Fast Rewards already stores continuous AFK time up to 24h. The backend AFK definition has now been aligned to the same 24h cap.

## What Was Behind The Notes

- `README.md` and `docs/NEXT_CHAT_CONTEXT.md` were refreshed earlier for the Home idle combat/Village/Dungeons/Paladin state; the latest Home idle map layout is now tracked here and in `docs/NEXT_CHAT_CONTEXT.md`.
- Parts of `docs/ROADMAP.md` still describe older batch goals and can be cleaned up later.
- The note "split Dungeons into a real screen" is now first-pass done.
- The note "make Fast Rewards real enough for testing" is now closer: local accumulation, 24h cap, Village rate bonuses, and Server Mode/backend-authoritative popup copy are in place. Claim timing still needs visual verification.

## Started This Pass

- Converted the built-village-plot panel from a debug demolish menu into a building detail panel.
- Added a Village building upgrade button in the Unity client.
- Wired the button to local Myth Essence spending and to the existing backend `/village/upgrade` action in Server Mode.
- Added an editor validation entry point for the Village UI so map, build panel, building detail, upgrade, demolish, close controls, bonus detail categories, and max-level upgrade lockout can be checked in Unity.
- Added visible placeholder Village bonuses; local mode applies small Team ATK/HP or Fast Rewards rate boosts from built building type and level.
- Kept Village bonuses local-only until a proper Village balance/definition pass, so Server Mode remains backend-authoritative.
- Polished the Fast Rewards popup so local mode shows stored time, rate, Village bonus, and ready rewards, while Server Mode shows backend min/cap/rate/ready estimate.
- Added an editor validation entry point for the Fast Rewards popup so local copy, 0s/capped 24h states, Server Mode fallback copy, redeem/claim labels, button state, text fit, and control bounds can be checked in Unity.
- Added Paladin to the local `Vanguard Oath` summon banner so the frontline banner actually features and rolls the Paladin.
- Added a Paladin integration editor validator that checks the client hero definition, local summon banners, formation/fight hook anchors, backend definition/migration anchors, EN/DE localization keys, runtime portrait, combat sheets, skeletal part textures, Paladin runtime rig part loading, and Formation/Fight runtime rig visibility.
- Added an editor validation entry point for the Summon UI so the Vanguard Oath banner, Paladin feature art, rates, carousel center card, Paladin result popup, Auto-Summon label, repeat costs, and x10/x300 gem-gated repeat states can be checked in Unity.
- Added an editor validation entry point for upgrade clutter so legacy Battle/Hero upgrade buttons stay hidden, Gear upgrade controls stay on Gear, and debug shortcuts stay in Shop/tools.
- Extended the upgrade clutter validator so the 8-slot Hero Detail gear layout also has non-overlap checks against the portrait, stats, resources, and action buttons.
- Added a `Mythwake/Validate Current Slice` editor validation entry point that runs Village UI, Dungeons UI, Fast Rewards UI, Summon UI, Upgrade Clutter, Home Idle Combat, Paladin integration, and Paladin Spine handoff checks in one pass.
- Added a first Home idle combat slice: the campaign map stays in the background, stage nodes still open a compact info preview, and a foreground patrol fight animates three formation heroes against current-stage monsters for small active local Gold/Myth Essence ticks without changing `enemyLevel`.
- Added `Mythwake/Validate Home Idle Combat` and included it in `Mythwake/Validate Current Slice`; it checks map art, clickable preview info, visible patrol units, one active reward tick, and that idle combat does not auto-clear campaign stages.
- Moved Home idle patrol combat below the map so it no longer covers checkpoint interaction, switched the campaign map to `area_map_scorched_plains`, enlarged it into a vertical scroll viewport, and anchored stage checkpoints to the scrollable map content.
- Lowered the Home idle patrol farther into the bottom free lane below the Battle button and extended the visible campaign-map viewport downward so the current-stage map occupies more of the screen.
- Expanded the Home campaign map viewport to fill the full marked play area behind side controls and the Battle button, leaving only the idle patrol in the lower lane.
- Pulled the Home campaign map up to the top edge under the resource bar and enlarged the lower idle patrol heroes and monsters so the fight reads better.
- Imported the remaining `area_map_*` region images into Runtime resources so Home progress maps can swap by stage region.
- Reworked the Home idle combat lane so its mini-map background connects directly to the main campaign map, spans the same width, and continues downward behind the fighting heroes/monsters.
- Updated `Mythwake/Validate Home Idle Combat` so it guards the connected Home map layout, the idle mini-map texture, the Battle button coverage, active reward ticks, and no automatic stage clear.
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
- Added a dedicated Dungeons UI validator and included it in Current Slice; it checks the world-map viewport, map art, pan/scroll handlers, zoom controls, all three dungeon markers, marker text/art fit, and Gold/Essence/Gear Formation entry flows.
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
- Hero Detail now labels starter Weapon/Armor slots and rows as training tracks instead of claiming they are equipped item instances; the validator guards both slot and row wording in the normal and German refresh paths.
- Hero Detail and Gear runtime equipment icons now use an Editor asset-path fallback when `Resources.Load` is stale, hide missing-texture RawImage placeholders instead of showing white blocks, and the validator rejects hidden/white-placeholder icon art.
- Hero Detail previous/next buttons now sit outside the gear-slot columns, and the upgrade clutter validator checks they do not overlap any of the 8 gear slots.
- Gear screen runtime showcase art now uses the new equipment icon set, and the upgrade clutter validator checks the hero, weapon, armor, headgear, gloves, boots, and accessory images for loaded textures, fit, and input passthrough.
- Gear screen runtime showcase copy now names all visible equipment slots from the title area, and the upgrade clutter validator checks that the full label fits without overlapping the icon rows.
- Gear screen summary text and upgrade/accessory controls now have clearer vertical spacing below the showcase, and the upgrade clutter validator checks those controls do not overlap the showcase or each other.
- Gear screen accessory action copy is now localized for selected fuse tier, equip, level, empty, fuse, target tier, and floor labels, with German refresh coverage in the upgrade clutter validator.
- Gear screen accessory inventory copy counts now render as a compact two-line summary with auto-sized Gear text blocks, keeping the control stack readable in localized layouts.
- The prototype UI builder now creates the Gear equipment/accessory controls in the current localized runtime layout with localized equipment names, runtime Gear navigation uses compact arrow labels, and the upgrade clutter validator rejects stale builder placeholder copy.
- Added backend `accessory_unequip` support, including the action catalog entry, `/gear/accessories/unequip` route, state mutation, and Player/HTTP tests.
- Backend definition snapshots and PostgreSQL seeds now include the sixth `headgear`/Helm accessory slot plus R0-R4 headgear definitions, matching the Hero Detail and Gear UI.
- Added `scripts/check-unity-current-slice.cmd` / `.ps1` so the current slice validator can be run from PowerShell or CI-style local checks once this Unity project is not already open.
- Added backend tests for service-level and HTTP-level Village upgrades.
- Added migration `0026_afk_reward_24h_cap.sql` so existing PostgreSQL dev databases pick up the 24h AFK cap.
- Refreshed `README.md` and `docs/NEXT_CHAT_CONTEXT.md` so the main handoff notes match the current pass.
- Unity batch validation is currently blocked because this project is already open in another Unity instance; C# runtime/editor MSBuild checks pass with the Unity .NET Framework path override.

## Next Small Steps

1. Close the extra Unity project instance and run `.\scripts\check-unity-current-slice.cmd`, or run `Mythwake/Validate Current Slice` in the open editor, then fix any validator failures before continuing.
2. Visually verify Home idle combat on device/editor: connected upper/lower map readability, foreground patrol spacing, reward tick pacing, and the stage-node info preview.
3. Visually verify Village, Fast Rewards, Vanguard Oath/Summon result, and Paladin formation/fight presentation in Unity/emulator.
4. Visually verify the 8-slot Hero Detail spacing in Unity/emulator, then continue the visible Hero/Gear polish pass behind `Mythwake/Validate Upgrade Clutter`.
