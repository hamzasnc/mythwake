# Mythwake

Mobile idle RPG prototype built with Unity.

Prototype version: 0.2.11
Local save version: 2

Current prototype:
- Android build profile
- Simple portrait UI
- First core loop: fight enemies, earn Myth Essence, upgrade heroes
- Auto attack while the app is open
- Local save data via a versioned JSON blob stored in PlayerPrefs
- Legacy PlayerPrefs scalar keys are migrated into the JSON save on load
- Currency spend/grant actions now go through local economy boundary methods for backend migration
- Shared service contracts now define player state, reward, action result, economy, battle, summon, and inventory boundaries
- Unity has a backend client component for health, guest auth, player snapshot sync, and server action endpoints
- Shop tab creates a small runtime Backend panel for Ping, Login, Sync, and Local/Server gameplay mode smoke tests
- Server Mode routes manual gameplay buttons through the Go backend and applies the returned player snapshot
- Auto Attack stays local-only for now and is paused while Server Mode is active
- Campaign fights, dungeon runs, and summons now return local action-result DTOs
- Accessory equip, level, and fuse actions now return local action-result DTOs
- Hero leveling, hero ascension, equipment leveling, daily claims, and mission track claims now return local action-result DTOs
- Basic offline Gold and Myth Essence calculation when reopening the app
- Mobile app shell with Home, Battle, Heroes, Gear, Summon, and Shop tabs
- Visible prototype/save version text for quick test builds
- Debug resource buttons for adding small Gold, Gems, Myth Essence, and accessory test amounts
- Starter hero collection with 5 heroes
- Each starter hero has role-flavored Attack and HP stats
- Team Attack and Team HP are summed from the active hero roster
- Starter roles now affect combat: Warrior and Mage add damage, Tank reduces incoming damage, Support heals, Ranger executes weak enemies
- Gold, Gems, Myth Essence, Pass XP, and Hero Shards are separated
- Gold can be spent on starter Weapon and Armor equipment upgrades
- Equipment upgrades are saved locally and add team-wide ATK and HP
- Starter equipment balance is defined through small data structs to prepare for later server/database config
- Core prototype balance is being reshaped into code-side definition rows with stable IDs for later database migration
- Accessory gear system with Ohrringe, Kette, Armband, Handschuhe, and Schuhe slots
- Accessory slot/rarity pairs now have item-like definitions with stable IDs, level caps, stat scaling, drop weights, and fuse targets
- Accessory items have rarity tiers R0-R4, can be equipped, leveled, and saved locally
- Accessory max level starts at 20 for R0 and increases by 10 per rarity tier
- Gear Dungeon drops random accessory copies
- Three copies of the same slot and rarity can be fused into the next rarity
- Hero upgrades use Myth Essence and are saved locally
- Home screen shows a Next Goal hint for the current progression bottleneck
- Campaign stages now use named enemy data with HP and progression-only clears
- Campaign and dungeon scaling now create clearer upgrade walls with recommended power labels
- First balance pass tunes Campaign pressure, dungeon rewards, summon pacing, daily missions, and offline reward caps for the early loop
- Campaign milestones reward Gems and Mission Track XP every 5 stages
- Campaign continues scaling after the starter stages
- Gold Dungeon and Essence Dungeon are endless tower prototypes
- Dungeon bonus floors pay extra resources every 5 floors
- Dungeon floors scale up in enemy HP, enemy damage, and rewards
- Campaign and dungeon fights now simulate win/loss with team HP and enemy damage
- Basic summon flow with Gem cost, rarity rates, hero shards, and saved summon count
- Hero shards add minor Attack and HP immediately
- Hero ascension consumes shards for larger saved stat upgrades
- Daily missions track battles, stage clears, and summons
- Daily mission claims reward Gold, Gems, Myth Essence, and reset by UTC day
- Mission Track XP is earned from daily mission claims
- Mission Track rewards can be claimed in the Shop tab

Backend:
- Go API skeleton in `backend/`
- Standard-library HTTP server
- Config via environment variables
- `GET /health`
- `GET /player/state` dev response using the same player-state shape as the Unity service contract
- In-memory pre-PostgreSQL action endpoints for auth, campaign, dungeons, heroes, equipment, accessories, summons, missions, and mission track
- Optional PostgreSQL connection using `MYTHWAKE_DATABASE_URL`
- Embedded PostgreSQL migrations for first core tables and definition seeds
- PostgreSQL player progress tables for currencies, campaign, dungeons, heroes, ascensions, and hero shards
- JSON player state snapshot remains as a debug/fallback mirror
- PostgreSQL schemas split tables into `account`, `common`, `player`, `logs`, and `debug`
- Navicat-friendly debug views expose player overview, hero overview, and economy history
- Gear/accessory inventory, accessory levels, and equipped accessories persist in PostgreSQL
- Starter Weapon and Armor training levels persist in PostgreSQL and affect team power
- Summon count, daily mission claims, and Battle Pass claims persist in PostgreSQL
- Claim and summon debug views are available for Navicat
- PostgreSQL writes now sit behind a durable state cache wrapper
- Critical server actions now use ledger write-behind by default, so the action/result is durable before success while materialized state can flush in batches
- Full write-through remains available as a debug mode
- Gameplay server actions require a valid `Idempotency-Key` by default, making timeout/retry flows part of the API contract instead of an optional client habit
- Successful idempotent action results are stored in `player.player_action_results`
- Per-action economy deltas are stored in `logs.player_action_ledger`
- Backend gameplay action IDs now live in a central action catalog instead of being scattered string literals
- Backend currency spends, grants, display names, and deltas now live in a central economy package
- Unity reuses pending idempotency keys after transport failures
- `POST /player/state/flush` is ready as the future app-pause/disconnect save hook
- `/player/state` returns a full client-ready player snapshot with heroes, equipment, accessories, claims, and summon count
- Guest auth and action responses include the full player snapshot for direct client UI updates
- Android emulator builds use `http://10.0.2.2:8080` as the default backend URL, while Editor/Desktop use `http://localhost:8080`
- Redis is planned but not connected yet
- Windows helper scripts:
  - `scripts/start-backend.cmd`
  - `scripts/check-backend.cmd`

Changelog:
- Backend 0.2.15: Added a central backend economy package for currency IDs, spends, grants, and delta calculation.
- Backend 0.2.14: Added a central gameplay action catalog and refactored HTTP gameplay handlers through one mutation validation path.
- Backend 0.2.13: Required idempotency headers for gameplay mutations, added key validation, health visibility, script support, and HTTP/config tests.
- Backend 0.2.12: Switched default persistence to durable action ledger plus materialized state write-behind flush.
- Backend 0.2.11: Added durable idempotent action results, replay-safe server action routing, and Unity pending action keys.
- Backend 0.2.10: Added durable player state cache wrapper, write-through default saves, optional write-behind mode, cache health fields, manual flush endpoint, and shutdown flushing.
- 0.2.11: Added Server Mode routing for manual gameplay buttons through the Unity backend client.
- 0.2.10: Added Unity backend HTTP client, full local player snapshots, and an ingame Backend panel for Ping/Login/Sync smoke tests.
- Backend 0.2.9: Added full player snapshots to guest auth and action responses.
- Backend 0.2.8: Expanded `/player/state` into a client-ready player snapshot response.
- Backend 0.2.7: Added PostgreSQL starter equipment training persistence and equipment debug views.
- Backend 0.2.6: Added PostgreSQL persistence for summon count, daily claims, Battle Pass claims, and claim/summon debug views.
- Backend 0.2.5: Added PostgreSQL accessory inventory/equipment persistence and fixed accessory fusion target IDs.
- Backend 0.2.4: Added PostgreSQL schemas, debug views, and economy transaction logging.
- Backend 0.2.3: Added normalized PostgreSQL player progression tables and load/save flow for core player state.
- Backend 0.2.2: Added Windows helper scripts to start and smoke-test the local backend without Docker.
- Backend 0.2.1: Added PostgreSQL connection, migrations, definition seed tables, Docker Compose Postgres, and player state snapshot persistence.
- Backend 0.2.0: Added in-memory action endpoints up to the PostgreSQL boundary.
- Backend 0.1.1: Added shared API response types and a dev `GET /player/state` endpoint.
- Backend 0.1.0: Added the first Go API skeleton with config, health endpoint, and graceful shutdown.
- 0.2.9: Routed hero progression, starter equipment upgrades, daily mission claims, and Mission Track claims through service action-result methods.
- 0.2.8: Routed accessory equip, level, and fusion through inventory service action-result methods for backend-ready item validation.
- 0.2.7: Routed campaign fights, dungeon runs, and summons through local action-result methods that match the future backend response shape.
- 0.2.6: Added shared service contracts and made the prototype controller expose player state/economy services for the future backend boundary.
- 0.2.5: Started backend-readiness by routing currency spends, reward grants, dungeon rewards, offline rewards, debug rewards, and summons through centralized economy methods.
- 0.2.4: Cleaned the local save into one versioned JSON save blob with legacy PlayerPrefs migration, preparing the client for service/backend boundaries.
- 0.2.3: Tuned early Campaign, dungeon, summon, daily mission, gear drop, and offline reward pacing for the first Balance Pass.
- 0.2.2: Added item-like accessory definitions while keeping the current copy-count inventory, so drops, stats, and fusion now have stable accessory IDs for future item instances.
- 0.2.1: Started Batch 2 data shaping with ID-based client definitions for currencies, heroes, stages, dungeons, rewards, missions, accessory slots/rarities, and the summon banner.
- 0.2.0: Added visible prototype/save version UI, Batch 1 debug resource buttons, compact dungeon labels, and a reset path that clears known local prototype save keys before writing a fresh save.

Development target:
- Android and iOS mobile idle game
- Unity client
- Backend planned separately
