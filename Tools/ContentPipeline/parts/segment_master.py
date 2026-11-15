"""Cuts a rig master (A-pose, few hidden areas) into named rig layers from a colour-coded part map.

usage:
  py -3 segment_master.py job <master png> <job dir>           writes a Codex image job: paint the part map
  py -3 segment_master.py cut <master png> <map png> <out pkg>   cuts layers/ + layers.json from the painted map
  py -3 segment_master.py points <pkg>                          root/end points for apron, scarf, hair

The part map is the master repainted with one flat colour per body part (LEGEND). Codex only has to generate an
image (its Windows sandbox may be read-only); everything else is code. Every opaque master pixel takes the label of
the nearest legend colour in the map; pixels whose map colour is unclear take the label of the nearest clear pixel.
Layers keep the master's own pixels, so the rest pose is exact. Hidden areas are filled afterwards with
make_part_jobs.py / merge_parts.py; build_hero.py derives the joints from where the layers overlap.
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
from part_check import JOINTS  # noqa: E402

# name: (colour, draw order). Near = screen-left arm/leg (the hero faces right); far limbs behind the body.
LEGEND = {
    "far_upperarm": ((0, 90, 255), 1), "far_forearm": ((0, 200, 255), 2), "far_hand": ((150, 230, 255), 3),
    "far_thigh": ((90, 0, 160), 5), "far_shin": ((170, 60, 255), 6), "far_foot": ((230, 170, 255), 7),
    "near_thigh": ((0, 110, 0), 8), "near_shin": ((60, 200, 60), 9), "near_foot": ((170, 255, 120), 10),
    "pelvis": ((140, 70, 0), 11), "torso": ((0, 0, 140), 12), "apron_front": ((0, 140, 140), 13),
    "neck": ((140, 140, 0), 14), "head": ((255, 230, 0), 15), "eye_near": ((0, 255, 255), 16),
    "eye_far": ((0, 255, 160), 17), "brow_near": ((255, 0, 255), 18), "brow_far": ((200, 0, 140), 19),
    "mouth": ((255, 130, 0), 20), "hair_front": ((255, 0, 0), 21), "scarf_front": ((255, 90, 160), 22),
    "near_upperarm": ((0, 255, 0), 23), "near_forearm": ((120, 255, 0), 24), "near_hand": ((200, 255, 0), 25),
}
LEGEND_TEXT = "\n".join(f"- {name}: RGB {c[0]},{c[1]},{c[2]}" for name, (c, _) in LEGEND.items())

PROMPT = """ARBEITSORDNER: {folder}
Angehängt: master.png (auch im Arbeitsordner): ein Chibi-Held in A-Pose, Blick nach rechts.

AUFGABE: Erzeuge mit deinem Bildgenerierungs-Tool (Bild-Edit mit master.png als Eingabe) eine FARBKARTE der Körperteile:
exakt dieselbe Figur, dieselbe Position, dieselbe Größe, dasselbe Format, aber jede Fläche mit EINER flachen,
vollkommen einfarbigen Farbe pro Körperteil gefüllt – keine Linien, keine Schatten, keine Verläufe, keine Details.
Hintergrund rein weiß. "near" = der Arm / das Bein auf der LINKEN Bildseite, "far" = RECHTE Bildseite.

Grenzen der Teile (so wie ein 2D-Rig sie schneidet):
- upperarm: Schulter (inkl. Schulterfell/Schulterteil, Ärmel, Armbinde) bis Ellbogen; forearm: Ellbogen bis
  Handgelenk (inkl. Armschiene); hand: Hand mit Handschuh.
- thigh: Hüfte bis Knie; shin: Knie (inkl. Knieschutz) bis Knöchel; foot: Stiefel ab dem Knöchel.
- torso: Oberkörper mit Rüstung, Gürteln über der Brust und Hemd, ohne Arme, Hals, Schal, Schürze.
- pelvis: Gürtel an der Hüfte und Hose bis zum Beinansatz, ohne Schürze.
- apron_front: die weiße Schürze; scarf_front: der rote Schal (beide Enden).
- head: Gesichtshaut und Ohren (inkl. Nasenpflaster), ohne Haare, Augen, Brauen, Mund; neck: sichtbarer Hals.
- hair_front: alle sichtbaren Haare inkl. Stirnband; eye_near / eye_far: linkes / rechtes Auge (ganzes Auge);
  brow_near / brow_far: linke / rechte Braue; mouth: Mund.

Farben (exakt diese RGB-Werte):
{legend}

Dieser Lauf ist nicht-interaktiv: nur EIN Bild erzeugen, prüfen, dass jede Fläche flach in der richtigen Farbe ist
und die Figur nicht verschoben oder skaliert wurde, sonst neu erzeugen. Danach beenden.
"""


def job(master, d):
    d.mkdir(parents=True, exist_ok=True)
    im = Image.open(master).convert("RGBA")
    white = Image.new("RGBA", im.size, (255, 255, 255, 255))
    white.alpha_composite(im)
    white.convert("RGB").save(d / "master.png")
    (d / "run_prompt.txt").write_text(PROMPT.format(folder=str(d.resolve()), legend=LEGEND_TEXT), encoding="utf-8")
    print("job in", d)


def extra_points(pkg):
    """root / end points for hanging cloth and hair, so the rig hangs them on the right bone and lets them swing."""
    meta = json.loads((pkg / "layers.json").read_text(encoding="utf-8"))
    L = {l["name"]: l for l in meta["layers"]}

    def mask(n):
        return np.asarray(Image.open(pkg / "layers" / f"{n}.png"))[..., 3] > 128

    if "apron_front" in L:  # hangs from the belt: top centre to bottom centre
        ys, xs = np.nonzero(mask("apron_front"))
        top = ys.min()
        L["apron_front"]["points"] = {"root": [int(xs[ys < top + 15].mean()), int(top + 10)],
                                      "end": [int(xs[ys > ys.max() - 15].mean()), int(ys.max())]}
    if "scarf_front" in L and "head" in L:  # knot under the chin, end = the tail tip farthest from it
        ys, xs = np.nonzero(mask("scarf_front"))
        hy, hx = np.nonzero(mask("head"))
        root = np.array([hx.mean(), hy.max() + 20])
        far = np.argmax((xs - root[0]) ** 2 + (ys - root[1]) ** 2)
        L["scarf_front"]["points"] = {"root": [int(root[0]), int(root[1])], "end": [int(xs[far]), int(ys[far])]}
    if "hair_front" in L and "head" in L:  # stays on the head: root and end on the head centre (rigid)
        hy, hx = np.nonzero(mask("head"))
        c = [int(hx.mean()), int(hy.mean())]
        L["hair_front"]["points"] = {"root": c, "end": c}
    meta["layers"] = list(L.values())
    (pkg / "layers.json").write_text(json.dumps(meta, indent=1), encoding="utf-8")
    print("extra points:", {n: L[n]["points"] for n in ("apron_front", "scarf_front", "hair_front") if n in L})


def cut(master, part_map, out):
    m = np.asarray(Image.open(master).convert("RGBA")).copy()
    alpha = m[..., 3] > 40
    # the figure is one piece: drop faint specks around it
    lab, n = ndimage.label(alpha)
    if n > 1:
        sizes = ndimage.sum(alpha, lab, range(1, n + 1))
        alpha = np.isin(lab, 1 + np.flatnonzero(sizes >= 0.002 * sizes.max()))
    m[~alpha] = 0
    pm = Image.open(part_map).convert("RGB")
    if pm.size != (m.shape[1], m.shape[0]):
        pm = pm.resize((m.shape[1], m.shape[0]), Image.NEAREST)
    pm = np.asarray(pm).astype(np.int32)
    names = list(LEGEND)
    cols = np.array([LEGEND[n][0] for n in names], np.int32)
    dist = ((pm[:, :, None, :] - cols[None, None]) ** 2).sum(-1)
    label = dist.argmin(-1)
    clear = (dist.min(-1) < 60 ** 2) & alpha
    # unclear pixels (outlines in the map, anti-aliasing) take the nearest clear pixel's label
    _, (iy, ix) = ndimage.distance_transform_edt(~clear, return_indices=True)
    label = label[iy, ix]
    # remove tiny islands per label (map noise)
    for i in range(len(names)):
        mask = (label == i) & alpha
        lab, n = ndimage.label(mask)
        if n > 1:
            sizes = ndimage.sum(mask, lab, range(1, n + 1))
            for j, s in enumerate(sizes):
                if s < max(60, 0.08 * sizes.max()):
                    label[lab == j + 1] = -1
    bad = (label < 0) & alpha
    _, (iy, ix) = ndimage.distance_transform_edt(bad | ~alpha, return_indices=True)  # nearest labelled figure pixel
    label = np.where(bad, label[iy, ix], label)
    # pairs painted in one colour (both eyes, both brows): split by side, screen-left = near
    for near, far in (("eye_near", "eye_far"), ("brow_near", "brow_far")):
        a, b = names.index(near), names.index(far)
        both = ((label == a) | (label == b)) & alpha
        lab, n = ndimage.label(both)
        if n >= 2:
            sizes = ndimage.sum(both, lab, range(1, n + 1))
            big = 1 + np.argsort(sizes)[-2:]
            cx = [np.nonzero(lab == k)[1].mean() for k in big]
            left, right = (big[0], big[1]) if cx[0] < cx[1] else (big[1], big[0])
            label[lab == left] = a
            label[lab == right] = b
    # every part is one piece: smaller pieces of a label go to their surrounding part
    for i, name in enumerate(names):
        mask = (label == i) & alpha
        lab, n = ndimage.label(mask)
        if n > 1:
            sizes = ndimage.sum(mask, lab, range(1, n + 1))
            label[mask & (lab != 1 + int(np.argmax(sizes)))] = -1
    bad = (label < 0) & alpha
    _, (iy, ix) = ndimage.distance_transform_edt(bad | ~alpha, return_indices=True)  # nearest labelled figure pixel
    label = np.where(bad, label[iy, ix], label)
    (out / "layers").mkdir(parents=True, exist_ok=True)
    # joint pivots: centre of the border between the two neighbouring parts
    points = {n: {} for n in names}
    for parent, pp, child, cp in JOINTS:
        if parent not in names or child not in names:
            continue
        pa, ch = (label == names.index(parent)) & alpha, (label == names.index(child)) & alpha
        border = ndimage.binary_dilation(pa, iterations=2) & ndimage.binary_dilation(ch, iterations=2)
        if border.sum() >= 3:
            # the longest stretch of shared border (stray touches elsewhere do not count)
            lab, n = ndimage.label(ndimage.binary_dilation(border, iterations=3))
            sizes = ndimage.sum(border, lab, range(1, n + 1))
            ys, xs = np.nonzero(border & (lab == 1 + int(np.argmax(sizes))))
            pt = [int(round(xs.mean())), int(round(ys.mean()))]
        else:
            # not touching (e.g. the neck hidden under a scarf): midway between their closest pixels
            d_ch, (iy, ix) = ndimage.distance_transform_edt(~pa, return_indices=True)
            cy, cx = np.unravel_index(np.where(ch, d_ch, np.inf).argmin(), ch.shape)
            pt = [int(round((cx + ix[cy, cx]) / 2)), int(round((cy + iy[cy, cx]) / 2))]
            print("not touching, midway:", parent, child, pt)
        points[parent][pp] = pt
        points[child][cp] = pt
    layers = []
    for i, name in enumerate(names):
        mask = (label == i) & alpha
        if mask.sum() < 30:
            print("missing:", name)
            continue
        layer = np.zeros_like(m)
        # one pixel of overlap so neighbouring layers meet without a hairline gap
        grow = ndimage.binary_dilation(mask, iterations=1) & alpha
        layer[grow] = m[grow]
        Image.fromarray(layer).save(out / "layers" / f"{name}.png")
        ys, xs = np.nonzero(mask)
        layers.append({"name": name, "order": LEGEND[name][1], "visible_default": True,
                       "bounds": [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())], "points": points[name]})
    Image.fromarray(m).save(out / "master.png")
    (out / "layers.json").write_text(json.dumps({"canvas": [m.shape[1], m.shape[0]], "source": "master.png",
                                                 "near_side": "image-left", "layers": layers}, indent=1),
                                     encoding="utf-8")
    preview = np.zeros((*label.shape, 3), np.uint8)
    preview[alpha] = cols[label[alpha]]
    Image.fromarray(preview).save(out / "part_map_clean.png")
    print(len(layers), "layers in", out)


if __name__ == "__main__":
    if sys.argv[1] == "points":
        extra_points(Path(sys.argv[2]))
    elif sys.argv[1] == "job":
        job(Path(sys.argv[2]), Path(sys.argv[3]))
    else:
        cut(Path(sys.argv[2]), Path(sys.argv[3]), Path(sys.argv[4]))
