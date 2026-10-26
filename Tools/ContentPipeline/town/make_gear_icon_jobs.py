"""Writes Codex jobs for the gear icons: one per set (the doc's six factions) and slot (Weapon, Armor, Boots,
Accessory), named gear_<set>_<slot> after the Faction / GearSlot enums (first letter lower case). Same runner as the town jobs; import with
import_icons.py. The grade is shown by the tile frame, not the picture.

usage: py -3 make_gear_icon_jobs.py <output dir> <style reference png>
"""
import shutil
import sys
from pathlib import Path

from make_town_jobs import HEADER, RUNNER

RULES = """REGELN:
- Ein einzelnes Icon (Ausrüstungsgegenstand im Inventar), zentriert, freigestellt auf reinem weißem Hintergrund (#FFFFFF), quadratisch 1024x1024, Objekt füllt ca. 75 %, leicht schräg präsentiert.
- Klare Silhouette, gut lesbar auch klein, wertig gemalt. Alle Teile eines Sets haben dieselbe Farb- und Formensprache.
- Kein Text, kein Rahmen, kein Hintergrund-Glow, keine Figuren, keine Wappen mit Schrift.
"""

# the doc's factions (Faction System) as gear sets: their look
SETS = {
    "orderOfTheVeil": ("Order of the Veil",
                       "Gelehrte, Priester und Magier, die die Fragmente verstehen wollen: silbern-weiß und tiefblau, feine Schleier-Stoffe, eingravierte leuchtende Runen, kleine Kristalllinsen."),
    "tribeOfTheCrimsonFang": ("Tribe of the Crimson Fang",
                              "Krieger-Clans, für die Stärke den Wert beweist: Karmesinrot und dunkles Leder, Fell, Knochen und große Reißzähne als Schmuck, grob geschmiedetes Eisen."),
    "empireOfTheShatteredSky": ("Empire of the Shattered Sky",
                                "Ein mächtiges Imperium, das Fragment-Macht mit Gewalt kontrolliert: polierter Stahl mit Gold, himmelblaue Emaille mit einem gesprungenen-Himmel-Muster, streng und prunkvoll."),
    "guildsOfTheHunt": ("Guilds of the Hunt",
                        "Monsterjäger, Söldner und Plünderer: praktisches braunes Leder, Waldgrün, Schnallen und Riemen, Trophäen verdorbener Bestien (Krallen, Schuppen), robust und benutzt."),
    "circleOfAether": ("Circle of Aether",
                       "Portalforscher und verbotene Magier: Violett und Türkis, schwebende Metallringe wie kleine Portale, leuchtende Aether-Kristalle, elegant und unheimlich."),
    "theHollowed": ("The Hollowed",
                    "Von Fragment-Energie verdorben: dunkles, rissiges Eisen und Knochen, violett leuchtende Risse und Kristallauswüchse, leicht organisch-verformt."),
}

SLOTS = {
    "weapon": ("Waffe", {
        "orderOfTheVeil": "ein Magierstab mit Kristalllinse",
        "tribeOfTheCrimsonFang": "eine schwere zweischneidige Kriegsaxt",
        "empireOfTheShatteredSky": "ein prunkvolles Langschwert",
        "guildsOfTheHunt": "ein Jagdspeer mit Widerhaken",
        "circleOfAether": "ein Zauberzepter mit schwebendem Ring",
        "theHollowed": "eine gezackte, verdorbene Klinge",
    }),
    "armor": ("Rüstung", {k: "ein Brustpanzer bzw. eine Robe (nur das Kleidungsstück, ohne Träger)" for k in SETS}),
    "boots": ("Stiefel", {k: "ein Paar Stiefel" for k in SETS}),
    "accessory": ("Accessoire", {k: "ein Amulett an einer Kette" for k in SETS}),
}


def main():
    out = Path(sys.argv[1])
    style = Path(sys.argv[2])
    out.mkdir(parents=True, exist_ok=True)
    jobs = []
    i = 0
    for set_id, (set_name, look) in SETS.items():
        for slot_id, (slot_name, items) in SLOTS.items():
            i += 1
            folder = f"{i:02d}_gear_{set_id}_{slot_id}"
            d = out / folder
            d.mkdir(exist_ok=True)
            shutil.copy(style, d / "style_reference.png")
            body = (f'AUSRÜSTUNGS-ICON – {slot_name} aus dem Set "{set_name}"\n'
                    f"- Gegenstand: {items[set_id]}.\n- Stil des Sets: {look}\n{RULES}")
            (d / "run_prompt.txt").write_text(HEADER.format(folder=str(d.resolve())) + "\n" + body, encoding="utf-8")
            jobs.append(folder)
    (out / "_jobs.txt").write_text("\n".join(jobs) + "\n", encoding="utf-8", newline="\n")
    (out / "run_jobs.sh").write_text(RUNNER, encoding="utf-8", newline="\n")
    print(len(jobs), "jobs in", out)


if __name__ == "__main__":
    main()
