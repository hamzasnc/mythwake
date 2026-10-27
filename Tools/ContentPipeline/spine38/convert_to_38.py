"""Converts our script-built Spine 4.2 rigs into Spine 3.8 projects the 3.8 editor can import.

usage: py -3 convert_to_38.py <rig dir with <id>.json/.atlas.txt/.png> <out dir> <id>

Writes <out>/<id>.json (Spine 3.8.99 data), <out>/images/<region>.png (one image per attachment, what the editor
imports) and <out>/<id>.atlas + <id>.png (3.8 atlas, for runtime checks).

The 4.2 rigs keep their images at about 2.85 pixels per skeleton unit; the 3.8 editor expects one image pixel per unit,
so the whole skeleton is scaled by that factor (bones, attachment offsets, mesh vertices, translate keys). Format
changes: rotate keys "value" -> "angle", slot "rgba" -> "color", per-channel bezier curves -> one normalised curve per
key ("curve", "c2", "c3", "c4"), version 3.8.99.
"""
import json
import statistics
import sys
from pathlib import Path

from PIL import Image


def read_atlas(path):
    lines = path.read_text(encoding="utf-8").splitlines()
    page = lines[0].strip()
    regions, current = {}, None
    for line in lines[1:]:
        line = line.strip()
        if not line:
            continue
        if ":" not in line:
            current = line
            regions[current] = {}
            continue
        key, value = line.split(":", 1)
        if current is None:
            continue
        regions[current][key.strip()] = [v.strip() for v in value.split(",")]
    return page, regions


def attachments(skins):
    for skin in skins:
        for slot, atts in skin["attachments"].items():
            for name, a in atts.items():
                yield slot, name, a


def image_of(name, a):
    return a.get("path") or a.get("name") or name


def bezier_38(key, nxt, curve, channels):
    """4.x stores absolute control points per channel; 3.8 one normalised curve for the whole key."""
    t0, t1 = key.get("time", 0.0), nxt.get("time", 0.0)
    dt = t1 - t0 if t1 != t0 else 1.0
    best = None
    for c, (v0, v1) in enumerate(channels):
        cx1, cy1, cx2, cy2 = curve[c * 4:c * 4 + 4]
        dv = v1 - v0
        if abs(dv) > 1e-6:
            best = ((cx1 - t0) / dt, (cy1 - v0) / dv, (cx2 - t0) / dt, (cy2 - v0) / dv)
            break
    if best is None:
        cx1, _, cx2, _ = curve[:4]
        c1, c3 = (cx1 - t0) / dt, (cx2 - t0) / dt
        best = (c1, c1, c3, c3)
    return [round(min(1.0, max(0.0, best[0])), 4), round(best[1], 4), round(min(1.0, max(0.0, best[2])), 4), round(best[3], 4)]


def convert_keys(keys, fields, scale=1.0, rename=None, defaults=None):
    out = []
    for i, key in enumerate(keys):
        k = {"time": key.get("time", 0.0)} if key.get("time", 0.0) else {}
        for f in fields:
            if f in key:
                k[(rename or {}).get(f, f)] = round(key[f] * scale, 4) if isinstance(key[f], (int, float)) else key[f]
            elif defaults and f in defaults:
                k[(rename or {}).get(f, f)] = defaults[f]
        curve = key.get("curve")
        if curve == "stepped":
            k["curve"] = "stepped"
        elif isinstance(curve, list) and i + 1 < len(keys):
            nxt = keys[i + 1]
            chans = [((key.get(f, (defaults or {}).get(f, 0)) or 0), (nxt.get(f, (defaults or {}).get(f, 0)) or 0)) for f in fields]
            raw = [(c if j % 2 == 0 else c / 1.0) for j, c in enumerate(curve)]
            c1, c2, c3, c4 = bezier_38(key, nxt, raw, chans)
            k["curve"], k["c2"], k["c3"], k["c4"] = c1, c2, c3, c4
        out.append(k)
    return out


def scale_vertices(a, s):
    v = a["vertices"]
    n_uv = len(a["uvs"]) // 2
    if len(v) == n_uv * 2:  # unweighted: x, y pairs
        return [round(x * s, 4) for x in v]
    out, i = [], 0
    while i < len(v):  # weighted: boneCount, then (bone, x, y, weight) per bone
        count = int(v[i])
        out.append(count)
        i += 1
        for _ in range(count):
            out += [int(v[i]), round(v[i + 1] * s, 4), round(v[i + 2] * s, 4), v[i + 3]]
            i += 4
    return out


def main():
    src, out, rid = Path(sys.argv[1]), Path(sys.argv[2]), sys.argv[3]
    data = json.loads((src / f"{rid}.json").read_text(encoding="utf-8"))
    page, regions = read_atlas(src / f"{rid}.atlas.txt")
    atlas = Image.open(src / page).convert("RGBA")

    # pixels per skeleton unit, from the region attachments (constant within one rig)
    ratios = []
    for _, name, a in attachments(data["skins"]):
        r = regions.get(image_of(name, a))
        if r and a.get("type", "region") == "region" and a.get("width"):
            ratios.append(int(r["bounds"][2]) / a["width"])
    s = statistics.median(ratios) if ratios else 1.0

    # images for the editor, cut from the atlas
    (out / "images").mkdir(parents=True, exist_ok=True)
    for name, r in regions.items():
        x, y, w, h = (int(v) for v in r["bounds"])
        img = atlas.crop((x, y, x + w, y + h))
        if r.get("rotate", ["false"])[0] in ("true", "90"):
            img = img.rotate(90, expand=True)
        target = out / "images" / f"{name}.png"
        target.parent.mkdir(parents=True, exist_ok=True)
        img.save(target)

    sk = data["skeleton"]
    skeleton = {"hash": sk.get("hash", rid), "spine": "3.8.99", "x": round(sk.get("x", 0) * s, 2), "y": round(sk.get("y", 0) * s, 2),
                "width": round(sk.get("width", 0) * s, 2), "height": round(sk.get("height", 0) * s, 2), "images": "./images/", "audio": ""}
    bones = []
    for b in data["bones"]:
        nb = dict(b)
        for f in ("x", "y", "length"):
            if f in nb:
                nb[f] = round(nb[f] * s, 4)
        if "inherit" in nb:
            nb["transform"] = nb.pop("inherit")
        bones.append(nb)
    skins = []
    for skin in data["skins"]:
        atts = {}
        for slot, items in skin["attachments"].items():
            atts[slot] = {}
            for name, a in items.items():
                na = dict(a)
                kind = a.get("type", "region")
                region = regions.get(image_of(name, a))
                if kind == "region":
                    for f in ("x", "y"):
                        if f in na:
                            na[f] = round(na[f] * s, 3)
                    if region:
                        # width/height = the image's pixels; a scale keeps images of another resolution (effects)
                        # exactly as large as in the game
                        pw, ph = int(region["bounds"][2]), int(region["bounds"][3])
                        sx = a["width"] * s / pw * a.get("scaleX", 1.0)
                        sy = a["height"] * s / ph * a.get("scaleY", 1.0)
                        na["width"], na["height"] = pw, ph
                        na.pop("scaleX", None)
                        na.pop("scaleY", None)
                        if abs(sx - 1) > 0.01:
                            na["scaleX"] = round(sx, 4)
                        if abs(sy - 1) > 0.01:
                            na["scaleY"] = round(sy, 4)
                elif kind == "mesh":
                    na["vertices"] = scale_vertices(a, s)
                    if region:
                        na["width"], na["height"] = int(region["bounds"][2]), int(region["bounds"][3])
                atts[slot][name] = na
        skins.append({"name": skin["name"], "attachments": atts})

    animations = {}
    for an, anim in data["animations"].items():
        na = {}
        if "bones" in anim:
            na["bones"] = {}
            for bone, tls in anim["bones"].items():
                nb = {}
                for tl, keys in tls.items():
                    if tl == "rotate":
                        nb["rotate"] = convert_keys(keys, ["value"], rename={"value": "angle"}, defaults={"value": 0})
                    elif tl == "translate":
                        nb["translate"] = convert_keys(keys, ["x", "y"], scale=s, defaults={"x": 0, "y": 0})
                    elif tl in ("scale", "shear"):
                        nb[tl] = convert_keys(keys, ["x", "y"], defaults={"x": 1 if tl == "scale" else 0, "y": 1 if tl == "scale" else 0})
                na["bones"][bone] = nb
        if "slots" in anim:
            na["slots"] = {}
            for slot, tls in anim["slots"].items():
                ns = {}
                for tl, keys in tls.items():
                    if tl == "attachment":
                        ns["attachment"] = [{**({"time": k["time"]} if k.get("time") else {}), "name": k.get("name")} for k in keys]
                    elif tl in ("rgba", "color"):
                        # colour curves: keep linear between keys (3.8 has one curve for all channels)
                        ns["color"] = [{**({"time": k["time"]} if k.get("time") else {}), "color": k["color"]} for k in keys]
                na["slots"][slot] = ns
        for key in ("events", "drawOrder", "ik"):
            if key in anim:
                na[key] = anim[key]
        animations[an] = na

    result = {"skeleton": skeleton, "bones": bones, "slots": data["slots"]}
    if "ik" in data:
        result["ik"] = data["ik"]
    result.update({"skins": skins, "events": data.get("events", {}), "animations": animations})
    (out / f"{rid}.json").write_text(json.dumps(result, ensure_ascii=False, indent=1), encoding="utf-8")

    # 3.8 atlas over the same page, for runtime checks
    atlas.save(out / f"{rid}.png")
    lines = [f"", f"{rid}.png", f"size: {atlas.width},{atlas.height}", "format: RGBA8888", "filter: Linear,Linear", "repeat: none"]
    for name, r in regions.items():
        x, y, w, h = (int(v) for v in r["bounds"])
        lines += [name, "  rotate: false", f"  xy: {x}, {y}", f"  size: {w}, {h}", f"  orig: {w}, {h}", "  offset: 0, 0", "  index: -1"]
    (out / f"{rid}.atlas").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"{rid}: scale {s:.3f}, {len(regions)} images, {len(animations)} animations")


if __name__ == "__main__":
    main()
