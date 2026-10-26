"""Writes the monster definitions (Assets/Mythwake/Content/Monsters/<id>/<id>.monster.json) from roster.json + the kits below.

The .monster.json files use the hero schema (stats, skills, passives) plus group/boss/size. Run again after changing
roster.json or the kits; it overwrites the generated files.

usage: py -3 make_monster_data.py
"""
import importlib.util
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
OUT = ROOT / "Assets" / "Mythwake" / "Content" / "Monsters"

# reuse the effect / trigger helpers of the hero migration
spec = importlib.util.spec_from_file_location("migrate_kits", HERE.parent / "migrate_kits.py")
mk = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mk)
dmg, heal, shield, status, steal, delay, lifesteal, drain = mk.dmg, mk.heal, mk.shield, mk.status, mk.steal, mk.delay, mk.lifesteal, mk.drain
cd, start, chance, self_below, P = mk.cd, mk.start, mk.chance, mk.self_below, mk.P

ROLE_STATS = {
    "dps": dict(hp=1000, attack=100, defense=50, critRate=150),
    "tank": dict(hp=1700, attack=70, defense=90, critRate=80),
    "support": dict(hp=1150, attack=85, defense=60, critRate=100),
    "debuffer": dict(hp=1050, attack=90, defense=55, critRate=120),
}
BOSS = dict(hp=2.5, attack=1.2, defense=1.2)
# normal monsters fight next to early (Common/Rare) heroes: slightly below the Rare hero baseline
NORMAL = dict(hp=0.85, attack=0.85, defense=0.85)
SIZE = {"normal": 1000, "boss": 1350, "raid": 1600}


def N(name, mult, target="enemyFront", hits=1, effects=None):
    return dict(name=name, trigger={"kind": "attackInterval"}, target=target, effects=[dmg(mult)] + (effects or []), hits=hits)


def S(name, trigger, target, effects, hits=1, count=1):
    return dict(name=name, trigger=trigger, target=target, effects=effects, hits=hits, targetCount=count)


def U(name, target, effects, hits=1):
    return dict(name=name, trigger={"kind": "energy"}, target=target, effects=effects, hits=hits)


K = {
    "forest_wolf": dict(
        normal=N("Biss", 100),
        special=S("Rudeljagd", cd(7), "enemyLowestHp", [dmg(170)], hits=2),
        passive=("Hetzjagd", [P("damageBonus", value=200, belowHp=500)])),
    "wild_boar": dict(
        normal=N("Hauerstoß", 90),
        special=S("Ansturm", cd(9), "enemyFront", [dmg(150), status("stun", 1000)]),
        passive=("Dickes Fell", [P("damageReduction", value=100)])),
    "corrupted_wolf": dict(
        normal=N("Fragmentbiss", 100),
        special=S("Wilder Sprung", cd(7), "enemyLowestHp", [dmg(180), status("burn", 2000, 120)], hits=2),
        passive=("Verdorbene Fänge", [P("onHit", chance=200, effect=status("burn", 2000, 120))])),
    "corrupted_boar": dict(
        normal=N("Kristallhauer", 90),
        special=S("Splitteransturm", cd(9), "enemyFrontRow", [dmg(120), status("slow", 2000, 200)]),
        passive=("Wucherndes Fleisch", [P("regen", value=15)])),
    "corrupted_crow": dict(
        range="melee",
        normal=N("Schnabelhieb", 85, target="enemyBack"),
        special=S("Drittes Auge", cd(10), "enemyHighestAttack", [dmg(80), status("confuse", 2000)]),
        passive=("Flatterflug", [P("dodge", value=150)])),
    "hollowed_soldier": dict(
        normal=N("Schartiger Hieb", 90),
        special=S("Schildwall", cd(10), "self", [shield(30, 6000, scaling="casterMaxHp")]),
        passive=("Kristallhaut", [P("damageReduction", growth=30, cap=150)])),
    "hollowed_archer": dict(
        normal=N("Kristallpfeil", 95, target="enemyBack"),
        special=S("Schlafpfeil", cd(9), "enemyHighestAttack", [dmg(110), status("sleep", 1500)]),
        passive=("Lähmende Spitzen", [P("onHit", chance=150, effect=status("slow", 1000, 150))])),
    "crimson_wraith": dict(
        normal=N("Purpurflamme", 100),
        special=S("Feuerkreis", cd(8), "enemyCircle", [dmg(140), status("burn", 3000, 150)]),
        passive=("Glühender Zorn", [P("damageBonus", value=250, belowHp=500)])),
    "riftborn_stalker": dict(
        normal=N("Klingenfinger", 95, hits=2),
        special=S("Rift-Schritt", cd(7), "enemyBack", [dmg(180)], hits=3),
        passive=("Diebischer Riss", [P("onHit", chance=200, effect=steal(1))])),
    "riftborn_wisp": dict(
        normal=N("Lichtfunke", 70),
        special=S("Rift-Licht", cd(7), "allyLowestHp", [heal(200)]),
        passive=("Schimmernde Nähe", [P("regen", team=True, value=5)])),
    "fragment_construct": dict(
        normal=N("Steinfaust", 85),
        special=S("Kernpuls", cd(10), "enemyFrontRow", [dmg(110), shield(25, 6000, scaling="casterMaxHp", target="self")]),
        passive=("Instabiler Kern", [P("damageReduction", value=150)])),

    "hollowtusk_alpha": dict(
        normal=N("Hauerschlag", 120),
        special=S("Rottenansturm", cd(8), "enemyFrontRow", [dmg(170), status("stun", 1500)]),
        special2=S("Hohler Schrei", self_below(50, once=True), "self", [status("attackUp", 8000, 300), status("regen", 5000, 50)]),
        passive=("Leitkeiler", [P("damageBonus", growth=50), P("regen", value=10)]),
        ultimate=U("Wilde Raserei", "enemyAll", [dmg(450), status("slow", 3000, 250)], hits=3)),
    "riftborn_treant": dict(
        normal=N("Wurzelschlag", 110),
        special=S("Wurzelgriff", cd(9), "enemyBackRow", [dmg(140), status("stun", 1500)]),
        special2=S("Rift-Saft", cd(14), "self", [status("regen", 6000, 40)]),
        passive=("Uralte Rinde", [P("regen", value=15), P("damageReduction", value=150)]),
        ultimate=U("Mossveil-Zorn", "enemyAll", [dmg(420), status("poison", 5000, 120)], hits=2)),
    "crimson_wraith_adept": dict(
        normal=N("Purpurstrahl", 120, target="enemyBack"),
        special=S("Flammenrune", cd(8), "enemyAll", [dmg(130), status("burn", 4000, 150)]),
        special2=S("Wraith-Schleier", self_below(50, 15), "self", [status("immune", 2000), status("damageTakenDown", 6000, 300)]),
        passive=("Halb verwandelt", [P("damageBonus", value=300, belowHp=500)]),
        ultimate=U("Purpurne Feuersbrunst", "enemyAll", [dmg(550), status("burn", 5000, 200)], hits=4)),
    "hollow_spire_warden": dict(
        normal=N("Hellebardenstoß", 110),
        special=S("Schlaflaterne", cd(10), "enemyBackRow", [dmg(100), status("sleep", 2000)]),
        special2=S("Turmwacht", cd(14), "self", [shield(20, 6000, scaling="casterMaxHp"), status("taunt", 4000)]),
        passive=("Hohle Rüstung", [P("damageReduction", value=200)]),
        ultimate=U("Ewige Nacht", "enemyAll", [dmg(400), status("slow", 4000, 300), status("sleep", 1500)], hits=2)),
    "obsidian_vault_sentinel": dict(
        normal=N("Riegelschlag", 110),
        special=S("Gewölbe schließen", cd(9), "enemyFront", [dmg(160), status("stun", 2000)]),
        special2=S("Obsidianpanzer", cd(12), "self", [shield(25, 6000, scaling="casterMaxHp")]),
        passive=("Tresorwächter", [P("damageReduction", value=250), P("onHitTaken", chance=200, effect=steal(1))]),
        ultimate=U("Tresorsturz", "enemyAll", [dmg(450), status("defenseDown", 6000, 250)], hits=3)),
    "fragment_devourer": dict(
        normal=N("Kristallbiss", 120, hits=2),
        special=S("Verschlingen", cd(8), "enemyHighestAttack", [dmg(220), lifesteal(50), steal(2)]),
        special2=S("Hunger", self_below(50, 15), "self", [status("attackSpeedUp", 8000, 400)]),
        passive=("Fragmentfresser", [P("lifesteal", value=100), P("damageBonus", growth=40)]),
        ultimate=U("Fragmentschlund", "enemyAll", [dmg(500), drain(30)], hits=3)),
    "corrupted_guardian_of_the_deep": dict(
        normal=N("Tentakelschlag", 110, target="enemyFrontRow"),
        special=S("Tiefenstrom", cd(9), "enemyAll", [dmg(110), status("slow", 3000, 250)]),
        special2=S("Korallenpanzer", cd(14), "self", [shield(15, 6000, scaling="casterMaxHp")]),
        passive=("Uralter Guardian", [P("regen", value=10), P("damageReduction", value=150)]),
        ultimate=U("Ruf der Tiefe", "enemyAll", [dmg(500), status("sleep", 2000)], hits=2)),
    "dragon_echo_of_the_sky": dict(
        normal=N("Sturmklaue", 130, hits=2),
        special=S("Blitzatem", cd(8), "enemyBackRow", [dmg(200), status("burn", 3000, 150)]),
        special2=S("Wolkenflug", self_below(40, once=True), "self", [status("evade", 8000, 3)]),
        passive=("Echo des Himmels", [P("critRate", value=150), P("damageBonus", value=300, belowHp=400)]),
        ultimate=U("Himmelssturz", "enemyAll", [dmg(650), status("stun", 1000)], hits=4)),
    "false_guardian_construct": dict(
        normal=N("Spiegelschlag", 120),
        special=S("Falscher Segen", cd(9), "enemyHighestAttack", [dmg(120), status("charm", 2500)]),
        special2=S("Spiegelschilde", cd(12), "self", [shield(20, 6000, scaling="casterMaxHp")]),
        passive=("Trugbild der Heiligkeit", [P("onHitTaken", chance=250, effect=status("confuse", 1500))]),
        ultimate=U("Zerbrochene Heiligkeit", "enemyAll", [dmg(550), status("confuse", 3000)], hits=3)),
    # Guardian Raid bosses (doc: Guardian Raid System). Their HP comes from the raid level, not from these stats.
    "celestial_turtle": dict(
        normal=N("Sternenflosse", 110, target="enemyFrontRow"),
        special=S("Gezeitenpanzer", cd(10), "self", [shield(10, 6000, scaling="casterMaxHp"), status("damageTakenDown", 6000, 250)]),
        special2=S("Sternenregen", cd(14), "enemyAll", [dmg(140), status("slow", 3000, 250)]),
        passive=("Uralter Schutz", [P("regen", value=5), P("damageReduction", value=200)]),
        ultimate=U("Flut der Himmel", "enemyAll", [dmg(520), status("sleep", 2000)], hits=3)),
    "dragon_of_the_sky": dict(
        normal=N("Gewitterklaue", 130, hits=2),
        special=S("Donneratem", cd(8), "enemyBackRow", [dmg(220), status("burn", 4000, 180)]),
        special2=S("Sturmflügel", cd(13), "enemyAll", [dmg(150), status("stun", 1000)]),
        passive=("Zorn des Himmels", [P("damageBonus", growth=60, cap=600), P("critRate", value=150)]),
        ultimate=U("Himmelsbruch", "enemyAll", [dmg(700), status("burn", 5000, 200)], hits=4)),
    "hollowed_leviathan": dict(
        normal=N("Tiefenbiss", 120),
        special=S("Hohle Strömung", cd(9), "enemyAll", [dmg(130), status("slow", 3000, 300)]),
        special2=S("Verschlingende Tiefe", cd(14), "enemyHighestAttack", [dmg(300), lifesteal(60), status("sleep", 2000)]),
        passive=("Hohler Leib", [P("lifesteal", value=100), P("onHitTaken", chance=200, effect=delay(400))]),
        ultimate=U("Mahlstrom", "enemyAll", [dmg(600), drain(40)], hits=3)),
    "six_legged_worldbearer": dict(
        normal=N("Stampfen", 110, target="enemyFrontRow"),
        special=S("Erdbeben", cd(9), "enemyAll", [dmg(130), status("stun", 1000)]),
        special2=S("Gestohlene Welt", cd(13), "enemyAll", [steal(2), status("defenseDown", 6000, 250)]),
        passive=("Last der Welt", [P("damageReduction", value=250), P("regen", value=5)]),
        ultimate=U("Weltenlast", "enemyAll", [dmg(560), status("defenseDown", 6000, 300)], hits=3)),
    "craterborn_guardian": dict(
        normal=N("Meteorfaust", 125),
        special=S("Kratersplitter", cd(8), "enemyBackRow", [dmg(200), status("confuse", 2000)]),
        special2=S("Glühender Kern", self_below(50, once=True), "self", [status("attackUp", 10000, 400), status("immune", 2000)]),
        passive=("Kratergeboren", [P("onHitTaken", chance=250, effect=status("charm", 1500)), P("damageBonus", value=300, belowHp=500)]),
        ultimate=U("Einschlag", "enemyAll", [dmg(680), status("charm", 2000)], hits=3)),
}

TIMING = {"attack": (950, 400), "skill": (1400, 800), "ultimate": (1950, 1300)}


def skill(mid, slot, spec, animation):
    hits = spec.get("hits", 1)
    duration, first = TIMING[animation]
    times = [first + 120 * i for i in range(hits)]
    effects = mk.resolve(spec["effects"], "rare", mk.shape_of(spec["target"]))
    return {
        "id": f"{mid}.{slot}", "name": spec["name"], "description": "", "slot": slot,
        "trigger": spec["trigger"], "target": spec["target"], "targetCount": spec.get("targetCount", 1),
        "effects": effects, "hits": hits, "hitTimesMs": times, "durationMs": max(duration, times[-1] + 150), "animation": animation,
    }


def main():
    mk.grade_budget.update({"common": [60, 110, 450], "rare": [70, 140, 560], "epic": [85, 180, 700], "legendary": [100, 230, 900]})
    roster = json.loads((HERE / "roster.json").read_text(encoding="utf-8"))["monsters"]
    OUT.mkdir(parents=True, exist_ok=True)
    for m in roster:
        mid, kit = m["id"], K[m["id"]]
        base = ROLE_STATS[m["role"]]
        boss = m["tier"] in ("boss", "raid")
        f = BOSS if boss else NORMAL
        front = m["row"] == "front"
        data = {
            "id": mid, "name": m["name"], "title": "", "faction": "theHollowed", "race": "hollowed",
            "aspect": m["aspect"], "role": m["role"], "row": m["row"], "grade": "rare",
            "group": m["group"], "boss": boss, "raidBoss": m["tier"] == "raid", "sizePermille": SIZE[m["tier"]], "body": m["body"],
            "spineId": mid, "growthPermille": 80,
            "baseStats": {
                "hp": round(base["hp"] * f["hp"]), "attack": round(base["attack"] * f["attack"]),
                "defense": round(base["defense"] * f["defense"]), "attackIntervalMs": 1500 if front else 1700,
                "critRate": base["critRate"], "critDamage": 500,
            },
            "designNotes": [],
            "normal": skill(mid, "normal", kit["normal"], "attack"),
        }
        if "range" in kit:
            # data["row"] decides the default; the kit overrides it (the crow swoops into the back row)
            items = list(data.items())
            data = dict(items[:items.index(("row", data["row"])) + 1] + [("range", kit["range"])] + items[items.index(("row", data["row"])) + 1:])
        for slot in ("special", "special2"):
            if slot in kit:
                data[slot] = skill(mid, slot, kit[slot], "skill")
        name, passives = kit["passive"]
        data["passive"] = {"id": f"{mid}.passive", "name": name, "description": "", "slot": "passive",
                           "trigger": {"kind": "passive"}, "target": "self",
                           "passives": [mk.resolve_passive(p, "rare") for p in passives]}
        if "ultimate" in kit:
            data["ultimate"] = skill(mid, "ultimate", kit["ultimate"], "ultimate")
        dest = OUT / mid
        dest.mkdir(exist_ok=True)
        (dest / f"{mid}.monster.json").write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"{len(roster)} monsters written to {OUT}")


if __name__ == "__main__":
    main()
