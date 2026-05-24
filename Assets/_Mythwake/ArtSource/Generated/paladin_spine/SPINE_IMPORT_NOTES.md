# Paladin Spine Import Notes

Goal: turn the existing Paladin combat art into a Spine-ready cutout rig that follows the existing Ravik handoff pattern.

## Files

- `hero_paladin_spine_source_pose.png` - setup pose sampled from the Paladin attack sheet because the sword is raised and easier to rig.
- `hero_paladin_spine_head_reference.png` - idle-frame head reference used to avoid the vertical sword overlapping the helmet.
- `parts/` - transparent PNG attachments for Spine.
- `hero_paladin_spine_parts_manifest.json` - part names, recommended bones, pivots, and source boxes.
- `hero_paladin_spine_parts_preview.png` - contact sheet for visual QA.
- `hero_paladin_spine_setup_preview.png` - source-vs-assembled preview.
- `spine_export/` - generated Spine JSON, atlas text, and atlas PNG for import/cleanup in Spine.

## Suggested Draw Order

1. `shadow_holy_ring`
2. `cape_back`
3. `leg_left`, `leg_right`
4. `torso_armor`
5. `belt_gem`
6. `arm_sword`, `sword`
7. `head_helmet`
8. `shield`
9. `fx_sword_slash`, `fx_shield_flash`, `fx_holy_barrier`

## Animation Pass

The Spine JSON now contains the requested clips: `idle`, `wait`, `walk`, `run`, `attack1`, and `attack2`.

- `idle`: short breathing loop, small shield/sword counter-sway.
- `wait`: longer alert loop with a subtle head check and heavier armor settle.
- `walk`: slower two-step locomotion loop with restrained tank weight.
- `run`: faster bounce, stronger leg swing, cape drag, and steadier shield silhouette.
- `attack1`: quick horizontal sword slash with `fx_sword_slash`.
- `attack2`: heavier shield-forward smite with `fx_shield_flash`, `fx_holy_barrier`, and a late sword slash.

## Transition Pass

The locomotion clips begin and end on matching guard poses, and both attacks recover to the same neutral guard silhouette. For runtime blending, use `hero_paladin_spine_transition_mixes.json`; the key mixes are short into attacks (`0.05`-`0.06s`), slightly longer out of attacks (`0.10`-`0.14s`), and gentle idle/wait/walk blends (`0.16`-`0.18s`).

## Notes

The source Paladin was not a layered PSD, so some pieces are practical cutout chunks rather than perfect hidden-surface layers. Before final production export, inspect overlaps around helmet/shoulder and shield/torso at game scale, then repaint hidden joint coverage where the animator needs larger rotations.
