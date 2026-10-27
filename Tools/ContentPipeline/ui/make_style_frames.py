"""Writes Codex jobs for UI style frames: complete mockups of one screen (the campaign hub) in a few directions, painted
in one pass so every element shares one style. The team picks a direction before the UI kit is redone from it.

usage: py -3 make_style_frames.py <output dir> <screenshot of the current campaign screen>
"""
import shutil
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "town"))
from make_town_jobs import RUNNER  # noqa: E402

PROMPT = """ARBEITSORDNER: {folder}
Angehängt ist current_screen.png (liegt auch im Arbeitsordner): der aktuelle Kampagnen-Bildschirm meines Mobile-Idle-RPGs Mythwake (Hochformat-Handy). Die Welt (Dorf Dawnhollow, Hintergrund) und die Chibi-Helden bleiben genau so; NUR die Benutzeroberfläche wird neu gestaltet. Die jetzige UI wirkt zu sehr nach KI: zu viele Goldornamente, zu viel Glanz, uneinheitlich.
Dieser Lauf ist nicht-interaktiv: Generiere das Bild mit deinem Bildgenerierungs-Tool, kopiere das fertige Bild als master.png in den Arbeitsordner (generierte Bilder landen sonst nur unter ~/.codex/generated_images) und speichere deinen verwendeten Bild-Prompt als master-prompt.txt daneben. Prüfe das Ergebnis selbst und generiere neu, falls etwas klar verletzt ist. Danach beenden.

BILD: ein kompletter Mockup-Screenshot des Kampagnen-Bildschirms, Hochformat 1024x1536, wie ein echter Screenshot aus einem fertigen Top-Mobile-Game.
INHALT (von oben nach unten):
- Obere Leiste: kleiner runder Spieler-Avatar mit Stufe links, rechts 3–4 Währungen (Gold-Münze, grünes Elixier, blauer Edelstein, Schriftrolle) jeweils Icon + Zahl.
- Oben mittig: Kapitel-/Stage-Anzeige.
- Links und rechts am Rand je eine senkrechte Reihe runder Menü-Knöpfe mit kleinem Icon und kurzem Beschriftungsfeld (Quests, Post, Events / Karte, Beutel, Freunde), dezent.
- Mitte: die Welt mit drei Chibi-Helden, die gegen einen Eber kämpfen (aus der Vorlage).
- Unter dem Team: eine Schatztruhe mit Münzhaufen (Idle-Belohnung) mit kleiner Fortschrittsanzeige.
- Ein großer Haupt-Button für den Kampf.
- Untere Navigationsleiste mit 5 Tabs (Dorf, Abenteuer, Kampagne in der Mitte hervorgehoben, Helden, Beschwörung), jeweils Icon + kurze Beschriftung.
TEXT: Verwende nur kurze, korrekt geschriebene deutsche Wörter: "Kampf", "Stage 1-2", "Quests", "Helden", "Dorf", "Kampagne", "Abenteuer", "Beschwörung" und Zahlen. Wenn ein Wort nicht sauber gelingt, lieber ein schlichtes leeres Schildchen ohne Text.

STIL-RICHTUNG: {direction}

ALLGEMEIN: ein einheitlicher Stil für ALLE UI-Elemente (gleiche Linienstärke, gleiche Materialien, gleiche Ecken), sauber, ruhig, professionell, gut lesbar, viel Welt sichtbar. Keine Überladung, keine unnötigen Ornamente, kein Plastik-Glanz.
"""

DIRECTIONS = [
    ("afk_warm",
     "Wie AFK Arena: warmes dunkles Holz und helles Pergament, dünne gedeckte Goldkanten, flache klare Formen mit sanfter Schattierung, ein leuchtend türkis-blauer Haupt-Button, Menü-Icons als einfarbiges Sepia-Relief, viel Zurückhaltung."),
    ("sds_bright",
     "Wie The Seven Deadly Sins: IDLE: freundlich und hell, verspielt-cartoonig, kräftige dunkle Outlines, rundliche dicke Formen, satte Farben (Türkis, Gold, Rot), Comic-Charme, große runde Knöpfe, Icons bunt und knuffig."),
    ("clean_fantasy",
     "Modern-cleane Fantasy wie neuere Top-Gacha-Games: halbtransparente dunkle Glas-Paneele mit feinen hellen Linien, sehr wenig Ornament, große klare Typografie mit Schatten, ein kräftiger goldener Haupt-Button, flache weiße Linien-Icons."),
]


def main():
    out = Path(sys.argv[1])
    screen = Path(sys.argv[2])
    out.mkdir(parents=True, exist_ok=True)
    jobs = []
    for i, (id, direction) in enumerate(DIRECTIONS, start=1):
        d = out / f"{i:02d}_{id}"
        d.mkdir(exist_ok=True)
        shutil.copy(screen, d / "current_screen.png")
        shutil.copy(screen, d / "style_reference.png")
        (d / "run_prompt.txt").write_text(PROMPT.format(folder=str(d.resolve()), direction=direction), encoding="utf-8")
        jobs.append(d.name)
    (out / "_jobs.txt").write_text("\n".join(jobs) + "\n", encoding="utf-8", newline="\n")
    (out / "run_jobs.sh").write_text(RUNNER, encoding="utf-8", newline="\n")
    print(len(jobs), "jobs in", out)


if __name__ == "__main__":
    main()
