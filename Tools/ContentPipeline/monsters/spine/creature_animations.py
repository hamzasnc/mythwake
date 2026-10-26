"""Generic animations for creature rigs (build_creature.py), driven by part roles and names.

Values are offsets from the setup pose: rotation in degrees counter-clockwise, translation in Spine units (y up).
Creatures face +x. Sign conventions (setup pose facing right):
  body / head  +  = rear up / lean back,      -  = lunge / dip forward
  jaw          -  = open
  legs/arms    +  = swing forward (towards +x) for limbs hanging down
Hit events sit at the times the battle rules use (the monster's hitTimesMs), so damage numbers land on the strike.
"""
import math

EASE = {"smooth": (0.33, 0.0, 0.67, 1.0), "out": (0.18, 0.75, 0.35, 1.0), "in": (0.6, 0.0, 0.85, 0.35),
        "snap": (0.05, 0.9, 0.2, 1.0), "linear": None}


def _curve(t0, v0, t1, v1, e):
    c = EASE[e]
    if c is None:
        return None
    return [round(t0 + (t1 - t0) * c[0], 4), round(v0 + (v1 - v0) * c[1], 4), round(t0 + (t1 - t0) * c[2], 4), round(v0 + (v1 - v0) * c[3], 4)]


def rot(keys):
    out = []
    for i, (t, v, e) in enumerate(keys):
        k = {"time": round(t, 4), "value": round(v, 3)}
        if i + 1 < len(keys):
            c = _curve(t, v, keys[i + 1][0], keys[i + 1][1], e)
            if c:
                k["curve"] = c
        out.append(k)
    return out


def xy(keys):
    out = []
    for i, (t, x, y, e) in enumerate(keys):
        k = {"time": round(t, 4), "x": round(x, 3), "y": round(y, 3)}
        if i + 1 < len(keys):
            n = keys[i + 1]
            cx, cy = _curve(t, x, n[0], n[1], e), _curve(t, y, n[0], n[2], e)
            if cx:
                k["curve"] = cx + cy
        out.append(k)
    return out


def wave(amp, period, phase, dur, n=8):
    w = 2 * math.pi / period
    f = lambda t: amp * math.sin(w * t - phase)
    df = lambda t: amp * w * math.cos(w * t - phase)
    ts = [dur * i / n for i in range(n + 1)]
    out = []
    for i, t in enumerate(ts):
        k = {"time": round(t, 4), "value": round(f(t), 3)}
        if i < n:
            t1, h = ts[i + 1], (ts[i + 1] - t) / 3
            k["curve"] = [round(t + h, 4), round(f(t) + df(t) * h, 4), round(t1 - h, 4), round(f(t1) - df(t1) * h, 4)]
        out.append(k)
    return out


def scale_keys(keys, s, delay=0.0, end=None):
    end = end if end is not None else keys[-1][0]
    return [(min(t + delay, end) if t > 0 else t, v * s, e) for t, v, e in keys]


def build_animations(WORLD, LOCAL, PARENT, bones, slots, slot_bone, own, chain, role, body, variants, height, timing=None):
    timing = timing or {}
    H = height / 230.0                       # motion scales with the creature's size (heroes are 230 units)
    has = set(bones)
    body_bone = own.get(body, body)
    head = next((own[n] for n, r in role.items() if r == "head" and n in own), None)
    jaw = next((own[n] for n, r in role.items() if r == "jaw" and n in own), None)
    chains = {n: b for n, b in chain.items() if n != body}
    body_chain = chain.get(body)
    necks = {n: b for n, b in chains.items() if "neck" in n}
    wings = {n: b for n, b in chains.items() if "wing" in n}
    tails = {n: b for n, b in chains.items() if n not in necks and n not in wings}
    rigid_wings = [own[n] for n in role if "wing" in n and n in own and n not in chain]

    def limbs(*words):
        return [own[n] for n, r in role.items() if r == "limb" and n in own and own[n] == n and all(w in n for w in words)]

    front_up = limbs("leg", "front", "upper") + limbs("leg_1", "upper") + limbs("arm", "upper") + [b for b in limbs("arm") if "upper" not in b and "lower" not in b]
    front_lo = limbs("leg", "front", "lower") + limbs("leg_1", "lower") + limbs("arm", "lower")
    hind_up = limbs("leg", "hind", "upper") + limbs("leg_3", "upper") + limbs("leg", "far", "upper") + limbs("leg", "near", "upper")
    hind_up = [b for b in hind_up if b not in front_up and "front" not in b]
    mid_up = limbs("leg_2", "upper")
    single_legs = [b for b in limbs("leg") if "upper" not in b and "lower" not in b]
    hands = limbs("hand")
    # floating props (halos, orbiting shards) move on their own; carried ones (shell, shields, weapons) stay rigid
    CARRIED = ("shell", "shield", "halberd", "weapon", "crown", "cape", "quiver", "hat")
    props = [own[n] for n, r in role.items() if r == "prop" and n in own and own[n] == n and not any(w in n for w in CARRIED)]
    glows = [s for s in slots if s.startswith("glow")]
    eyes = [s for s in slots if s in variants.values()]
    humanoid = any("arm_" in b for b in front_up) and not any("leg_front" in b or "leg_1" in b for b in front_up)
    # heavy creatures move less; legs of four- and six-legged bodies only swing a little (their joints are covered
    # by the body, a big swing would open the cut)
    MASS = 0.65 if height >= 250 else 1.0
    LEG = 1.0 if humanoid else 0.5

    def leg(keys):
        return [(t, v * LEG, e) for t, v, e in keys]

    def mass(keys):
        return [(t, v * MASS, e) for t, v, e in keys]

    def body_keys(B, keys):
        keys = mass(keys)
        if body_chain:
            for k, b in enumerate(body_chain):
                B[b] = {"rotate": rot(scale_keys(keys, 0.6 + 0.25 * k, 0.03 * k))}
        else:
            B[body_bone] = {"rotate": rot(keys)}

    def secondary(B, impulse, strength=1.0, skip=()):
        """Chains swing after the body: each bone a bit later and stronger, against the body's motion."""
        for i, (n, ch) in enumerate(sorted(tails.items()) + sorted(necks.items())):
            if n in skip:
                continue
            sgn = 0.6 if n in necks else -1.0
            for k, b in enumerate(ch):
                # chain rotations add up bone by bone: keep each link small
                B[b] = {"rotate": rot(scale_keys(impulse, sgn * (0.22 + 0.08 * k) * strength, 0.04 * (k + 1) + 0.01 * (i % 3)))}

    def events(names_times):
        return [{"time": round(t, 4), "name": n} for n, t in sorted(names_times, key=lambda x: x[1])]

    A = {}
    # ------------------------------------------------------------ idle (loop)
    D = 2.0
    B = {"root": {"translate": xy([(0, 0, 0, "smooth"), (D / 2, 0, 2.2 * H, "smooth"), (D, 0, 0, "linear")])}}
    if body_chain:
        for k, b in enumerate(body_chain):
            B[b] = {"rotate": wave(3.0 + 1.5 * k, D, math.pi * (0.2 + 0.35 * k), D)}
    else:
        B[body_bone] = {"rotate": wave(1.4, D, 0.0, D),
                        "scale": xy([(0, 1.0, 1.0, "smooth"), (D / 2, 1.015, 0.985, "smooth"), (D, 1.0, 1.0, "linear")])}
    if head:
        B[head] = {"rotate": wave(3.0, D, math.pi * 0.45, D)}
    if jaw:
        B[jaw] = {"rotate": wave(-2.5, D, math.pi * 0.6, D)}
    for i, (n, ch) in enumerate(sorted(tails.items())):
        for k, b in enumerate(ch):
            B[b] = {"rotate": wave((3.5 + 2.5 * k) * (1 if i % 2 == 0 else -1), D, math.pi * (0.5 + 0.35 * k + 0.2 * i), D)}
    for n, ch in necks.items():
        for k, b in enumerate(ch):
            B[b] = {"rotate": wave(1.6 + 0.8 * k, D, math.pi * (0.3 + 0.2 * k), D)}
    for i, (n, ch) in enumerate(sorted(wings.items())):
        for k, b in enumerate(ch):
            B[b] = {"rotate": wave(max(1.0, 4.0 - 1.2 * k), D / 2, math.pi * (0.15 * k + 0.5 * i), D)}
    for b in rigid_wings:
        B[b] = {"rotate": wave(7.0, D / 2, 0.0, D)}
    for b in front_up + hind_up + mid_up:
        B[b] = {"rotate": wave(1.5 if not humanoid else 3.0, D, math.pi * (0.4 if b in front_up else 0.9), D)}
    for b in props:
        B[b] = {"translate": xy([(0, 0, 0, "smooth"), (D / 2, 0, 5 * H, "smooth"), (D, 0, 0, "linear")]),
                "rotate": wave(4.0, D, math.pi * 0.3, D)}
    S = {}
    for e in eyes:
        closed = next(v for v, b in variants.items() if b == e)
        S[e] = {"attachment": [{"time": 0, "name": e}, {"time": 1.3, "name": closed}, {"time": 1.42, "name": e}]}
    for g in glows:
        S[g] = {"rgba": [{"time": 0, "color": "ffffffaa"}, {"time": D / 2, "color": "ffffffff"}, {"time": D, "color": "ffffffaa"}]}
    A["idle"] = {"bones": B, "slots": S}

    # ------------------------------------------------------------ attack: wind-up, lunge, strike, recover
    dur_a, hits_a = timing.get("attack", (0.95, [0.4]))
    t_hit = hits_a[0]
    t_w = t_hit * 0.62
    lean = [(0, 0, "smooth"), (t_w, 8, "snap"), (t_hit, -10, "out"), (t_hit + 0.22, -4, "smooth"), (dur_a, 0, "linear")]
    B = {"root": {"translate": xy([(0, 0, 0, "smooth"), (t_w, -12 * H, 2 * H, "snap"), (t_hit, 34 * H, 0, "out"),
                                   (t_hit + 0.25, 14 * H, 0, "smooth"), (dur_a, 0, 0, "linear")])}}
    body_keys(B, lean)
    if head:
        B[head] = {"rotate": rot([(0, 0, "smooth"), (t_w, 12, "snap"), (t_hit, -16, "out"), (t_hit + 0.25, -5, "smooth"), (dur_a, 0, "linear")])}
    if jaw:
        B[jaw] = {"rotate": rot([(0, 0, "smooth"), (t_w, -6, "snap"), (t_hit, -30, "out"), (t_hit + 0.18, -8, "smooth"), (dur_a, 0, "linear")])}
    strike = front_up if not humanoid else [b for b in front_up if "near" in b] or front_up
    for b in front_up:
        main = b in strike
        if humanoid:
            keys = [(0, 0, "smooth"), (t_w, -55 if main else -15, "snap"), (t_hit, 70 if main else 20, "out"), (t_hit + 0.25, 25 if main else 8, "smooth"), (dur_a, 0, "linear")]
        else:
            keys = leg([(0, 0, "smooth"), (t_w, 22, "snap"), (t_hit, -18, "out"), (dur_a, 0, "linear")])
        B[b] = {"rotate": rot(keys)}
    for b in front_lo:
        B[b] = {"rotate": rot(leg([(0, 0, "smooth"), (t_w, 20 if humanoid else -25, "snap"), (t_hit, -10 if humanoid else 8, "out"), (dur_a, 0, "linear")]))}
    for b in hind_up:
        B[b] = {"rotate": rot(leg([(0, 0, "smooth"), (t_w, 6, "snap"), (t_hit, -14, "out"), (dur_a, 0, "linear")]))}
    secondary(B, lean)
    for n, ch in wings.items():
        for k, b in enumerate(ch):
            w_ = max(1.5, 7 - 2.5 * k)
            B[b] = {"rotate": rot([(0, 0, "smooth"), (t_w, w_, "snap"), (t_hit, -w_, "out"), (dur_a, 0, "linear")])}
    A["attack"] = {"bones": B, "events": events([("hit", t) for t in hits_a])}

    # ------------------------------------------------------------ skill: rear up / roar, then slam
    dur_s, hits_s = timing.get("skill", (1.4, [0.8]))
    t_hit = hits_s[0]
    t_c = t_hit * 0.7
    lean = [(0, 0, "smooth"), (t_c, 14, "smooth"), (t_hit, -12, "snap"), (t_hit + 0.3, -4, "smooth"), (dur_s, 0, "linear")]
    B = {"root": {"translate": xy([(0, 0, 0, "smooth"), (t_c, -6 * H, 12 * H, "smooth"), (t_hit, 18 * H, -2 * H, "snap"),
                                   (t_hit + 0.3, 6 * H, 0, "smooth"), (dur_s, 0, 0, "linear")])}}
    body_keys(B, lean)
    if head:
        B[head] = {"rotate": rot([(0, 0, "smooth"), (t_c, 22, "smooth"), (t_hit, -14, "snap"), (dur_s, 0, "linear")])}
    if jaw:
        B[jaw] = {"rotate": rot([(0, 0, "smooth"), (t_c * 0.6, -10, "smooth"), (t_c, -38, "snap"), (t_hit + 0.2, -20, "smooth"), (dur_s, 0, "linear")])}
    for b in front_up:
        B[b] = {"rotate": rot(leg([(0, 0, "smooth"), (t_c, 110 if humanoid else 30, "smooth"), (t_hit, 35 if humanoid else -20, "snap"), (dur_s, 0, "linear")]))}
    for b in front_lo:
        B[b] = {"rotate": rot(leg([(0, 0, "smooth"), (t_c, 25 if humanoid else -35, "smooth"), (t_hit, 0, "snap"), (dur_s, 0, "linear")]))}
    for b in hind_up:
        B[b] = {"rotate": rot(leg([(0, 0, "smooth"), (t_c, -10, "smooth"), (t_hit, 8, "snap"), (dur_s, 0, "linear")]))}
    secondary(B, lean, 1.2)
    for n, ch in wings.items():
        for k, b in enumerate(ch):
            w_ = max(2.0, 10 - 3 * k)
            B[b] = {"rotate": rot([(0, 0, "smooth"), (t_c, w_, "smooth"), (t_hit, -w_, "snap"), (dur_s, 0, "linear")])}
    S = {g: {"rgba": [{"time": 0, "color": "ffffffff"}, {"time": t_c, "color": "ffffff66"}, {"time": t_hit, "color": "ffffffff"},
                      {"time": dur_s, "color": "ffffffff"}]} for g in glows}
    A["skill"] = {"bones": B, "slots": S, "events": events([("cast", t_c)] + [("hit", t) for t in hits_s])}

    # ------------------------------------------------------------ ultimate: big wind-up, roar, several heavy hits
    dur_u, hits_u = timing.get("ultimate", (1.95, [1.3]))
    t1 = hits_u[0]
    t_c = t1 * 0.62
    lean = [(0, 0, "smooth"), (t_c, 11, "smooth"), (t1 - 0.08, 13, "in"), (t1, -10, "snap")]
    for k, t in enumerate(hits_u[1:]):
        lean.append((t, -10 + (3 if k % 2 == 0 else -2), "snap"))
    lean += [(min(dur_u - 0.05, hits_u[-1] + 0.3), -4, "smooth"), (dur_u, 0, "linear")]
    root_keys = [(0, 0, 0, "smooth"), (t_c, -8 * H, 10 * H, "smooth"), (t1, 24 * H, -3 * H, "snap")]
    for k, t in enumerate(hits_u[1:]):
        root_keys.append((t, (26 - 4 * (k + 1)) * H, (-3 if k % 2 else 3) * H, "snap"))
    root_keys += [(min(dur_u - 0.05, hits_u[-1] + 0.3), 8 * H, 0, "smooth"), (dur_u, 0, 0, "linear")]
    B = {"root": {"translate": xy(root_keys)}}
    body_keys(B, lean)
    if head:
        B[head] = {"rotate": rot([(0, 0, "smooth"), (t_c, 28, "smooth"), (t1, -18, "snap"), (dur_u, 0, "linear")])}
    if jaw:
        B[jaw] = {"rotate": rot([(0, 0, "smooth"), (t_c * 0.5, -12, "smooth"), (t_c, -45, "snap"), (hits_u[-1] + 0.1, -30, "smooth"), (dur_u, 0, "linear")])}
    for b in front_up:
        k_ = [(0, 0, "smooth"), (t_c, 130 if humanoid else 38, "smooth"), (t1, 40 if humanoid else -24, "snap")]
        for k, t in enumerate(hits_u[1:]):
            k_.append((t, (70 if k % 2 == 0 else 40) if humanoid else (-8 if k % 2 == 0 else -24), "snap"))
        k_.append((dur_u, 0, "linear"))
        B[b] = {"rotate": rot(leg(k_))}
    for b in hind_up:
        B[b] = {"rotate": rot(leg([(0, 0, "smooth"), (t_c, -14, "smooth"), (t1, 10, "snap"), (dur_u, 0, "linear")]))}
    secondary(B, lean, 1.4)
    for n, ch in wings.items():
        for k, b in enumerate(ch):
            w_ = max(2.0, 12 - 3.5 * k)
            flap = [(0, 0, "smooth"), (t_c * 0.5, w_, "smooth"), (t_c, -w_ * 0.7, "smooth"), (t1 - 0.05, w_ * 1.2, "in"), (t1, -w_, "snap"),
                    (dur_u, 0, "linear")]
            B[b] = {"rotate": rot(flap)}
    for b in props:
        B[b] = {"translate": xy([(0, 0, 0, "smooth"), (t_c, 0, 16 * H, "smooth"), (t1, 0, -4 * H, "snap"), (dur_u, 0, 0, "linear")]),
                "rotate": rot([(0, 0, "smooth"), (t1, 180, "out"), (dur_u, 360, "linear")])}
    S = {g: {"rgba": [{"time": 0, "color": "ffffffff"}, {"time": t_c, "color": "ffffff55"}, {"time": t1, "color": "ffffffff"},
                      {"time": dur_u, "color": "ffffffff"}]} for g in glows}
    A["ultimate"] = {"bones": B, "slots": S,
                     "events": events([("cast", t_c), ("shake", t1)] + [("hit", t) for t in hits_u])}

    # ------------------------------------------------------------ hit: recoil and a red flash
    D = 0.36
    B = {"root": {"translate": xy([(0, 0, 0, "snap"), (0.07, -10 * H, 0, "smooth"), (D, 0, 0, "linear")])}}
    body_keys(B, [(0, 0, "snap"), (0.07, 7, "smooth"), (D, 0, "linear")])
    if head:
        B[head] = {"rotate": rot([(0, 0, "snap"), (0.08, 10, "smooth"), (D, 0, "linear")])}
    secondary(B, [(0, 0, "snap"), (0.07, 7, "smooth"), (D, 0, "linear")], 0.8)
    S = {}
    for s in slots:
        if s in glows:
            continue
        S[s] = {"rgba": [{"time": 0, "color": "ffffffff"}, {"time": 0.04, "color": "ff8a8aff"}, {"time": 0.22, "color": "ffffffff"}]}
    A["hit"] = {"bones": B, "slots": S}

    # ------------------------------------------------------------ death: stagger, collapse, fade
    D = 1.3
    lean = [(0, 0, "smooth"), (0.18, 12, "out"), (0.7, -18, "in"), (0.85, -15, "smooth"), (D, -16, "linear")]
    B = {"root": {"translate": xy([(0, 0, 0, "smooth"), (0.18, -8 * H, 6 * H, "out"), (0.7, -4 * H, -10 * H, "in"), (D, -4 * H, -10 * H, "linear")])}}
    body_keys(B, lean)
    if head:
        B[head] = {"rotate": rot([(0, 0, "smooth"), (0.18, 18, "out"), (0.75, -30, "in"), (D, -28, "linear")])}
    if jaw:
        B[jaw] = {"rotate": rot([(0, 0, "smooth"), (0.2, -26, "out"), (0.8, -14, "smooth"), (D, -14, "linear")])}
    for b in front_up + hind_up + mid_up + single_legs:
        B[b] = {"rotate": rot(leg([(0, 0, "smooth"), (0.7, 18 if b in front_up else -14, "in"), (D, 18 if b in front_up else -14, "linear")]))}
    for n, ch in sorted(tails.items()) + sorted(wings.items()):
        for k, b in enumerate(ch):
            B[b] = {"rotate": rot([(0, 0, "smooth"), (0.75, -6 - 2 * k, "in"), (D, -7 - 2 * k, "linear")])}
    for b in props:
        B[b] = {"translate": xy([(0, 0, 0, "smooth"), (0.9, 0, -40 * H, "in"), (D, 0, -40 * H, "linear")])}
    S = {s: {"rgba": [{"time": 0, "color": "ffffffff"}, {"time": 0.75, "color": "ffffffff"}, {"time": D, "color": "ffffff00"}]} for s in slots}
    A["death"] = {"bones": B, "slots": S}
    return A
