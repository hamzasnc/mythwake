# Paladin Spine Export

This folder contains a first-pass Spine handoff for the Mythwake Paladin.

- `hero_paladin_spine.json` - skeleton, slots, skins, and the requested starter animation curves (`idle`, `wait`, `walk`, `run`, `attack1`, `attack2`).
- `hero_paladin_spine_atlas.atlas.txt` - atlas text for spine-unity import.
- `hero_paladin_spine_atlas.atlas` - same atlas text for Spine/editor workflows.
- `hero_paladin_spine_atlas.png` - packed transparent texture atlas.

Import the JSON in Spine 4.2, review pivots in setup pose, then re-export from Spine after animation cleanup. The current rig uses separated cutout chunks from the existing Paladin sheets and is intended as a solid animator starting point, not a final painted layered source file.

Transition mix recommendations live one folder up in `hero_paladin_spine_transition_mixes.json`.
