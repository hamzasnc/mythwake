"""Writes Codex jobs for ultimate cut-ins of heroes that have none yet (Content/Heroes/<id>/<id>_cutin.png missing).

The reference is the hero's own full-body render (<id>_full.png), so the cut-in shows exactly that character; the
ultimate's name and description from <id>.hero.json set the pose. Same runner as the town jobs; then
import_cutin_jobs.py cuts out the masters into <id>_cutin.png (1024x512).

usage: py -3 make_cutin_jobs.py <output dir> [id,id]
"""
import json
import shutil
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent / "town"))
from make_town_jobs import RUNNER  # noqa: E402

HEROES = Path(__file__).resolve().parents[2] / "Assets" / "Mythwake" / "Content" / "Heroes"

PROMPT = """ARBEITSORDNER: {folder}
Angehängt ist style_reference.png (liegt auch im Arbeitsordner): die Figur "{name}" aus meinem Mobile-Idle-RPG Mythwake (Stil wie AFK Arena / 7DS Idle Adventure). Genau DIESE Figur zeichnest du – gleiches Gesicht, gleiche Frisur und Haarfarbe, gleiche Kleidung, Rüstung, Farben und Waffe/Requisiten. Nichts hinzuerfinden, nichts weglassen.
Dieser Lauf ist nicht-interaktiv: Generiere das Bild mit deinem Bildgenerierungs-Tool, kopiere das fertige Bild als master.png in den Arbeitsordner (generierte Bilder landen sonst nur unter ~/.codex/generated_images) und speichere deinen verwendeten Bild-Prompt als master-prompt.txt daneben. Prüfe das Ergebnis selbst gegen die Regeln unten und generiere neu, falls etwas klar verletzt ist. Danach beenden.

BILD: Ultimate-Cut-in (das kurze Einblendbild, wenn die Figur ihr Ultimate auslöst).
- Querformat 1536x768 (2:1). Die Figur von der Hüfte aufwärts, groß und dynamisch, leicht schräg zur Kamera, dramatische Aktionspose mitten im Ultimate: {ultimate}
- Ausdrucksstarkes Gesicht (entschlossen), Bewegung in Haaren und Kleidung, Effekte der Fähigkeit eng um die Figur.
- Freigestellt auf reinem weißem Hintergrund (#FFFFFF): kein Boden, keine Szenerie, kein Rahmen, kein Text. Die Figur darf links/rechts/unten angeschnitten sein, der Kopf ist ganz im Bild.
- Hand-gemalt, kräftige dunkle Outlines, sauberes Cel-Shading, satte Farben – wie die Vorlage.
"""


def main():
    out = Path(sys.argv[1])
    only = set(sys.argv[2].split(",")) if len(sys.argv) > 2 else None
    out.mkdir(parents=True, exist_ok=True)
    jobs = []
    for hero_dir in sorted(p for p in HEROES.iterdir() if p.is_dir()):
        id = hero_dir.name
        if (only and id not in only) or (hero_dir / f"{id}_cutin.png").exists() or not (hero_dir / f"{id}_full.png").exists():
            continue
        hero = json.loads((hero_dir / f"{id}.hero.json").read_text(encoding="utf-8"))
        ult = hero.get("ultimate") or {}
        ultimate = f'"{ult.get("name", "Ultimate")}" – {ult.get("description", "")}'.strip(" –")
        folder = f"{len(jobs) + 1:02d}_{id}"
        d = out / folder
        d.mkdir(exist_ok=True)
        shutil.copy(hero_dir / f"{id}_full.png", d / "style_reference.png")
        (d / "run_prompt.txt").write_text(PROMPT.format(folder=str(d.resolve()), name=hero["name"], ultimate=ultimate),
                                          encoding="utf-8")
        jobs.append(folder)
    (out / "_jobs.txt").write_text("\n".join(jobs) + "\n", encoding="utf-8", newline="\n")
    (out / "run_jobs.sh").write_text(RUNNER, encoding="utf-8", newline="\n")
    print(len(jobs), "jobs in", out)


if __name__ == "__main__":
    main()
