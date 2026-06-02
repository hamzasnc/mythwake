# Mythwake Hero Progression

Last updated: 2026-06-01

## Current Rules

- Normal Hero level cap: `100`.
- Awakening stages: `0-10`.
- Awakening is locked until the hero is Lv. `100`.
- Hero leveling costs Myth Essence.
- Awakening costs Awakening Shards.
- First Awakening cost: `20` Awakening Shards.
- Additional Awakening cost scaling: `+15` Awakening Shards per current Awakening stage.
- Current Awakening bonuses are per-hero ATK and HP from shared hero definitions.
- Hero Star levels are separate from Awakening. Every hero starts at star level `0`, can reach star level `5`, and uses that hero's own shards.
- Hero Star costs start at `5` hero-specific shards, then `10`, `15`, `20`, and `25`.
- Hero Shard Chests are openable items that grant shards for one hero.
- Shard Rift is the first source for Awakening Shards and Hero Shard Chests. It is an endless dungeon and keeps rewards from defeated enemies even after defeat or manual end.
- Future bonus slots can add Crit, Defense, Skill, or role bonuses without changing the Lv. 100 gate.

## Compatibility Note

The UI and player-facing copy now say `Awakening`. Some persistence/API fields still use the older `ascension` name so existing saves, PostgreSQL rows, DTOs, and legacy clients remain compatible. The backend accepts both:

- `POST /heroes/{hero_id}/awaken`
- `POST /heroes/{hero_id}/ascend`

Both routes apply the same Awakening rules.

## Client/Backend Parity

- Client fallback max level is `100`.
- Backend hero definitions have `MaxLevel: 100`.
- Client fallback max Awakening is `10`.
- Backend hero definitions have `MaxAscension: 10`.
- Client fallback Hero level cost mirrors backend `hero_level_any`: `14 + current_level * 6`.
- Client fallback Awakening shard cost still mirrors backend `hero_ascension_any`: `20 + current_awakening * 15`, paid from Awakening Shards.
- Client local stat formulas mirror backend definitions for base stats, per-level growth, per-Awakening ATK/HP, and Hero Star ATK/HP/Defense-style bonuses.
- Backend/PostgreSQL now persists Awakening Shards, Hero Star levels, Hero Shard Chests, and Shard Rift best/total kill progress for Server Mode and Email accounts.

## Hero Detail UX

- Below Lv. 100, the main action remains `Level Up`.
- Hero-specific shards are shown as Star progress and can be spent before or after Lv. 100.
- At Lv. 100, the main action becomes `Awaken` when the hero is below Awakening 10.
- Missing resources should name the current gate clearly: Level, Myth Essence, Awakening Shards, Hero Shards, or Hero Shard Chest.
- At Awakening 10, the hero shows max bonus state.

## Tests And Validators

- Backend service tests still cover Lv. 100 cap, `max_level`, early Awakening `level_required`, Awakening Shard spend, Hero Star shard spend, Hero Shard Chest opening, Shard Rift rewards, and ATK/HP/Power growth after progression.
- Unity `HeroProgressionValidation` now covers local Awakening Shard spend, hero-specific Star shard spend, Hero Shard Chest opening, and Hero Detail copy/action state.
- Current Slice includes Hero Progression validation.

## Open Follow-Ups

- Rename persisted/API DTO `ascension` fields only after a migration/compatibility plan exists.
- Decide if later Awakening stages unlock Crit, Defense, Skill, or role bonuses.
- Run a deeper Account/Server Mode Android smoke before external tester handoff.
