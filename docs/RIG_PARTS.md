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

## See-through test (Dorn, 15.11.2026)

[See-through](https://github.com/shitagaki-lab/see-through) (SIGGRAPH 2026, open source) decomposes one picture into
up to 23 semantic layers and paints what was hidden. Dorn went through the authors' online demo at 1280 px (the local
run on the team PC's RX 9070 XT failed: AMD's ROCm PyTorch for Windows is a preview; the integrated GPU blocks it,
MIOpen crashed the driver, matrix products came out wrong afterwards). Result in `output/SeeThrough_Dorn_2026-11-15`.

`Tools/ContentPipeline/parts/seethrough_parts.py` combines it with the cut-out layers: whole See-through arms are cut
into segments by nearest bone with round overlapping joints, other parts take hidden pixels from their See-through
layer, visible pixels stay the master's, the rest comes from the Codex-repainted package.

What we learned:
- Clearly better than our Codex peel: bald head with face painted under the hair, separate eye white / iris / lashes /
  brows / mouth, complete hair, complete shield and sword, both arms painted whole.
- No help where it matters most for Spine: it splits by clothing, not bones; torso, pelvis, legs, boots and scarf are
  one `topwear` layer, so nothing behind the scarf or between the legs is known.
- Its hidden areas are softer and sometimes wrong (grey smudges where the shield lay on the shield hand, washed-out
  far shoulder), and it runs at ~83 % of the master's resolution.
- Joint check: Codex pilot 2 failing joints, combination 4. The combination was not used for the game rig.
Next worth trying: take only head, face features, hair and props from See-through and keep the Codex parts for body
and limbs.
