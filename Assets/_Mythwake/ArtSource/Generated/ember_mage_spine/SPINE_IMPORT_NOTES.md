# Ravik Spine Import Notes

Goal: turn `hero_ravik_spine_cutout_alpha.png` into a Spine cutout rig for Mythwake.

## Files

- `hero_ravik_spine_cutout_chromakey.png` - original generated sheet on magenta key.
- `hero_ravik_spine_cutout_alpha.png` - transparent cutout sheet.
- `parts/` - cleaned individual PNG attachments for Spine.
- `hero_ravik_spine_parts_manifest.json` - part names, recommended bones, pivots, and source boxes.
- `hero_ravik_spine_parts_preview.png` - quick contact sheet for visual QA.
- `spine_export/` - generated Spine JSON, atlas text, and atlas PNG for import/cleanup in Spine.

## In-Game Runtime

Mythwake now uses `RavikSkeletalCombatView` for Ravik in formation and combat. The current implementation is a Unity UI skeletal rig driven by the same cutout parts, so Ravik no longer depends on per-frame combat PNGs for his battle presentation.

The Unity package manifest also includes the official spine-csharp and spine-unity 4.2 UPM git packages. Once Unity resolves those packages, `spine_export/hero_ravik_spine.json` and its atlas can be re-imported/exported through your local Spine install and swapped into an official `SkeletonGraphic`/`SkeletonAnimation` prefab if you want the full Esoteric runtime path.

## Suggested Draw Order

1. `shadow_fire_ring`
2. `cloak_left`, `cloak_mid`, `cloak_right`
3. `leg_left_boot`, `leg_right_boot`, `leg_left_lower`, `leg_right_lower`
4. `torso`
5. `belt_vials`
6. `arm_left_bent`, `arm_right_open`
7. `collar`
8. `hair_back`
9. `head_smirk` or `head_neutral` or `head_shout`
10. `hair_front`
11. `projectile_fireball`, `fx_flame_burst`

## Suggested Bone Tree

```text
root
  shadow
  hips
    cloak_l
    cloak_mid
    cloak_r
    leg_l
    leg_r
    chest
      neck
        head
      upper_arm_l
        hand_l
      upper_arm_r
        hand_r
  fx_projectile
  fx_ground
```

## Animation Pass

- Idle: 1.0-1.2s loop, chest bob 3-5 px, head counter-bob, cloak delayed by 2-3 frames.
- Attack: 0.55-0.7s, shoulders pull back, casting hand snaps forward, show `projectile_fireball` with squash/stretch.
- Skill: 0.9s, both hands lift, collar and cloak flare outward.
- Ultimate: 1.2-1.5s, brief anticipation, spawn `fx_flame_burst`, then strong screen-facing fire flash.
- Death: reuse the existing frame-based death idea from `CombatAnimated` if you need a fast implementation before a full Spine death rig.

## Notes

The parts are generated and cleaned for a first production pass. The oversized flame-hand attachments remain in the source parts for later repainting, but they are intentionally disabled in the Unity rig and Spine animation timelines for now. Before final export, check small hair/flame edges at game scale, then tighten the pivots in Spine where the manifest suggestions do not match your assembled pose.
