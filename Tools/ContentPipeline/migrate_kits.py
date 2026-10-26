"""One-time migration: hero kit design (hero_kits.json) + built Spine rigs -> Mythwake hero content.

Writes Assets/Mythwake/Content/Heroes/<id>/<id>.hero.json (the data the game loads) and copies the
Spine skeleton, atlas and texture next to it. After this migration the .hero.json files are the
source of truth; edit them directly.

Hit times come from the Spine "hit" events of each animation, so damage lands on the frame the
animation shows it.

usage: py -3 migrate_kits.py <hero_kits.json> <Heroes_Spine dir> [--extra id=dir ...]
"""
import argparse
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "Mythwake" / "Content" / "Heroes"

FACTION = {"veil": "orderOfTheVeil", "fang": "tribeOfTheCrimsonFang", "empire": "empireOfTheShatteredSky",
           "hunt": "guildsOfTheHunt", "aether": "circleOfAether", "hollowed": "theHollowed"}
RACE = {"human": "human", "veyr": "veyr", "drakari": "drakari", "thalassari": "thalassari",
        "stoneborn": "stoneborn", "hollowed": "hollowed", "aetherial": "aetherial"}
ROLE = {"DPS": "dps", "Tank": "tank", "Support": "support", "Debuffer": "debuffer", "Hero Leader": "heroLeader"}
GRADE_FACTOR = {"common": 0.85, "rare": 1.0, "epic": 1.15, "legendary": 1.3, "transcendent": 1.5}
# level-1 base stats per role (grade factor applied on top)
ROLE_STATS = {
    "dps": dict(hp=1000, attack=100, defense=50, critRate=150),
    "tank": dict(hp=1700, attack=70, defense=90, critRate=80),
    "support": dict(hp=1150, attack=85, defense=60, critRate=100),
    "debuffer": dict(hp=1050, attack=90, defense=55, critRate=120),
    "heroLeader": dict(hp=1000, attack=80, defense=50, critRate=100),
}
# proc / secondary skills without a kit multiplier: 1.4x the grade's Normal Skill, times the shape factor
SHAPE = {"target": 1.0, "circle": 0.75, "row": 0.7, "all": 0.55}


# ---------------------------------------------------------------- effect helpers (multipliers in kit %)

def dmg(pct, **kw):
    """pct None = proc damage derived from the grade budget (see proc_multiplier)."""
    return {"kind": "damage", "multiplier": None if pct is None else round(pct * 10), **kw}


def heal(pct, **kw):
    return {"kind": "heal", "multiplier": round(pct * 10), **kw}


def shield(pct, dur=6000, scaling=None, **kw):
    e = {"kind": "shield", "multiplier": round(pct * 10), "shieldDurationMs": dur, **kw}
    if scaling:
        e["scaling"] = scaling
    return e


def status(kind, dur, value=0, **kw):
    return {"kind": "applyStatus", "status": {"kind": kind, "durationMs": dur, "value": value}, **kw}


def energy(n, **kw):
    return {"kind": "gainEnergy", "multiplier": n * 10, **kw}


def drain(n, **kw):
    return {"kind": "drainEnergy", "multiplier": n * 10, **kw}


def cleanse(n=0, **kw):
    return {"kind": "cleanse", "multiplier": n, **kw}


def steal(n=1, **kw):
    return {"kind": "stealBuff", "multiplier": n, **kw}


def delay(ms, **kw):
    return {"kind": "delayAction", "multiplier": ms, **kw}


def interrupt(**kw):
    return {"kind": "interrupt", **kw}


def lifesteal(pct):
    return {"kind": "lifesteal", "multiplier": round(pct * 10)}


# ---------------------------------------------------------------- trigger helpers

def cd(sec, first=None):
    t = {"kind": "cooldown", "cooldownMs": int(sec * 1000)}
    if first is not None:
        t["initialDelayMs"] = int(first * 1000)
    return t


def start(then=None):
    t = {"kind": "battleStart"}
    if then:
        t["cooldownMs"] = int(then * 1000)
    return t


def chance(p):
    return {"kind": "chanceOnAttack", "chancePermille": p * 10}


def nth(n):
    return {"kind": "everyNthAttack", "everyN": n}


def ally_below(p, sec=0, once=False):
    return {"kind": "allyBelowHp", "hpThresholdPermille": p * 10, "cooldownMs": int(sec * 1000), "oncePerBattle": once}


def self_below(p, sec=0, once=False):
    return {"kind": "selfBelowHp", "hpThresholdPermille": p * 10, "cooldownMs": int(sec * 1000), "oncePerBattle": once}


def enemy_below(p, sec):
    return {"kind": "enemyBelowHp", "hpThresholdPermille": p * 10, "cooldownMs": int(sec * 1000)}


def S(trigger, target, effects, hits=1, count=1):
    return dict(trigger=trigger, target=target, effects=effects, hits=hits, targetCount=count)


def U(target, effects, count=1):
    return dict(target=target, effects=effects, targetCount=count)


def P(kind, **kw):
    return {"kind": kind, **kw}


# ---------------------------------------------------------------- per-hero mechanics
# Reading of each kit text into rule data. "note" lists approximations of mechanics the rules do not model yet.

K = {
    "elara": dict(
        special=S(cd(6), "allyLowestHp", [heal(260)]),
        special2=S(start(14), "allyFrontRow", [shield(180)]),
        passive=[P("healingBonus", growth=40, cap=400)],
        ultimate=U("allyAll", [heal(420), cleanse(2), status("damageTakenDown", 4000, 200)])),
    "vesper": dict(
        special=S(cd(8), "enemyBackRow", [dmg(126), status("slow", 3000, 300)]),
        special2=S(ally_below(40, 12), "allyLowestHp", [shield(150)]),
        passive=[P("onHit", everyN=4, effect=status("sleep", 1000))],
        ultimate=U("enemyFrontRow", [dmg(490, bonusVsStatus="sleep", bonusVsStatusPermille=400)]),
        note=["Bosse: Verlangsamung statt Schlaf ist noch nicht abgebildet."]),
    "liriel": dict(
        special=S(cd(10), "enemyHighestAttack", [dmg(90), status("charm", 2000)]),
        special2=S(chance(30), "enemyBackRow", [dmg(None)]),
        passive=[P("dodge", value=200)],
        ultimate=U("enemyAll", [dmg(385), status("confuse", 3000)])),
    "seraphine": dict(
        passive=[P("leaderAura", healBonus=80)],
        note=["Hero Leader: kaempft nicht. Nur der Leader-Segen (+8 % Heilkraft) ist aktiv.",
              "Debuff-Widerstand, 'Stimme der Wahrheit' und 'Schleiersegen' brauchen Leader-Faehigkeiten im Kampf (noch nicht gebaut)."]),
    "maren": dict(
        special=S(cd(7), "allyLowestHp", [heal(140)], count=2),
        special2=S(cd(15), "enemyHighestAttack", [status("sleep", 1500)]),
        passive=[P("onHitTaken", team=True, effect=status("slow", 2000, 150))],
        ultimate=U("allyAll", [shield(300, 6000)]),
        note=["'in ihrer Naehe' ist als ganzes Team abgebildet."]),
    "ostric": dict(
        special=S(cd(9), "enemyFrontRow", [dmg(50), status("provoke", 3000), status("damageTakenDown", 3000, 250, target="self")]),
        special2=S(self_below(50, once=True), "self", [status("regen", 4000, 60)]),
        passive=[P("onHitTaken", effect=status("slow", 2000, 200))],
        ultimate=U("enemyAll", [dmg(310), status("sleep", 1500)])),
    "tobias": dict(
        special=S(cd(8), "enemyLowestHp", [dmg(110)]),
        special2=S(chance(25), "enemyFront", [dmg(60), status("slow", 2000, 200)]),
        passive=[P("onHit", effect=delay(500))],
        ultimate=U("enemyAll", [dmg(250)])),
    "kaelen": dict(
        special=S(cd(7), "enemyLowestHp", [dmg(230), status("burn", 3000, 150)], hits=2),
        special2=S(nth(4), "enemyCircle", [dmg(None, shape="circle")]),
        passive=[P("damageBonus", value=300, belowHp=500), P("attackSpeed", value=150, belowHp=500)],
        ultimate=U("enemyFrontRow", [dmg(630), status("burn", 4000, 150)])),
    "ignara": dict(
        special=S(cd(8), "enemyFront", [dmg(126)], count=2),
        special2=S(chance(30), "enemyFrontRow", [dmg(60), delay(1000)]),
        passive=[P("critRate", value=200, belowHp=500), P("onHit", belowHp=500, effect=status("burn", 2000, 150))],
        ultimate=U("enemyFrontRow", [dmg(490)]),
        note=["'Ziel und Gegner dahinter' ist als die zwei vordersten Gegner abgebildet."]),
    "kara": dict(
        special=S(cd(7), "enemyBackRow", [dmg(98)]),
        special2=S(self_below(40, once=True), "self", [status("immune", 2000), status("attackUp", 6000, 200)]),
        passive=[P("stackingAttackSpeed", value=30, cap=10, stackRegen=5)],
        ultimate=U("enemyFront", [dmg(280), status("attackSpeedUp", 5000, 400, target="self"), status("attackUp", 5000, 400, target="self")]),
        note=["Ultimate 'jeder Normal Skill trifft dreifach' ist als +40 % Angriffstempo und +40 % Angriff fuer 5 s abgebildet."]),
    "ysra": dict(
        special=S(cd(9), "allyFrontRow", [status("attackSpeedUp", 6000, 200)]),
        special2=S(cd(12), "allyLowestHp", [heal(200)]),
        passive=[P("damageBonus", team=True, value=150, belowHp=500), P("attackSpeed", team=True, value=100, belowHp=500)],
        ultimate=U("allyAll", [status("attackUp", 8000, 250), heal(250)])),
    "vala": dict(
        special=S(cd(6), "enemyLowestHp", [dmg(140)], hits=2),
        special2=S(cd(14), "enemyLowestHp", [status("damageTakenUp", 6000, 150)]),
        passive=[P("regen", value=15), P("damageBonus", growth=50)],
        ultimate=U("enemyLowestHp", [dmg(560, executeBonus=1000, executeBelowHp=300)]),
        note=["Der Wolf ist Teil von Valas Angriffen, keine eigene Einheit."]),
    "dorn": dict(
        special=S(cd(7), "enemyFront", [dmg(110), interrupt()]),
        passive=[P("damageBonus", value=200, belowHp=500)],
        ultimate=U("enemyFront", [dmg(450), status("attackUp", 5000, 100, target="allyAll")]),
        note=["'Mutiger Sprung' (Platzwechsel nach vorne) fehlt: Platzwechsel im Kampf ist noch nicht gebaut."]),
    "grom": dict(
        special=S(cd(9), "enemyFrontRow", [dmg(126), status("slow", 3000, 200)]),
        special2=S(self_below(60, 15), "self", [shield(20, 6000, scaling="casterMaxHp")]),
        passive=[P("damageBonus", value=250, belowHp=500), P("damageReduction", value=150, belowHp=500),
                 P("onHit", belowHp=500, effect=status("burn", 2000, 150))],
        ultimate=U("enemyAll", [dmg(385), status("stun", 1000)])),
    "veyl": dict(
        special=S(cd(10), "allyAll", [status("damageTakenDown", 6000, 250)]),
        special2=S(cd(12), "allyHighestEnergy", [energy(25)]),
        passive=[P("battleStart", target="allyFrontRow", effect=shield(160))],
        ultimate=U("enemyAll", [dmg(495), status("defenseUp", 8000, 150, target="allyAll")]),
        note=["'Kaiserlicher Befehl' (Konstrukt, das Angriffe auf sich zieht) ist als -25 % erlittener Schaden fuers Team abgebildet."]),
    "aldric": dict(
        special=S(cd(9), "allyFrontRow", [shield(180)]),
        special2=S(cd(14), "enemyHighestAttack", [status("provoke", 3000)], count=2),
        passive=[P("damageReduction", growth=30, cap=240), P("regen", value=5)],
        ultimate=U("allyAll", [status("damageTakenDown", 5000, 350)])),
    "pim": dict(
        special=S(cd(8), "enemyHighestAttack", [dmg(90), steal(1)]),
        special2=S(chance(30), "enemyBackRow", [dmg(None)]),
        passive=[P("periodic", intervalMs=15000, target="enemyHighestAttack", effect=steal(1))],
        ultimate=U("enemyAll", [dmg(385)]),
        note=["'Reverse Engineering' kopiert nicht, sondern stiehlt den Buff."]),
    "cassian": dict(
        special=S(cd(7), "enemyHighestAttack", [dmg(180)], hits=3),
        special2=S(chance(35), "enemyFront", [dmg(85), status("poison", 5000, 120)]),
        passive=[P("onHit", requireCrit=True, effect=steal(1))],
        ultimate=U("enemyLowestHp", [dmg(700, executeBonus=1000, executeBelowHp=250)])),
    "hilde": dict(
        special=S(cd(8), "enemyFrontRow", [dmg(98)]),
        special2=S(enemy_below(30, 10), "enemyLowestHp", [dmg(175)]),
        passive=[P("onHit", chance=150, effect=steal(1))],
        ultimate=U("enemyFrontRow", [dmg(390)])),
    "garrick": dict(
        special=S(cd(8), "enemyFront", [dmg(77)], count=2),
        special2=S(cd(15), "enemyFrontRow", [status("slow", 3000, 200)]),
        passive=[P("onHit", effect=delay(500))],
        ultimate=U("enemyAll", [dmg(250)])),
    "spellblade": dict(
        special=S(cd(8), "enemyLowestHp", [dmg(180), status("burn", 3000, 120)]),
        special2=S(chance(30), "enemyFrontRow", [dmg(None)]),
        passive=[P("onHit", belowHp=500, effect=status("burn", 2000, 120))],
        ultimate=U("enemyFrontRow", [dmg(490)]),
        note=["Rig hat noch keine skill/ultimate-Animation; Timing nutzt Standardwerte."]),
    "thalindra": dict(
        special=S(cd(6), "enemyLowestHp", [dmg(230)], count=2),
        special2=S(cd(15), "enemyBack", [status("stun", 2000)]),
        passive=[P("onHit", chance=150, effect=status("confuse", 1000))],
        ultimate=U("enemyAll", [dmg(495)]),
        note=["'gegnerischer Assassine' zielt auf die hintere Reihe (Rollen-Zielwahl kommt spaeter)."]),
    "ilyas": dict(
        special=S(cd(9), "enemyHighestAttack", [dmg(98), status("sleep", 2000)]),
        special2=S(cd(14), "enemyBackRow", [status("damageTakenUp", 6000, 120)]),
        passive=[P("onHit", effect=status("slow", 1000, 150))],
        ultimate=U("enemyFront", [dmg(560)])),
    "nyssa": dict(
        special=S(cd(7), "enemyLowestHp", [dmg(180)], hits=3),
        special2=S(cd(13), "enemyHighestAttack", [status("stun", 1500)]),
        passive=[P("onHit", effect=drain(5))],
        ultimate=U("enemyCircle", [dmg(525)])),
    "kai": dict(
        special=S(cd(9), "enemyFrontRow", [dmg(90), status("stun", 1500)]),
        special2=S(cd(15), "allyBackRow", [shield(160), shield(160, target="self")]),
        passive=[P("onHitTaken", effect=status("slow", 2000, 150))],
        ultimate=U("enemyAll", [dmg(385), status("slow", 3000, 200)])),
    "bombo": dict(
        special=S(cd(8), "enemyBackRow", [dmg(98)]),
        special2=S(cd(14), "enemyAll", [status("accuracyDown", 4000, 200)]),
        passive=[P("damageBonus", value=250, belowHp=500)],
        ultimate=U("enemyAll", [dmg(310)])),
    "durgan": dict(
        special=S(cd(8), "allyLowestHp", [heal(150)]),
        special2=S(cd(14), "allyAll", [status("attackUp", 6000, 100)]),
        passive=[P("regen", team=True, value=8)],
        ultimate=U("allyAll", [heal(220)])),
    "arion": dict(
        special=S(cd(8), "self", [status("attackSpeedUp", 6000, 600)]),
        special2=S(ally_below(30, 18), "allyLowestHp", [status("immune", 1500)]),
        passive=[P("periodic", intervalMs=15000, target="enemyLowestHp", effect=dmg(138))],
        ultimate=U("enemyAll", [dmg(495)]),
        note=["Echo-Kopie ist als +60 % Angriffstempo fuer 6 s abgebildet.",
              "Phasenverschiebung: Teleport fehlt, nur 1,5 s unverwundbar.",
              "'Zersplitterter Geist' kopiert keinen Special Skill, sondern feuert alle 15 s 138 % ATK aufs schwaechste Ziel."]),
    "celeste": dict(
        special=S(cd(9), "allyLowestHp", [heal(200)]),
        special2=S(cd(12), "allyFrontRow", [status("attackSpeedUp", 6000, 150), energy(10)]),
        passive=[P("onHit", effect=drain(5)), P("onHit", target="allyHighestEnergy", effect=energy(5))],
        ultimate=U("enemyAll", [status("stun", 2000), status("attackSpeedUp", 5000, 200, target="allyAll")]),
        note=["Rueckspulen heilt pauschal 200 % ATK statt 'Schaden der letzten 3 s'."]),
    "pyra": dict(
        special=S(cd(8), "enemyFrontRow", [status("burn", 5000, 196)]),
        special2=S(chance(30), "enemyBackRow", [dmg(None)]),
        passive=[P("damageBonus", value=300, belowHp=500)],
        ultimate=U("enemyAll", [dmg(310)])),
    "zephyr": dict(
        special=S(cd(7), "enemyRandom", [dmg(135)], count=3),
        special2=S(cd(14), "self", [shield(140)]),
        passive=[P("onHit", effect=delay(400)), P("onHit", chance=100, effect=status("stun", 1000))],
        ultimate=U("enemyAll", [dmg(385)]),
        note=["Sturmschild: Rueckschlag auf Angreifer (20 % ATK) fehlt noch."]),
    "nerissa": dict(
        special=S(cd(9), "self", [shield(25, 6000, scaling="casterMaxHp")]),
        special2=S(cd(13), "enemyFrontRow", [delay(1000)]),
        passive=[P("onHitTaken", chance=200, effect=steal(1))],
        ultimate=U("enemyAll", [dmg(310), status("stun", 1000)])),
    "kiro": dict(
        special=S(cd(7), "enemyLowestHp", [dmg(140)], hits=2),
        special2=S(cd(15), "self", [status("evade", 6000, 2)]),
        passive=[P("onHit", chance=150, effect=status("confuse", 1000))],
        ultimate=U("enemyAll", [dmg(310)])),
    "malachar": dict(
        special=S(cd(8), "enemyBackRow", [status("poison", 6000, 268)]),
        special2=S(cd(12), "enemyHighestAttack", [dmg(180), lifesteal(100)]),
        passive=[P("lifesteal", value=100), P("damageBonus", growth=40)],
        ultimate=U("enemyAll", [dmg(495)])),
    "noxia": dict(
        special=S(cd(7), "enemyBack", [dmg(180)], hits=2),
        passive=[P("lifesteal", value=120), P("onKill", effect=steal(99))],
        ultimate=U("enemyAll", [dmg(385), lifesteal(30)]),
        note=["Lebensraub (12 %) ist immer aktiv; 'ab Rang 3' kommt mit dem Rang-System.",
              "'gegnerischer Unterstuetzer' zielt auf die hintere Reihe."]),
    "vex": dict(
        special=S(cd(8), "enemyFrontRow", [dmg(126)]),
        special2=S(self_below(50, 10), "enemyFront", [dmg(180, missingHpBonus=1000)]),
        passive=[P("lifesteal", value=150), P("damageBonus", growth=40, cap=240)],
        ultimate=U("enemyFrontRow", [dmg(490)])),
    "morrigan": dict(
        special=S(cd(10), "enemyHighestAttack", [dmg(90), status("charm", 2000)]),
        special2=S(ally_below(40, 12), "allyLowestHp", [shield(150)]),
        passive=[P("battleStart", target="enemyAll", effect=status("accuracyDown", 999000, 100))],
        ultimate=U("enemyAll", [dmg(385), status("attackDown", 6000, 200), shield(150, target="allyAll")])),
    "mawrek": dict(
        special=S(cd(9), "enemyBack", [dmg(100), status("provoke", 2000)]),
        special2=S(cd(14), "self", [status("damageTakenDown", 3000, 300)]),
        passive=[P("onHit", effect=status("provoke", 3000))],
        ultimate=U("enemyFrontRow", [dmg(390), lifesteal(50)]),
        note=["Kettenzug (nach vorne ziehen) ist als Provokation abgebildet.",
              "Verschlingen (Schaden zu 30 % als Heilung) ist als -30 % erlittener Schaden abgebildet."]),
    "ysolde": dict(
        special=S(cd(10), "enemyHighestAttack", [dmg(90), status("stun", 2000)]),
        special2=S(cd(14), "allyLowestHp", [status("evade", 6000, 1)]),
        passive=[P("onHit", effect=status("confuse", 2000))],
        ultimate=U("enemyAll", [dmg(310)])),
    "wren": dict(
        special=S(cd(8), "enemyLowestHp", [dmg(110)]),
        special2=S(cd(15), "self", [status("evade", 15000, 1)]),
        passive=[P("damageBonus", growth=50, growthIntervalMs=8000, cap=300), P("regen", value=5)],
        ultimate=U("enemyAll", [dmg(250)])),
}

SHAPE_TARGET = {"ziel": "enemyFront", "kreis": "enemyCircle", "reihe": "enemyFrontRow", "alle": "enemyAll",
                "selbst": "self", "team": "allyAll"}


# ---------------------------------------------------------------- spine timing

def spine_timing(path):
    if not path or not path.exists():
        return {}
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    out = {}
    for name, anim in data.get("animations", {}).items():
        end = 0.0
        for section in anim.values():
            if not isinstance(section, dict):
                continue
            for item in section.values():
                if not isinstance(item, dict):
                    continue
                for timeline in item.values():
                    if isinstance(timeline, list) and timeline:
                        end = max(end, timeline[-1].get("time", 0))
        hits = [round(e.get("time", 0) * 1000) for e in anim.get("events", []) if e.get("name") == "hit"]
        out[name] = (round(end * 1000), hits)
    return out


DEFAULT_TIMING = {"attack": (950, [400]), "skill": (1400, [800]), "ultimate": (1950, [1300])}


def timing(timings, anim, hits):
    duration, times = timings.get(anim, DEFAULT_TIMING[anim])
    times = sorted(times) or list(DEFAULT_TIMING[anim][1])
    while len(times) < hits:
        times.append(times[-1] + 120)
    times = times[:hits]
    duration = max(duration, times[-1] + 150)
    return times, duration


# ---------------------------------------------------------------- build

def clean_text(s):
    return (s or "").strip()


def proc_multiplier(grade, shape):
    return grade_budget[grade][0] * 1.4 * SHAPE[shape]


def resolve(effects, grade, default_shape):
    out = []
    for e in effects:
        e = dict(e)
        if e["kind"] == "damage" and e.get("multiplier") is None:
            shape = e.pop("shape", default_shape)
            e["multiplier"] = round(proc_multiplier(grade, shape) * 10)
        e.pop("shape", None)
        if "target" in e and e["target"] is None:
            del e["target"]
        out.append(e)
    return out


def shape_of(target):
    if target in ("enemyFrontRow", "enemyBackRow"):
        return "row"
    if target == "enemyCircle":
        return "circle"
    if target == "enemyAll":
        return "all"
    return "target"


def skill_json(hero_id, slot, kit, spec, timings, grade, animation):
    hits = spec.get("hits", kit.get("hits") or 1) if spec else (kit.get("hits") or 1)
    times, duration = timing(timings, animation, hits)
    return {
        "id": f"{hero_id}.{slot}",
        "name": clean_text(kit.get("name")),
        "description": clean_text(kit.get("text")),
        "slot": slot,
        "trigger": spec["trigger"],
        "target": spec["target"],
        "targetCount": spec.get("targetCount", 1),
        "effects": resolve(spec["effects"], grade, shape_of(spec["target"])),
        "hits": hits,
        "hitTimesMs": times,
        "durationMs": duration,
        "animation": animation,
    }


def build(kit, timings):
    hid = kit["id"]
    grade = kit["grade"]
    role = ROLE[kit["type"]]
    spec = K[hid]
    front = kit["row"] == "vorne"
    base = ROLE_STATS[role]
    gf = GRADE_FACTOR[grade]
    body = kit["body"]
    body = "small" if "klein" in body else "large" if "groß" in body else "monster" if "Monster" in body else "normal"

    hero = {
        "id": hid,
        "name": kit["name"],
        "title": kit.get("title", ""),
        "faction": FACTION[kit["faction"]],
        "race": RACE[kit["race"]],
        "aspect": kit["aspect"],
        "role": role,
        "row": "front" if front else "back",
        "grade": grade,
        "body": body,
        "spineId": hid,
        "growthPermille": 80,
        "baseStats": {
            "hp": round(base["hp"] * gf),
            "attack": round(base["attack"] * gf),
            "defense": round(base["defense"] * gf),
            "attackIntervalMs": 1500 if front else 1700,
            "critRate": base["critRate"],
            "critDamage": 500,
        },
        "designNotes": spec.get("note", []),
    }

    n = kit.get("normal") or {}
    if n.get("mult"):
        hero["normal"] = skill_json(hid, "normal", n, {
            "trigger": {"kind": "attackInterval"}, "target": "enemyFront",
            "effects": [dmg(n["mult"])], "hits": n.get("hits") or 1}, timings, grade, "attack")
    for slot in ("special", "special2"):
        if slot in spec and kit.get(slot):
            hero[slot] = skill_json(hid, slot, kit[slot], spec[slot], timings, grade, "skill")
    if kit.get("passive"):
        p = kit["passive"]
        hero["passive"] = {
            "id": f"{hid}.passive", "name": clean_text(p.get("name")), "description": clean_text(p.get("text")),
            "slot": "passive", "trigger": {"kind": "passive"}, "target": "self",
            "passives": [resolve_passive(x, grade) for x in spec.get("passive", [])],
        }
    if role == "heroLeader":
        # the Leader Blessing is written as the leader's "special" in the kit; it lives in the passive slot here
        hero["passive"]["name"] = clean_text(kit["special"]["name"])
        hero["passive"]["description"] = clean_text(kit["special"]["text"])
    u = kit.get("ultimate")
    if u and "ultimate" in spec:
        us = dict(spec["ultimate"])
        us["trigger"] = {"kind": "energy"}
        us["hits"] = u.get("hits") or 1
        hero["ultimate"] = skill_json(hid, "ultimate", u, us, timings, grade, "ultimate")
    return hero


def resolve_passive(p, grade):
    p = dict(p)
    if p.get("effect"):
        p["effect"] = resolve([p["effect"]], grade, "target")[0]
    return p


grade_budget = {}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("kits")
    ap.add_argument("spine_dir")
    ap.add_argument("--extra", action="append", default=[], help="id=path/to/skeleton.json for rigs outside spine_dir")
    args = ap.parse_args()

    kits = json.loads(Path(args.kits).read_text(encoding="utf-8"))
    grade_budget.update(kits["budget"])
    grade_budget.setdefault("transcendent", [120, 280, 1100])
    extras = dict(x.split("=", 1) for x in args.extra)
    spine_dir = Path(args.spine_dir)

    OUT.mkdir(parents=True, exist_ok=True)
    report = []
    for kit in kits["heroes"]:
        hid = kit["id"]
        skel = Path(extras[hid]) if hid in extras else spine_dir / hid / f"hero_{hid}.json"
        timings = spine_timing(skel)
        hero = build(kit, timings)
        dest = OUT / hid
        dest.mkdir(exist_ok=True)
        (dest / f"{hid}.hero.json").write_text(json.dumps(hero, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
        if skel.exists():
            stem = skel.with_suffix("")
            for src, name in ((skel, f"{hid}.json"), (Path(str(stem) + ".atlas.txt"), f"{hid}.atlas.txt"), (stem.with_suffix(".png"), f"{hid}.png")):
                if src.exists():
                    shutil.copyfile(src, dest / name)
            fix_atlas_page(dest / f"{hid}.atlas.txt", f"{hid}.png")
        report.append(f"{hid:12} {'rig' if skel.exists() else 'NO RIG':7} notes={len(hero['designNotes'])}")
    print("\n".join(report))


def fix_atlas_page(atlas, page):
    """The atlas references its texture by file name; keep it in sync with the renamed png."""
    if not atlas.exists():
        return
    lines = atlas.read_text(encoding="utf-8").splitlines()
    for i, line in enumerate(lines):
        if line.strip().endswith(".png"):
            lines[i] = page
            break
    atlas.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
