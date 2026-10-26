# Mythwake v2 – Status

Ground-up rebuild on branch `rebuild/v2` (orphan branch, no v1 code). As of 26.10.2026.

## What works

| Area | State |
|---|---|
| Combat rules (`Mythwake.Core.Combat`) | Deterministic simulation: energy/ultimates (Auto + manual taps + Auto toggled mid-fight), multi-hit, crit, accuracy/evasion, Fragment Aspect cycle, 21 statuses, shields, passives as components (auras, on-hit, periodic …), taunt/provoke/charm/confuse targeting |
| Heroes | 40 heroes as data (`Content/Heroes/<id>/<id>.hero.json`) migrated from the kit design, with Spine rigs, portraits and full-body renders from the rig, 28 cut-ins |
| Team building | Level, stars, faction synergy (2/3/5), race synergy, Hero Leader aura |
| Meta rules (`Mythwake.Core.Meta`) | New player, formation (slots unlock by stage), campaign (200 stages, boss every 10th, server-checkable re-simulation), idle rewards (12 h cap), level up, star up with Hero Shards, Normal Summon with pity (Legendary at 50, Epic+ every 10), duplicates → shards |
| Village (`Mythwake.Core.Meta.Village`) | Dawnhollow rebuild: Survivor Camp produces Gold/Wood/Stone (12 h storage), Summoning Gate unlocks summoning (stage 2), Healer's Tent adds healing in battle (stage 5); hero slots per building (Village Style, hero bonus by grade/stars/level, heroes keep fighting); the other doc buildings are shown as ruins for later updates |
| Gear (`Mythwake.Core.Meta.Gear`) | Weapon / Armor / Boots / Accessory in four grades, enhancing with Gear materials + Gold, set bonus for four pieces of one set (sets = the doc's factions, bonus scales with the lowest grade), Broken Forge crafting (forge level raises the grade odds, forge heroes improve them), boss stages drop gear, Gear materials from stages and idle, salvage |
| Quests (`Mythwake.Core.Meta.Quests`) | Concept: `docs/systems/QUESTS.md`. Daily (6 tasks, activity chests at 20–100) and weekly quests with fixed reset (04:00 UTC, Monday), measured by lifetime stats; main/village quest chain (31 steps) through every unlock up to stage 200 |
| Hunt Board (`Mythwake.Core.Meta.Hunts`) | Concept: `docs/systems/HUNT_BOARD.md`. Hunter's Hut (stage 12, 10 levels): 3–6 daily hunts and 1–2 weekly elite hunts from the doc's mission list, ★–★★★ difficulty, rewards and difficulty grow with the hut; board is a pure function of player seed + day; Hunt Tokens and Hunt Shop with daily/weekly limits |
| Server (`server/`) | Go API + PostgreSQL + .NET rules service running the same Core (see `docs/SERVER.md`): device login (JWT), server-authoritative commands with client prediction (state hash check, validated client timestamps, idempotent retries, optimistic versioning), content version check, one-command local start (`server/dev.ps1`) |
| Arena | Concept: `docs/systems/ARENA.md`. Defense snapshots, opponents by rating, server-simulated fights replayed in the client, Elo, ranks from the doc, daily fights, rewards, 14-day seasons with rank rewards, ranking, 30 training bots |
| Monsters & bosses | Roster from the doc's Enemy System and Boss System (`Tools/ContentPipeline/monsters/roster.json`): 11 monsters in 6 groups, the 9 named bosses; kits as data (`Content/Monsters`); chapters mix monsters with corrupted heroes and end with their boss; art via Codex masters, drawn as cut-out sprites with procedural motion until rigged |
| Dungeons | Concept: `docs/systems/DUNGEONS.md`. Portal Chamber (stage 20): Material and Gear Dungeon, 100 difficulty levels, doc bosses + escort, special rules (enemy regeneration, halved healing), 3 attempts per day, first-clear rewards, sweep |
| Client | Home (idle chest, next stage, team idling, current quest with badge), Quests (Main/Daily/Weekly), Adventure hub, Hunt Board + Hunt Shop, Dungeons, Arena (online), Dawnhollow (buildings, storage, hero slots, forge crafting), Formation, Battle (Spine playback, hit-stop, camera shake, cut-ins, damage numbers, ultimate portraits, Auto/x2), Result (with gear drops), Heroes, Hero detail (with equipment, enhancing), Summon |
| Save | Online: on the server (PostgreSQL). Offline/dev: local JSON, atomic write + backup, version field for migrations |
| Tests | 103 C# tests (dotnet + Unity), Go unit tests, end-to-end tests against the running server, online autopilot; (rules, every hero's kit, content validity, economy pacing), green in dotnet and in Unity |

## How to run

```bash
dotnet test Tools/CoreTests
```

Server (PostgreSQL must be installed; Docker alternative in `docs/SERVER.md`):

```bash
powershell -File server/dev.ps1
```

The client connects to `http://127.0.0.1:8080` and falls back to offline play when no server answers.

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
  (done for 11 monsters + 9 bosses as masters; normal monster names are descriptive working names and need the team's OK; Spine rigs for monsters are next).
- Kit mechanics approximated in data are listed per hero in `designNotes` (slot swap, summons, copying skills,
  boss sleep immunity, rank-gated skills). Leader abilities in battle are not built yet.
- Balance is a first pass: base stats per role/grade, economy numbers in `Content/Data/*.json`. The pacing test
  (`EarlyGamePacing`) keeps chapter 1 at a few hours of idle time.
- Missing art: cut-ins for 12 heroes, Spellblade skill/ultimate animations, battle/home backgrounds (placeholder
  gradient now), UI icons and fonts, sound.
- Village buildings upgrade instantly (no build timers yet). Broken Forge, Hunter's Hut, Watchtower, Fragment Shrine,
  Guardian Shrine and Portal Chamber are ruins until their systems exist.
- Gear: no item art yet (tiles show slot, stat and set), playstyle-changing gear variants from the doc (shield → tank,
  wand → mage) need extra animations per hero and are not built; refining is not built.
- Hunts use heroes as stand-in monsters; "Gather Fragment Dust" waits for Fragments.
- Server: Arena Shop, guilds, chat and Guardian Raids are next; production needs TLS, platform logins (Google/Apple) and Redis for rate limits with several API instances.
- Next systems from the doc (open questions first): Fragments (Fragment Shrine), relics, Hunt Board,
  dungeons, Arena, guilds, server.
