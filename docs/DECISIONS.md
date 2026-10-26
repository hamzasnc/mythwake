# Decisions

Decisions made by the team or the project lead that the design doc (`docs/GAME_DESIGN.md`) leaves open.
Lore and names still come only from the doc; everything here is structure, balancing or gameplay.

| Date | Decision | Source |
|---|---|---|
| 2026-10-25 | Advantage cycle over the Fragment Aspects (Ravak → Gorath → Somnara → Veloria → Envyra → Ravak), no STR/DEX/INT/VIT. | Lead |
| 2026-10-25 | Ultimates are manual (tap the portrait) with an Auto toggle in battle. | Lead |
| 2026-10-25 | Hero grades Common, Rare, Epic, Legendary, Transcendent. Event heroes are Legendary. | Lead (40-hero kits) |
| 2026-10-26 | **Mythwake stays at five Fragment Aspects.** The doc's "seven" is not filled up. | Lead |
| 2026-10-26 | No extra special hero groups (nothing like Celestial/Hypogean); rarity is the grade ladder above. | Lead |
| 2026-10-26 | Hubs follow AFK Arena / 7DS Idle: Dawnhollow is a drawn town with tappable buildings, the other modes sit on a world map, the campaign is the centre button of the bottom bar. | Lead |
| 2026-10-26 | Fragment system numbers and rules (slots by Shrine level, scaling, fuse keeps the first Fragment's effect, drawbacks never scale): `docs/systems/FRAGMENTS.md`. | Developer |
| 2026-10-26 | Relics: one per doc boss, named object + boss name, slot opens with Fragment Shrine level 2, numbers in `docs/systems/RELICS.md`. | Developer |
| 2026-10-26 | Trial of Fragments: five towers (one per Aspect), no daily limit, no Hero Leader, opens with Fragment Shrine level 4 (`docs/systems/TRIALS.md`). | Developer |
| 2026-10-26 | Shattered Realms: two free entries per day shared by all realms, "Shattered Realm Key" for more (the doc's key), Portal Chamber level 5 (`docs/systems/SHATTERED_REALMS.md`). | Developer |
| 2026-10-26 | Guardian Awakening for the doc's four examples first; awakened = Transcendent with a Trait (combat design §7); no one-per-formation limit yet (`docs/systems/AWAKENING.md`). | Developer |
| 2026-10-26 | Open balancing and gameplay numbers are decided by the developer, documented in `docs/systems/*.md` and pinned by tests. | Lead |
| 2026-10-26 | Intro story from the doc's lore sneak-peek (five pictures, German text with the doc's English terms) and progress-driven coach marks for the first steps; the player character is never shown from the front (`docs/systems/TUTORIAL.md`). | Developer |
| 2026-10-26 | Painted battle backgrounds per campaign chapter and per game mode; chapters without a doc name are drawn as their boss's lair, without inventing place names. | Developer |
| 2026-10-26 | Full painted UI kit (icons, 9-slice chrome, screen backgrounds, logo, app icon) made with Codex from the game's own item icons as style reference; pictures never carry text, labels stay localized UI text (`docs/UI_KIT.md`). | Developer |
| 2026-10-26 | UI v2 after studying AFK Arena and 7DS Idle screens (`docs/UI_STYLE.md`): world-first campaign hub, parchment content with dark text, one glowing teal primary button, wooden dock with one-tone relief icons, full-screen results, hero detail as a stage. | Developer |

## Navigation (from 2026-10-26)

Bottom bar, left to right: **Dawnhollow** (town) · **Abenteuer** (world map) · **Kampagne** (centre, larger) ·
**Helden** · **Beschwörung**. Quests and the idle chest stay on the campaign screen like AFK Arena.

- Town: background picture of Dawnhollow, every building of `buildings.json` has a hotspot (position in
  `town.json`). Not yet built buildings show as ruins. Tapping opens the building panel (upgrade, workers,
  the mode the building unlocks).
- World map: pins for Hunt Board, Dungeons, Arena; Shattered Realms and Guardian Raids show as locked pins
  until those systems exist. Positions in `town.json` as well.
