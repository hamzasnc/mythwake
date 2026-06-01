# Mythwake Hero Progression

Last updated: 2026-06-01

## Current Rules

- Normal Hero level cap: `100`.
- Awakening stages: `0-10`.
- Awakening is locked until the hero is Lv. `100`.
- Hero leveling costs Myth Essence.
- Awakening costs Hero Shards.
- First Awakening cost: `20` shards.
- Additional Awakening cost scaling: `+15` shards per current Awakening stage.
- Current Awakening bonuses are per-hero ATK and HP from shared hero definitions.
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
- Client fallback Awakening shard cost mirrors backend `hero_ascension_any`: `20 + current_awakening * 15`.
- Client local stat formulas mirror backend definitions for base stats, per-level growth, and per-Awakening ATK/HP.

## Hero Detail UX

- Below Lv. 100, the main action remains `Level Up`.
- Below Lv. 100, shards are shown as future Awakening progress, not as passive stat power.
- At Lv. 100, the main action becomes `Awaken` when the hero is below Awakening 10.
- Missing resources should name the current gate clearly: Level, Myth Essence, or Shards.
- At Awakening 10, the hero shows max bonus state.

## Tests And Validators

- Backend service tests cover Lv. 100 cap, `max_level`, early Awakening `level_required`, shard spend, and ATK/HP/Power growth after Awakening.
- Unity `HeroProgressionValidation` covers the same local rules and Hero Detail copy/action state.
- Current Slice includes Hero Progression validation.

## Open Follow-Ups

- Rename persisted/API DTO `ascension` fields only after a migration/compatibility plan exists.
- Decide if later Awakening stages unlock Crit, Defense, Skill, or role bonuses.
- Add backend/PostgreSQL Tower Trial persistence so Tower shard rewards can feed Email-account Awakening progress.
- Run a deeper Account/Server Mode Android smoke before external tester handoff.
