"""Cuts monster masters (white background) into game sprites until their Spine rigs exist.

For every <NN>_<id>/master.png in the masters folder: removes the white background by flood fill from the image
border (white highlights inside the outline survive), un-blends the anti-aliased edge from white, crops, and writes
  Assets/Mythwake/Content/Monsters/<id>/<id>_sprite.png    (max 900 px high)
  Assets/Mythwake/Content/Monsters/<id>/<id>_portrait.png  (320x400 card, figure fitted)

usage: py -3 make_cutouts.py <masters dir>
"""
import sys
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "Assets" / "Mythwake" / "Content" / "Monsters"
WHITE_TOLERANCE = 28  # channel distance from pure white counted as background


def background_mask(rgb):
    h, w, _ = rgb.shape
    near_white = (255 - rgb.min(axis=2)) <= WHITE_TOLERANCE
    bg = np.zeros((h, w), dtype=bool)
    queue = deque()
    for x in range(w):
        for y in (0, h - 1):
            if near_white[y, x] and not bg[y, x]:
                bg[y, x] = True
                queue.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if near_white[y, x] and not bg[y, x]:
                bg[y, x] = True
                queue.append((y, x))
    while queue:
        y, x = queue.popleft()
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < h and 0 <= nx < w and not bg[ny, nx] and near_white[ny, nx]:
                bg[ny, nx] = True
                queue.append((ny, nx))
    return bg


def cut(master):
    rgb = np.asarray(master.convert("RGB")).astype(np.float32)
    bg = background_mask(rgb.astype(np.uint8))
    alpha = np.where(bg, 0.0, 1.0)

    # edge band: pixels next to the background are blends with white -> estimate coverage and un-blend
    padded = np.pad(bg, 1)
    near_bg = padded[:-2, 1:-1] | padded[2:, 1:-1] | padded[1:-1, :-2] | padded[1:-1, 2:]
    edge = near_bg & ~bg
    whiteness = rgb.min(axis=2)
    coverage = np.clip((255.0 - whiteness) / 110.0, 0.0, 1.0)
    alpha = np.where(edge, coverage, alpha)
    safe = np.maximum(alpha, 1e-3)[..., None]
    unblended = np.clip((rgb - (1.0 - alpha[..., None]) * 255.0) / safe, 0, 255)
    rgb = np.where(edge[..., None], unblended, rgb)

    rgba = np.dstack([rgb, alpha * 255.0]).astype(np.uint8)
    image = Image.fromarray(rgba, "RGBA")
    box = image.getbbox()
    return image.crop(box) if box else image


def fit(image, width, height, margin=0.06):
    canvas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    scale = min(width * (1 - 2 * margin) / image.width, height * (1 - 2 * margin) / image.height)
    resized = image.resize((max(1, int(image.width * scale)), max(1, int(image.height * scale))), Image.LANCZOS)
    canvas.alpha_composite(resized, ((width - resized.width) // 2, height - resized.height - int(height * margin)))
    return canvas


def main():
    masters = Path(sys.argv[1])
    done = []
    for folder in sorted(p for p in masters.iterdir() if p.is_dir()):
        master = folder / "master.png"
        if not master.exists() or "_" not in folder.name:
            continue
        mid = folder.name.split("_", 1)[1]
        dest = OUT / mid
        if not dest.exists():
            print(f"skip {mid}: no monster definition")
            continue
        sprite = cut(Image.open(master))
        if sprite.height > 900:
            sprite = sprite.resize((int(sprite.width * 900 / sprite.height), 900), Image.LANCZOS)
        sprite.save(dest / f"{mid}_sprite.png", optimize=True)
        fit(sprite, 320, 400).save(dest / f"{mid}_portrait.png", optimize=True)
        done.append(mid)
    print(f"cut {len(done)}: {', '.join(done)}")


if __name__ == "__main__":
    main()
