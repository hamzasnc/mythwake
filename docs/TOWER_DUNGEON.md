# Tower Dungeon MVP

Last updated: 2026-05-31

## Scope

Prototype `0.2.173` keeps the first playable Tower Trial / Tower Dungeon available in the Unity client and fixes the device tap path for selecting it from the Dungeons menu. It is a local-mode MVP for tester feel, balancing, and UI validation. Existing Gold, Essence, Gear, Guest auth, Email auth, and Server Mode flows are kept intact.

The tower is intentionally blocked while Server Mode is active. Backend/PostgreSQL tower definitions, rewards, action routing, and account-bound tower progress are the next persistence step.

## Player Rules

- Floors: `1` to `1000`.
- Sections: 100-floor bands (`F1-100`, `F101-200`, and so on).
- Unlocking: floor `1` starts unlocked; clearing the current highest unlocked floor unlocks the next floor.
- Selection: testers can browse floor sections and preview cleared, ready, locked, and future floors.
- Run rule: only the ready highest unlocked floor can be started.
- Boss rules:
  - Mini-boss every 25 floors.
  - Apex boss every 100 floors.
  - Apex boss takes priority on floors divisible by both 25 and 100.
- Fight model: uses the existing local dungeon fight flow with 30-second combat presentation, team HP/damage, enemy HP/damage, win/loss result popup, and Formation entry.

## Rewards

Every cleared floor grants:
- Gold
- Myth Essence

Hero Shards are granted on:
- Regular shard floors every 10 floors
- Every mini-boss floor
- Every apex boss floor

Shard rewards rotate through the starter hero indexes by floor, so long tower progress slowly touches the whole starter roster.

## UI

The Dungeons overview now has a playable `Tower Trial` card after Gold, Essence, and Gear, before the locked future Shard Rift card. Selecting it shows:
- Highest unlocked floor
- Highest cleared floor
- Selected section
- Selected floor
- Boss marker when relevant
- Recommended power
- Enemy HP and enemy damage
- Reward preview
- Floor list with section previous/next controls

The same Formation entry path is used as other dungeon runs. The Dungeons validator now checks the Tower card, selector-card raycast target, section label, mini-boss and apex-boss floor labels, reward copy, and tower Formation entry.

## Local Persistence

Tower state is stored in the versioned local PlayerPrefs JSON save and mirrored through legacy scalar keys for migration compatibility:

- `towerDungeonHighestUnlockedFloor`
- `towerDungeonHighestClearedFloor`
- `towerDungeonSelectedFloor`
- `towerDungeonSectionStartFloor`

Resetting the local prototype save resets tower progress to floor `1` unlocked and `0` cleared. Logout from an Email account does not delete local tower state, but tower state is currently not attached to the Email account on the backend.

## Server Mode Boundary

Server Mode currently blocks Tower Trial runs with a clear local-only message. This avoids creating progress that looks account-bound but is not persisted in PostgreSQL.

Backend work still needed:
- Add tower dungeon definition rows.
- Add tower progress persistence per player/account.
- Add server-authoritative tower reward generation.
- Add idempotent tower run action IDs/results.
- Add PostgreSQL E2E coverage for restart/re-login tower progress.
- Expose tower snapshot fields to Unity bootstrap.

## Balancing Notes

The current formulas are first-pass client formulas for testing:
- Enemy HP and damage scale by floor and boss type.
- Recommended power rises by floor and gets boss multipliers.
- Gold and Myth Essence rewards rise by floor.
- Mini-boss and apex boss floors pay larger resource and shard bundles.

Before public testing, move the formulas into backend-owned definitions so client previews and authoritative rewards are sourced from the same catalog.
