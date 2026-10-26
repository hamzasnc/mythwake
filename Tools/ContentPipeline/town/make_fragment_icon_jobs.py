"""Writes Codex jobs for the three Fragment icons (Core, Aspect, Echo). The game tints them with the Aspect colour,
so they are painted in neutral light tones. Runs with the town job runner (run_jobs.sh in the output dir).

usage: py -3 make_fragment_icon_jobs.py <output dir> <style reference png>
"""
import shutil
import sys
from pathlib import Path

from make_town_jobs import HEADER, RUNNER

RULES = """REGELN:
- Ein einzelnes Icon, zentriert, freigestellt auf reinem weißem Hintergrund (#FFFFFF), quadratisch 1024x1024, Objekt füllt ca. 75 %.
- WICHTIG: fast einfarbig in hellen, neutralen Silber-/Weißtönen mit dunklen Outlines und klarem Cel-Shading, weil das Spiel das Icon später je nach Fragment-Aspekt einfärbt (Rot, Grün, Violett, Pink, Blau). Keine kräftigen eigenen Farben.
- Kein Text, kein Rahmen, kein Hintergrund-Glow, keine Figuren.
"""

ICONS = [
    ("core_fragment", "Core Fragment",
     "Ein großer, kräftiger, facettierter Kristallsplitter (Fragment) – das Herzstück: kompakt, massiv, mit einem hellen leuchtenden Kern in der Mitte und ein paar kleinen Splittern, die ihn umschweben."),
    ("aspect_fragment", "Aspect Fragment",
     "Ein schlanker, spitzer Kristallsplitter mit eingravierter, leuchtender Rune auf der Oberfläche und einer dynamischen Energie-Spirale, die sich um ihn windet – wirkt aktiv, wie ein Werkzeug für den Kampf."),
    ("echo_fragment", "Echo Fragment",
     "Ein halb durchscheinender, geisterhafter Kristallsplitter, von dem zwei, drei konzentrische Echo-Ringe/Wellen ausgehen, dahinter ein blasser Nachhall derselben Form (wie ein Echo)."),
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
        body = f'ICON – "{name}" (Gegenstand im Inventar)\n- {text}\n{RULES}'
        (d / "run_prompt.txt").write_text(HEADER.format(folder=str(d.resolve())) + "\n" + body, encoding="utf-8")
        jobs.append(folder)
    (out / "_jobs.txt").write_text("\n".join(jobs) + "\n", encoding="utf-8", newline="\n")
    (out / "run_jobs.sh").write_text(RUNNER, encoding="utf-8", newline="\n")
    print(len(jobs), "jobs in", out)


if __name__ == "__main__":
    main()
