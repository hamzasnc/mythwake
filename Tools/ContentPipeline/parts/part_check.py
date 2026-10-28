"""Checks whether a hero's peeled layers are rig-ready body parts, not cut-outs.

usage: py -3 part_check.py <hero package dir> [--sheet out.png]

For every joint of the standard hero skeleton (shoulder, elbow, wrist, hip, knee, ankle, waist, neck) both parts must be
painted over a disk around the pivot (radius = 45 % of the thinner part's width, 14–55 px): then the child can rotate without
opening a gap. Body parts must also have no enclosed holes and no stray specks. A bend sheet rotates every child part
±35° around its pivot over its parent, so gaps are visible at a glance.
Exit code 1 when a check fails.
"""
import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

# (parent, parent point, child, child point)
JOINTS = [
    ("torso", "shoulder_near", "near_upperarm", "shoulder"),
    ("torso", "shoulder_far", "far_upperarm", "shoulder"),
    ("near_upperarm", "elbow", "near_forearm", "elbow"),
    ("far_upperarm", "elbow", "far_forearm", "elbow"),
    ("near_forearm", "wrist", "near_hand", "wrist"),
    ("far_forearm", "wrist", "far_hand", "wrist"),
    ("pelvis", "hip_near", "near_thigh", "hip"),
    ("pelvis", "hip_far", "far_thigh", "hip"),
    ("near_thigh", "knee", "near_shin", "knee"),
    ("far_thigh", "knee", "far_shin", "knee"),
    ("near_shin", "ankle", "near_foot", "ankle"),
    ("far_shin", "ankle", "far_foot", "ankle"),
    ("torso", "waist", "pelvis", "waist"),
    ("torso", "neck", "neck", "base"),
    ("neck", "top", "head", "neck"),
]
BODY = sorted({j[0] for j in JOINTS} | {j[2] for j in JOINTS})
MIN_COVER = 0.92


def load(pkg):
    meta = json.loads((pkg / "layers.json").read_text(encoding="utf-8-sig"))
    layers = {l["name"]: l for l in meta["layers"]}
    alpha, rgba = {}, {}
    for name, l in layers.items():
        f = pkg / (l.get("file") or f"layers/{name}.png")
        if f.exists():
            im = np.asarray(Image.open(f).convert("RGBA"))
            rgba[name] = im
            alpha[name] = im[..., 3] > 128
    return layers, alpha, rgba


def disk(shape, c, r):
    yy, xx = np.ogrid[:shape[0], :shape[1]]
    return (xx - c[0]) ** 2 + (yy - c[1]) ** 2 <= r * r


def local_width(mask, p):
    """Width of the part near point p: twice the distance-transform maximum in a window around p."""
    x, y = int(p[0]), int(p[1])
    win = mask[max(0, y - 60):y + 60, max(0, x - 60):x + 60]
    if not win.any():
        return 0.0
    return 2.0 * float(ndimage.distance_transform_edt(win).max())


def cap_radius(child, parent, pivot):
    """A joint end must be round over the full limb width: half of the thinner part's width at the pivot."""
    w = min(local_width(child, pivot), local_width(parent, pivot) or local_width(child, pivot))
    return min(55.0, max(14.0, 0.45 * w))


def check(pkg):
    layers, alpha, rgba = load(pkg)
    rows, ok = [], True
    for parent, pp, child, cp in JOINTS:
        if parent not in alpha or child not in alpha:
            rows.append((f"{parent}->{child}", "missing layer", False))
            ok = False
            continue
        pivot = layers[child]["points"].get(cp) or layers[parent]["points"].get(pp)
        width = local_width(alpha[child], pivot)
        r = cap_radius(alpha[child], alpha[parent], pivot)
        d = disk(alpha[child].shape, pivot, r)
        cov_child = float((alpha[child] & d).sum() / d.sum())
        cov_parent = float((alpha[parent] & d).sum() / d.sum())
        good = cov_child >= MIN_COVER and cov_parent >= MIN_COVER
        ok &= good
        rows.append((f"{parent}->{child}", f"r={r:.0f}px child {cov_child:.0%} parent {cov_parent:.0%}", good))
    for name in BODY:
        if name not in alpha:
            continue
        a = alpha[name]
        holes = int((ndimage.binary_fill_holes(a) & ~a).sum())
        labels, n = ndimage.label(a)
        sizes = ndimage.sum(a, labels, range(1, n + 1)) if n else []
        # specks: small islands more than 3 px away from the part (edge pixels right at the outline are harmless)
        near = ndimage.binary_dilation(labels == 1 + int(np.argmax(sizes)), iterations=3) if n else a
        specks = int(sum(1 for i, sz in enumerate(sizes) if sz < 0.01 * a.sum() and not (near & (labels == i + 1)).any()))
        good = holes < 0.003 * a.sum() and specks == 0
        ok &= good
        rows.append((name, f"holes {holes}px, specks {specks}", good))
    return ok, rows, layers, rgba


def bend_sheet(layers, rgba, out):
    """Each joint: parent + child rotated -35°, 0°, +35° around the pivot, on grey."""
    tiles = []
    for parent, pp, child, cp in JOINTS:
        if parent not in rgba or child not in rgba:
            continue
        pivot = layers[child]["points"].get(cp)
        row = []
        for angle in (-35, 0, 35):
            canvas = Image.new("RGBA", (rgba[parent].shape[1], rgba[parent].shape[0]), (110, 110, 118, 255))
            rotated = Image.fromarray(rgba[child]).rotate(angle, center=tuple(pivot), resample=Image.BICUBIC)
            ordered = [(layers[parent]["order"], Image.fromarray(rgba[parent])), (layers[child]["order"], rotated)]
            for _, im in sorted(ordered, key=lambda t: t[0]):
                canvas.alpha_composite(im)
            x, y = int(pivot[0]), int(pivot[1])
            crop = canvas.crop((x - 110, y - 110, x + 110, y + 110))
            ImageDraw.Draw(crop).ellipse((106, 106, 114, 114), outline=(255, 60, 60))
            row.append(crop)
        tiles.append((f"{parent}->{child}", row))
    sheet = Image.new("RGB", (3 * 220 + 10, len(tiles) * 238), (30, 30, 34))
    d = ImageDraw.Draw(sheet)
    for i, (label, row) in enumerate(tiles):
        d.text((4, i * 238 + 2), label, fill="white")
        for j, im in enumerate(row):
            sheet.paste(im.convert("RGB"), (j * 222, i * 238 + 16))
    sheet.save(out)


def main():
    pkg = Path(sys.argv[1])
    ok, rows, layers, rgba = check(pkg)
    for name, info, good in rows:
        print(f"{'OK  ' if good else 'FAIL'} {name:30} {info}")
    if "--sheet" in sys.argv:
        bend_sheet(layers, rgba, sys.argv[sys.argv.index("--sheet") + 1])
    print("RIG-READY" if ok else "NOT RIG-READY")
    sys.exit(0 if ok else 1)


if __name__ == "__main__":
    main()
