# Monster Spine rigs

Monsters and bosses get Spine 4.2 rigs built by script (the team's editor is Spine 3.8, so the JSON is authored by
code, not exported). Bosses first, then the normal monsters.

## Pipeline

1. **Master** — one approved Codex picture per monster (`output/Monsters_<date>/<NN>_<id>/master.png`,
   raid Guardians in `output/RaidGuardians_<date>/`).
2. **Peel** — `Tools/ContentPipeline/monsters/make_peel_jobs.py <masters dir> <id,id>` writes a Codex prompt per
   monster from its part list (`PARTS`) and `run_peel.sh` (5 jobs in parallel). Codex cuts the master into layers and
   paints hidden areas with the image generator, then writes `layers.json`: canvas, facing, ground line and per layer
   name, file, draw order, parent, role (`body`, `head`, `jaw`, `limb`, `chain`, `eye`, `glow`, `prop`), joint points
   and, for chains, the spine points.
3. **Rig** — `monsters/spine/build_all.py <masters dirs> [--only id,id]` runs `build_creature.py` per finished
   package: bones from the parent tree, chain parts as weighted mesh bone chains (tails, wings, necks, robes,
   tentacles), closed-eye variants as extra attachments, glow slots additive, a 0.5-scale atlas. Height by body type
   (Spine units): quadruped 160, flying/floating 210, humanoid 240, huge 270; the monster's `SizePermille` scales on top.
4. **Animations** — `creature_animations.py` builds idle, attack, skill, ultimate, hit and death from the roles. Hit
   events sit at the monster's `hitTimesMs` so damage lands on the swing. Huge bodies move with less amplitude
   (`MASS`), carried props (shell, shield, weapon, quiver, …) follow their bone instead of spinning.
5. **Unity** — spine-unity imports `<id>.json` / `.atlas.txt` / `.png`; **Rebuild Content Catalog** then prefers the
   rig over the cut-out sprite. The autopilot plays a short preview battle per rigged monster (`rig_<id>_a/b` shots).

## Checking

`spine/make_preview.py` + `serve.py` render grid sheets of each animation in the browser (spine-webgl 4.2) for review
before the Unity check. `_setup_check.png` in each package compares the setup pose with the master.

## State

Rigged: all 9 bosses of the Boss System (Hollowtusk Alpha, Riftborn Treant, Crimson Wraith Adept, Hollow Spire
Warden, Obsidian Vault Sentinel, Fragment Devourer, Corrupted Guardian of the Deep, Dragon Echo of the Sky, False
Guardian Construct) and the 5 Guardian Raid bosses (Celestial Turtle, Dragon of the Sky, Hollowed Leviathan,
Six-Legged Worldbearer, Craterborn Guardian).
Next: the 11 monsters (part lists are in `PARTS`).
