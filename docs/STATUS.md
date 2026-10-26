# Mythwake v2 – Status

Ground-up rebuild on branch `rebuild/v2` (orphan branch, no v1 code). As of 26.10.2026.

## What works

| Area | State |
|---|---|
| Combat rules (`Mythwake.Core.Combat`) | Deterministic simulation: energy/ultimates (Auto + manual taps + Auto toggled mid-fight), multi-hit, crit, accuracy/evasion, Fragment Aspect cycle, 21 statuses, shields, passives as components (auras, on-hit, periodic …), taunt/provoke/charm/confuse targeting |
| Heroes | 40 heroes as data (`Content/Heroes/<id>/<id>.hero.json`) migrated from the kit design, with Spine rigs, portraits and full-body renders from the rig, 28 cut-ins |
| Team building | Skill upgrades with Upgrade stones (`docs/systems/SKILLS.md`), target priority per hero (front first, weakest, strongest, back row, supports, damage dealers), level, stars, faction synergy (2/3/5), race synergy, Hero Leader aura and leader abilities cast from off the field (Seraphine: Stimme der Wahrheit, Schleiersegen) |
| Meta rules (`Mythwake.Core.Meta`) | New player, formation (slots unlock by stage), campaign (200 stages, boss every 10th, server-checkable re-simulation), idle rewards (12 h cap), level up, star up with Hero Shards, Normal Summon, Faction Summon and Fragment Summon (Summoning Gate levels 1–3, `docs/systems/SUMMONING.md`) with pity per banner (Legendary at 50, Epic+ every 10), duplicates → shards |
| Village (`Mythwake.Core.Meta.Village`) | Dawnhollow rebuild: Survivor Camp produces Gold/Wood/Stone (12 h storage), Summoning Gate unlocks summoning (stage 2), Healer's Tent adds healing in battle (stage 5); hero slots per building (Village Style, hero bonus by grade/stars/level, heroes keep fighting); all nine doc buildings now have their system |
| Gear (`Mythwake.Core.Meta.Gear`) | Weapon / Armor / Boots / Accessory in four grades, enhancing with Gear materials + Gold, set bonus for four pieces of one set (sets = the doc's factions, bonus scales with the lowest grade), Broken Forge crafting (forge level raises the grade odds, forge heroes improve them), boss stages drop gear, Gear materials from stages and idle, salvage |
| Quests (`Mythwake.Core.Meta.Quests`) | Concept: `docs/systems/QUESTS.md`. Daily (6 tasks, activity chests at 20–100) and weekly quests with fixed reset (04:00 UTC, Monday), measured by lifetime stats; main/village quest chain (31 steps) through every unlock up to stage 200 |
| Hunt Board (`Mythwake.Core.Meta.Hunts`) | Concept: `docs/systems/HUNT_BOARD.md`. Hunter's Hut (stage 12, 10 levels): 3–6 daily hunts and 1–2 weekly elite hunts from the doc's mission list, ★–★★★ difficulty, rewards and difficulty grow with the hut; board is a pure function of player seed + day; Hunt Tokens and Hunt Shop with daily/weekly limits |
| Server (`server/`) | Go API + PostgreSQL + .NET rules service running the same Core (see `docs/SERVER.md`): device login (JWT), server-authoritative commands with client prediction (state hash check, validated client timestamps, idempotent retries, optimistic versioning), content version check, one-command local start (`server/dev.ps1`) |
| Arena | Concept: `docs/systems/ARENA.md`. Defense snapshots, opponents by rating, server-simulated fights replayed in the client, Elo, ranks from the doc, daily fights, rewards, 14-day seasons with rank rewards, ranking, 30 training bots |
| Monsters & bosses | Roster from the doc's Enemy System and Boss System (`Tools/ContentPipeline/monsters/roster.json`): 11 monsters in 6 groups, the 9 named bosses; kits as data (`Content/Monsters`); chapters mix monsters with corrupted heroes and end with their boss; art via Codex masters, drawn as cut-out sprites with procedural motion until rigged |
| Fragments | Concept: `docs/systems/FRAGMENTS.md`. Core / Aspect / Echo slots per hero (opened by Fragment Shrine level 1 / 3 / 5), Core = ATK/HP % (×1.5 on the hero's own Aspect), Aspect effects themed per Aspect, Echo effects incl. team auras with drawbacks; grades Common–Legendary, levels with Fragment Dust, fuse 3 → next grade, dissolve, Stabilize in the Shrine; drops from boss stages, the Fragment Dungeon and Hunts, Dust from idle from stage 15; Arena snapshots keep the effects |
| Relics | Concept: `docs/systems/RELICS.md`. Nine Relics, one per doc boss (Rare–Legendary, fixed), each chapter boss drops its Relic on its defeat, Relic Dungeon drops random ones; one per hero (slot opens with Fragment Shrine level 2); effects follow the doc examples: healing after the ultimate, bonus vs monster types, shield strength, Fragment boost, starting energy, bonus dungeon rewards; upgrade with Relic materials, dissolve copies |
| Trial of Fragments | Concept: `docs/systems/TRIALS.md`. Five towers (one per Fragment Aspect, 100 floors), only heroes of the tower's Aspect, enemies of that Aspect carry the doc's trial mechanic (fire + low-HP rage, slow/delay/sleep, buff steal/energy drain, regeneration + growth, charm/confuse); opens with Fragment Shrine level 4, world-map pin; Fragment Dust per floor, an Aspect Fragment of the tower every 10 floors |
| Shattered Realms | Concept: `docs/systems/SHATTERED_REALMS.md`. The doc's four realms (Ash, Stillness, Reflection, Hunger) with their rules, Corrupted Guardian–class bosses with Riftborn escorts, 60 levels each; two free entries per day for all realms, Shattered Realm Keys (daily chest, Hunt Shop) for more; Guardian Sigils, Fragment Dust, Relic materials; Portal Chamber level 5, world-map pin |
| Guardian Awakening | Concept: `docs/systems/AWAKENING.md`. Guardian Shrine (stage 60) awakens the doc's four examples (Elara, Kaelen, Veyl, Arion) at max stars for Guardian Sigils, Fragment Dust, Relic materials and Gold; awakened heroes count as Transcendent (star cap 7), +20 % HP/ATK, an awakened passive following the doc's description and a team-wide Trait; new artwork still open |
| Boss Fragments | Concept: `docs/systems/BOSS_FRAGMENTS.md`. Refight the four dungeon bosses (7 entries per day for all, no stacking) for boss chests with a 20 % copy chance (pity 5); one Boss Fragment equipped for the account: shield, all-enemy damage or heal on a cooldown from off the field, or a team damage passive; upgrades with copies + Fragment Dust |
| Watchtower defense | Concept: `docs/systems/WATCHTOWER.md`. Watchtower (stage 35, 5 levels): 2 defense runs per day of up to 30 waves; HP and energy carry over (survivors recover 10 %), fallen heroes stay out; boss every 5th wave; Gold/Wood/Stone per wave, Gems every 10th; best wave recorded |
| Shops | Concept: `docs/systems/SHOPS.md`. General, Village, Fragment and Arena Shop (plus the Hunt Shop) with daily / weekly limits, building and stage unlocks, Fragments, gear and daily rotating hero shards |
| Dungeons | Concept: `docs/systems/DUNGEONS.md`. Portal Chamber (stage 20): Material, Gear, Fragment and Relic Dungeon, 100 difficulty levels, doc bosses + escort, special rules (enemy regeneration, halved healing), 3 attempts per day, first-clear rewards, sweep |
| Hubs (AFK Arena / 7DS Idle style, `docs/DECISIONS.md`) | Bottom bar Dawnhollow · Abenteuer · Kampagne (centre) · Helden · Beschwörung. Dawnhollow is a painted town: the doc's 9 buildings on their plots (ruins until rebuilt, name plates, red dots when an upgrade is affordable, collect bubble over the Survivor Camp), tap opens the building panel. Abenteuer is a painted world map with pins for Kampagne, Dawnhollow, Hunt Board, Dungeons, Arena and the locked Shattered Realms / Guardian Raids. Pictures can be dragged where they overflow; positions in `Content/Town/town.json`, art via `Tools/ContentPipeline/town` |
| Client | Home (idle chest, next stage, team idling, current quest with badge), Quests (Main/Daily/Weekly), world map, Hunt Board + Hunt Shop, Dungeons, Arena (online), Dawnhollow (buildings, storage, hero slots, forge crafting), Formation, Battle (Spine playback, hit-stop, camera shake, cut-ins, damage numbers, ultimate portraits, Auto/x2), Result (with gear drops), Heroes, Hero detail (with equipment, enhancing), Summon |
| Save | Online: on the server (PostgreSQL). Offline/dev: local JSON, atomic write + backup, version field for migrations |
| Tests | 182 C# tests (dotnet + Unity), Go unit tests, end-to-end tests against the running server, online autopilot; (rules, every hero's kit, content validity, economy pacing), green in dotnet and in Unity |

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
4. Item icons (Fragments, Relics) → `Tools/ContentPipeline/town/make_fragment_icon_jobs.py` / `make_relic_icon_jobs.py`, then `import_icons.py <jobs dir>`.
5. Town and world map art → `Tools/ContentPipeline/town/make_town_jobs.py` (Codex jobs), then `import_town.py <jobs dir>`.

## Open points

- **Questions for the team (doc gaps, not invented):** region names after
  Dawnhollow and Mossveil Woods (chapters 3–10 show "Kapitel N"); enemy art for the doc's enemy groups
  (done for 11 monsters + 9 bosses as masters; normal monster names are descriptive working names and need the team's OK; Spine rigs for monsters are next).
- Kit mechanics approximated in data are listed per hero in `designNotes` (slot swap, summons, copying skills,
  boss sleep immunity, rank-gated skills). 
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
