# Rig-ready body parts

Spine needs complete body parts, not pieces cut out of the finished picture: every part painted where it is hidden,
with a round end under each joint, nothing of other objects stuck to it. Otherwise gaps and square corners show when
a joint bends (team feedback on Dorn, 28.10.2026).

Tools in `Tools/ContentPipeline/parts/`:

1. `part_check.py <hero package> --sheet bend.png` – measures every joint of the standard hero skeleton (both parts
   must cover a disk of 45 % of the thinner part's width around the pivot), enclosed holes and loose specks, and draws
   a bend sheet (each child rotated −35°/0°/+35°). Exit code 1 = not rig-ready.
2. `make_part_jobs.py <hero package> <jobs dir> [part,part] [--props]` – per part a crop with the missing area in
   magenta (joint caps, what other layers covered, prop pixels stuck to the part) for Codex to paint.
3. `merge_parts.py <hero package> <jobs dir> <out package>` – takes the painted pixels only where the part was missing
   (every visible pixel stays exact, so the rest pose does not change), clears magenta fringe, cuts both ends of every
   limb joint round where the neighbour hides them, removes loose specks.

Dorn is the pilot (three passes: all parts, then torso/pelvis/neck/head/thighs, then the shield arm). Known leftovers
are listed in the pilot's LIESMICH.
