"""Builds rig-ready hero parts from a See-through decomposition plus the hero's cut-out layers.

usage: py -3 seethrough_parts.py <hero package> <see-through psd> <out package> [--fallback <package>]
                                 [--parts head,hair_back,...]

With --parts only those layers are rebuilt; every other layer of <hero package> is kept as it is (e.g. a Codex-repainted
package for body and limbs, See-through only for head, hair and props).

See-through (github.com/shitagaki-lab/see-through) paints hidden areas, but splits by clothing (topwear, handwear,
objects, ...), not by bones, and at a lower resolution. The cut-out layers have the right bone split and full
resolution, but nothing where the picture hid them. This combines them, per body part:

  1. Whole limbs (arms): the See-through limb is cut into upper arm / forearm / hand by nearest bone, plus a round
     disk around every joint for both neighbours. The overlap is real painted arm, so joints need no repainting.
  2. Every other part: its hidden area (round joint caps, convex hull, holes) is filled from the See-through layer it
     belongs to, but only where that layer is not itself covered by another part of the same layer (legs inside one
     topwear layer cannot see behind each other).
  3. Visible pixels stay the master's full-resolution pixels wherever the master really shows this part; pixels of
     other objects stuck to the cut-out (shield rim on the hand) are replaced by See-through's part underneath.
  4. Whatever the part still lacks (joint caps, hull, holes) comes from --fallback (e.g. the Codex-repainted package), else stays empty.
Then the joint ends are cut round and loose specks removed (merge_parts.py). SOURCES.txt says per part how much came
from the master, See-through and the fallback.
"""
import json
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from psd_tools import PSDImage
from scipy import ndimage
from skimage.morphology import convex_hull_image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from merge_parts import despeckle, round_ends  # noqa: E402
from part_check import JOINTS, cap_radius, disk, load  # noqa: E402

# which See-through layer paints each of our parts
SOURCE = {
    "torso": "topwear", "pelvis": "topwear", "near_thigh": "topwear", "far_thigh": "topwear",
    "near_shin": "topwear", "far_shin": "topwear", "near_foot": "topwear", "far_foot": "topwear",
    "scarf_front": "topwear", "scarf_tail": "topwear", "apron_front": "topwear", "coat_tail_near": "topwear",
    "neck": "neck", "head": "face", "hair_back": "back hair", "hair_front": "front hair",
    "shield_near": "objects", "weapon_far": "objects",
}
ARMS = {side: [f"{side}_upperarm", f"{side}_forearm", f"{side}_hand"] for side in ("near", "far")}
BODY = ["head", "neck", "torso", "pelvis", "near_upperarm", "far_upperarm", "near_forearm", "far_forearm",
        "near_hand", "far_hand", "near_thigh", "far_thigh", "near_shin", "far_shin", "near_foot", "far_foot",
        "shield_near"]
MATCH = 38  # mean RGB difference under which a master pixel counts as "this part" (See-through is softer)


def st_layers(psd_path, size):
    """See-through layers on the master canvas: {name: RGBA array}. Its canvas holds the master scaled to its height."""
    psd = PSDImage.open(psd_path)
    w, h = size
    scale = psd.size[1] / h
    box = ((psd.size[0] - w * scale) / 2, 0, (psd.size[0] + w * scale) / 2, psd.size[1])
    out = {}
    for layer in psd:
        canvas = Image.new("RGBA", psd.size, (0, 0, 0, 0))
        canvas.alpha_composite(layer.topil().convert("RGBA"), (layer.bbox[0], layer.bbox[1]))
        out[layer.name] = np.asarray(canvas.crop(box).resize((w, h), Image.LANCZOS)).copy()
    return out


def merged(st, base):
    """All See-through layers whose name starts with base (handwear-l + handwear-r, ears-l + ...)."""
    parts = [a for n, a in st.items() if n == base or n.startswith(base + "-")]
    if not parts:
        return None
    out = np.zeros_like(parts[0])
    for a in parts:
        k = a[..., 3:4] / 255.0
        out[..., :3] = (a[..., :3] * k + out[..., :3] * (1 - k)).astype(np.uint8)
        out[..., 3] = np.maximum(out[..., 3], a[..., 3])
    return out


def seg_distance(p, a, b):
    yy, xx = np.mgrid[:p[0], :p[1]]
    a, b = np.array(a, float), np.array(b, float)
    ab = b - a
    t = np.clip(((xx - a[0]) * ab[0] + (yy - a[1]) * ab[1]) / max(1e-6, ab @ ab), 0, 1)
    return np.hypot(xx - (a[0] + t * ab[0]), yy - (a[1] + t * ab[1]))


def split_arm(limb, layers, side):
    """Upper arm / forearm / hand masks of one whole See-through arm, overlapping round at elbow and wrist."""
    up, fore, hand = ARMS[side]
    pts = [layers[up]["points"]["shoulder"], layers[up]["points"]["elbow"], layers[fore]["points"]["wrist"]]
    palm = np.array(layers[hand]["points"]["palm"], float)
    wrist = np.array(pts[2], float)
    pts.append(list(wrist + (palm - wrist) * 2.0))  # past the fingers
    a = limb[..., 3] > 0
    d = np.stack([seg_distance(a.shape, pts[i], pts[i + 1]) for i in range(3)])
    nearest = d.argmin(axis=0)
    masks = [a & (nearest == i) for i in range(3)]
    for i, joint in ((0, 1), (1, 2)):
        r = cap_radius(masks[i + 1], masks[i], pts[joint])
        cap = a & disk(a.shape, pts[joint], r)
        masks[i] |= cap
        masks[i + 1] |= cap
    return dict(zip(ARMS[side], masks))


def main():
    pkg, psd_path, out = Path(sys.argv[1]), Path(sys.argv[2]), Path(sys.argv[3])
    fallback = Path(sys.argv[sys.argv.index("--fallback") + 1]) if "--fallback" in sys.argv else None
    todo = sys.argv[sys.argv.index("--parts") + 1].split(",") if "--parts" in sys.argv else BODY
    layers, alpha, rgba = load(pkg)
    master = np.asarray(Image.open(pkg / "master.png").convert("RGB"))
    h, w = master.shape[:2]
    st = st_layers(psd_path, (w, h))
    fb = load(fallback)[2] if fallback else {}
    if out.exists():
        shutil.rmtree(out)
    shutil.copytree(pkg / "layers", out / "layers")
    shutil.copy(pkg / "layers.json", out / "layers.json")
    shutil.copy(pkg / "master.png", out / "master.png")

    # whole arms: the See-through arm layer that overlaps the side's cut-out most
    arms = {}
    arm_layers = [n for n in st if n.startswith("handwear")]
    for side in ARMS:
        own = np.zeros((h, w), bool)
        for p in ARMS[side]:
            own |= alpha.get(p, own)
        name = max(arm_layers, key=lambda n: ((st[n][..., 3] > 128) & own).sum())
        arms[side] = (name, st[name], split_arm(st[name], layers, side))

    # which of our parts each master pixel belongs to (top-most cut-out), for "covered by the same layer"
    owner = np.full((h, w), "", dtype=object)
    for name, l in sorted(layers.items(), key=lambda t: t[1].get("order", -1)):
        if name in alpha and l.get("order", -1) >= 0:
            owner[alpha[name]] = name
    src_of = dict(SOURCE)
    for side, (name, _, _) in arms.items():
        for p in ARMS[side]:
            src_of[p] = name

    owner_src = np.vectorize(lambda o: src_of.get(o, ""))(owner)

    report = []
    for name in todo:
        if name not in alpha:
            continue
        a = alpha[name]
        src_name = src_of.get(name)
        if name in arms.get(name.split("_")[0], ("", None, {}))[2]:
            side = name.split("_")[0]
            src = arms[side][1]
            want = arms[side][2][name]
            base_want = want.copy()
        else:
            src = merged(st, src_name) if src_name else None
            want = convex_hull_image(a) | ndimage.binary_fill_holes(a)
            for parent, pp, child, cp in JOINTS:
                if name in (parent, child) and parent in alpha and child in alpha:
                    pivot = layers[child]["points"].get(cp) or layers[parent]["points"].get(pp)
                    want |= disk(a.shape, pivot, cap_radius(alpha[child], alpha[parent], pivot) + 6)
            base_want = want.copy()
            if src is not None:
                # hidden behind a part of the SAME See-through layer: See-through cannot know what is there
                same = (owner_src == src_name) & (owner != name)
                want &= (src[..., 3] > 128) & (~same | a)
            else:
                want &= a
        result = np.zeros((h, w, 4), np.uint8)
        st_ok = want & (src[..., 3] > 128) if src is not None else np.zeros_like(a)
        result[st_ok] = src[st_ok]
        # visible pixels that really are this part keep the master's full resolution
        if src is not None:
            diff = np.abs(master.astype(np.int16) - src[..., :3].astype(np.int16)).mean(axis=2)
            from_master = a & (rgba[name][..., 3] > 200) & ((diff < MATCH) | ~(src[..., 3] > 128))
        else:
            from_master = a
        if name in arms.get(name.split("_")[0], ("", None, {}))[2]:
            from_master &= want  # an arm segment keeps only its own share of the master's arm
        result[from_master] = rgba[name][from_master]
        # joint caps still empty: the fallback package
        got = result[..., 3] > 128
        caps = np.zeros_like(a)
        for parent, pp, child, cp in JOINTS:
            if name in (parent, child) and parent in alpha and child in alpha:
                pivot = layers[child]["points"].get(cp) or layers[parent]["points"].get(pp)
                caps |= disk(a.shape, pivot, cap_radius(alpha[child], alpha[parent], pivot) + 15)
        # everything the part should cover (hull, holes, joint caps) that See-through could not paint
        need = (base_want | caps) & ~got & ndimage.binary_dilation(got, iterations=40)
        used_fb = np.zeros_like(a)
        if name in fb:
            used_fb = need & (fb[name][..., 3] > 128)
            result[used_fb] = fb[name][used_fb]
        Image.fromarray(result).save(out / "layers" / f"{name}.png")
        total = max(1, int((result[..., 3] > 128).sum()))
        report.append(f"{name:15} master {from_master.sum() / total:5.0%}  see-through {(st_ok & ~from_master & ~used_fb).sum() / total:5.0%}"
                      f"  fallback {used_fb.sum() / total:5.0%}  (source {src_name or '-'})")
    if "--parts" not in sys.argv:
        round_ends(out)
    for name in todo:
        path = out / "layers" / f"{name}.png"
        if path.exists():
            Image.fromarray(despeckle(np.asarray(Image.open(path).convert("RGBA")).copy())).save(path)
    (out / "SOURCES.txt").write_text("\n".join(report) + "\n", encoding="utf-8")
    print("\n".join(report))


if __name__ == "__main__":
    main()
