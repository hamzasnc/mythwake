# Fragment System and Fragment Shrine – concept

Source: design doc, "Fragment System" (Core / Aspect / Echo slots; found in dungeons, dropped by bosses, crafted or
stabilized in the Fragment Shrine, upgraded with Fragment Dust, fused into higher rarity; build choices, not only
stats; drawbacks allowed), "Building System" (Fragment Shrine: Fragment upgrades and stabilization), "Dungeon
System" (Fragment Dungeons farm Fragment Dust), "Trial of Fragments" (what each Fragment Aspect stands for).
Numbers below are first-pass balancing decided by the developer (docs/DECISIONS.md).

## Fragments

Every Fragment has a **slot**, an **Aspect** (one of the five), a **grade** (Common, Rare, Epic, Legendary) and a
**level**. A hero has three Fragment slots; each holds one Fragment of that slot type.

| Slot | Doc | What it does |
|---|---|---|
| Core Fragment | "main Fragment, biggest stat or power bonus" | +ATK % and +HP %. **×1.5 when its Aspect matches the hero's Aspect** (Fragment setup matters). |
| Aspect Fragment | "changes the combat behaviour or adds a special effect" | One effect from its Aspect's theme (table below). |
| Echo Fragment | "passive bonuses, team synergies or special interactions" | One effect from the Echo pool, some with a drawback. |

**Scaling.** An effect's number is its Common level-0 value × grade factor × level factor.
Grade factor: Common 1.0, Rare 1.5, Epic 2.2, Legendary 3.2. Level factor: +6 % per level.
Max level: Common 10, Rare 15, Epic 20, Legendary 25. Drawbacks never scale.

Core Fragment at Common level 0: +3 % ATK, +3 % HP. Legendary level 25 with matching Aspect: +36 %.

### Aspect Fragment effects (themes from the Trial of Fragments)

| Aspect | Effect A | Effect B |
|---|---|---|
| Ravak (fire, stronger at low HP) | Attacks may set the target on fire (Burn) | More damage while below 50 % HP |
| Gorath (regenerate, grow stronger) | Regenerates max HP every second | Damage grows the longer the fight lasts |
| Somnara (slow, delay, sleep) | Attacks may slow the target | Attacks may push back the target's next action |
| Veloria (charm, confuse, targeting) | Attacks may charm the target | Attackers may become confused |
| Envyra (steal, copy) | Attacks may steal a buff | Attacks may drain the target's energy |

### Echo Fragment effects

| Effect | Drawback |
|---|---|
| Stronger ultimate (more ultimate damage) | – |
| More damage against Hollowed (enemy group or race) | – |
| More damage against Riftborn | – |
| Starts the battle with extra energy | – |
| More healing done | – |
| Whole team takes less damage | This hero deals 15 % less damage |
| Whole team deals more damage | This hero takes 15 % more damage |

The last two are the doc's "lower defense or damage of the selected hero while giving another a boost".

## Fragment Shrine

Rebuilt after stage 15, max level 10, hero slots like every building (hero bonus improves the stabilize odds).

| Shrine level | Unlocks |
|---|---|
| 1 | Core Fragment slot, Stabilize, upgrade, fuse, dissolve |
| 3 | Aspect Fragment slot |
| 5 | Echo Fragment slot |

Higher levels raise the grade odds of Stabilize (Common/Rare at 1 → Legendary possible from 6).

## Actions

- **Stabilize** (doc: "crafted or stabilized in the Fragment Shrine"): Fragment Dust + Gold → a random Fragment of the
  chosen (unlocked) slot, random Aspect and effect; grade by Shrine level.
- **Upgrade**: Fragment Dust + Gold, cost grows with level and grade.
- **Fuse**: three unequipped Fragments of the same slot and grade → one of the next grade. The first chosen
  Fragment keeps its Aspect and effect (so a good effect can be carried up); the other two are consumed. Half of the
  Fragment Dust spent on their levels comes back.
- **Dissolve**: an unequipped Fragment → Fragment Dust (by grade plus half its level cost).
- **Equip / unequip**: needs the Shrine level for the slot. One Fragment per slot per hero; equipping a Fragment worn
  by someone else moves it.

## Sources

| Source | Gives |
|---|---|
| Fragment Dungeon (Portal Chamber level 3, boss: Fragment Devourer) | Fragment Dust every clear, a Fragment on the first clear of every 5th level. Rule: enemies deal 30 % more damage. |
| Campaign boss stages (every 10th) once the Shrine is built | Fragment Dust; a Fragment on the first clear |
| Hunt Board (doc mission "Gather Fragment Dust") | Fragment Dust |
| Idle rewards from stage 15 | A trickle of Fragment Dust |

## Arena

Defense snapshots store each hero's Fragment effects together with its stats, so the defense fights exactly as it
was set.

## Not in this step

Boss Fragments (doc: refight bosses for their Fragment, global daily entries) are a separate system. Fragment Summon
waits for the summoning expansion. Relics come next and use the same Shrine.
