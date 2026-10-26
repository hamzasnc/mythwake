# Boss Fragments – concept

Source: design doc, "Boss Fragments" (after a main boss is defeated it can be refought to gain its Fragment; use the
dungeon bosses for now; gacha-like chests from defeated bosses; upgrade the Fragments, equip one at a time; a passive
bonus or a map-wide effect such as healing, shielding or a damaging ability with a cooldown so it can fire more than
once per fight; powerful and needed for late-game fights; a global daily limit, e.g. 7 entries to spend across 4
bosses; entries reset at a fixed time and do not stack). Numbers are first-pass balancing.

## The four Boss Fragments (the dungeon bosses)

| Boss Fragment | Dungeon | Effect (level 1) | Per level |
|---|---|---|---|
| Obsidian Vault Sentinel Fragment | Material | Every 20 s: shield on the whole team (100 % ATK) | +12 % |
| Hollow Spire Warden Fragment | Gear | Every 16 s: damage to all enemies (120 % ATK) | +12 % |
| Fragment Devourer Fragment | Fragment | Passive: the whole team deals +8 % damage | +1 % |
| Corrupted Guardian of the Deep Fragment | Relic | Every 15 s: heals the whole team (90 % ATK) | +12 % |

- **ATK** is the highest attack in the team (the Fragment acts from outside the field, like the Hero Leader).
- Equip **one** Boss Fragment for the account. It works in the campaign, dungeons, hunts, Shattered Realms and the
  Trial of Fragments (not in the Arena).

## Refights and chests

- A boss can be refought once its dungeon's level 1 is cleared. The refight uses the highest cleared level of that
  dungeon (boss plus escort, without the dungeon's special rule).
- **7 refight entries per day for all bosses together** (doc), reset at 04:00 UTC, unused entries do not stack. Only
  a won refight uses an entry.
- Every won refight opens a **boss chest**: Fragment Dust and Gold, and a 20 % chance for a copy of that boss's
  Fragment. Pity: at the latest every 5th chest of a boss has a copy.
- The first copy unlocks the Boss Fragment (level 1). Further copies upgrade it: level L → L+1 needs L copies and
  500 × L Fragment Dust. Max level 10.

## UI

Dungeons screen → **Boss Fragments**: entries left today, one card per boss (art, effect with numbers, level and
copies, refight, upgrade, equip). In battle a banner shows when the equipped Boss Fragment fires.
