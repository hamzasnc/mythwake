# Mythwake v2 – Status

Ground-up rebuild on branch `rebuild/v2` (orphan branch, no v1 code). As of 26.10.2026.

## What works

| Area | State |
|---|---|
| Combat rules (`Mythwake.Core.Combat`) | Deterministic simulation: energy/ultimates (Auto + manual taps + Auto toggled mid-fight), multi-hit, crit, accuracy/evasion, Fragment Aspect cycle, 21 statuses, shields, passives as components (auras, on-hit, periodic …), taunt/provoke/charm/confuse targeting |
| Heroes | 40 heroes as data (`Content/Heroes/<id>/<id>.hero.json`) migrated from the kit design, with Spine rigs, portraits and full-body renders from the rig, 28 cut-ins |
| Team building | Level, stars, faction synergy (2/3/5), race synergy, Hero Leader aura |
| Meta rules (`Mythwake.Core.Meta`) | New player, formation (slots unlock by stage), campaign (200 stages, boss every 10th, server-checkable re-simulation), idle rewards (12 h cap), level up, star up with Hero Shards, Normal Summon with pity (Legendary at 50, Epic+ every 10), duplicates → shards |
| Client | Home (idle chest, next stage, team idling), Formation, Battle (Spine playback, hit-stop, camera shake, cut-ins, damage numbers, ultimate portraits, Auto/x2), Result, Heroes, Hero detail, Summon |
| Save | Local JSON, atomic write + backup, version field for migrations |
| Tests | 43 tests (rules, every hero's kit, content validity, economy pacing), green in dotnet and in Unity |

## How to run

```bash
dotnet test Tools/CoreTests
```

Unity 6000.4.5f1: open the folder, scene `Assets/Mythwake/Scenes/Main.unity`, Play.
Menu **Mythwake** ▸ Setup Project / Rebuild Content Catalog / Render Hero Portraits / Build.

Smoke run of a build (walks through all screens, writes screenshots, quits):

```bash
Builds/Windows/Mythwake.exe -screen-width 540 -screen-height 960 -screen-fullscreen 0 -autopilot Logs/autopilot
```

## Content pipeline

1. Kit design → `Tools/ContentPipeline/migrate_kits.py` (done once; the `.hero.json` files are now the source of truth).
2. Cut-in art → `Tools/ContentPipeline/import_cutins.py <art package roots>`.
3. Unity imports the Spine export; **Mythwake ▸ Render Hero Portraits**, then **Rebuild Content Catalog**.

## Open points

- **Questions for the team (doc gaps, not invented):** names of Fragment Aspects 6 and 7; region names after
  Dawnhollow and Mossveil Woods (chapters 3–10 show "Kapitel N"); enemy art for the doc's enemy groups
  (Wild Beasts, Corrupted Beasts, …) – stages use heroes as enemies until then.
- Kit mechanics approximated in data are listed per hero in `designNotes` (slot swap, summons, copying skills,
  boss sleep immunity, rank-gated skills). Leader abilities in battle are not built yet.
- Balance is a first pass: base stats per role/grade, economy numbers in `Content/Data/*.json`. The pacing test
  (`EarlyGamePacing`) keeps chapter 1 at a few hours of idle time.
- Missing art: cut-ins for 12 heroes, Spellblade skill/ultimate animations, battle/home backgrounds (placeholder
  gradient now), UI icons and fonts, sound.
- Next systems from the doc: village (Survivor Camp, Broken Forge, …), gear, Fragments, relics, Hunt Board,
  dungeons, Arena, guilds, server.
