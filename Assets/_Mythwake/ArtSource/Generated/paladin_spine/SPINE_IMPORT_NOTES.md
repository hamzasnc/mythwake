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

- Idle: small chest bob, shield settle, sword counter-sway, cape lag.
- Run: alternate leg rotation and cape drag; keep shield steady for a tank silhouette.
- Attack: sword windup and slash, briefly show `fx_sword_slash`.
- Block Guard: shield snap-forward, show `fx_shield_flash` and `fx_holy_barrier`.
- Ultimate Smite: sword lift into a bright slash with barrier pulse.

## Notes

The source Paladin was not a layered PSD, so some pieces are practical cutout chunks rather than perfect hidden-surface layers. Before final production export, inspect overlaps around helmet/shoulder and shield/torso at game scale, then repaint hidden joint coverage where the animator needs larger rotations.
