"""Adds face variant layers (closed eyes, open mouth) from edited copies of the hero's own face.

usage: py -3 face_variants.py <package> <box.json> <eyes_closed png> <mouth_open png>

The edited images are the head crop (box.json: x0, y0, x1, y1 on the canvas) repainted by the image model with only
the eyes closed / the mouth open. eye_near_closed / eye_far_closed take the edited pixels over the open eye layers'
area (their patches include the skin around the eye, so swapping them never shows a hole); mouth_open takes the
edited pixels where the mouth changed, plus the closed mouth's area.
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage


def on_canvas(path, box, size):
    x0, y0, x1, y1 = box
    im = Image.open(path).convert("RGB").resize((x1 - x0, y1 - y0), Image.LANCZOS)
    canvas = np.zeros((size[1], size[0], 3), np.uint8)
    cx0, cy0 = max(0, x0), max(0, y0)
    canvas[cy0:y1, cx0:x1] = np.asarray(im)[cy0 - y0:, cx0 - x0:][: y1 - cy0, : x1 - cx0]
    return canvas


def main():
    pkg, box = Path(sys.argv[1]), json.loads(Path(sys.argv[2]).read_text())
    meta = json.loads((pkg / "layers.json").read_text(encoding="utf-8"))
    L = {l["name"]: l for l in meta["layers"]}
    size = tuple(meta["canvas"])
    master = np.asarray(Image.open(pkg / "master.png").convert("RGBA"))
    head_alpha = np.asarray(Image.open(pkg / "layers" / "head.png"))[..., 3] > 40

    def layer_alpha(name):
        return np.asarray(Image.open(pkg / "layers" / f"{name}.png"))[..., 3] > 40

    closed = on_canvas(sys.argv[3], box, size)
    for eye in ("eye_near", "eye_far"):
        if eye not in L:
            continue
        area = ndimage.binary_dilation(layer_alpha(eye), iterations=2)
        out = np.zeros_like(master)
        out[area, :3] = closed[area]
        out[area, 3] = 255
        Image.fromarray(out).save(pkg / "layers" / f"{eye}_closed.png")
        L[f"{eye}_closed"] = {"name": f"{eye}_closed", "order": -1, "visible_default": False,
                              "points": dict(L[eye]["points"])}

    opened = on_canvas(sys.argv[4], box, size)
    if "mouth" in L:
        mouth = layer_alpha("mouth")
        diff = np.abs(opened.astype(int) - master[..., :3].astype(int)).mean(axis=2) > 24
        near = ndimage.binary_dilation(mouth, iterations=30)
        area = ndimage.binary_closing((diff & near) | mouth, iterations=3)
        area = ndimage.binary_fill_holes(area)
        area = ndimage.binary_dilation(area, iterations=2) & (head_alpha | mouth)
        out = np.zeros_like(master)
        out[area, :3] = opened[area]
        out[area, 3] = 255
        Image.fromarray(out).save(pkg / "layers" / "mouth_open.png")
        L["mouth_open"] = {"name": "mouth_open", "order": -1, "visible_default": False,
                           "points": dict(L["mouth"]["points"])}
    meta["layers"] = list(L.values())
    (pkg / "layers.json").write_text(json.dumps(meta, indent=1), encoding="utf-8")
    print("face variants:", [n for n in L if n.endswith("_closed") or n == "mouth_open"])


if __name__ == "__main__":
    main()
