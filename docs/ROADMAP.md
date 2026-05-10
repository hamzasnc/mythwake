# Mythwake Roadmap

This document tracks the current prototype state and the useful next batches needed to reach a clean mobile idle RPG core with a custom Go backend, PostgreSQL, and Redis.

## Product Target

Mythwake is a mobile idle RPG inspired by games like AFK Arena and 7DS-style idle progression.

Target platforms:
- Android
- iOS

Core pillars:
- Short daily sessions
- Offline rewards
- Clear power progression
- Hero collection
- Gear farming
- Resource dungeons
- Long-term upgrade goals
- Server-authoritative economy before real monetization or public testing

## Current State

Client:
- Unity 6 mobile portrait prototype
- Android build profile working
- Versioned local JSON save data stored in `PlayerPrefs`
- Legacy scalar `PlayerPrefs` keys migrate into the JSON save on load
- Local economy boundary methods now handle currency spends, currency grants, and reward grants
- Shared service contracts define player state, rewards, action results, economy, battle, summon, and inventory boundaries
- Campaign fights, dungeon runs, and summons now return local action-result DTOs shaped like future backend responses
- Accessory equip, level, and fuse actions now return local action-result DTOs for future server-side inventory validation
- Hero progression, starter equipment upgrades, daily mission claims, and Mission Track claims now return local action-result DTOs
- Visible prototype/save version text
- Debug buttons for small Gold, Gems, Myth Essence, and accessory test amounts
- Code-side definition rows with stable IDs for early client balance data
- Bottom-tab app shell
- Current tabs: Home, Battle, Heroes, Gear, Summon, Shop

Progression:
- Campaign stages with enemy HP, enemy damage, recommended power, win/loss simulation
- Auto battle while app is open
- Offline Gold and Myth Essence rewards
- Gold Dungeon
- Essence Dungeon
- Gear Dungeon
- Campaign milestones every 5 stages
- Dungeon bonus floors every 5 floors
- Home screen `Next Goal` hint

Currencies:
- Gold
- Gems
- Myth Essence
- Pass XP
- Hero Shards

Heroes:
- 5 starter heroes
- Hero levels use Myth Essence
- Hero shards from summons
- Ascension consumes shards
- Hero stats affect team ATK, HP, and Power

Gear:
- Starter Weapon and Armor upgrades using Gold
- Accessory slots:
  - Ohrringe
  - Kette
  - Armband
  - Handschuhe
  - Schuhe
- Accessory rarities: R0-R4
- R0 max level 20
- Each rarity adds +10 max levels
- Slot/rarity accessory pairs have item-like definition rows with stable IDs, level caps, stat scaling, drop weights, and fuse targets
- Accessories can be equipped and leveled
- Gear Dungeon drops random accessory copies
- 3 copies of the same slot and rarity fuse into the next rarity
- Accessory stats affect team ATK, HP, and Power

Meta:
- Daily missions
- Mission Track rewards
- Basic summon flow with rarity rates
- Local-only prototype, no backend yet

Known design note:
- Role mechanics exist in prototype code, but roles are not a design focus right now. Do not deepen role-specific gameplay until real/free character assets and character kits are chosen.

## Definition Of A Clean Core

The core is considered clean when these are true:
- A new player understands what to do from the UI without explanation.
- Campaign eventually walls the player.
- The player farms the correct dungeon to solve the wall.
- Gold has meaningful use through equipment/accessory leveling.
- Myth Essence has meaningful use through hero leveling.
- Gems have meaningful use through summons.
- Shards have meaningful use through ascension/future hero ownership.
- Gear Dungeon creates repeatable long-term goals.
- Fusion creates a useful duplicate sink.
- Offline rewards feel useful but not stronger than active dungeon progress.
- Save/load is reliable.
- Balance values are centralized enough to later move into config tables.
- The client can be migrated to server-authoritative state without redesigning every system.

## Batch Plan

### Batch 1: Stabilize The Current Prototype

Goal:
Make the existing client loop easier to test and less fragile.

Progress:
- Prototype version/save version text is visible in the UI.
- Debug resource buttons can add small Gold, Gems, Myth Essence, and selected accessory copy test amounts.
- Reset now deletes known local prototype save keys before writing a fresh save.
- Dungeon labels/result text are more compact for mobile readability.

Tasks:
- Add a basic debug/reset panel or debug buttons for adding small test amounts of resources.
- Add visible save version text.
- Add a simple changelog/version field in the UI or README.
- Make sure all tabs fit on mobile resolution after adding Gear.
- Check that long texts do not overlap.
- Make Gear Dungeon, Gold Dungeon, Essence Dungeon result text readable.
- Verify reset fully clears accessories, dungeons, heroes, mission track, and currencies.

Done when:
- A fresh reset can run through Campaign, Dungeons, Gear drops, Equip, Level, Fuse, Summon, Daily, and Mission Track without broken UI references.

### Batch 2: Data Definitions In Client

Goal:
Move core balancing data away from scattered methods and toward table-like definitions.

Progress:
- Added ID-based definition structs for currencies, heroes, rewards, daily missions, mission track rewards, dungeons, accessory slots, accessory rarities, summon rates, and summon banners.
- Added `stage_id`, `hero_id`, `currency_id`, `item_slot_id`, `rarity_id`, `dungeon_id`, and `reward_id` style fields in code-side data rows.
- Replaced parallel hero, daily mission, battle pass, summon rate, dungeon scaling, and accessory rarity arrays with definition-driven lookups where practical.

Tasks:
- Create data structs/classes for:
  - Currencies
  - Hero definitions
  - Stage definitions
  - Dungeon definitions
  - Accessory slot definitions
  - Accessory rarity definitions
  - Reward definitions
  - Summon banner definitions
- Keep them in code for now, but shape them like database rows.
- Replace hardcoded formula islands where it makes sense.
- Add IDs for important definitions:
  - `hero_id`
  - `currency_id`
  - `item_slot_id`
  - `rarity_id`
  - `dungeon_id`
  - `reward_id`

Done when:
- Most progression data could be copied into PostgreSQL tables without redesigning names and relationships.

### Batch 3: Real Item Model Preparation

Goal:
Prepare the accessory system for real inventory later.

Progress:
- Added item-like `AccessoryDefinition` rows for every current slot/rarity pair while keeping the current copy-count inventory.
- Accessory definitions now carry slot, rarity, level cap, attack scaling, health scaling, drop weight, and fuse target data.
- Gear Dungeon drops, accessory stat calculation, inventory indexing, and fusion now route through accessory definitions.

Current state:
- Accessories are stored as copy counts per slot and rarity.

Next useful step:
- Keep copy-count inventory for now, but introduce item-like definitions:
  - slot
  - rarity
  - level cap
  - attack scaling
  - health scaling
  - drop weight
  - fuse target
- Add a clear distinction between:
  - definition data
  - player-owned counts
  - equipped state

Done when:
- The current copy-count system can later become real item instances without changing all UI and combat code.

### Batch 4: Balance Pass 1

Goal:
Make the first 30-60 minutes of progression feel intentional.

Progress:
- Added a first campaign balance definition row for recommended power, enemy damage, milestone rewards, and overflow growth.
- Tuned Gold, Essence, and Gear Dungeon difficulty/reward curves against current upgrade costs.
- Adjusted summon cost, starter Gems, daily Gem income, daily mission targets, gear drop weights, and offline reward cap.

Tasks:
- Tune Campaign HP/damage growth.
- Tune Gold Dungeon rewards against Weapon/Armor/accessory costs.
- Tune Essence Dungeon rewards against hero level costs.
- Tune Gear Dungeon difficulty and rarity drop rates.
- Tune summon cost and Gem income.
- Tune daily missions so they support normal play instead of forcing weird behavior.
- Tune offline reward cap.

Done when:
- Campaign push, fail, dungeon farming, upgrades, and retrying all feel connected.

### Batch 5: Save System Cleanup

Goal:
Stop relying on scattered `PlayerPrefs` keys before the save grows too much.

Progress:
- Introduced local save version 2.
- Added a single `PrototypeSaveData` JSON blob stored under one `PlayerPrefs` key.
- Legacy scalar `PlayerPrefs` saves still load and migrate through the normal save path.
- Save/load now copies fixed-size arrays for heroes, accessories, daily missions, and mission track rewards into one state object.
- Offline reward timing now reads from the loaded save state instead of reaching directly into scattered keys.

Tasks:
- Introduce a save version number.
- Centralize save/load key names.
- Add migration handling for future save changes.
- Consider serializing a single local save JSON blob for prototype clarity.
- Keep `PlayerPrefs` as storage backend for now.

Done when:
- Adding a new field does not require hunting through many save/load sections.

### Batch 6: Backend Readiness Layer

Goal:
Create the client boundary that later talks to the backend.

Progress:
- Added central local economy methods:
  - `TrySpendCurrency`
  - `GrantCurrency`
  - `GrantReward`
- Routed hero leveling, equipment leveling, accessory leveling, summons, dungeon rewards, offline rewards, debug rewards, campaign milestones, daily claims, and mission track claims through those methods.
- Added a compact player state snapshot shape for future load/state endpoint work.
- Added shared service contract DTOs/interfaces for player state, economy, battle, summons, and inventory.
- The prototype controller now exposes player state and local economy service methods using those contracts.
- Campaign fights, resource dungeons, Gear Dungeon, and summons now route through local action-result methods with success/error/message/player-state output.
- Accessory equip, accessory leveling, and accessory fusion now route through inventory service action-result methods.
- Hero leveling, hero ascension, starter equipment leveling, daily claims, and Mission Track claims now route through progression/mission service action-result methods.
- Expanded the Unity service contracts to full player snapshots matching the Go backend.
- Added a Unity `MythwakeBackendClient` for health, guest auth, player snapshots, and all current action endpoints.
- Added a runtime Shop-tab backend panel for Ping/Login/Sync smoke tests without making the local prototype depend on the server.
- Added Local/Server mode in the backend panel.
- Manual gameplay buttons can now route through the backend in Server Mode:
  - campaign fight
  - resource and gear dungeons
  - hero level/ascend
  - starter equipment level
  - accessory equip/level/fuse
  - summons
  - daily mission and Mission Track claims
- Auto Attack is intentionally paused in Server Mode until AFK claims are server-designed.

Tasks:
- Add service-style classes/interfaces for:
  - player state
  - rewards
  - battle results
  - inventory
  - summons
  - daily reset
- Local implementation uses current save.
- Future implementation calls backend.
- Real HTTP exists now as a smoke-test layer, with manual gameplay actions routed through it behind a Local/Server toggle.

Done when:
- The gameplay code no longer directly owns every economy decision.
- Server snapshots can refresh the client UI without manual JSON inspection.

## Backend Plan

Backend stack:
- Go
- PostgreSQL
- Redis

Current backend state:
- Added initial `backend/` Go module.
- Added `cmd/api` entrypoint.
- Added environment-based config.
- Added standard-library HTTP router.
- Added `GET /health`.
- Added shared API response types for player state, rewards, and action results.
- Added dev `GET /player/state` endpoint.
- Added in-memory guest auth endpoint.
- Added in-memory action endpoints for campaign, dungeons, heroes, equipment, accessories, summons, missions, and Mission Track.
- Added HTTP route tests for health, guest auth, campaign fight, and accessory request validation.
- Added graceful shutdown.
- Added local Docker Compose service for PostgreSQL.
- Added optional PostgreSQL connection through `MYTHWAKE_DATABASE_URL`.
- Added embedded SQL migrations.
- Added first core PostgreSQL tables for players, player state snapshots, economy transactions, currencies, dungeons, accessory slots, accessory rarities, and accessory definitions.
- Seeded the current currency, dungeon, accessory slot, rarity, and accessory definition IDs.
- Added first PostgreSQL player state snapshot store for the dev player.
- Added normalized player progression tables for currencies, campaign progress, dungeon progress, hero levels, hero ascensions, and hero shards.
- Backend load/save now uses normalized progression tables first and keeps the JSON snapshot as fallback/debug mirror.
- Added PostgreSQL schemas for account, common definitions, player state, logs, and debug views.
- Added Navicat-friendly debug views for player overview, hero overview, and economy history.
- Added economy transaction logging for currency deltas.
- Added normalized accessory inventory/equipped persistence and a debug accessory overview view.
- Fixed accessory fusion to produce the next real rarity ID instead of a placeholder `_fused` ID.
- Added persistent starter equipment training and a debug equipment overview view.
- Added persistent summon count, daily mission claims, Battle Pass claims, summon history, and debug claim/summon views.
- Expanded `GET /player/state` into a client-ready player snapshot containing core state, heroes, equipment, accessories, claims, and summon count.
- Added the same full player snapshot to guest auth and action responses so the client can refresh UI from a single response.
- Unity can now ping the backend, guest-login, and apply `/player/state` snapshots through the ingame Backend panel.
- Unity Server Mode can now execute manual gameplay actions through the backend and refresh from the returned `playerSnapshot`.
- Added a durable state cache wrapper in front of PostgreSQL.
- Server gameplay actions now update hot server state first, synchronously write a durable action/result ledger, then queue materialized player state for batched flush by default.
- Full write-through mode exists for debugging, and local/dev write-behind mode remains available for experiments.
- Added `POST /player/state/flush` as a future app-pause/disconnect hook.
- Added durable idempotent action results through `Idempotency-Key`.
- Gameplay mutation endpoints now require valid `Idempotency-Key` headers by default.
- Gameplay action IDs now live in a central backend action catalog for routing, ledgers, persistence sources, and tests.
- Successful idempotent action results save in `player.player_action_results`.
- Per-action economy deltas save in `logs.player_action_ledger`.
- Startup restores from the latest durable action result snapshot if materialized tables are behind.
- Unity Server Mode now sends and reuses pending idempotency keys for gameplay actions after transport failures.
- Redis is not connected yet.

Recommended Go shape:
- `cmd/api`
- `internal/config`
- `internal/http`
- `internal/auth`
- `internal/player`
- `internal/economy`
- `internal/heroes`
- `internal/items`
- `internal/dungeons`
- `internal/campaign`
- `internal/summons`
- `internal/missions`
- `internal/store/postgres`
- `internal/store/cache`
- `internal/cache/redis`

Backend should become authoritative for:
- Account state
- Player currencies
- Reward claims
- Summons
- Inventory
- Gear drops
- Fusion
- Level-up costs
- Daily reset
- Battle pass / mission track
- Purchases later

Backend should not initially simulate every combat frame.
For MVP, the server can validate combat using deterministic formulas and player power snapshots.

## PostgreSQL Plan

Use PostgreSQL for durable state and definitions.

Definition tables:
- `currency_definitions`
- `hero_definitions`
- `hero_rarity_definitions`
- `campaign_stage_definitions`
- `dungeon_definitions`
- `dungeon_floor_scaling_definitions`
- `item_slot_definitions`
- `item_rarity_definitions`
- `accessory_definitions`
- `summon_banner_definitions`
- `summon_rate_definitions`
- `reward_definitions`
- `mission_definitions`
- `battle_pass_track_definitions`

Player state tables:
- `players`
- `player_auth_identities`
- `player_currencies`
- `player_campaign_progress`
- `player_dungeon_progress`
- `player_heroes`
- `player_hero_shards`
- `player_equipment_training`
- `player_accessory_inventory`
- `player_equipped_accessories`
- `player_daily_missions`
- `player_battle_pass`
- `player_reward_claims`
- `player_summon_history`
- `player_action_results`
- `player_transactions`

Important rules:
- Use integer currency amounts.
- Never trust client-submitted reward amounts.
- Store reward claim IDs to avoid duplicate claims.
- Require idempotency keys for retryable economy actions before production.
- Keep definition IDs stable.
- Prefer append-only transaction history for economy-affecting actions.

## Redis Plan

Use Redis for fast temporary state, not durable core state.

Good Redis uses:
- Session tokens
- Login rate limits
- API rate limits
- Daily reset cache
- Leaderboard snapshots
- Event counters
- Temporary battle validation nonces
- Short-lived claim locks

Avoid Redis for:
- Permanent currency balances
- Permanent inventory
- Purchase records
- Anything that must survive cache loss

Current cache stance:
- The current Go cache wrapper is an in-process MVP for one API instance.
- Default mode is `ledger_write_behind`: critical economy actions write a durable action ledger/result before success is returned, while materialized player tables flush in batches.
- Full `write_through` mode is available for debugging direct table writes.
- Plain `write_behind` mode is not safe for production economy state unless every critical action also has a durable ledger/result.
- Gameplay mutations require valid idempotency keys by default; disabling that is a local debugging escape hatch only.
- Redis later should handle cross-process sessions, locks, rate limits, and short-lived coordination.
- PostgreSQL remains the durable source of truth for player economy and inventory.

## Engineering Standard

Build Mythwake as a long-lived game core, not as disposable quick-and-dirty code.

Rules for future batches:
- Mutating server actions must be replay-safe, idempotent, and validated before touching player state.
- The backend owns rewards, spends, drops, claims, and inventory transitions.
- PostgreSQL remains durable truth; cache layers may improve throughput but must not be required to recover permanent player state.
- Definition IDs must remain stable and table-shaped so balance can move into SQL/admin tooling.
- New gameplay systems should ship with focused tests for economy, persistence, and invalid-state cases.
- Local/dev shortcuts are allowed only behind explicit config or debug UI.

## Server MVP Timing

Do not build the backend before the client data boundaries exist.

Start backend after:
- Batch 2 is done
- Batch 5 is done or mostly done
- Reward/economy flows are stable enough to name properly

Minimum backend MVP:
- Register/login as guest account
- Load player state
- Save player state
- Claim dungeon reward
- Level hero
- Level equipment/accessory
- Fuse accessory
- Perform summon
- Claim daily mission

Keep the first backend intentionally small.

## API Draft

Initial endpoints:
- `POST /auth/guest`
- `GET /player/state`
- `POST /player/state/flush`
- `POST /campaign/fight`
- `POST /dungeons/{dungeon_id}/run`
- `POST /heroes/{hero_id}/level-up`
- `POST /heroes/{hero_id}/ascend`
- `POST /gear/accessories/equip`
- `POST /gear/accessories/level-up`
- `POST /gear/accessories/fuse`
- `POST /summons/{banner_id}/pull`
- `POST /missions/{mission_id}/claim`
- `POST /battle-pass/{reward_id}/claim`

Response style:
- Return updated player state slices after each action.
- Return server-generated rewards.
- Return error codes for insufficient currency, invalid state, already claimed, and stale client state.

## Migration Strategy

Current local save:
- `PlayerPrefs`

Next:
- Add save versioning.
- Reshape data into local state objects.
- Keep local prototype working.

Backend migration:
- On first backend login, upload local progress only for development builds.
- For public builds, new accounts should start server-authoritative.
- Do not allow arbitrary local save import in production.

## PostgreSQL Batch State

Goal:
Create the durable database foundation before moving individual economy actions server-side.

Progress:
- Added `docker-compose.yml` with local PostgreSQL.
- Added `backend/internal/database` with PostgreSQL open/ping and embedded migration runner.
- Added migration `0001_core.sql`.
- Added migration `0002_player_progress.sql`.
- Added definition seed tables for current currencies, dungeons, accessory slots, accessory rarities, and accessory definitions.
- Added normalized player progression tables for currencies, campaign, dungeons, heroes, ascensions, and hero shards.
- Kept `player_state_snapshots` as a fallback/debug mirror.
- Added `backend/internal/store/postgres` with a player state store.
- Server startup now connects, migrates, and wires persistence when `MYTHWAKE_DATABASE_URL` is set.
- Existing backend tests still run without a database.
- Local PostgreSQL smoke test confirmed state survives an API restart.
- Added schema namespace migration:
  - `account`
  - `common`
  - `player`
  - `logs`
  - `debug`
- Added `debug.v_player_overview`, `debug.v_player_hero_overview`, and `debug.v_player_economy_overview`.
- Added economy transaction logs for currency delta inspection.
- Added `player.player_accessory_inventory`, `player.player_equipped_accessories`, and `debug.v_player_accessory_overview`.
- Added `player.player_equipment_training` and `debug.v_player_equipment_overview`.
- Added `player.player_summon_state`, `player.player_daily_mission_claims`, `player.player_battle_pass_claims`, `logs.summon_history`, `debug.v_player_claim_overview`, and `debug.v_player_summon_overview`.
- Added state cache wrapper with ledger write-behind default, optional full write-through mode, and graceful shutdown flush.
- Added a manual player state flush endpoint for client disconnect/app-pause flows.
- Added `player.player_action_results` and `debug.v_player_action_result_overview` for idempotent action replay.
- Added `logs.player_action_ledger` and `debug.v_player_action_ledger_overview` for per-action durable economy deltas.
- Default persistence mode is now durable action ledger plus batched materialized state flush.
- Added required idempotency headers and validation for gameplay mutation endpoints.
- Added `internal/gameplay` as the first action catalog slice before splitting the large player service into domain services.

Next useful step:
- Split backend gameplay services out of the current single `player.Service` while keeping the API behavior stable:
  - economy/reward validation
  - dungeons
  - hero progression
  - equipment/accessories
  - missions/summons
- Add Redis after the first PostgreSQL path is stable.

Done when:
- A local Postgres-backed API can restart without losing core player state.
- Definition and player progression rows exist in SQL with stable IDs.
- The next Redis/session batch can start without touching core persistence setup.

## Useful Next Batches

Recommended order:

1. Stabilize current UI and save reset.
2. Create client definition structs with stable IDs.
3. Clean local save into versioned state.
4. Tune first 30-60 minutes.
5. Add local service layer.
6. Start Go backend skeleton. (done)
7. Add PostgreSQL definitions and player state tables. (first pass done)
8. Add Redis for sessions/rate limits.
9. Move economy actions server-side one by one.
10. Prepare for real assets and better UI.

## Not Yet

Avoid for now:
- PvP
- Guilds
- Real-money shop
- Complex substats
- Full item instance inventory
- Advanced role kits
- Live events
- Admin panel
- Anti-cheat beyond basic server authority

These matter later, but they would slow down the clean core right now.

## Current Priority

The best next step is:

Harden the server-authoritative core before adding more gameplay breadth.

That means:
- Keep every economy mutation replay-safe.
- Split the large backend player service into focused domain services.
- Move definition/balance ownership toward PostgreSQL-backed tables.
- Add Redis only for sessions, locks, rate limits, and other temporary coordination.
- Keep Unity Server Mode working after each backend batch.
