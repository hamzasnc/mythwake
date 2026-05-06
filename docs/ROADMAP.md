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
- Local `PlayerPrefs` save data
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
- Do not build real HTTP yet unless the client boundary is stable.

Done when:
- The gameplay code no longer directly owns every economy decision.

## Backend Plan

Backend stack:
- Go
- PostgreSQL
- Redis

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
- `player_transactions`

Important rules:
- Use integer currency amounts.
- Never trust client-submitted reward amounts.
- Store reward claim IDs to avoid duplicate claims.
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

## Useful Next Batches

Recommended order:

1. Stabilize current UI and save reset.
2. Create client definition structs with stable IDs.
3. Clean local save into versioned state.
4. Tune first 30-60 minutes.
5. Add local service layer.
6. Start Go backend skeleton.
7. Add PostgreSQL definitions and player state tables.
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

Stabilize and data-shape the current prototype before adding more features.

That means:
- Make current systems reliable.
- Make current data table-like.
- Make saves cleaner.
- Tune the loop.
- Then start backend.
