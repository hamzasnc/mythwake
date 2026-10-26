"""Spine 4.2 builder for monster layer packages (Codex peel, see ../make_peel_jobs.py).

usage: py -3 build_creature.py <package dir> <out dir> <monster id> <height units> [monster.json]

<package dir> holds layers.json + layers/*.png. Each layer names its parent layer, a role and its joints
(root = pivot on the parent, end = free end); chains also list "spine" points along their centre line.
Writes <id>.json / <id>.atlas.txt / <id>.png (+ _setup_check.png) in <out dir>.

Conventions: master pixels (y down) -> Spine units (y up), origin at the package's ground point, scaled so the
creature's visible height is <height units> (the battle draws heroes at about 230 units).
"""
import json
import math
import os
import sys

import cv2
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from creature_animations import build_animations  # noqa: E402

SRC, OUT, NAME, HEIGHT = sys.argv[1], sys.argv[2], sys.argv[3], float(sys.argv[4])
MONSTER = sys.argv[5] if len(sys.argv) > 5 else None
ATLAS_SCALE = 0.5
PAD = 3
CHAIN_SPACING = 30


# ---------------------------------------------------------------- package
raw = json.load(open(os.path.join(SRC, "layers.json"), encoding="utf-8-sig"))
layers = [dict(l) for l in raw["layers"]]
for i, l in enumerate(layers):
    l.setdefault("order", i)
    l["joints"] = {k: ([v["x"], v["y"]] if isinstance(v, dict) else v) for k, v in (l.get("joints") or {}).items()}
    l.setdefault("role", "limb")
    l["file"] = os.path.join(SRC, l.get("file") or os.path.join("layers", l["name"] + ".png"))
layers = [l for l in layers if os.path.exists(l["file"])]
layers.sort(key=lambda l: l["order"])
L = {l["name"]: l for l in layers}
VARIANT = {l["name"]: l["variant_of"] for l in layers if l.get("variant_of")}
for l in layers:
    if l["name"].endswith("_closed") and l["name"] not in VARIANT:
        base = l["name"][: -len("_closed")]
        if base in L:
            VARIANT[l["name"]] = base
            l["visible"] = False

body = (next((l for l in layers if l["role"] == "body" and not l.get("parent")), None)
        or next((l for l in layers if l["role"] == "body"), None) or next(l for l in layers if not l.get("parent")))
BODY = body["name"]


def alpha_of(path):
    return np.array(Image.open(path).convert("RGBA"))


# union of the visible layers: height reference
union = None
for l in layers:
    if l.get("visible") is False:
        continue
    a = alpha_of(l["file"])[..., 3] > 40
    union = a if union is None else (union | a)
ys, xs = np.nonzero(union)
TOP, BOTTOM = ys.min(), ys.max()
ground = raw.get("ground") or [float((xs.min() + xs.max()) / 2), float(BOTTOM)]
OX, OY = float(ground[0]), float(ground[1])
K = HEIGHT / max(1.0, OY - TOP)


def sp(p):
    return ((p[0] - OX) * K, (OY - p[1]) * K)


def ang(a, b):
    return math.degrees(math.atan2(-(b[1] - a[1]), b[0] - a[0]))


def joint(l, key):
    v = l["joints"].get(key)
    return tuple(v) if v else None


def centroid(l):
    a = alpha_of(l["file"])[..., 3]
    yy, xx = np.nonzero(a > 40)
    return (float(xx.mean()), float(yy.mean())) if len(xx) else (OX, OY)


# ---------------------------------------------------------------- bones
BONES = [("root", None, (OX, OY), None)]
OWN = {}            # layer -> bone its attachment is weighted / attached to
CHAIN = {}          # layer -> (bones, polyline points)


def parent_bone(l):
    p = l.get("parent")
    if not p or p not in L:
        return "root" if l["name"] == BODY else BODY
    if p in CHAIN:
        # attach to the chain bone nearest to this layer's root
        r = joint(l, "root") or centroid(l)
        bones, pts = CHAIN[p]
        d = [seg_dist(r, pts[k], pts[k + 1]) for k in range(len(bones))]
        return bones[int(np.argmin(d))]
    return OWN.get(p, BODY)


def seg_dist(p, a, b):
    p, a, b = np.array(p, float), np.array(a, float), np.array(b, float)
    ab = b - a
    t = np.clip(np.dot(p - a, ab) / max(np.dot(ab, ab), 1e-9), 0, 1)
    return float(np.linalg.norm(p - (a + t * ab)))


pending = list(layers)
guard = 0
while pending and guard < 10:
    guard += 1
    rest = []
    for l in pending:
        n = l["name"]
        p = l.get("parent")
        if n in VARIANT:
            continue
        if p and p in L and p not in OWN and p != n and p not in VARIANT and L[p]["role"] not in ("eye", "glow"):
            rest.append(l)          # parent not built yet
            continue
        par = parent_bone(l)
        root = joint(l, "root") or joint(l, "anchor") or centroid(l)
        end = joint(l, "end") or joint(l, "tip")
        role = l["role"]
        if role in ("eye", "glow") and n != BODY:
            OWN[n] = par
            continue
        if p in L and L[p]["role"] == role and role in ("body", "head") and p not in CHAIN:
            OWN[n] = par            # overlap piece of its parent (body_back, head_front_overlap): moves with it
            continue
        if role == "chain":
            pts = [tuple(p_) for p_ in (l.get("spine") or [])]
            if len(pts) < 2:
                pts = [root, end or centroid(l)]
            if len(pts) == 2:
                a_, b_ = np.array(pts[0], float), np.array(pts[1], float)
                n_seg = 3 if np.linalg.norm(b_ - a_) > 220 else 2
                pts = [tuple(a_ + (b_ - a_) * k / n_seg) for k in range(n_seg + 1)]
            pts = pts[:7]
            bones = []
            prev = par
            for k in range(len(pts) - 1):
                bn = f"{n}_{k + 1}"
                BONES.append((bn, prev, pts[k], pts[k + 1]))
                bones.append(bn)
                prev = bn
            CHAIN[n] = (bones, pts)
            OWN[n] = bones[0]
            continue
        BONES.append((n, par, root, end))
        OWN[n] = n
    pending = rest
for l in pending:      # cyclic or broken parent references: hang them on the body
    OWN.setdefault(l["name"], BODY)

BI = {b[0]: i for i, b in enumerate(BONES)}
WORLD, LOCAL, PARENT = {}, {}, {}
bones_json = []
for name, parent, piv, tip in BONES:
    wx, wy = sp(piv)
    wr = ang(piv, tip) if tip and math.dist(piv, tip) > 1 else (WORLD[parent][2] if parent else 0.0)
    j = {"name": name}
    if parent:
        px, py, pr = WORLD[parent]
        dx, dy = wx - px, wy - py
        c, s = math.cos(math.radians(-pr)), math.sin(math.radians(-pr))
        lx, ly, lr = dx * c - dy * s, dx * s + dy * c, wr - pr
        j.update(parent=parent, x=round(lx, 3), y=round(ly, 3), rotation=round(lr, 3))
        LOCAL[name] = (lx, ly, lr)
    else:
        LOCAL[name] = (wx, wy, wr)
    if tip and math.dist(piv, tip) > 1:
        j["length"] = round(math.dist(piv, tip) * K, 2)
    WORLD[name] = (wx, wy, wr)
    PARENT[name] = parent
    bones_json.append(j)


def to_local(bone, p_px):
    x, y = sp(p_px)
    bx, by, br = WORLD[bone]
    dx, dy = x - bx, y - by
    c, s = math.cos(math.radians(-br)), math.sin(math.radians(-br))
    return dx * c - dy * s, dx * s + dy * c


def smooth01(t):
    t = min(1.0, max(0.0, t))
    return t * t * (3 - 2 * t)


def chain_weights(p, parent, bones, pts):
    """Arc-length position of p along the chain polyline -> blend between neighbouring bones."""
    seg = [np.array(pts[k + 1], float) - np.array(pts[k], float) for k in range(len(bones))]
    best, s_best = 1e18, 0.0
    for k, v in enumerate(seg):
        a = np.array(pts[k], float)
        t = float(np.clip(np.dot(np.array(p) - a, v) / max(np.dot(v, v), 1e-9), -0.3 if k == 0 else 0, 1.3 if k == len(seg) - 1 else 1))
        d = float(np.linalg.norm(np.array(p) - (a + t * v)))
        if d < best:
            best, s_best = d, k + t
    names = [parent] + bones
    centres = [0.0] + [k + 0.5 for k in range(len(bones))]
    centres[1] = 0.3
    if s_best <= 0:
        return [(parent, 1.0)]
    if s_best >= centres[-1]:
        return [(names[-1], 1.0)]
    for k in range(len(names) - 1):
        if centres[k] <= s_best < centres[k + 1]:
            t = smooth01((s_best - centres[k]) / (centres[k + 1] - centres[k]))
            if t <= 0.002:
                return [(names[k], 1.0)]
            if t >= 0.998:
                return [(names[k + 1], 1.0)]
            return [(names[k], 1 - t), (names[k + 1], t)]
    return [(parent, 1.0)]


# ---------------------------------------------------------------- textures
def clean_alpha(rgba):
    a = rgba[..., 3]
    m = (a > 8).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(m, connectivity=8)
    if n > 2:
        big = st[1:, 4].max()
        for i in range(1, n):
            if st[i, 4] < 30 and st[i, 4] < big * 0.05:
                rgba[lab == i] = 0
    return rgba


def defringe(rgba, passes=3):
    """Remove the light halo that cut-outs from the white master background leave along the outline."""
    for _ in range(passes):
        a = rgba[..., 3]
        solid = (a > 0).astype(np.uint8)
        edge = solid - cv2.erode(solid, np.ones((3, 3), np.uint8))
        rgb = rgba[..., :3].astype(np.int16)
        light = (rgb.min(-1) > 200) & ((rgb.max(-1) - rgb.min(-1)) < 40)
        kill = (edge > 0) & light
        if not kill.any():
            break
        rgba[kill] = 0
    a = rgba[..., 3].astype(np.float32)
    soft = cv2.GaussianBlur((a > 0).astype(np.float32), (3, 3), 0)
    rgba[..., 3] = np.minimum(a, soft * 255).astype(np.uint8)
    return rgba


def straight_bled(prem):
    a = prem[..., 3:4]
    rgb = np.where(a > 0.5, prem[..., :3] * 255.0 / np.maximum(a, 1e-6), 0)
    solid = (prem[..., 3] > 0.5).astype(np.uint8)
    if solid.any() and not solid.all():
        _, labels = cv2.distanceTransformWithLabels(1 - solid, cv2.DIST_L2, 5, labelType=cv2.DIST_LABEL_PIXEL)
        yy, xx = np.nonzero(solid)
        lut = np.zeros((labels.max() + 1, 3), np.float32)
        lut[labels[yy, xx]] = rgb[yy, xx]
        rgb = np.where(solid[..., None] > 0, rgb, lut[labels])
    out = np.dstack([np.clip(rgb, 0, 255), np.clip(prem[..., 3:4], 0, 255)])
    return Image.fromarray((out + 0.5).astype(np.uint8), "RGBA")


regions = []
first = alpha_of(layers[0]["file"])
CHECK = Image.new("RGBA", (first.shape[1], first.shape[0]), (40, 44, 52, 255))
for l in layers:
    nm = l["name"]
    rgba = alpha_of(l["file"])
    if l["role"] != "glow":
        rgba = defringe(clean_alpha(rgba))
    if l.get("visible") is not False:
        CHECK.alpha_composite(Image.fromarray(rgba, "RGBA"))
    yy, xx = np.where(rgba[..., 3] > 0)
    if len(xx) == 0:
        continue
    x0, y0 = max(xx.min() - PAD, 0), max(yy.min() - PAD, 0)
    x1, y1 = min(xx.max() + 1 + PAD, rgba.shape[1]), min(yy.max() + 1 + PAD, rgba.shape[0])
    crop = rgba[y0:y1, x0:x1].astype(np.float32)
    crop[..., :3] *= crop[..., 3:4] / 255.0
    w2, h2 = max(1, round((x1 - x0) * ATLAS_SCALE)), max(1, round((y1 - y0) * ATLAS_SCALE))
    regions.append((nm, (x0, y0, x1 - x0, y1 - y0), straight_bled(cv2.resize(crop, (w2, h2), interpolation=cv2.INTER_AREA))))


def pack(width):
    order = sorted(regions, key=lambda r: -r[2].height)
    x = y = shelf = 0
    pos = {}
    for nm, _, im in order:
        if x + im.width + 2 > width:
            x, y, shelf = 0, y + shelf + 2, 0
        pos[nm] = (x + 1, y + 1)
        x += im.width + 2
        shelf = max(shelf, im.height)
    return pos, y + shelf + 2


for W in (1024, 2048, 4096):
    pos, H = pack(W)
    Hp = 1 << (H - 1).bit_length()
    if Hp <= W:
        break
page = Image.new("RGBA", (W, Hp), (0, 0, 0, 0))
atlas = [f"{NAME}.png", f"size:{W},{Hp}", "filter:Linear,Linear"]
for nm, crop, im in regions:
    page.paste(im, pos[nm])
    atlas += [nm, f"bounds:{pos[nm][0]},{pos[nm][1]},{im.width},{im.height}"]
os.makedirs(OUT, exist_ok=True)
CHECK.convert("RGB").save(os.path.join(OUT, "_setup_check.png"))
page.save(os.path.join(OUT, f"{NAME}.png"))
open(os.path.join(OUT, f"{NAME}.atlas.txt"), "w", newline="\n").write("\n".join(atlas) + "\n")


# ---------------------------------------------------------------- attachments + slots
def region_att(bone, crop):
    x0, y0, w, h = crop
    lx, ly = to_local(bone, (x0 + w / 2, y0 + h / 2))
    return {"x": round(lx, 2), "y": round(ly, 2), "rotation": round(-WORLD[bone][2], 2),
            "width": round(w * K, 2), "height": round(h * K, 2)}


def mesh_att(crop, spacing, wfn):
    x0, y0, w, h = crop
    nx, ny = max(2, round(w / spacing) + 1), max(2, round(h / spacing) + 1)
    gx, gy = np.linspace(x0, x0 + w, nx), np.linspace(y0, y0 + h, ny)
    hull = [(i, 0) for i in range(nx)] + [(nx - 1, j) for j in range(1, ny)] + \
           [(i, ny - 1) for i in range(nx - 2, -1, -1)] + [(0, j) for j in range(ny - 2, 0, -1)]
    inner = [(i, j) for j in range(1, ny - 1) for i in range(1, nx - 1)]
    order = hull + inner
    idx = {ij: k for k, ij in enumerate(order)}
    uvs, verts = [], []
    for i, j in order:
        p = (gx[i], gy[j])
        uvs += [round((p[0] - x0) / w, 5), round((p[1] - y0) / h, 5)]
        ws = wfn(p)
        verts.append(len(ws))
        for bn, wt in ws:
            lx, ly = to_local(bn, p)
            verts += [BI[bn], round(lx, 3), round(ly, 3), round(wt, 4)]
    tris = []
    for j in range(ny - 1):
        for i in range(nx - 1):
            a, b, c, d = idx[(i, j)], idx[(i + 1, j)], idx[(i + 1, j + 1)], idx[(i, j + 1)]
            tris += [a, b, c, a, c, d]
    return {"type": "mesh", "uvs": uvs, "triangles": tris, "vertices": verts, "hull": len(hull),
            "width": round(w * K, 2), "height": round(h * K, 2)}


crops = {nm: c for nm, c, _ in regions}
slots, skin = [], {}
for l in layers:
    nm = l["name"]
    if nm not in crops:
        continue
    slot = VARIANT.get(nm, nm)
    owner = L[slot]
    if owner["name"] in CHAIN:
        bones_, pts = CHAIN[owner["name"]]
        par = PARENT[bones_[0]]
        bone = par
        att = mesh_att(crops[nm], CHAIN_SPACING, lambda p, par=par, b=bones_, pts=pts: chain_weights(p, par, b, pts))
    else:
        bone = OWN.get(owner["name"], BODY)
        att = region_att(bone, crops[nm])
    skin.setdefault(slot, {})[nm] = att
    if nm in VARIANT:
        continue
    s = {"name": slot, "bone": bone}
    if l.get("visible") is not False:
        s["attachment"] = nm
    if l["role"] == "glow" or nm.startswith("glow"):
        s["blend"] = "additive"
    slots.append(s)

ROLE = {l["name"]: l["role"] for l in layers}
# hit timing of the battle rules (the monster's skills) -> animation length and hit events
TIMING = {}
if MONSTER and os.path.exists(MONSTER):
    m = json.load(open(MONSTER, encoding="utf-8"))
    for slot_, anim in (("normal", "attack"), ("special", "skill"), ("ultimate", "ultimate")):
        sk = m.get(slot_)
        if sk:
            TIMING[anim] = (sk["durationMs"] / 1000.0, [t / 1000.0 for t in sk["hitTimesMs"]])
anims = build_animations(WORLD=WORLD, LOCAL=LOCAL, PARENT=PARENT, bones=[b[0] for b in BONES], slots=[s["name"] for s in slots],
                         slot_bone={s["name"]: s["bone"] for s in slots}, own=OWN, chain={k: v[0] for k, v in CHAIN.items()},
                         role=ROLE, body=BODY, variants=VARIANT, height=HEIGHT, timing=TIMING)

xs_, ys_ = [], []
for c in crops.values():
    for px, py in ((c[0], c[1]), (c[0] + c[2], c[1] + c[3])):
        x, y = sp((px, py))
        xs_.append(x)
        ys_.append(y)
skel = {
    "skeleton": {"hash": f"{NAME}-c1", "spine": "4.2.43", "x": round(min(xs_), 2), "y": round(min(ys_), 2),
                 "width": round(max(xs_) - min(xs_), 2), "height": round(max(ys_) - min(ys_), 2), "images": "./images/", "audio": ""},
    "bones": bones_json,
    "slots": slots,
    "skins": [{"name": "default", "attachments": skin}],
    "events": {"hit": {}, "cast": {}, "shake": {}},
    "animations": anims,
}
json.dump(skel, open(os.path.join(OUT, f"{NAME}.json"), "w", newline="\n"), separators=(",", ":"))
print(NAME, "atlas", W, Hp, "regions", len(regions), "slots", len(slots), "bones", len(BONES), "chains", list(CHAIN),
      "anims", list(anims), "K", round(K, 4))
