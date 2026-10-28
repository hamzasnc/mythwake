"""Puts the Codex-completed parts (make_part_jobs.py) back into a copy of the hero package.

usage: py -3 merge_parts.py <hero package dir> <jobs dir> <out package dir>

Per part: painted.png is scaled back to its crop, aligned to the part's known pixels (small shift search), cut out from
its white background, and used ONLY where the part was missing (magenta in target.png). Every pixel the part already
had stays exactly as it was, so the rebuilt character still matches the approved master. Stray specks are removed.
Writes the complete package (layers.json, layers/, master.png) to <out package dir>; then run part_check.py on it.
"""
import json
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "ui"))
from import_ui import cut_full  # noqa: E402

sys.path.insert(0, str(Path(__file__).resolve().parent))
from part_check import BODY, local_width  # noqa: E402


def align(painted, target, known, search=10):
    """Shift of painted (pixels) that best matches target over the known pixels."""
    best, best_err = (0, 0), None
    p = painted.astype(np.int16)
    t = target.astype(np.int16)
    for dy in range(-search, search + 1, 2):
        for dx in range(-search, search + 1, 2):
            shifted = np.roll(np.roll(p, dy, 0), dx, 1)
            err = np.abs(shifted - t)[known].mean()
            if best_err is None or err < best_err:
                best, best_err = (dx, dy), err
    return best, best_err


def despeckle(rgba, max_share=0.1):
    """Keeps the part's main piece; loose islands further than 3 px from it (and under 10 % of it) are removed."""
    a = rgba[..., 3] > 128
    labels, n = ndimage.label(a)
    if n <= 1:
        return rgba
    sizes = ndimage.sum(a, labels, range(1, n + 1))
    main = labels == 1 + int(np.argmax(sizes))
    near = ndimage.binary_dilation(main, iterations=3)
    keep = main.copy()
    for i, size in enumerate(sizes):
        piece = labels == i + 1
        if (near & piece).any() or size >= max_share * sizes.max():
            keep |= piece
    # soft edge pixels go with the solid piece they border
    rgba[~ndimage.binary_dilation(keep, iterations=2), 3] = 0
    return rgba


def clear_magenta(rgb, region):
    """Magenta fringe the painter left at the edge of the marked area: take the nearest clean pixel's colour."""
    r, g, b = (rgb[..., i].astype(np.int16) for i in range(3))
    bad = region & (r > 140) & (b > 140) & (g < r - 70) & (g < b - 70)
    if bad.any():
        _, (iy, ix) = ndimage.distance_transform_edt(bad, return_indices=True)
        rgb[bad] = rgb[iy[bad], ix[bad]]
    return rgb


# limb joints whose hidden ends are rounded (parent, parent point, child, child point, parent's other end)
LIMBS = [
    ("near_upperarm", "elbow", "near_forearm", "elbow", "shoulder"),
    ("far_upperarm", "elbow", "far_forearm", "elbow", "shoulder"),
    ("near_forearm", "wrist", "near_hand", "wrist", "elbow"),
    ("far_forearm", "wrist", "far_hand", "wrist", "elbow"),
    ("near_thigh", "knee", "near_shin", "knee", "hip"),
    ("far_thigh", "knee", "far_shin", "knee", "hip"),
    ("near_shin", "ankle", "near_foot", "ankle", "knee"),
    ("far_shin", "ankle", "far_foot", "ankle", "knee"),
]


def cross_width(mask, pivot, axis):
    """Width of the part across its bone at the pivot (perpendicular line through the pivot)."""
    normal = np.array([-axis[1], axis[0]])
    inside = []
    for t in range(-200, 201):
        x, y = (pivot + normal * t).round().astype(int)
        if 0 <= y < mask.shape[0] and 0 <= x < mask.shape[1] and mask[y, x]:
            inside.append(t)
    return float(max(inside) - min(inside)) if inside else 0.0


def round_cut(rgba, cover, pivot, axis):
    """
    Cuts the end of a part round: everything in its end zone (from one radius behind the pivot, along axis) outside a
    disk of half its cross width that the other part covers in the rest pose. Never punches holes.
    """
    a = rgba[..., 3] > 0
    r = max(10.0, cross_width(rgba[..., 3] > 128, pivot, axis) / 2.0) * 1.05
    yy, xx = np.mgrid[:a.shape[0], :a.shape[1]]
    zone = (xx - pivot[0]) * axis[0] + (yy - pivot[1]) * axis[1] > -r
    outside = (xx - pivot[0]) ** 2 + (yy - pivot[1]) ** 2 > r * r
    cut = a & zone & outside & ndimage.binary_erosion(cover, iterations=2)
    kept = a & ~cut
    cut &= ~(ndimage.binary_fill_holes(kept) & ~kept)
    rgba[cut, 3] = 0
    return rgba


def round_ends(out):
    """
    Both ends meeting at a limb joint are cut round, wherever the other part hides them in the rest pose: the
    parent's end under the child, and the child's end over the parent (the same art lies underneath, so the rest pose
    does not change). When the joint bends, no square corner or diagonal cut edge pokes out.
    """
    meta = json.loads((out / "layers.json").read_text(encoding="utf-8-sig"))
    layers = {l["name"]: l for l in meta["layers"]}
    for parent, pp, child, cp, other in LIMBS:
        if parent not in layers or child not in layers:
            continue
        pivot = np.array(layers[parent]["points"][pp], float)
        base = np.array(layers[parent]["points"][other], float)
        axis = (pivot - base) / max(1e-6, np.linalg.norm(pivot - base))
        ppath, cpath = out / "layers" / f"{parent}.png", out / "layers" / f"{child}.png"
        prgba = np.asarray(Image.open(ppath).convert("RGBA")).copy()
        crgba = np.asarray(Image.open(cpath).convert("RGBA")).copy()
        if layers[child]["order"] > layers[parent]["order"]:
            top, top_path, top_axis, bottom, bottom_path, bottom_axis = crgba, cpath, -axis, prgba, ppath, axis
        else:
            top, top_path, top_axis, bottom, bottom_path, bottom_axis = prgba, ppath, axis, crgba, cpath, -axis
        bottom = round_cut(bottom, top[..., 3] > 200, pivot, bottom_axis)
        top = round_cut(top, bottom[..., 3] > 200, pivot, top_axis)
        Image.fromarray(bottom).save(bottom_path)
        Image.fromarray(top).save(top_path)


def main():
    pkg, jobs, out = Path(sys.argv[1]), Path(sys.argv[2]), Path(sys.argv[3])
    if out.exists():
        shutil.rmtree(out)
    shutil.copytree(pkg / "layers", out / "layers")
    shutil.copy(pkg / "layers.json", out / "layers.json")
    shutil.copy(pkg / "master.png", out / "master.png")
    report = []
    for d in sorted(p for p in jobs.iterdir() if (p / "crop.json").exists()):
        meta = json.loads((d / "crop.json").read_text(encoding="utf-8"))
        name = meta["part"]
        if not (d / "painted.png").exists():
            report.append(f"{name}: no painted.png, kept as it was")
            continue
        x0, y0, x1, y1 = meta["box"]
        size = tuple(meta["size"])
        target = np.asarray(Image.open(d / "target.png").convert("RGB"))
        painted_img = Image.open(d / "painted.png").convert("RGB")
        if painted_img.size != size:
            painted_img = painted_img.resize(size, Image.LANCZOS)
        painted = np.asarray(painted_img)
        magenta = (target[..., 0] > 240) & (target[..., 1] < 20) & (target[..., 2] > 240)
        white = target.min(axis=2) > 245
        known = ~magenta & ~white
        (dx, dy), err = align(painted, target, known)
        painted = np.roll(np.roll(painted, dy, 0), dx, 1)
        left = int(((painted[..., 0] > 230) & (painted[..., 1] < 40) & (painted[..., 2] > 230) & magenta).sum())
        # back to canvas resolution, cut from white
        w, h = x1 - x0, y1 - y0
        painted = clear_magenta(painted.copy(), ndimage.binary_dilation(magenta, iterations=6))
        small = Image.fromarray(painted).resize((w, h), Image.LANCZOS)
        cut = np.asarray(cut_full(small)).copy()
        fill = np.load(d / "fill.npy")
        layer_path = out / "layers" / f"{name}.png"
        layer = np.asarray(Image.open(layer_path).convert("RGBA")).copy()
        region = layer[y0:y1, x0:x1]
        grow = ndimage.binary_dilation(fill, iterations=2)
        region[grow] = cut[grow]
        layer[y0:y1, x0:x1] = region
        layer = despeckle(layer)
        Image.fromarray(layer).save(layer_path)
        report.append(f"{name}: shift ({dx},{dy}) err {err:.1f}, magenta left {left}px")
    # every body part, also the ones not repainted in this pass: round hidden joint ends, no stray specks
    round_ends(out)
    for name in BODY:
        path = out / "layers" / f"{name}.png"
        if path.exists():
            Image.fromarray(despeckle(np.asarray(Image.open(path).convert("RGBA")).copy())).save(path)
    (out / "MERGE.txt").write_text("\n".join(report) + "\n", encoding="utf-8")
    print("\n".join(report))


if __name__ == "__main__":
    main()
