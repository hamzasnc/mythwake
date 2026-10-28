"""Writes Codex jobs that turn a hero's cut-out body layers into complete, rig-ready parts.

usage: py -3 make_part_jobs.py <hero package dir> <jobs dir> [part,part] [--props]

Per body part a crop around it (scaled so its long side is 1024 px) with
  target.png       the part as it is; MAGENTA where it must be painted, white everywhere else
  master_crop.png  the same area of the approved master, for context
The magenta area is what the cut-out lacks: round caps around every joint of the part (so it can rotate under its
neighbour without a gap) and everything that other layers covered (inside the part's convex hull). Parts of props
(shield rim, weapon) that ended up in the layer are marked for repainting too. merge_parts.py puts the results back.
"""
import json
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage
from skimage.morphology import convex_hull_image

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "town"))
from part_check import JOINTS, cap_radius, disk, load, local_width  # noqa: E402
from make_town_jobs import RUNNER  # noqa: E402

BODY = ["head", "torso", "pelvis", "neck", "near_upperarm", "far_upperarm", "near_forearm", "far_forearm", "near_hand", "far_hand",
        "near_thigh", "far_thigh", "near_shin", "far_shin", "near_foot", "far_foot"]
PROPS = ("shield", "weapon")
MAGENTA = (255, 0, 255)
MARGIN = 70

PROMPT = """ARBEITSORDNER: {folder}
Angehängt: target.png und master_crop.png (beide auch im Arbeitsordner).
- master_crop.png: Ausschnitt des fertigen, freigegebenen Charakterbilds (Held "{hero}", Chibi-Stil, handgemalt, dunkle Outlines, Cel-Shading).
- target.png: daraus das einzelne Körperteil "{part}" ({what}) auf weißem Grund. Die MAGENTA-Flächen (#FF00FF) fehlen dem Teil noch.

AUFGABE: Bearbeite target.png mit deinem Bildgenerierungs-Tool (Bild-Edit mit target.png als Eingabe): male die Magenta-Flächen als natürliche Fortsetzung dieses einen Körperteils aus. Das Teil wird später in einem Spine-Rig an den Gelenken gedreht und muss deshalb vollständig sein:
- Gelenke (Schulter, Ellbogen, Handgelenk, Hüfte, Knie, Knöchel, Hals, Taille) bekommen ein rundes, vollständig gemaltes Ende, das unter dem Nachbarteil weiterläuft – keine geraden Schnittkanten, keine Löcher.
- Verdeckte Stellen so malen, wie das Teil darunter aussieht: gleiche Kleidung, gleiche Farben, gleiche Linienstärke und Schattierung wie die sichtbaren Pixel. Unter Kleidung, die im Master nicht durchsichtig ist, KEINE nackte Haut; dort die Kleidung bzw. ein eng anliegendes Unterteil in Outfit-Farbe fortsetzen.
- Nur dieses Körperteil malen. Fremde Dinge (Schildrand, Waffe, andere Körperteile, Haare) gehören NICHT dazu: wo welche im Teil kleben, entfernen und das Körperteil darunter malen.
- Die Magenta-Fläche kann größer sein als das Teil: male das Teil nur in seiner natürlichen Form und Breite (wie im Master), der Rest der Magenta-Fläche wird weiß. Wo das Teil auf einem Schild lag (Holzbrett, Schildrand), gehört dieses Holz NICHT zum Teil.
- Alle nicht-magentafarbenen Pixel des Teils bleiben genau so (Position, Größe, Farbe). Weiß bleibt weiß. Am Ende darf KEIN Magenta mehr im Bild sein.
- Gleiches Format wie target.png, keine Verschiebung, kein Zoom, kein Rahmen, kein Text.

Dieser Lauf ist nicht-interaktiv: kopiere das fertige Bild als painted.png in den Arbeitsordner (generierte Bilder landen sonst nur unter ~/.codex/generated_images). Prüfe das Ergebnis selbst (kein Magenta übrig, Teil nicht verschoben, Gelenk-Enden rund und vollständig) und bearbeite neu, falls nicht. Danach beenden.
"""

WHAT = {
    "head": "Kopf mit Gesicht und Ohren, ohne Haare",
    "torso": "Oberkörper mit Kleidung, ohne Arme, Kopf und Hals",
    "pelvis": "Becken/Hüfte mit Gürtel und Hose, ohne Beine",
    "neck": "Hals",
    "upperarm": "Oberarm mit Ärmel/Schulterteil",
    "forearm": "Unterarm mit Armschiene",
    "hand": "Hand (mit Handschuh)",
    "thigh": "Oberschenkel mit Hose",
    "shin": "Unterschenkel mit Knieschutz und Stiefelschaft",
    "foot": "Fuß/Stiefel",
}


def part_joints(name, layers):
    """Every joint this part takes part in: (pivot, child layer, parent layer)."""
    out = []
    for parent, pp, child, cp in JOINTS:
        if name in (parent, child):
            pivot = layers[child]["points"].get(cp) or layers[parent]["points"].get(pp)
            if pivot:
                out.append((pivot, child, parent))
    return out


def main():
    pkg, out = Path(sys.argv[1]), Path(sys.argv[2])
    only = set(sys.argv[3].split(",")) if len(sys.argv) > 3 and not sys.argv[3].startswith("--") else None
    hero = pkg.name.split("_")[-1]
    layers, alpha, rgba = load(pkg)
    master = np.asarray(Image.open(pkg / "master.png").convert("RGB"))
    out.mkdir(parents=True, exist_ok=True)
    jobs = []
    for i, name in enumerate(BODY, start=1):
        if (only and name not in only) or name not in alpha:
            continue
        a = alpha[name]
        order = layers[name]["order"]
        # what covered this part in the master: every layer drawn above it
        above = np.zeros_like(a)
        for other, l in layers.items():
            if other != name and other in alpha and l.get("order", -1) > order:
                above |= alpha[other]
        fill = (convex_hull_image(a) & ~a & above) | (ndimage.binary_fill_holes(a) & ~a)
        for p, child, parent in part_joints(name, layers):
            if child in alpha and parent in alpha:
                fill |= disk(a.shape, p, cap_radius(alpha[child], alpha[parent], p) + 10) & ~a
        # prop pixels that ended up in the part (shield rim in the hand): repaint
        props = np.zeros_like(a)
        for other in alpha:
            if other.startswith(PROPS):
                props |= alpha[other]
        # grey metal rims are always foreign; with --props every pixel lying on a prop is repainted (wood boards)
        foreign = a & props & ((np.asarray(rgba[name])[..., :3].std(axis=2) < 18) | ("--props" in sys.argv))
        ys, xs = np.nonzero(a | fill)
        x0, y0 = max(0, xs.min() - MARGIN), max(0, ys.min() - MARGIN)
        x1, y1 = min(a.shape[1], xs.max() + MARGIN), min(a.shape[0], ys.max() + MARGIN)
        scale = 1024 / max(x1 - x0, y1 - y0)
        size = (round((x1 - x0) * scale), round((y1 - y0) * scale))

        target = np.full((a.shape[0], a.shape[1], 3), 255, np.uint8)
        part = rgba[name][..., :3].astype(np.float32)
        k = (rgba[name][..., 3:4] / 255.0)
        target = (part * k + target * (1 - k)).astype(np.uint8)
        target[fill | foreign] = MAGENTA
        crop = lambda im: Image.fromarray(im[y0:y1, x0:x1]).resize(size, Image.LANCZOS)

        d = out / f"{i:02d}_{name}"
        d.mkdir(exist_ok=True)
        crop(target).save(d / "target.png")
        crop(master).save(d / "master_crop.png")
        shutil.copy(d / "target.png", d / "style_reference.png")
        np.save(d / "fill.npy", (fill | foreign)[y0:y1, x0:x1])
        (d / "crop.json").write_text(json.dumps({"part": name, "box": [int(x0), int(y0), int(x1), int(y1)], "size": size}), encoding="utf-8")
        kind = name.split("_", 1)[-1]
        (d / "run_prompt.txt").write_text(PROMPT.format(folder=str(d.resolve()), hero=hero.capitalize(), part=name,
                                                        what=WHAT.get(kind, name)), encoding="utf-8")
        jobs.append(d.name)
    (out / "_jobs.txt").write_text("\n".join(jobs) + "\n", encoding="utf-8", newline="\n")
    runner = RUNNER.replace("master.png", "painted.png").replace("-i style_reference.png", "-i target.png -i master_crop.png")
    (out / "run_jobs.sh").write_text(runner.replace("xargs -P 5", "xargs -P 7"), encoding="utf-8", newline="\n")
    print(len(jobs), "jobs in", out)


if __name__ == "__main__":
    main()
