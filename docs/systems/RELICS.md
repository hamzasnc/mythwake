# Relics – concept

Source: design doc, "Relics" (rare items connected to boss-monsters, dungeons and events; unique effects; upgradeable;
rarity; each character can equip a relic), "Gear System" (Relic as a gear slot), "Building System" (Fragment Shrine
unlocks relics and their upgrades), "Dungeon System" (Relic Dungeons farm Relics and Relic upgrade materials),
"Idle Progression" (Relic materials as idle reward). Numbers are first-pass balancing (docs/DECISIONS.md).

## Relics

Every Relic belongs to one of the doc's bosses. Its rarity is fixed (Rare, Epic or Legendary); its effect grows
with its level (+8 % of the base value per level). Max level: Rare 10, Epic 15, Legendary 20.

| Relic | Boss (source) | Rarity | Effect (doc example it follows) |
|---|---|---|---|
| Tusk of the Hollowtusk Alpha | Hollowtusk Alpha (chapter 1) | Rare | More damage against Wild Beasts and Corrupted Beasts ("bonus damage against a type of monster") |
| Root of the Riftborn Treant | Riftborn Treant (chapter 2) | Epic | After its ultimate the hero heals the whole team ("increase healing after an ultimate") |
| Sigil of the Crimson Wraith Adept | Crimson Wraith Adept (chapter 3) | Epic | Starts the battle with energy ("special starting energy") |
| Lantern of the Hollow Spire Warden | Hollow Spire Warden (chapter 4) | Epic | More damage against Hollowed |
| Key of the Obsidian Vault Sentinel | Obsidian Vault Sentinel (chapter 5) | Epic | More rewards in the Material Dungeon ("bonus rewards from certain areas") |
| Core of the Fragment Devourer | Fragment Devourer (chapter 6) | Legendary | Stronger Fragment effects of its wearer ("boost Fragment effects") |
| Shell of the Corrupted Guardian of the Deep | Corrupted Guardian of the Deep (chapter 7) | Legendary | Stronger shields given and received, a little less damage taken ("improve shield strength") |
| Scale of the Dragon Echo of the Sky | Dragon Echo of the Sky (chapter 8) | Legendary | More ultimate damage |
| Heart of the False Guardian Construct | False Guardian Construct (chapter 9) | Legendary | The whole team deals more damage |

The names only combine an object with the doc's boss name; no new world terms.

## Slot and unlocks

- One Relic per hero (doc). The Relic slot opens with **Fragment Shrine level 2** (doc: the Shrine unlocks Relics).
- Relics can be won before the slot is open; they wait in the collection.

## Sources

| Source | Gives |
|---|---|
| First defeat of a chapter boss (stage 20, 40, …) | That boss's Relic |
| Relic Dungeon (Portal Chamber level 4, boss: Corrupted Guardian of the Deep, rule: ultimates charge 30 % slower) | Relic materials every clear, a random Relic on the first clear of every 5th level |
| Idle rewards from stage 25 | Relic materials |

## Actions

- **Equip / unequip** (equipping a Relic worn by someone else moves it).
- **Upgrade**: Relic materials + Gold, cost grows with level and rarity.
- **Dissolve** a spare copy: Relic materials by rarity plus half of its level materials.

## UI

Hero detail: Relic tile next to the Fragments. Fragment Shrine panel → **Relics**: the collection of all nine
Relics (owned copies with level, missing ones greyed with the boss that drops them), details with upgrade and dissolve.
