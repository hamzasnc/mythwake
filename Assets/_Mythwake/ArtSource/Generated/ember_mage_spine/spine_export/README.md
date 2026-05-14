# Ravik Spine Export

This folder contains a generated Spine-style handoff for Ravik:

- `hero_ravik_spine.json` - skeleton, slots, skins, and first-pass animation curves.
- `hero_ravik_spine_atlas.atlas.txt` - atlas text for spine-unity.
- `hero_ravik_spine_atlas.png` - packed texture atlas.

Open/import the JSON in Spine for animator cleanup, then export again from your installed Spine version with animation cleanup disabled for setup keys.

The large flame-hand attachments are present in the atlas as source pieces, but every exported animation keeps those slots hidden. The current in-game version uses the normal arms plus `projectile_fireball` and `fx_flame_burst` only.
