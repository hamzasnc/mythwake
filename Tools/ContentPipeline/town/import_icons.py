"""Imports Codex item icons (see make_fragment_icon_jobs.py) into Assets/Mythwake/Content/Icons.

  NN_<name>/master.png -> Icons/<name>.png   (white background removed, cropped, max 512 px)

usage: py -3 import_icons.py <jobs dir>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "monsters"))
from make_cutouts import cut  # noqa: E402

OUT = Path(__file__).resolve().parents[3] / "Assets" / "Mythwake" / "Content" / "Icons"
MAX = 512
# enclosed background (inside a chain or ring) is near-white and larger than a highlight
HOLE_WHITE = 245
HOLE_MIN_AREA = 0.004


def clear_holes(icon):
    """The cut-out only removes background connected to the border; this also clears white pockets it encloses."""
    rgba = np.asarray(icon).copy()
    white = (rgba[..., :3].min(axis=2) >= HOLE_WHITE) & (rgba[..., 3] > 0)
    labels, count = ndimage.label(white)
    if count == 0:
        return icon
    areas = ndimage.sum(white, labels, range(1, count + 1))
    big = np.isin(labels, 1 + np.flatnonzero(areas >= HOLE_MIN_AREA * white.size))
    # soften the rim: pixels next to a hole that are still very light become partly transparent
    rim = ndimage.binary_dilation(big, iterations=2) & ~big
    light = rgba[..., :3].min(axis=2).astype(np.float32)
    rgba[big, 3] = 0
    rgba[rim, 3] = np.minimum(rgba[rim, 3], np.clip((255 - light[rim]) / 60 * 255, 0, 255)).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA")


def main():
    jobs = Path(sys.argv[1])
    OUT.mkdir(parents=True, exist_ok=True)
    done = []
    for folder in sorted(p for p in jobs.iterdir() if p.is_dir()):
        master = folder / "master.png"
        if not master.exists():
            continue
        name = folder.name.split("_", 1)[1]
        icon = clear_holes(cut(Image.open(master)))
        # square canvas so every icon sits the same in its box
        side = max(icon.size)
        canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        canvas.alpha_composite(icon, ((side - icon.width) // 2, (side - icon.height) // 2))
        if side > MAX:
            canvas = canvas.resize((MAX, MAX), Image.LANCZOS)
        canvas.save(OUT / f"{name}.png", optimize=True)
        done.append(name)
    print(f"imported {len(done)}: {', '.join(done)}")


if __name__ == "__main__":
    main()
