# Tower Dungeon

Last updated: 2026-09-03

## Scope

Prototype `0.2.177` / Backend `0.2.63` keeps the Tower Trial playable through the existing Dungeons → Formation → Fight flow. Local Mode remains available for offline UI/balance checks. In Server Mode, the Go backend is authoritative for the active floor, combat result, rewards, hero-shard grant, idempotency, and durable account progress.

## Player Rules

- Floors: `1` to `1000`.
- Sections: 100-floor bands (`F1-100`, `F101-200`, and so on).
- Fresh progress starts with floor `1` unlocked and `0` cleared.
- Only the current highest unlocked floor can be run; cleared or skipped floors are rejected.
- Clearing a floor unlocks the next floor. Floor `1000` remains the terminal unlocked floor.
- Mini-boss every 25 floors.
- Apex/big boss every 100 floors; it takes priority on floors divisible by both intervals.
- Combat uses the existing server combat replay contract and a 30-second maximum combat window.

## Rewards

Every successful floor grants Gold and Myth Essence. Hero Shards are granted on regular shard milestones and on mini-/big-boss floors. The shard hero rotates deterministically through the authoritative hero definition order.

The balance curve lives in `common.tower_definitions` and is included in `/definitions`. The static Go catalog is the fallback when PostgreSQL definition loading is disabled; client previews consume the definition snapshot when Server Mode is active.

## Server Contract

Run a floor with:

```text
POST /dungeons/tower_dungeon/run?floor=N
Authorization: Bearer <session-token>
Idempotency-Key: tower-floor-<unique-key>
```

The action ID is `tower_run`. The response includes the normal `ActionResult` fields, `combat.mode = "tower"`, `combat.targetLevel = N`, the authoritative `playerSnapshot.tower`, and any `reward.heroShards` entries.

Tower progress is stored in `player.player_tower_progress` with highest unlocked, highest cleared, selected floor, and section start. Migration `0032_tower_progression.sql` also adds the definition seed and `debug.v_player_tower_overview`. Reset removes the account's Tower progress row.

## Unity UI

The Dungeons overview shows the Tower card, current progress, section controls, floor rows, boss markers, recommended power, enemy HP/damage, and reward preview. Bootstrap and later action responses replace the local Tower mirror from `playerSnapshot.tower`. The server run result then uses the existing visible combat presentation and result popup.

## Local Mode

Local Tower state remains in the versioned PlayerPrefs JSON plus the legacy scalar keys for migration compatibility:

- `towerDungeonHighestUnlockedFloor`
- `towerDungeonHighestClearedFloor`
- `towerDungeonSelectedFloor`
- `towerDungeonSectionStartFloor`

Local Mode is useful for isolated UI and balance checks; it is not the durable tester-account path.

## Verification

Backend unit and HTTP tests cover definition curves, first-floor progression, rejection of a cleared floor, invalid query values, and idempotent replay. The PostgreSQL E2E script passes definition loading, Tower progression, restart recovery, and replay against the available local PostgreSQL instance. The Unity Current Slice validator and Android packaging still require a valid Unity Editor license; real-device Tower checks remain an owner-run release gate.
