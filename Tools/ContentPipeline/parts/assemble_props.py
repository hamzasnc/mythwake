"""Puts separately generated props into a rig package: weapon in a fist on the far hand, shield on the near hand.

usage: py -3 assemble_props.py <package> --fist <png> --weapon <png> --shield <png>
       [--weapon-len 0.30] [--weapon-angle 35] [--shield-size 0.20] [--shield-squash 0.75]

Each prop image is a transparent PNG of the object alone (weapon: handle at the top, blade pointing down).
- The fist replaces far_hand: scaled to the cut-out hand's width, turned along the forearm (elbow -> wrist),
  wrist end on the wrist pivot.
- The weapon's grip sits in the fist, blade pointing down and outwards (angle from vertical, towards screen-right);
  its length is a share of the figure height. Layer weapon_far, drawn just behind far_hand, points grip and tip.
- The shield's centre sits on the near hand, size a share of the figure height, squashed sideways for the
  three-quarter view. Layer shield_near, drawn just above near_hand, points root/grip and end/tip.
"""
import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image


def arg(name, default=None):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else default


def tight(path):
    im = Image.open(path).convert("RGBA")
    return im.crop(im.getchannel("A").point(lambda v: 255 if v > 40 else 0).getbbox())


def paste_rotated(canvas_size, im, angle_deg, anchor_src, anchor_dst):
    """im rotated by angle (counter-clockwise, degrees) so that its pixel anchor_src lands on anchor_dst."""
    w, h = im.size
    big = Image.new("RGBA", (w * 3, h * 3), (0, 0, 0, 0))
    big.paste(im, (w, h))
    centre = (w + anchor_src[0], h + anchor_src[1])
    big = big.rotate(angle_deg, resample=Image.BICUBIC, center=centre)
    layer = Image.new("RGBA", canvas_size, (0, 0, 0, 0))
    layer.alpha_composite(big, (int(round(anchor_dst[0] - centre[0])), int(round(anchor_dst[1] - centre[1]))))
    return layer


def rot(p, centre, angle_deg):
    a = math.radians(-angle_deg)  # PIL rotates counter-clockwise on screen (y down)
    x, y = p[0] - centre[0], p[1] - centre[1]
    return [centre[0] + x * math.cos(a) - y * math.sin(a), centre[1] + x * math.sin(a) + y * math.cos(a)]


def main():
    pkg = Path(sys.argv[1])
    meta = json.loads((pkg / "layers.json").read_text(encoding="utf-8"))
    L = {l["name"]: l for l in meta["layers"]}
    size = tuple(meta["canvas"])
    master = np.asarray(Image.open(pkg / "master.png").convert("RGBA"))[..., 3] > 40
    ys = np.nonzero(master.any(axis=1))[0]
    fig_h = ys.max() - ys.min()

    # fist on the far hand
    hand = np.asarray(Image.open(pkg / "layers" / "far_hand.png"))[..., 3] > 40
    hx = np.nonzero(hand.any(axis=0))[0]
    fist = tight(arg("--fist"))
    # generated fists often come with a long cuff: keep the hand and a short piece of cuff to slide under the forearm
    cut_top = float(arg("--fist-cut", 0.35))
    fist = fist.crop((0, round(fist.height * cut_top), fist.width, fist.height))
    s = (hx.max() - hx.min()) * 1.05 / fist.width
    fist = fist.resize((round(fist.width * s), round(fist.height * s)), Image.LANCZOS)
    elbow, wrist = L["far_forearm"]["points"]["elbow"], L["far_forearm"]["points"]["wrist"]
    # image "down" (wrist at top, fingers below) turned to the forearm direction
    angle = math.degrees(math.atan2(wrist[0] - elbow[0], wrist[1] - elbow[1]))
    wrist_src = (fist.width / 2, fist.height * 0.15)
    layer = paste_rotated(size, fist, angle, wrist_src, wrist)
    layer.save(pkg / "layers" / "far_hand.png")
    palm = rot((wrist[0], wrist[1] + fist.height * 0.55), wrist, angle)
    L["far_hand"]["points"].update({"wrist": wrist, "palm": [round(v) for v in palm], "grip": [round(v) for v in palm]})

    # weapon in the fist
    weapon = tight(arg("--weapon"))
    s = fig_h * float(arg("--weapon-len", 0.30)) / weapon.height
    weapon = weapon.resize((round(weapon.width * s), round(weapon.height * s)), Image.LANCZOS)
    w_angle = float(arg("--weapon-angle", 35))
    grip_src = (weapon.width / 2, weapon.height * 0.12)
    layer = paste_rotated(size, weapon, w_angle, grip_src, palm)
    layer.save(pkg / "layers" / "weapon_far.png")
    tip = rot((palm[0], palm[1] + weapon.height * 0.88), palm, w_angle)
    order_hand = L["far_hand"]["order"]
    L["weapon_far"] = {"name": "weapon_far", "order": order_hand - 0.5, "visible_default": True,
                       "points": {"grip": [round(v) for v in palm], "tip": [round(v) for v in tip]}}

    # shield on the near hand
    shield = tight(arg("--shield"))
    d = fig_h * float(arg("--shield-size", 0.20))
    shield = shield.resize((round(d * float(arg("--shield-squash", 0.75))), round(d)), Image.LANCZOS)
    nh = np.asarray(Image.open(pkg / "layers" / "near_hand.png"))[..., 3] > 40
    yy, xx = np.nonzero(nh)
    centre = [float(xx.mean()) - shield.width * 0.15, float(yy.mean())]
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    layer.alpha_composite(shield, (round(centre[0] - shield.width / 2), round(centre[1] - shield.height / 2)))
    layer.save(pkg / "layers" / "shield_near.png")
    grip = [round(float(xx.mean())), round(float(yy.mean()))]
    L["shield_near"] = {"name": "shield_near", "order": L["near_hand"]["order"] + 0.5, "visible_default": True,
                        "points": {"root": grip, "grip": grip, "end": [round(centre[0] - shield.width / 2), round(centre[1])],
                                   "tip": [round(centre[0] - shield.width / 2), round(centre[1])]}}

    layers = sorted(L.values(), key=lambda l: l["order"])
    for i, l in enumerate(layers):
        l["order"] = i
    meta["layers"] = layers
    (pkg / "layers.json").write_text(json.dumps(meta, indent=1), encoding="utf-8")
    print("props placed: far_hand (fist), weapon_far, shield_near")


if __name__ == "__main__":
    main()
