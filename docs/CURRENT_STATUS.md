# Mythwake Current Status

Last updated: 2026-05-25

## Where We Are

- Current branch: `codex/batch-1-stabilize-prototype`.
- Unity client code is at Prototype `0.2.75`, save version `2`.
- Backend API default version is `0.2.56`.
- Backend core tests for balance, player, and HTTP routes are green.
- Server-authoritative core is already broad: guest auth, sessions, idempotent gameplay actions, PostgreSQL state, definition snapshots, AFK, daily progress, combat results, dungeons, summons, gear, and village building state.
- Client has moved beyond the older roadmap notes: Dungeons have their own map screen, Village has a scrollable map with 12 build plots, building art is imported, and Paladin/Ravik art plus combat presentation hooks exist.
- Local Fast Rewards already stores continuous AFK time up to 24h. The backend AFK definition has now been aligned to the same 24h cap.

## What Was Behind The Notes

- `README.md` and `docs/NEXT_CHAT_CONTEXT.md` have been refreshed for the current `0.2.75` Village/Dungeons/Paladin state.
- Parts of `docs/ROADMAP.md` still describe older batch goals and can be cleaned up later.
- The note "split Dungeons into a real screen" is now first-pass done.
- The note "make Fast Rewards real enough for testing" is now closer: local accumulation, 24h cap, Village rate bonuses, and Server Mode/backend-authoritative popup copy are in place. Claim timing still needs visual verification.

## Started This Pass

- Converted the built-village-plot panel from a debug demolish menu into a building detail panel.
- Added a Village building upgrade button in the Unity client.
- Wired the button to local Myth Essence spending and to the existing backend `/village/upgrade` action in Server Mode.
- Added an editor validation entry point for the Village UI so map, build panel, building detail, upgrade, demolish, and close controls can be checked in Unity.
- Added visible placeholder Village bonuses; local mode applies small Team ATK/HP or Fast Rewards rate boosts from built building type and level.
- Kept Village bonuses local-only until a proper Village balance/definition pass, so Server Mode remains backend-authoritative.
- Polished the Fast Rewards popup so local mode shows stored time, rate, Village bonus, and ready rewards, while Server Mode shows backend min/cap/rate/ready estimate.
- Added an editor validation entry point for the Fast Rewards popup so local copy, Server Mode fallback copy, redeem/claim labels, button state, and control bounds can be checked in Unity.
- Added Paladin to the local `Vanguard Oath` summon banner so the frontline banner actually features and rolls the Paladin.
- Added a Paladin integration editor validator that checks the client hero definition, local summon banners, formation/fight hook anchors, backend definition/migration anchors, EN/DE localization keys, runtime portrait, combat sheets, skeletal part textures, and Paladin runtime rig part loading.
- Added an editor validation entry point for the Summon UI so the Vanguard Oath banner, Paladin feature art, rates, carousel center card, and Paladin result popup can be checked in Unity.
- Added an editor validation entry point for upgrade clutter so legacy Battle/Hero upgrade buttons stay hidden, Gear upgrade controls stay on Gear, and debug shortcuts stay in Shop/tools.
- Extended the upgrade clutter validator so the 8-slot Hero Detail gear layout also has non-overlap checks against the portrait, stats, resources, and action buttons.
- Added a `Mythwake/Validate Current Slice` editor validation entry point that runs Village UI, Fast Rewards UI, Summon UI, Upgrade Clutter, Paladin integration, and Paladin Spine handoff checks in one pass.
- Polished the Hero Detail gear slice: it now exposes all 2 equipment tracks plus all 6 accessory slots, `Equip Gear` opens the selected gear list for equipment and accessories, and `Remove Gear` can unequip accessories locally or through the Server Mode backend action.
- Added validator coverage for the Hero Detail gear list popup so equipment slots open the inline list first and only the list's `Open Gear` row navigates to the Gear screen.
- Added contextual Hero Detail gear action labels so equipment slots show `Open Gear` while accessory slots keep `Equip Gear`.
- Routed Hero Detail action labels through localization keys so Level Up, Open/Equip Gear, and Remove Gear follow the active language.
- Extended the upgrade clutter validator to switch Hero Detail to German during validation and confirm the open gear, equip gear, and remove gear labels refresh live.
- Added Hero Detail action-label overflow checks so localized Level Up, Open/Equip Gear, and Remove Gear labels must fit inside their buttons.
- Added Hero Detail gear-list text overflow checks so localized list titles and option rows must fit in equipment/accessory popups.
- Added a Hero Detail gear-list state guard so equipment lists expose only summary/open rows while accessory lists restore every rarity row after switching slots.
- Empty Hero Detail accessory slots now show the best available bag copy hint, use a rarity-tinted highlight when copies are available, and the validator checks that the hint appears, fits, and is visually highlighted.
- Hero Detail accessory option lists now put owned, equippable copies above empty rows and keep higher rarity first inside those groups, matching the best-copy slot hint; the validator locks that ordering.
- Hero Detail now renders the new armory background and equipment/accessory slot icon art, and the upgrade clutter validator checks that the art loads, fits, and does not intercept slot input.
- Gear screen runtime showcase art now uses the new equipment icon set, and the upgrade clutter validator checks the hero, weapon, armor, headgear, gloves, boots, and accessory images for loaded textures, fit, and input passthrough.
- Gear screen runtime showcase copy now names all visible equipment slots from the title area, and the upgrade clutter validator checks that the full label fits without overlapping the icon rows.
- Added backend `accessory_unequip` support, including the action catalog entry, `/gear/accessories/unequip` route, state mutation, and Player/HTTP tests.
- Backend definition snapshots and PostgreSQL seeds now include the sixth `headgear`/Helm accessory slot plus R0-R4 headgear definitions, matching the Hero Detail and Gear UI.
- Added `scripts/check-unity-current-slice.cmd` / `.ps1` so the current slice validator can be run from PowerShell or CI-style local checks once this Unity project is not already open.
- Added backend tests for service-level and HTTP-level Village upgrades.
- Added migration `0026_afk_reward_24h_cap.sql` so existing PostgreSQL dev databases pick up the 24h AFK cap.
- Refreshed `README.md` and `docs/NEXT_CHAT_CONTEXT.md` so the main handoff notes match the current pass.
- Unity batch validation is currently blocked because this project is already open in another Unity instance.

## Next Small Steps

1. Close the extra Unity project instance and run `.\scripts\check-unity-current-slice.cmd`, or run `Mythwake/Validate Current Slice` in the open editor, then fix any validator failures before continuing.
2. Visually verify Village, Fast Rewards, Vanguard Oath/Summon result, and Paladin formation/fight presentation in Unity/emulator.
3. Visually verify the 8-slot Hero Detail spacing in Unity/emulator, then continue the visible Hero/Gear polish pass behind `Mythwake/Validate Upgrade Clutter`.
