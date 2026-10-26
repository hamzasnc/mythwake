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
| 2026-10-26 | Open balancing and gameplay numbers are decided by the developer, documented in `docs/systems/*.md` and pinned by tests. | Lead |

## Navigation (from 2026-10-26)

Bottom bar, left to right: **Dawnhollow** (town) · **Abenteuer** (world map) · **Kampagne** (centre, larger) ·
**Helden** · **Beschwörung**. Quests and the idle chest stay on the campaign screen like AFK Arena.

- Town: background picture of Dawnhollow, every building of `buildings.json` has a hotspot (position in
  `town.json`). Not yet built buildings show as ruins. Tapping opens the building panel (upgrade, workers,
  the mode the building unlocks).
- World map: pins for Hunt Board, Dungeons, Arena; Shattered Realms and Guardian Raids show as locked pins
  until those systems exist. Positions in `town.json` as well.
