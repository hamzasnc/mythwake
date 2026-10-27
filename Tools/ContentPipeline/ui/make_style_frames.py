"""Writes Codex jobs for UI style frames: complete mockups of key screens in the AFK Arena look described in
docs/UI_STYLE.md, each painted in one pass so every element shares one style. The team approves the frames before the
UI kit is cut from them.

usage: py -3 make_style_frames.py <output dir> <campaign screenshot> <hero render> <quests screenshot>
"""
import shutil
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "town"))
from make_town_jobs import RUNNER  # noqa: E402

COMMON = """ARBEITSORDNER: {folder}
Angehängt ist style_reference.png (liegt auch im Arbeitsordner): {reference}
Dieser Lauf ist nicht-interaktiv: Generiere das Bild mit deinem Bildgenerierungs-Tool, kopiere das fertige Bild als master.png in den Arbeitsordner (generierte Bilder landen sonst nur unter ~/.codex/generated_images) und speichere deinen verwendeten Bild-Prompt als master-prompt.txt daneben. Prüfe das Ergebnis selbst gegen die Regeln unten und generiere neu, falls etwas klar verletzt ist. Danach beenden.

BILD: ein kompletter Screenshot aus meinem fertigen Mobile-Idle-RPG "Mythwake", Hochformat 1024x1536, so poliert wie ein echtes Top-Mobile-Game.

UI-STIL (sehr wichtig, so nah wie möglich an AFK Arena, aber eigene Grafik):
- Nur drei Materialien: dunkles warmes Holz (dunkelbraun #3b2414 bis #8a5a2b) für Leisten, Rahmen und Titelschilder; helles Pergament (#ecd9b0) für alle Inhaltsflächen mit dunkelbrauner Schrift; und Türkis-Cyan (#47e3f0) NUR für den einen Haupt-Button pro Screen, sechseckig-länglich mit sanftem Leuchten und weißer Schrift.
- Gold nur als dünne Kante und für Sterne, nie als Fläche. Keine dicken Goldornamente, kein Plastik-Glanz, keine Neon-Effekte.
- Flache, klare, handgemalte Formen mit sanfter Schattierung, gedeckte warme Farben, sehr aufgeräumt, viel Ruhe. Einheitliche Linienstärke und Ecken bei allen Elementen.
- Untere Navigationsleiste: dunkle Holzleiste mit 5 Kacheln; Icons als einfarbige Sepia-Relief-Illustrationen (beige mit brauner Schattierung) mit kurzer weißer Beschriftung mit dunkler Kontur; der aktive Tab ist helleres Holz mit warmem Glimmen. Tabs: "Dorf", "Abenteuer", "Kampagne", "Helden", "Beschwörung".
- Obere Leiste: schmale dunkle Holzleiste; links runder Spieler-Avatar mit Goldring, Name, Stufe; rechts Währungen in dunklen halbtransparenten Pillen (Icon links, weiße Zahl, kleines goldenes Plus-Quadrat beim Edelstein).
- Zurück-Knopf (falls vorhanden) unten links als kleines Holzquadrat mit geschwungenem Pfeil.
- Schrift: kräftige, runde Spielschrift; auf Bildern weiß mit dunkler Kontur, auf Pergament dunkelbraun.
TEXT: nur die genannten kurzen deutschen Wörter und Zahlen, korrekt geschrieben. Wenn ein Wort nicht sauber gelingt, lieber ein leeres Schildchen.
"""

SCREENS = [
    ("campaign", "der aktuelle Kampagnen-Bildschirm des Spiels: Welt (zerstörtes Dorf Dawnhollow) und Chibi-Helden bleiben genau so, nur die UI wird neu gestaltet.",
     """SCREEN: Kampagne (Hauptbildschirm).
- Die Welt aus der Vorlage im Vollbild, das Helden-Team kämpft in der Mitte gegen einen Eber.
- Oben: Leiste wie beschrieben. Darunter mittig "Stage 1-2" klein auf einem Holzschild.
- Links am Rand eine senkrechte Reihe von 3 runden Knöpfen (Holzring, Sepia-Icon, kleine Beschriftung darunter): "Quests" (Schriftrolle, roter Punkt), "Events" (Stern), "Shop" (Beutel). Rechts ebenso: "Post" (Brief), "Beutel" (Lederbeutel), "Freunde" (zwei Figuren).
- Unter dem Team: eine geöffnete Schatztruhe auf einem Münzhaufen mit Edelsteinen (Idle-Beute), darüber ein kleiner Timer "02:14:33".
- Unten mittig über der Navigationsleiste der türkise Haupt-Button "Kampf" mit gekreuzten Schwertern; links daneben ein kleiner runder Knopf "Karte".
- Untere Navigationsleiste, "Kampagne" aktiv."""),
    ("hero", "die Ganzkörper-Darstellung eines unserer Helden (Dorn, Chibi-Krieger mit Schwert, Schild und rotem Schal). Genau diesen Helden zeigen.",
     """SCREEN: Heldenansicht.
- Hintergrund: eine gemalte Bühne in einer Halle mit Torbogen, sanftes Licht, runder magischer Kreis am Boden; der Held (aus der Vorlage) groß in der Mitte.
- Oben mittig: kleines Wappen mit Edelstein (Seltenheit), darunter "Common", dann der Name "Dorn" groß in weiß mit Kontur.
- Links und rechts vom Helden je 2 runde Fähigkeits-Icons in Holz-Gold-Ringen mit kleiner Stufenzahl.
- Kleine Pfeile links/rechts zum Wechseln des Helden.
- Darunter ein dunkles halbtransparentes Band: Kampfkraft (Faust-Icon + gelbe Zahl "1.200") und eine Zeile Werte mit Icons: Stufe "30", Herz "1.050", Schwert "97", Schild "50".
- Darunter kleine Kosten (Gold-Icon "74", grüner Trank "46") und der türkise Button "Level Up".
- Unten rechts zwei runde Knöpfe mit Beschriftung: "Ausrüstung", "Fähigkeiten". Unten links der Zurück-Knopf. Keine Navigationsleiste."""),
    ("quests", "ein Screenshot unseres aktuellen Quest-Bildschirms (nur als Inhalts-Hinweis, die Gestaltung wird komplett neu).",
     """SCREEN: Popup "Aufgaben" über dem abgedunkelten Kampagnen-Bildschirm.
- Großes Pergament-Popup mit geschnitztem Holzrahmen und kleinen Eckverzierungen; oben mittig ragt ein dunkles Holz-Titelschild mit "Aufgaben" heraus.
- Darunter drei Reiter: "Täglich" (aktiv, hell), "Wöchentlich", "Haupt".
- Oben im Popup eine Aktivitäts-Leiste: türkiser Fortschrittsbalken mit 5 kleinen Truhen darauf (20/40/60/80/100).
- Darunter 4 Listenzeilen als hellere Pergament-Karten: links ein quadratisches Belohnungs-Icon im Rahmen (Gold-Münzen, blauer Edelstein, grüner Trank, Schriftrolle) mit Zahl unten rechts, Mitte Titel in dunkelbraun ("Kämpfe 3x", "Sammle Beute", "Level einen Helden", "Beschwöre einmal") mit kleinem Fortschritt "1/3", rechts türkiser Knopf "Abholen" bzw. bei offenen Aufgaben ein Holzknopf "Los".
- Unten im Popup: Holzknopf "Schließen" links, türkiser Knopf "Alle abholen" rechts.
- Untere Navigationsleiste des Spiels darunter sichtbar."""),
]


def main():
    out = Path(sys.argv[1])
    refs = [Path(p) for p in sys.argv[2:5]]
    out.mkdir(parents=True, exist_ok=True)
    jobs = []
    for i, ((id, reference, screen), ref) in enumerate(zip(SCREENS, refs), start=1):
        d = out / f"{i:02d}_{id}"
        d.mkdir(exist_ok=True)
        shutil.copy(ref, d / "style_reference.png")
        (d / "run_prompt.txt").write_text(COMMON.format(folder=str(d.resolve()), reference=reference) + "\n" + screen + "\n",
                                          encoding="utf-8")
        jobs.append(d.name)
    (out / "_jobs.txt").write_text("\n".join(jobs) + "\n", encoding="utf-8", newline="\n")
    (out / "run_jobs.sh").write_text(RUNNER, encoding="utf-8", newline="\n")
    print(len(jobs), "jobs in", out)


if __name__ == "__main__":
    main()
