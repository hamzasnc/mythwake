"""Writes Codex jobs for the nine Relic icons (docs/systems/RELICS.md). Same runner as the town jobs.

usage: py -3 make_relic_icon_jobs.py <output dir> <style reference png>
"""
import shutil
import sys
from pathlib import Path

from make_town_jobs import HEADER, RUNNER

RULES = """REGELN:
- Ein einzelnes Icon (Gegenstand im Inventar), zentriert, freigestellt auf reinem weißem Hintergrund (#FFFFFF), quadratisch 1024x1024, Objekt füllt ca. 75 %.
- Kräftige eigene Farben passend zum Boss, wertig und magisch (seltenes Relikt), klare Silhouette, gut lesbar auch klein.
- Kein Text, kein Rahmen, kein Hintergrund-Glow, keine Figuren.
"""

ICONS = [
    ("relic_hollowtusk_tusk", "Tusk of the Hollowtusk Alpha",
     "Ein großer, gebogener Eberhauer des verdorbenen Ebers Hollowtusk Alpha, elfenbeinfarben mit violetten Kristallsplittern und Riss-Leuchten, mit Lederband und Perlen umwickelt."),
    ("relic_treant_root", "Root of the Riftborn Treant",
     "Ein knorriges, lebendiges Wurzelstück des Riftborn Treant, braun-grün mit Moos und kleinen Blättern, in der Mitte ein warm glühender rot-oranger Herzkern (Lebenskraft, Heilung)."),
    ("relic_wraith_sigil", "Sigil of the Crimson Wraith Adept",
     "Ein rundes, scharlachrotes Siegel-Amulett eines Feuermagiers mit Goldrand und einer eingravierten Flammen-Rune, umgeben von kleinen roten Flammen (Energie, Magie)."),
    ("relic_warden_lantern", "Lantern of the Hollow Spire Warden",
     "Eine gotische schwarz-eiserne Laterne mit spitzen Turmformen, darin ein kaltes blaues Licht, das Verderbtes aufdeckt."),
    ("relic_vault_key", "Key of the Obsidian Vault Sentinel",
     "Ein großer, schwerer Schlüssel aus schwarzem Obsidian mit Gold-Beschlägen und einem grün leuchtenden Smaragd im Griff (öffnet Schatzkammern)."),
    ("relic_devourer_core", "Core of the Fragment Devourer",
     "Ein pulsierender Kristallkern aus dunkelblauem Panzer und grün-violett leuchtenden Fragment-Splittern, als hätte ein Biest Fragmente verschlungen."),
    ("relic_deep_shell", "Shell of the Corrupted Guardian of the Deep",
     "Ein Stück blau-türkiser Schildkrötenpanzer mit Korallen und Muscheln, schimmernd wie Wasser, mit einem schützenden Wasser-Schild-Glanz."),
    ("relic_dragon_scale", "Scale of the Dragon Echo of the Sky",
     "Eine große himmelblau-weiße Drachenschuppe mit Goldmaserung und Wolken-Ornamenten, an der Spitze ein roter Kristall, aus dem ein kleiner Blitz zuckt."),
    ("relic_construct_heart", "Heart of the False Guardian Construct",
     "Ein mechanisches Herz aus weißem Marmor und Gold mit rosa leuchtendem Kern und kleinen Zahnrädern, wie das Herz eines künstlichen Wächters."),
]


def main():
    out = Path(sys.argv[1])
    style = Path(sys.argv[2])
    out.mkdir(parents=True, exist_ok=True)
    jobs = []
    for i, (id, name, text) in enumerate(ICONS, start=1):
        folder = f"{i:02d}_{id}"
        d = out / folder
        d.mkdir(exist_ok=True)
        shutil.copy(style, d / "style_reference.png")
        body = f'RELIKT-ICON – "{name}"\n- {text}\n{RULES}'
        (d / "run_prompt.txt").write_text(HEADER.format(folder=str(d.resolve())) + "\n" + body, encoding="utf-8")
        jobs.append(folder)
    (out / "_jobs.txt").write_text("\n".join(jobs) + "\n", encoding="utf-8", newline="\n")
    (out / "run_jobs.sh").write_text(RUNNER, encoding="utf-8", newline="\n")
    print(len(jobs), "jobs in", out)


if __name__ == "__main__":
    main()
