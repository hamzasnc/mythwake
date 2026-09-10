# Kael V2 – Unity 2D Animation

The active, reproducible source pipeline is documented in [V2/README.de.md](V2/README.de.md).

The repository contains the prepared runtime textures, version-4 body atlas layout with 46 parts, rig, animation clips and VFX. A normal Unity build needs no original source drawings. To rebuild the rig from these prepared assets, run `scripts/run-kael-background.ps1 -Method KaelAssetReview.Run -LogName rig-rebuild.log` from the project root.

`scripts/rebuild-kael.ps1` additionally prepares the original V2 drawings before rebuilding the rig. These original and intermediate PNGs remain local at the owner's request; restore the source images listed in [V2/README.de.md](V2/README.de.md) before running this complete source export. Unity runs hidden with lower priority.

Source export requirements: Unity 6000.4.5f1, 2D Animation 14.0.3, Python with Pillow and numpy. No paid Spine editor is needed for Kael. Historical V1 files remain local and are not inputs to the active pipeline.

Editable animation overrides belong in `Assets/_Mythwake/Resources/Characters/Kael/Animations/Authored`. The generator preserves these files and rejects unexpectedly modified generated clips. See the V2 guide for exact filenames, contact timing and source provenance.

The versioned [integration and validation record](../../docs/KAEL_INTEGRATION.de.md) summarizes the delivered state. Detailed reports and gameplay recordings remain local under `artifacts/kael`. Rig stills are technical review material and do not replace genuine combat capture or artistic acceptance.
