# Mythwake Next Chat Context

Last updated: 2026-05-26

This file is meant to be pasted/read first in a new Codex chat so the project can continue without re-explaining everything.

## TLDR For The Next Chat

Mythwake is a mobile idle RPG inspired by AFK Arena and 7DS-style idle games. The target is not "quick prototype trash", but a long-lived, game-studio-quality core that can later support real accounts, PostgreSQL-backed state, Redis-assisted runtime coordination, real UI art, and Android/iOS release builds.

Current direction:
- Unity 6 mobile client.
- Go backend.
- PostgreSQL durable source of truth.
- Redis optional for sessions, rate limits, locks, and temporary coordination.
- No Docker requirement. Local Windows PostgreSQL is the expected setup for now.
- Android and iOS are both required.
- Android testing currently uses Android Studio emulator / device installs.
- User wants practical game progress and visible in-game UI, not endless backend-only work.

Current branch:
- `codex/batch-1-stabilize-prototype`

Remote:
- `https://github.com/hamzasnc/mythwake.git`

Important Git rule:
- Pushes/commits should use account/author `xMiepsen <160346173+xMiepsen@users.noreply.github.com>`.

Latest known pushed commit before the current continuation:
- `36f4134 Mark campaign detail states`

Current continuation:
- Village building definitions now exist in backend/static definitions, `/definitions`, PostgreSQL migration `0028_village_building_definitions.sql`, and the injected backend balance catalog. Backend Village build/upgrade uses catalog costs, IDs, max level, and server-side Team ATK/HP bonuses; client Server Mode keeps local Village bonuses paused to avoid double counting.
- Home idle patrol now keeps the middle hero/enemy lane clear of the reward strip, and `Validate Home Idle Combat` guards mobile touch target size, reward shelf fit, unit/reward separation, and loot-popup separation from the reward strip.
- Village building data now has a client-side definition layer per plot/option: stable building ID, display name, texture, build cost, max level, upgrade cost-per-level, bonus type, and bonus value per level live together instead of being spread across local arrays/helpers.
- `Validate Village UI` now checks all 12x3 Village definitions for stable IDs, build costs, max level, loaded texture, and the expected placeholder bonus category while Server Mode still pauses local Village bonuses.
- Prototype Builder Gear defaults now use `gear.selected_rarity` instead of the stale selected fuse-tier key, so `Validate Upgrade Clutter` and the full Current Slice no longer fail after a UI rebuild.
- `scripts/check-unity-current-slice.cmd`, `scripts/check-unity-csharp.cmd`, and `git diff --check` passed on 2026-05-26 after that fix.
- Village building detail now shows both current bonus and `Naechster Bonus` / `Max Bonus`, and `Validate Village UI` checks the extra detail line.
- Fast Rewards now has a compact progress bar/status line under the popup copy. Local mode shows stored percent plus cap-left state, Server Mode shows synced timer progress plus wait/ready state, and `Validate Fast Rewards UI` checks progress text fit and fill percentages.
- Dungeons map zoom buttons now register their listeners through the runtime zoom-control setup path, so `Validate Dungeons UI` can exercise zoom in/out and clamp behavior without relying on a separate startup listener path.
- Dungeons map markers now normalize and immediately refresh reused title/progress/detail text fields, and Gear dungeon details explicitly label accessory drops, so `Validate Dungeons UI` can inspect marker labels reliably even when scene text fields already existed.
- Dungeons run buttons now use idempotent runtime listener registration, so Gold/Essence/Gear marker clicks keep their Formation routes and back-navigation coverage after runtime map setup refreshes the screen.
- Home campaign stage-detail action buttons now switch label and color by state: `Zur Formation` for the current target, `Erledigt` for cleared stages, and `Gesperrt` for locked stages; `Validate Home Idle Combat` checks label, fit, color, and interactability.
- Home stage-detail reward rows and the connected campaign-map-to-idle strip were tightened for Current Slice: reward copy fits, the map extends behind the Battle button, and the idle root stays inside the generated Home root with the reward text as a direct overlay child.
- Summon result popup controls now re-register through the runtime popup setup path, keeping close/repeat/max buttons and the Auto-Summon checkbox mark reliable when the result popup already exists.
- Gear/Hero runtime UI was stabilized for Upgrade Clutter: the Hero Detail armory background is opaque/non-intercepting, accessory candidates sort owned copies before empty rows, Gear opening ensures the runtime showcase exists, and multi-line Gear action labels auto-fit.
- Home campaign stage-detail popups now mirror the checkpoint state with a tinted panel, a non-intercepting side accent, and an OK/ZIEL/LOCK badge; `Validate Home Idle Combat` checks cleared/current/locked detail popups.
- Home campaign stage previews now tint the panel and show a non-intercepting state accent for cleared/current/locked selections; `Validate Home Idle Combat` checks the three preview states.
- Home campaign current nodes now show a small ZIEL badge in addition to the current halo; `Validate Home Idle Combat` checks current/cleared/locked target-badge separation and input passthrough.
- Home campaign locked nodes now show a small LOCK badge; `Validate Home Idle Combat` checks locked/cleared/current badge state and input passthrough.
- Home campaign cleared nodes now show a small OK badge; `Validate Home Idle Combat` checks cleared/current/locked badge state and input passthrough.
- Home campaign nodes now have a separate selected-stage halo for the tapped checkpoint; `Validate Home Idle Combat` checks that selecting a locked future node keeps the true current-stage halo on the actual current stage.
- Home campaign stage-detail reward rows now distinguish normal next-bonus hints, Bonus Gems/Pass XP, and Boss Gems/Boss XP; `Validate Home Idle Combat` checks all reward labels and fit.
- Home campaign stage-detail popups now mirror Boss/Bonus/Normal tags plus compact special-reward hints, and `Validate Home Idle Combat` checks the detail body copy and fit for normal, bonus, and boss stages.
- Home campaign stage previews now include Boss/Bonus/Normal tags plus compact special-reward hints, and `Validate Home Idle Combat` checks the preview copy and fit for normal, bonus, and boss stages.
- Home campaign non-boss milestone nodes now show visible Bonus badges while boss milestones keep the Boss badge; `Validate Home Idle Combat` checks milestone/boss badge separation.
- Home campaign boss nodes now show visible Boss badges; `Validate Home Idle Combat` checks boss and non-boss badge state.
- Home campaign path segments now color reached paths brighter and keep future locked paths dim; `Validate Home Idle Combat` checks that selecting a locked future node does not fake path progress.
- Home campaign nodes now show a visible current-stage halo, and `Validate Home Idle Combat` checks that only the actual unlocked current node gets it.
- Home idle combat lower mini-map now uses a stage-progress crop instead of a static slice, and `Validate Home Idle Combat` checks progress-map UV sync across the idle lane and stage-detail preview.
- Local Fast Rewards redeem now refreshes the open popup after claim and has validator coverage for reward grant, stored-time reset, and disabled button state.
- Server Mode Fast Rewards now shows claim-status copy and gates the Claim button until the backend min claim time is reached and a backend session exists.
- Fast Rewards now shows remaining time before the 24h cap and a progress bar/status line, with validator coverage for normal, empty, capped cap-left states, progress copy, and fill percentages.
- Home idle Patrol Info now shows last reward, next reward countdown, and tick cadence details, with validator coverage before and after local reward ticks.
- Home idle reward summary now has a taller two-line last/next reward label and validator coverage in `Assets/_Mythwake/Editor/HomeIdleCombatValidation.cs`.
- Paladin integration validator in `Assets/_Mythwake/Editor/PaladinSpineValidation.cs`, now including formation/fight hook anchors, backend definition/migration anchors, runtime rig part loading, and Formation/Fight runtime rig visibility.
- Dungeons UI validator in `Assets/_Mythwake/Editor/DungeonsUiValidation.cs`.
- Fast Rewards UI validator in `Assets/_Mythwake/Editor/FastRewardsUiValidation.cs`.
- Summon UI validator in `Assets/_Mythwake/Editor/SummonUiValidation.cs`.
- Upgrade clutter validator in `Assets/_Mythwake/Editor/UpgradeClutterValidation.cs` checks that old Battle/Hero upgrade controls stay hidden, Gear upgrade controls live on Gear, Gear navigation uses compact arrow labels, Gear builder defaults do not recreate stale placeholder copy, Gear showcase art loads/fits without intercepting input, the Gear showcase label names all visible equipment slots, fits, and does not overlap the icon rows, Hero Detail gear slots do not overlap the portrait/stats/actions, Hero Detail gear lists stay inside their popup, equipment/accessory list rows switch correctly, localized/contextual Hero Detail action labels and gear-list rows are correct after a German language refresh and do not overflow, and debug shortcuts live in Shop/tools.
- Current slice validator in `Assets/_Mythwake/Editor/CurrentSliceValidation.cs`; use `Mythwake/Validate Current Slice` in the editor or `scripts/check-unity-current-slice.cmd` from PowerShell to run Village UI, Dungeons UI, Fast Rewards UI, Summon UI, Upgrade Clutter, Home Idle Combat, Paladin integration, and Paladin Spine handoff checks in one pass.
- Current status summary in `docs/CURRENT_STATUS.md` and this handoff note.

## User Preferences And Product Intent

The user wants Mythwake to feel like a real idle RPG test stand, not a toy sample.

Reference feel:
- AFK Arena
- 7 Deadly Sins Idle Adventure
- Mobile portrait idle RPG with strong fantasy UI, big buttons, bottom navigation, side shortcut icons, top resource bar, campaign stage presentation, and eventually visible battles.

Design intent:
- Mobile portrait first.
- Designer can work around 1080 x 1920 as a safe reference canvas.
- iPhone screenshots may be 1284 x 2778, but Unity UI should scale from 1080 x 1920 via CanvasScaler.
- Current UI should avoid overlapping text and should be immediately usable on phone/emulator.

Engineering standard:
- Build systems as if they will survive into production.
- Avoid "prototype quick and dirty" when touching core economy, persistence, auth, backend, or inventory.
- Debug shortcuts are allowed only when clearly local/dev.
- Keep every economy mutation server-validatable, replay-safe, and idempotent once it touches backend paths.

Do not deepen these yet:
- Character roles.
- Complex hero kits.
- PvP.
- Guilds.
- Monetization/live shop.
- Events.
- Advanced substats.
- Full production admin tooling.

## Current Client State

Unity project path:
- `D:\Github\mythwake`

Key Unity scene:
- `Assets/Scenes/SampleScene.unity`

Core runtime script:
- `Assets/_Mythwake/Scripts/IdlePrototypeController.cs`

Current client version:
- Prototype `0.2.130`
- Save version `2`

Important Unity scripts:
- `Assets/_Mythwake/Scripts/IdlePrototypeController.cs`

Latest local gameplay/UI batch:
- Dungeons now have a dedicated map screen opened from the bottom Dungeons nav item, with Gold, Essence, and Gear dungeon cards; `Validate Dungeons UI` checks the world-map viewport, map art, pan/scroll handlers, zoom controls and clamps, all three dungeon markers, marker spacing/text/art fit, and Gold/Essence/Gear Formation entry plus back-navigation flows.
- Village now has a dedicated scrollable map screen opened from the bottom Village nav item, with 12 build plots and imported building art.
- Village free plots open a build panel. Built plots open a building detail panel with level, next upgrade cost, available Myth Essence, visible HP/ATK/Fast Rewards bonus categories, `Aufwerten`, `Abreissen`, and `Schliessen`; the Village validator also checks the scrollable map/content wiring, all 12 plot buttons, loaded map/building art, build/detail close flows, built-plot hidden build marks, max-level upgrade lockout, and the Village bonus hint.
- Village building upgrades spend Myth Essence locally and route through the existing backend Village upgrade action in Server Mode.
- Village building details show current and next/max placeholder bonuses. In local mode, built building type and level apply small Team ATK/HP or Fast Rewards Gold/Essence rate boosts and the Village hint line summarizes the active totals. The client reads those costs/levels/bonuses from a per-building definition layer with stable IDs, and the backend now exposes the same definition shape through `/definitions`.
- Server Mode pauses client-side local Village bonuses so local stats do not double-add over backend-authoritative snapshots. Backend Team ATK/HP Village bonuses are now catalog-driven; AFK-rate Village bonuses still need a server-authoritative reward-design pass.
- Fast Rewards popup now separates local and Server Mode: local shows stored time, remaining cap time, rate, Village bonus, and ready rewards; Server Mode shows backend min/cap/rate, claim status, ready estimate, and notes that Village local bonuses do not modify server rewards yet.
- A Unity editor validator now checks Fast Rewards popup controls, local copy, cap-left copy, progress bar fill/text, local redeem grant/reset/button-disable flow, 0s/capped 24h states, popup exclusivity, close flow, Server Mode fallback copy, disabled no-session fallback, waiting/ready server claim status, redeem/claim labels, text fit, and button bounds through `Mythwake/Validate Fast Rewards UI`.
- Home now has a first AFK-Arena-style idle combat slice: the campaign map remains in the background with clickable stage-node info, while a foreground patrol fight animates three formation heroes against current-stage monsters and grants small active local Gold/Myth Essence ticks without changing `enemyLevel`.
- The Home campaign map now uses `area_map_scorched_plains`, is a larger vertical ScrollRect with the checkpoints on the scrollable content, and has a connected lower idle mini-map background behind the patrol fight.
- Latest Home layout pass imports the remaining `area_map_*` region images, keeps the main map pulled up under the resource bar, and extends the lower idle map background directly from the main map down behind heroes and monsters.
- The current Home map viewport fills the marked play area behind side controls and the Battle button; the idle patrol is below the Battle button but sits on a same-width connected map image rather than a separate dark lane.
- The Home lower idle patrol heroes and monsters are enlarged, the middle lane now stays clear of the reward strip, and `Validate Home Idle Combat` now guards the connected upper/lower map layout, current-stage halos and target badges, selected-stage node halos, cleared-stage and locked-stage badges, boss-node badges, milestone bonus badges, stage-preview/detail state tint plus Boss/Bonus/Normal tags, stage-detail status badges/action states, stage-detail reward labels, path progress colors, progress-map region texture/UV sync across main/detail/idle maps, mobile touch target size, reward shelf fit, unit/reward separation, reward progress fill behavior, two-line local last/next reward summary copy, Server Mode local-reward blocking, Server Mode Patrol Info copy, stale loot popup clearing, active reward tick, and no automatic stage clear. `Validate Dungeons UI` also checks the runtime zoom control path.
- Home idle combat now shows a short floating loot popup when the local active Gold/Myth Essence tick is granted, and the Home idle validator checks that it appears and fits.
- The Home idle combat area is now tappable and opens a `Patrol Info` popup with current stage, enemy, last reward, next reward countdown, tick cadence, and no-auto-clear copy; the validator clicks it before and after a local reward tick.
- Tapping a Home campaign checkpoint now opens a larger `Abschnitt Details` popup with map preview, enemy formation, completion reward row, and Battle/Close controls; `Validate Home Idle Combat` checks the popup and closes it.
- Local campaign stage clears now grant the displayed stage Myth Essence reward and return it in the action result payload; `Validate Home Idle Combat` creates a won clear and checks the stage advance plus currency increase.
- The stage-detail Battle action is now guarded to current unlocked checkpoints only, and `Validate Home Idle Combat` clicks the current checkpoint detail Battle button into the Formation screen before returning Home.
- The Home idle validator also checks locked checkpoint detail Battle guards under direct invocation and verifies local campaign clear action results carry a non-empty Myth Essence reward payload.
- The Home idle validator now also checks popup exclusivity between Fast Rewards, Patrol Info, and checkpoint details.
- The Summon UI validator now checks visible result-slot text fit/art, hidden unused result cards, Auto-Summon toggle mark state, and result close flow.
- Unity editor validators now cover Village map/build/detail/upgrade/demolish, Dungeons map/zoom clamp/Formation entry and back navigation, Fast Rewards, Summon/Vanguard Oath, Upgrade Clutter, Home Idle Combat, Paladin integration, and Paladin Spine handoff. `Mythwake/Validate Current Slice` runs them together, and `scripts/check-unity-current-slice.cmd` runs the same check in Unity batchmode; the latest batchmode Current Slice passed after this continuation.
- Hero Detail now exposes all 2 equipment tracks plus all 6 accessory slots with armory background, visible starter Weapon/Armor training icons, and equipped-only accessory slot icon art. The localized main gear action shows Open Gear for starter Weapon/Armor training tracks and Equip Gear for accessory slots, starter Weapon/Armor slots and rows are labeled as training, empty accessory slots stay visually empty even when bag copies exist and after German refresh, accessory lists put owned copies above empty rows and higher rarity first inside each group with visible copy/tap-to-equip text, the selected equipment/accessory slot list opens instead of immediately leaving for Gear, the equipment list's Open Gear row navigates to the Gear screen, the validator keeps training slot/row wording through German refresh, and Remove Gear unequips accessories locally or through Server Mode via `/gear/accessories/unequip`.
- Hero Detail and Gear equipment icon loading now has an Editor asset-path fallback plus blank-placeholder protection, so missing textures no longer appear as white RawImage blocks and the upgrade clutter validator catches hidden/white placeholder art, including equipped Hero Detail accessory icons after German refresh.
- Hero Detail previous/next buttons have been pulled inward below the hero stage so they no longer collide with the lower gear slots; the upgrade clutter validator now checks that spacing.
- Gear screen summary text and gear/accessory action controls were tightened into a clearer stacked layout below the showcase; the upgrade clutter validator now checks the controls do not overlap the showcase or each other.
- Gear screen accessory action copy is localized for selected rarity, bag/equipped copy count, equip, level, empty, fuse, target tier, and floor labels; the upgrade clutter validator switches to German, checks the old English/fuse-tier strings are gone, and confirms the selected-rarity plus equip-action copy summaries fit.
- Local accessory equip, level, unequip, and fuse action-result messages now use EN/DE localization keys, and the Gear validator exercises the German action flow while restoring the test inventory and equipped state.
- Gear screen accessory inventory copy counts are compacted into a two-line summary, and the Gear text blocks use auto-sizing so localized layouts have room to breathe.
- Backend action catalog now includes `accessory_unequip`, with Player and HTTP tests covering accessory removal and body validation.
- Backend accessory definitions now include the sixth `headgear`/Helm slot and R0-R4 headgear item definitions in both the static snapshot and PostgreSQL migrations, so Server Mode matches the 6-slot client UI.
- Local Fast Rewards and backend AFK definitions now both use a 24h stored reward cap.
- Paladin combat assets, combat preview, and Spine handoff validation are present.
- Paladin is now also featured in the local `Vanguard Oath` summon banner and included in that banner's Epic pool.
- A Unity editor validator now checks Paladin client definition, local summon banners, formation/fight hook anchors, backend definition/migration anchors, EN/DE localization, runtime portrait, combat sheets, skeletal part textures, Paladin runtime rig part loading, and Formation/Fight runtime rig visibility through `Mythwake/Validate Paladin Integration`.
- Home now has a runtime campaign map with clickable stage nodes and a stage preview.
- Battle no longer starts immediately from the main button. Flow is now map/stage selection -> Battle -> Formation -> Confirm -> visible fight.
- Dungeons now use the same Formation -> Confirm -> visible fight flow.
- Each dungeon currently spawns one larger boss enemy: Gold = Treasure Golem, Essence = Rift Dragon, Gear = Iron Hound.
- Dungeon boss HP is multiplied by 1.8 on client and backend balance paths.
- Combat visuals now use curated 2D idle/run/attack frame sequences under `Assets/_Mythwake/Resources/Mythwake/Art/CombatAnimated/`.
- Latest combat loop uses per-unit visual state: each unit has position, current target, attack cooldown, and attack animation timing.
- Melee units run to the nearest living target, stay beside it, and keep attacking there on their own attack-speed timer.
- Melee units now close directly beside their current target instead of stopping at the shared midpoint, including ranged targets.
- Ranged units stay back and fire projectiles on their own timer.
- Multiple heroes/enemies can attack at the same time; combat is no longer an A -> B -> C alternating sequence.
- Local Campaign/Dungeon fight result bodies now mirror the server combat summary shape more closely: Team HP, Enemy HP, Team ATK, Enemy DMG, dealt/taken damage, healing, crits, misses, and execute flags are shown consistently and covered by Upgrade Clutter validation.
- Normal hits reduce only one target HP bar.
- Fight UI now has bottom hero skill cards with portrait, per-character mana bar, ready glow, click-to-queue ultimate, and an AUTO toggle above the right side of the cards.
- Character mana is per hero, not team-wide. Heroes start at 0, no longer gain passive timer mana, gain +2 mana on successful hits, and each hero has a different max mana.
- Current local visual loop supports AA mana gain, Elowen passive heals, queued/manual ultimates, and auto-ultimates when AUTO is enabled.
- Formation can be adjusted before campaign/dungeon fights with tap-to-swap slots: tap a hero slot, other valid slots glow, tap one to swap. Fight start positions now follow the chosen formation order, and the chosen order is saved locally.
- Formation now has an `Auto next after win (skills AUTO)` checkbox. When enabled, a won campaign/dungeon fight automatically starts the next stage/floor with the same formation and forces skill AUTO on.
- Visible fight playback now uses real seconds instead of compressing the timeout into a shorter visual playback.
- Enemy HP bars now show HP percentage text. Dungeon bosses use a large top boss HP bar with percentage instead of the small overhead enemy bar.
- Prototype combat stats now include Crit, Accuracy, and Defense. Local combat uses Accuracy/Crit for expected damage and Defense for incoming damage reduction; backend combat has a matching deterministic miss/crit/defense layer.
- Heroes screen now opens a dedicated hero detail overlay when tapping a hero card. The overlay shows rarity/title/name, a large centered hero portrait, side gear slots, live level/power/stats/resources, Level Up, Gear buttons, and Story/Hero/Skills-style tabs.
- Hero detail no longer shows infinite local caps as `2147483647`. Visible/local combat now applies hero stats per hit: ATK drives hit damage, HP/DEF reduce per-target damage pressure, CRIT can multiply hits, and ACC can miss before mana/passives trigger.
- Fight screen now has an `End Fight` button. It cancels the current visible fight, disables Auto Continue/skill AUTO, stops pending auto-continue coroutines, and returns to the result popup without applying local fight rewards. Auto-next only queues after victory while the Formation checkbox is enabled.
- Fight screen now has a small `x2` speed toggle next to the skill `AUTO` button. When enabled, the visible fight timer, movement, attacks, animation playback, mana gain cadence, and result timing run at double speed.
- Hero ultimates now create a short AFK-Arena-style moment: combat time/timeout pauses, regular attacks pause, the arena slows/dims, and the casting hero stays highlighted while their ultimate animation plays at normal speed before combat resumes.
- Default combat duration is now 30 seconds on client and backend, and hero ultimates have higher damage multipliers so they feel more visible and worth using.
- Hero detail gear slots are clickable. Weapon/Armor and accessories are now tracked per hero locally, so equipping or leveling gear on Astra no longer makes Dante wear the same item. Clicking accessory slots shows all compatible rarity pieces for that slot with copy counts, equipped state, and tap-to-equip when a copy is available.
- Heroes screen has a bottom-style `Hero` / `Set Team` subtab flow. `Hero` lists team members first, then sorts by rarity and power with Asc/Desc plus attack-type filters. `Set Team` lets the player tap or drag slots/hero cards to place/swap heroes, and includes Auto-Set for the highest-power lineup.
- Hero overview was cleaned toward an AFK-style roster: old selected-hero summary/header/upgrade/essence layers are hidden, the screen uses one dark roster backdrop with a teal filter bar, larger name-less hero cards, level/stars/shard progress, and bottom `Held` / `Team festlegen` subtabs.
- Summon screen now starts an AFK-style hero draw banner section with a fantasy background, visible `Summon` and `Summon 10` buttons, gem costs on each button, and a 10-pull cost discount. The layout is ordered top-to-bottom as selected hero banner image, summon buttons, then the rotation carousel; the summon buttons use a brown AFK-style look with the `mythic_gem` icon at 20x27 on the left and a small white cost label below it. The old large yellow parchment is hidden on Summon, summon count is a small left-side chip, and rates sit in their own highlighted teal/gold box. A bottom banner carousel now shows preview boxes with left/right arrows and swipe gestures; switching banners changes the featured heroes and local summon odds for now.
- Summoning now opens a result popup that groups all drawn heroes and shows how many times each appeared. Local and backend summon pools now grant 1 shard per duplicate pull. The result popup has bottom `x10` and `x300` buttons, disabled when gems are insufficient, plus an `Auto-Summon` checkbox that keeps pulling in x10 chunks up to 300 total pulls.
- A Unity editor validator now checks the Vanguard Oath Summon slice, Paladin feature art, local rates, carousel selected card, Paladin result popup, Auto-Summon label, repeat costs, and x10/x300 gem-gated repeat states through `Mythwake/Validate Summon UI`.
- Campaign/Village fights can now keep running while the player leaves through the bottom nav to edit heroes/gear, then pressing Village/Battle resumes the active fight/result. Team formation changes during such a fight abort the current fight without granting rewards. Dungeon fights/formations hide the top resource bar and bottom navbar for a focused boss-fight view.
- Visible fight end conditions now stop immediately once the winning side has killed/disappeared all enemies, instead of waiting out the remaining visual duration while the displayed damage total keeps climbing.
- Ravik is now added as a playable Epic fire mage. His generated transparent portrait and combat frames live under `Assets/_Mythwake/Resources/Mythwake/Art/Runtime/hero_ravik.png` and `Assets/_Mythwake/Resources/Mythwake/Art/CombatAnimated/hero_ravik_*`. The source sheet/manifest live under `Assets/_Mythwake/ArtSource/Generated/ember_mage/`. The current Ravik pass uses a production-style split: stable bottom-center body frames plus separate attack/ultimate VFX layers, so large fire effects no longer change his body frame size or root position.
- Melee engagement is stabilized: units lock a fixed melee position when acquiring a target, instead of recalculating from a moving target every frame. Fight positions are also clamped to the arena bounds.
- Victory/defeat result screens wait for the visible HP bars to reach their intended end state, with a short cleanup extension if the backend result finished before the visual target deaths did.
- Dante and Iron Hound were flipped to face the correct direction.
- Dead units now disappear from the arena instead of standing at 0% HP. Hero HP moved out of the field and onto the bottom hero skill cards above the mana bar; Dante's bottom portrait uses the same right-facing flip as the fight sprite.
- The old static slash/magic image VFX are no longer used in the fight loop.
- Asset source tracking is in `docs/ART_SOURCES.md`.

Latest backend combat direction:
- Server combat now returns hero combat metadata and replay events through `api.CombatResult`.
- Replay data includes per-hero max mana/current mana, passive IDs/names, ultimate IDs/names, and events like `auto_attack`, `ultimate`, `passive_heal`, and `enemy_attack`.
- This is still request/response replay, not a live combat command stream. True manual server-authoritative ultimate clicks still need a follow-up endpoint or websocket-style command path.

Latest verification notes:
- `go test ./internal/balance ./internal/definitions ./internal/player ./internal/http` passes from `backend/`.
- `scripts/check-unity-csharp.cmd` passes runtime/editor C# MSBuild checks through Unity's bundled .NET Framework references, with existing Unity serialization/Paladin JSON field warnings.
- `git diff --check` passes for touched client/backend/docs files, with existing LF->CRLF warnings on some backend/docs files.
- Direct `dotnet build` fails on this machine because .NET Framework 4.7.1 reference assemblies are not installed globally.
- Plain MSBuild without `/p:LangVersion=latest` can fail because the generated Unity csproj still says C# 7.3 while current code uses newer syntax.
- Unity batchmode validation command is prepared, and the `.cmd` wrapper now propagates PowerShell failures correctly; full current-slice execution is currently blocked because another Unity instance has this project open.
  - Main local gameplay, UI runtime construction, backend mode switching, save/load, action handlers.
  - It is currently large/monolithic. Be careful with surgical edits.
- `Assets/_Mythwake/Scripts/MythwakeBackendClient.cs`
  - HTTP client for backend health, auth, bootstrap, definitions, actions, flush, reset.
- `Assets/_Mythwake/Scripts/MythwakeServiceContracts.cs`
  - DTO/service contracts shared by local/client/backend-shaped systems.
- `Assets/_Mythwake/Scripts/MythwakeRuntimeArtPresenter.cs`
  - Runtime art presentation helper.
- `Assets/_Mythwake/Editor/MythwakePrototypeBuilder.cs`
  - Editor menu helpers.
  - Menus include `Tools/Mythwake/Build Prototype UI` and `Tools/Mythwake/Bind Home Navbar Assets`.
- `Assets/_Mythwake/Editor/VillageUiValidation.cs`
  - Editor menu `Mythwake/Validate Village UI` checks Village map/build/detail/upgrade/demolish controls, bonus detail categories, text fit, and Max Level upgrade lockout.

Unity builder caution:
- `Build Prototype UI` recreates the scene UI and can reset layout/object references.
- Use it carefully.
- `Bind Home Navbar Assets` is safer when only rebinding navbar/currency icon textures.

Current client systems:
- Versioned JSON local save in PlayerPrefs.
- Legacy PlayerPrefs migration into save v2.
- Local economy boundary methods.
- Shared service contracts.
- Server Mode toggle from the Backend panel.
- Backend bootstrap through `/client/bootstrap`.
- Server Mode persists across restarts.
- Server Mode pauses local auto attack.
- Server Mode blocks local debug grants/reset.
- Gameplay requests are gated while backend request is in flight.
- Unity sends request IDs.
- Unity sends idempotency keys for gameplay actions.
- Unity keeps/reuses pending idempotency key after transport failure.
- Unity sends last known server state revision.
- Unity flushes active backend session on app pause/quit.

## Current Gameplay Design

Core loop:
- Push campaign.
- Hit a wall.
- Farm dungeons.
- Upgrade heroes/equipment/accessories.
- Push campaign again.
- Claim AFK/idle rewards.

Currencies:
- Gold
  - Later mainly for equipment/accessory upgrades.
  - Visible on main HUD.
- Myth Essence
  - Hero level-up currency.
  - Not visible on main screen.
  - Show it when upgrading heroes/character detail.
- Gems
  - Summons/shop.
  - Visible on main HUD.
- Pass XP
  - Mission track / battle pass style progression.
- Hero Shards
  - Summon/ascension.

AFK/offline rewards:
- Should grant Gold and Myth Essence.
- Rewards should accumulate continuously in the background, not only when the app is actually closed.
- Fast Rewards popup should show current stored AFK rewards and reward rate per second.
- Current user request: cap stored AFK rewards at 24h.
- Active resource gain should mainly come from dungeons, not normal campaign fights, for now.

Combat:
- Combat should be time-based, not round-based.
- Default fight duration is 30 seconds.
- Backend/client text should talk in seconds, not rounds.
- Campaign/dungeon fights can win or lose based on team HP/damage/enemy stats.

Dungeons:
- Gold Dungeon: endless tower, increasing floor difficulty/rewards.
- Essence Dungeon: endless tower, increasing floor difficulty/rewards.
- Gear Dungeon: endless tower, drops accessories.
- Dungeons have a first-pass dedicated screen/tab and still need visual/layout polish.

Village:
- Village has a first-pass dedicated scrollable map with 12 plots.
- Buildings can be placed, viewed, upgraded, and demolished.
- Building upgrades increase saved level, cost Myth Essence, and show/apply small placeholder bonuses in local mode.
- Village bonuses should stay local-only until the Village balance pass moves them into backend-owned definitions.

Gear/accessories:
- Accessory slots:
  - Ohrringe
  - Kette
  - Armband
  - Handschuhe
  - Schuhe
- Rarities:
  - R0-R4 currently.
- Max level:
  - R0 max level 20.
  - Each rarity adds +10 max levels.
- Fusion:
  - 3 copies of same slot and rarity fuse into next rarity.
- Current storage is count-like, not full item instances.
- Definition data is already shaped for future DB rows.

Roles:
- Prototype code has role-flavored stats/logic from earlier.
- User said roles should be ignored for now.
- Do not deepen role systems until real/free characters are picked.

## Current UI Direction

The current push moved the Home screen toward a 7DS/AFK-style mobile HUD.

User wants the main screen currently to be mostly clean:
- Bottom navbar.
- Top bar with player name/power/resources.
- Stage badge area.
- Side shortcut buttons.
- Battle button.
- Fast rewards button.
- World map button.
- Chat floating button.
- No old prototype battle text/debug clutter on the main screen.

Current home HUD assets:
- Bottom navbar source slices:
  - `Assets/_Mythwake/UI/Home Screen/bottom_navbar/navbar.png`
  - `Assets/_Mythwake/UI/Home Screen/bottom_navbar/heroes_btn.png`
  - `Assets/_Mythwake/UI/Home Screen/bottom_navbar/village_btn.png`
  - `Assets/_Mythwake/UI/Home Screen/bottom_navbar/dungeons_btn.png`
  - `Assets/_Mythwake/UI/Home Screen/bottom_navbar/summon_btn.png`
- Currency icons:
  - `Assets/_Mythwake/UI/icons/exp_shard.png`
  - `Assets/_Mythwake/UI/icons/gold_coin.png`
  - `Assets/_Mythwake/UI/icons/mythic_gem.png`
- Generated home UI art:
  - `Assets/_Mythwake/UI/Home Screen/generated/home_topbar_frame.png`
  - `Assets/_Mythwake/UI/Home Screen/generated/home_battle_button.png`
  - `Assets/_Mythwake/UI/Home Screen/generated/home_shop_button.png`
  - `Assets/_Mythwake/UI/Home Screen/generated/home_quest_button.png`
  - `Assets/_Mythwake/UI/Home Screen/generated/home_treasure_chest_button.png`
  - `Assets/_Mythwake/UI/Home Screen/generated/home_fast_rewards_button.png`
  - `Assets/_Mythwake/UI/Home Screen/generated/home_world_map_button.png`
  - `Assets/_Mythwake/UI/Home Screen/generated/home_chat_button.png`
  - `Assets/_Mythwake/UI/Home Screen/generated/home_power_icon.png`
  - `Assets/_Mythwake/UI/Home Screen/generated/home_stage_level_badge.png`
  - `Assets/_Mythwake/UI/Home Screen/generated/home_stage_mode_badge.png`
  - `Assets/_Mythwake/UI/Home Screen/generated/home_stage_extra_badge.png`
- Runtime-loadable duplicates live under:
  - `Assets/_Mythwake/Resources/Mythwake/UI/HomeScreen/Generated/`

Current home behavior:
- Player name appears in the topbar instead of hardcoded "Mythwake".
- Topbar shows Gems and Gold.
- Myth Essence should not show on main HUD.
- A plus button next to Gems routes to Shop.
- Power is displayed near the player section with a power icon.
- Stage badge shows `Stufe X`.
- Mode badge shows `Albtraum`.
- Extra badge below mode.
- Bottom navbar routes:
  - Heroes -> Heroes
  - Village -> Village
  - Center/Campaign -> Home
  - Dungeons -> Dungeons
  - Summon -> Summon
- Chest button opens inventory popup placeholder.
- Fast Rewards button opens AFK popup and can redeem.
- World Map button currently routes home/placeholder.
- Chat button opens chat popup placeholder.
- Right side shortcut group is collapsed by default.
  - Collapsed: show only first item, `Chest`.
  - Expanded: show `Chest` and `Quests`.
- Left side shortcut group is collapsed by default.
  - Collapsed: show only first item, `Shop`.
  - Expanded: currently still mostly shop-side placeholder.
- Side shortcut groups have semi-transparent dark panels and arrow toggles.

Recent specific UI feedback already addressed before this file:
- Player name should be further right than the avatar.
- Power should be lower/right and use a combat-power icon.
- Top stage/shop/quest area should be higher.
- Battle/Fast Rewards/World Map/Chat should be lower toward bottom navbar.
- Side shortcuts default collapsed, while still showing first icon.

Potential next UI fixes:
- Run or visually inspect the new Village UI validator once Unity is not blocking batchmode.
- Polish the Village building detail panel spacing on device/emulator.
- Verify collapsed shortcut layout in Unity/emulator visually.
- Make side shortcut expand/collapse polished with proper arrow art instead of text.
- Add real background map/campaign art to the empty main field.
- Replace placeholder popups with proper parchment/fantasy panels.
- Polish the dedicated Dungeons screen/cards now that the split exists.
- Move hero upgrade UI into Heroes/detail, not Battle.
- Move equipment/accessory upgrades into Gear/detail, not Heroes or Battle.

## Backend State

Backend path:
- `backend/`

Backend entrypoint:
- `backend/cmd/api/main.go`

Current API version:
- `0.2.56`

Core backend status:
- Go standard library HTTP server.
- Environment config.
- PostgreSQL optional but expected for local test.
- Redis optional.
- Embedded SQL migrations.
- Schema namespaces:
  - `account`
  - `common`
  - `player`
  - `logs`
  - `debug`
- Guest auth exists.
- Account/auth tables are shaped for future guest, email, Google, and Apple login providers.
- Session tokens are random.
- PostgreSQL stores token hashes, not raw tokens.
- Protected gameplay/state routes require Bearer session.
- Logout revokes sessions.
- Player state and action routes resolve active player from session.
- `/client/bootstrap` returns server time, definitions, player snapshot.
- `/definitions` exposes cacheable server-owned definition snapshot with content hash/ETag.
- `/time` exposes authoritative server time with daily/weekly reset boundaries.
- `/health` exposes DB/cache/catalog/lock/write-mode diagnostics.
- `/player/state/flush` exists for app pause/disconnect save hook.
- `/dev/player/reset` exists only for local/dev.

Important backend rules:
- PostgreSQL is durable source of truth.
- Redis must never be required to recover permanent state.
- Critical gameplay/economy actions must be idempotent and replay-safe.
- Client-submitted reward amounts are never trusted.
- Backend owns rewards, spends, drops, claims, inventory transitions.
- Normal Fight/Dungeon spam must not show user-visible HTTP 429.
- Gameplay spam should be handled through Unity request gating, idempotency, and per-player locks.

Persistence/cache design:
- Default write mode is `ledger_write_behind`.
- Successful gameplay/economy actions write durable action/result data first.
- Materialized normalized player state can flush in batches.
- Startup can restore from latest durable action result if materialized tables lag.
- Full `write_through` exists for debugging.
- Plain unsafe write-behind is local/dev only.
- API shutdown flushes loaded player contexts.
- Idle hot-player contexts flush/unload over time.
- State revisions prevent older materialized state from overwriting newer accepted actions.

Backend packages to know:
- `backend/internal/player`
- `backend/internal/gameplay`
- `backend/internal/economy`
- `backend/internal/balance`
- `backend/internal/definitions`
- `backend/internal/auth`
- `backend/internal/store/postgres`
- `backend/internal/store/cache`
- `backend/internal/cache/redis`
- `backend/internal/cache/ratelimit`
- `backend/internal/cache/actionlock`
- `backend/internal/database/migrations`

Important tests/smoke:
- Go tests under `backend/internal/...`
- Local backend smoke:
  - `scripts/check-backend.ps1`
  - `scripts/check-backend.cmd`
- PostgreSQL E2E:
  - `scripts/check-postgres-e2e.ps1`
  - `scripts/check-postgres-e2e.cmd`
- Unity C# build validation:
  - `scripts/check-unity-csharp.ps1`
  - `scripts/check-unity-csharp.cmd`
- Unity current slice validation:
  - `scripts/check-unity-current-slice.ps1`
  - `scripts/check-unity-current-slice.cmd`

## Local Development Commands

Repo root:

```powershell
cd "D:\Github\mythwake"
```

Start backend with local PostgreSQL:

```powershell
.\scripts\start-backend.cmd
```

Start backend without DB:

```powershell
.\scripts\start-backend.cmd -NoDatabase
```

Backend smoke:

```powershell
.\scripts\check-backend.cmd
```

PostgreSQL E2E smoke:

```powershell
.\scripts\check-postgres-e2e.cmd
```

Unity C# validation:

```powershell
.\scripts\check-unity-csharp.cmd
```

Unity current slice validation:

```powershell
.\scripts\check-unity-current-slice.cmd
```

Manual Go run with DB:

```powershell
cd "D:\Github\mythwake\backend"
$env:MYTHWAKE_DATABASE_URL='postgres://mythwake:mythwake@localhost:5432/mythwake?sslmode=disable'
go run ./cmd/api
```

Unity backend URLs:
- Editor/Desktop: `http://localhost:8080`
- Android emulator: `http://10.0.2.2:8080`

Navicat:
- Connect to local PostgreSQL.
- DB: `mythwake`
- User/password currently expected by scripts: `mythwake` / `mythwake`
- Useful schemas: `account`, `common`, `player`, `logs`, `debug`
- Inspect debug views first when checking player state.

## Assets And Art Direction

Already created/imported:
- Mythwake logo/icon transparent PNGs in repo root.
- Bottom navbar fantasy slices in `Assets/_Mythwake/UI/Home Screen/bottom_navbar`.
- Currency icons in `Assets/_Mythwake/UI/icons`.
- Generated HUD/button/stage assets in `Assets/_Mythwake/UI/Home Screen/generated`.
- Runtime Resources copies in `Assets/_Mythwake/Resources/Mythwake/UI/HomeScreen/Generated`.

User wants next visible art improvements:
- UI icons.
- Fantasy backgrounds.
- Button/panel style.
- Placeholder heroes/enemies.
- Simple VFX for summon/dungeon/battle.
- Actual visible fights, even if placeholder 2D sprites first.

Important art stance:
- Free assets are okay for placeholder/testing.
- Final models/designs can be paid/commissioned later.
- Do not wait for final assets before making the game visually testable.

Good next asset tasks:
- Add a fantasy campaign/home background.
- Add a battle background.
- Add simple 2D hero/enemy sprites.
- Add simple idle/attack animation or tweening.
- Add floating damage/heal numbers.
- Add basic summon reveal VFX.
- Add dungeon popup visuals.

## Account/Auth Plan

Future login methods:
- Guest/dev currently exists.
- Email login later.
- Google login later.
- Apple login later.

Keep account architecture flexible:
- Account identity table should support multiple provider identities per player/account.
- Never hardwire only one login provider into player state.
- Auth provider definitions already exist conceptually in backend definitions.

## What Is Done Enough

Done or mostly done:
- Unity prototype with local save and server-shaped service boundaries.
- Go backend skeleton plus major server-authoritative gameplay route coverage.
- PostgreSQL schemas/migrations/definitions/player persistence.
- Optional Redis interfaces for sessions/rate limits/locks.
- Backend idempotency/action ledger/revisions/flush design.
- Server Mode in Unity.
- Local scripts for backend start and smoke checks.
- Home HUD first pass with custom navbar/topbar/buttons.
- Main screen cleanup from older prototype debug clutter.

Still rough:
- Unity UI is still runtime-built in one large controller.
- Campaign map and battle scene exist, but they are still runtime-composed and need proper background art/layout polish.
- Popups are placeholders.
- Hero/enemy visuals use free starter animated assets, not final Mythwake art direction.
- Dungeons have a first-pass separate screen and single-boss fights, but the screen still needs polish.
- Village has a first-pass map/building loop, but building effects/bonuses are still placeholders/not designed.
- Gear/Hero/Summon screens need real mobile layouts.
- No production auth providers yet.
- No purchase/monetization.
- No real item-instance inventory.
- No admin balance tooling.

## Next Practical Batch Plan

The next chat should continue in this order unless the user redirects:

1. Visually verify the current slice in Unity/editor or on device.
   - The latest batchmode Current Slice, Unity C# check, `go test ./...`, temporary no-DB `check-backend.cmd` on `http://localhost:18080`, and whitespace check passed after the backend Village definition pass.
   - Verify the connected Home upper/lower map, Village, Fast Rewards, Vanguard Oath/Summon result, Paladin formation/fight pose, and spacing on editor/device.

2. Finish the Home idle combat visual pass.
   - Verify the main campaign map and lower idle mini-map read as one continuous area.
   - Check foreground patrol spacing, reward tick pacing, stage-node preview readability, and Battle button overlap on editor/device.
   - Keep `Mythwake/Validate Home Idle Combat` updated if the Home layout shifts again.

3. Continue the Village building test slice.
   - Decide how AFK-rate Village bonuses should affect server-authoritative AFK rewards.
   - Keep local and Server Mode state display in sync; do not apply local bonuses on top of server-authoritative stats.
   - Tune bonus values only after a visual/emulator pass.

4. Refresh remaining docs only when the code changes again.
   - `README.md`, `docs/NEXT_CHAT_CONTEXT.md`, and `docs/CURRENT_STATUS.md` are now the main handoff notes.

5. Polish Fast Rewards.
   - Run `Mythwake/Validate Fast Rewards UI`.
   - Visually verify popup text and button spacing on editor/device.
   - Continuous accumulation display, 24h cap, local rates, Village bonus line, and Server Mode copy now have a validator, but still need a real visual pass.

6. Continue Paladin integration checks.
   - Run `Mythwake/Validate Summon UI`.
   - Run `Mythwake/Validate Paladin Integration`.
   - Run `Mythwake/Validate Paladin Spine Handoff`.
   - Visually verify roster/detail screen, formation, fight pose/preview, and summon result display.

7. Continue the Hero/Gear polish pass behind the new upgrade-clutter guard.
   - Run `Mythwake/Validate Upgrade Clutter` after layout changes.
   - Visually verify the 8-slot Hero Detail gear layout in Unity/emulator.
   - Gear builder defaults now match the current localized runtime control stack, including equipment names; keep future Gear UI rebuilds behind that validator.
   - Server Mode accessory removal now has an endpoint; next work can focus on UI feedback/spacing rather than missing backend plumbing.
   - Hero level-up belongs in Heroes or hero detail.
   - Weapon/Armor/accessory upgrades belong in Gear.
   - Battle screen should not contain upgrade clutter.

8. Keep backend tests green after client-facing changes.
   - Run Go tests if backend touched.
   - Run `check-backend` / `check-postgres-e2e` when backend contracts change.

8. Only after visible UI/battle is decent:
   - Decide final starter hero/enemy art direction.
   - Replace free starter assets with coherent production-style assets.
   - Start character/faction visual direction.

## Known User Requests To Preserve

- "Nicht Prototype quick and dirty, sondern dauerhaft game-studio-nah."
- "Praxisnah und professionell, wie high-end game studios."
- "Rollen erstmal auslassen."
- "Gold fuer Equipments oder so."
- "Hero-Level-Up eigene Myth-Waehrung."
- Myth Essence should be AFK/offline + dungeon based, not normal fight reward for now.
- Offline/AFK gives both Gold and Myth Essence.
- Dungeons should be tower-like and endless upward.
- Combat should be seconds-based, 30 seconds default.
- User wants visible in-game progress now.
- Normal play must never show rate-limit errors from fighting/running dungeons.
- No data loss ever after successful gameplay responses.

## Useful File Hotspots

Client:
- `Assets/_Mythwake/Scripts/IdlePrototypeController.cs`
- `Assets/_Mythwake/Scripts/MythwakeBackendClient.cs`
- `Assets/_Mythwake/Scripts/MythwakeServiceContracts.cs`
- `Assets/Scenes/SampleScene.unity`
- `Assets/_Mythwake/UI/Home Screen/bottom_navbar/`
- `Assets/_Mythwake/UI/Home Screen/generated/`
- `Assets/_Mythwake/Resources/Mythwake/UI/HomeScreen/Generated/`

Backend:
- `backend/cmd/api/main.go`
- `backend/internal/http/router.go`
- `backend/internal/player/`
- `backend/internal/gameplay/actions.go`
- `backend/internal/economy/currency.go`
- `backend/internal/balance/definitions.go`
- `backend/internal/definitions/catalog.go`
- `backend/internal/store/postgres/`
- `backend/internal/store/cache/write_behind_state_store.go`
- `backend/internal/database/migrations/`

Docs:
- `README.md`
- `docs/ROADMAP.md`
- `docs/UNITY_TEST_STAND.md`
- `backend/README.md`
- `docs/NEXT_CHAT_CONTEXT.md`

## Verification Notes

Before saying a batch is done:
- Check `git status --short`.
- If C# changed, at least run a compile check or open Unity if possible.
- If backend changed, run Go tests and relevant smoke scripts.
- If UI changed, visually inspect in Unity/emulator when possible.
- Avoid committing generated Unity Library/Temp files.

Recent status before creating this file:
- Working tree was clean.
- Latest push was successful to `hamzasnc/mythwake`.
- Current branch was `codex/batch-1-stabilize-prototype`.
