# Mythwake Next Chat Context

Last updated: 2026-05-14

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
- Pushes/commits should use account/author `hamzasnc`, not `devasperity`.

Latest known pushed commit at time of this file:
- `d71f0a5 Refine home HUD layout and shortcuts`

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
- `C:\Users\Hamza\Desktop\Idle Game\Mythwake`

Key Unity scene:
- `Assets/Scenes/SampleScene.unity`

Core runtime script:
- `Assets/_Mythwake/Scripts/IdlePrototypeController.cs`

Current client version:
- Prototype `0.2.73`
- Save version `2`

Important Unity scripts:
- `Assets/_Mythwake/Scripts/IdlePrototypeController.cs`

Latest local gameplay/UI batch:
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
- Campaign/Village fights can now keep running while the player leaves through the bottom nav to edit heroes/gear, then pressing Village/Battle resumes the active fight/result. Team formation changes during such a fight abort the current fight without granting rewards. Dungeon fights/formations hide the top resource bar and bottom navbar for a focused boss-fight view.
- Visible fight end conditions now stop immediately once the winning side has killed/disappeared all enemies, instead of waiting out the remaining visual duration while the displayed damage total keeps climbing.
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
- `go test ./internal/balance ./internal/player ./internal/http` passes from `backend/`.
- `dotnet msbuild Assembly-CSharp.csproj /p:FrameworkPathOverride="C:\Users\Hamza\.nuget\packages\microsoft.netframework.referenceassemblies.net471\1.0.3\build\.NETFramework\v4.7.1" /v:minimal` passes with existing Unity serialized-field warnings.
- `git diff --check` passes for touched client/backend/docs files, with existing LF->CRLF warnings on some backend/docs files.
- Direct `dotnet build` fails on this machine because .NET Framework 4.7.1 reference assemblies are not installed globally.
- MSBuild can compile Unity csproj files when passed a temp ReferenceAssemblies path from `Microsoft.NETFramework.ReferenceAssemblies.net471`.
- `Assembly-CSharp.csproj` compiled successfully this way after the combat changes.
- `Assembly-CSharp-Editor.csproj` compiled successfully this way, with existing Unity package/reference warnings.
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
- Dungeons should later live in their own menu/tab, not clutter battle.

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
  - Village -> Home
  - Center/Campaign -> Home
  - Dungeons -> Battle/Dungeons area for now
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
- Verify collapsed shortcut layout in Unity/emulator visually.
- Make side shortcut expand/collapse polished with proper arrow art instead of text.
- Add real background map/campaign art to the empty main field.
- Replace placeholder popups with proper parchment/fantasy panels.
- Move Dungeons into its own clean screen/tab.
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

## Local Development Commands

Repo root:

```powershell
cd "C:\Users\Hamza\Desktop\Idle Game\Mythwake"
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

Manual Go run with DB:

```powershell
cd "C:\Users\Hamza\Desktop\Idle Game\Mythwake\backend"
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
- Dungeons have formation and single-boss fights, but not yet a polished separate screen.
- Gear/Hero/Summon screens need real mobile layouts.
- No production auth providers yet.
- No purchase/monetization.
- No real item-instance inventory.
- No admin balance tooling.

## Next Practical Batch Plan

The next chat should continue in this order unless the user redirects:

1. Verify the latest Home HUD visually in Unity/emulator.
   - Confirm collapsed side shortcuts show only first icon.
   - Confirm name/power/resources positions are acceptable.
   - Confirm no main screen overlap.
   - Confirm campaign map, stage preview, formation, and visible fights fit the mobile viewport.

2. Polish the new combat/map visuals.
   - Replace runtime-painted campaign/fight backdrops with better fantasy art.
   - Inspect melee pathing, HP bar positions, and projectile timing.
   - Add more varied melee/ranged enemy animations if suitable free packs are found.
   - Keep Dante and Iron Hound facing verified.

3. Split Dungeons into a real screen.
   - Bottom navbar Dungeons should open Dungeons screen.
   - Show Gold, Essence, Gear dungeon cards/floors/reward preview.
   - Keep run buttons and result text there.

4. Move upgrades into proper screens.
   - Hero level-up belongs in Heroes or hero detail.
   - Weapon/Armor/accessory upgrades belong in Gear.
   - Battle screen should not contain upgrade clutter.

5. Deepen visible battle mechanics.
   - Preserve nearest-target single-hit behavior for normal attacks.
   - Add explicit AoE skill flags later, only when skills exist.
   - Add cast/hit timing so damage numbers land at impact, not just action start.
   - Start separating visual combat state from aggregate backend combat results.

6. Make Fast Rewards real enough for testing.
   - Continuous accumulation display.
   - Rate per second based on stage.
   - Cap at 24h.
   - Claim updates Gold and Myth Essence.
   - In Server Mode, route to server claim behavior where possible.

7. Keep backend tests green after client-facing changes.
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
