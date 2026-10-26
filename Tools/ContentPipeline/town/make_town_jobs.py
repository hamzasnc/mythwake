"""Writes the Codex jobs for the town hub art (Dawnhollow background, building sprites, ruin) and the world map.

Every job gets its own folder <NN>_<id>/ with run_prompt.txt and style_reference.png; run_jobs.sh generates them
with Codex, 5 in parallel. Only names from docs/GAME_DESIGN.md are used.

usage: py -3 make_town_jobs.py <output dir> <style reference png>
"""
import shutil
import sys
from pathlib import Path

HEADER = """ARBEITSORDNER: {folder}
Angehängt ist style_reference.png (liegt auch im Arbeitsordner): Figuren aus demselben Spiel. Sie zeigen NUR den Zeichenstil (Linienstärke, Cel-Shading, Farbauftrag, Highlights) – nicht Motiv oder Farben.
Dieser Lauf ist nicht-interaktiv: Generiere das Bild mit deinem Bildgenerierungs-Tool, kopiere das fertige Bild als master.png in den Arbeitsordner (generierte Bilder landen sonst nur unter ~/.codex/generated_images) und speichere deinen verwendeten Bild-Prompt als master-prompt.txt daneben. Prüfe das Ergebnis selbst gegen die Regeln unten und generiere neu, falls etwas klar verletzt ist. Danach beenden.

Grafik für mein Mobile-Idle-RPG Mythwake (Stil wie AFK Arena / 7DS Idle Adventure: handgemalt, kräftige dunkle Outlines, sauberes Cel-Shading, warme satte Farben, klare große Formen, leicht verspielt-niedliche Proportionen, keine Detailüberladung). Originales Design, keine Ähnlichkeit zu existierenden Franchise-Grafiken. KEIN Text, keine Schrift, keine Buchstaben, keine Logos, keine UI-Elemente im Bild.

WELT: Atheris ist zerbrochen. Die alten Königreiche sind gefallen, die Guardians verschwunden, und die Fragmente – Überreste vergessener Macht – haben Narben in die Welt gerissen. Im ruhigen Grenzdorf Dawnhollow brach unter dem Mossveil Shrine ein Rift im Wald auf: violettes Licht, verdrehte Tiere. Dawnhollow wurde zerstört; die Überlebenden bauen das Dorf jetzt mit Holz und Stein wieder auf.
"""

CAMERA = ("KAMERA (für alle Gebäude gleich, sehr wichtig): erhöhte 3/4-Ansicht von schräg oben vorne (ca. 35° von oben), "
          "wie die Gebäude in der Stadt von AFK Arena; Licht leicht von rechts.")

BUILDING_RULES = """REGELN:
- Ein einzelnes Gebäude, komplett im Bild mit Rand, freigestellt auf reinem weißem Hintergrund (#FFFFFF), kein Boden außer einem kleinen eigenen Sockel/Erdfleck direkt unter dem Gebäude, keine Schlagschatten auf den Hintergrund.
- Quadratisches Bild (1024x1024). Gebäude füllt ca. 80 % der Breite.
- Keine Figuren, keine Tiere, kein Text, keine Schilder mit Schrift.
- Das Dorf wird gerade von Überlebenden wieder aufgebaut: rustikal, Holz, Stein, Stroh, Segeltuch, hier und da frische Bretter neben alten verkohlten Balken.
"""

BUILDINGS = [
    ("survivor_camp", "Survivor Camp",
     "Das Lager der Überlebenden und die erste Produktion von Grundressourcen (Gold, Wood, Stone): zwei, drei geflickte Zelte um ein Lagerfeuer, gestapeltes Holz, ein Haufen behauener Steine, eine kleine offene Truhe mit Goldmünzen, Kisten und Säcke. Gemütlich und improvisiert."),
    ("summoning_gate", "Summoning Gate",
     "Ein großer alter Steinbogen, durch den Helden gerufen werden. Im Bogen ein wirbelndes, leuchtendes Portal aus hellem Gold- und Türkislicht. Der Bogen ist teilweise restauriert: Holzgerüst an einer Seite, eingemeißelte Runen leuchten schwach."),
    ("healers_tent", "Healer's Tent",
     "Ein großes, helles Heiler-Zelt aus Segeltuch (weiß/creme mit grünen Akzenten), offener Eingang, davor Kräutertöpfe, Bündel getrockneter Kräuter, eine Steinschale mit klarem leuchtendem Wasser, Verbandsrollen auf einem Tisch."),
    ("broken_forge", "Broken Forge",
     "Eine Schmiede, die nach der Katastrophe notdürftig repariert wurde: gemauerte Esse mit gerissenem, teils geflicktem Schornstein, glühende Kohle, ein großer Amboss, Wasserbottich, Hämmer und Zangen an der Wand, halbes Holzdach. Orangefarbenes Glühen aus dem Ofen."),
    ("hunters_hut", "Hunter's Hut",
     "Eine robuste Holzhütte der Jäger mit Fellen und einem Geweih über der Tür, Bögen und Speere an einem Gestell, davor ein hölzernes Brett mit angehefteten Zetteln und Jagd-Trophäen (Zettel nur als leere Papiere, keine Schrift)."),
    ("watchtower", "Watchtower",
     "Ein hoher, schlanker Wachturm aus Holz auf einem Steinfundament, oben eine überdachte Plattform mit Glocke und einem Stoffbanner (ohne Symbol oder Schrift), Leiter an der Seite, Fackeln."),
    ("fragment_shrine", "Fragment Shrine",
     "Ein kleiner runder Schrein aus hellem Stein mit Säulen, in der Mitte schwebt ein großer kantiger Kristallsplitter (Fragment), der violett und leicht golden pulsiert; schwebende kleine Splitter darum, Risse im Boden leuchten schwach violett. Geheimnisvoll aber gezähmt."),
    ("guardian_shrine", "Guardian Shrine",
     "Ein uralter, moosbewachsener Schrein aus großen grauen Steinblöcken mit einer verwitterten Wächter-Statue (gesichtslose, gerüstete Gestalt mit Schild, keine erkennbare Tierform), Opferkerzen und Blumen davor, sanftes himmelblaues Licht aus Ritzen."),
    ("portal_chamber", "Portal Chamber",
     "Ein runder, kuppelförmiger Steinbau mit großem offenem Torbogen vorne; drinnen sieht man einen wirbelnden Portalriss aus violettem und blauem Licht. Metallringe und Ketten halten das Portal, Kristalle an den Seiten, Holzgerüst an der Kuppel."),
]

RUIN = ("ruin", "Ruine",
        "Generische Ruine eines zerstörten Dorfgebäudes, die als Platzhalter für noch nicht wiederaufgebaute Gebäude dient: "
        "zerbrochenes Steinfundament, verkohlte umgestürzte Balken, Schutt, ein paar Steine, ein kleines leeres Holzgerüst, ein wenig Rauch. "
        "Flach und breit (nicht hoch), damit sie überall hinpasst.")

TOWN = """BILD: Hintergrund der Dorf-Ansicht "Dawnhollow" (wie die Stadt Ranhorn in AFK Arena oder das Dorf in 7DS Idle Adventure).
- Hochformat 1024x1536, randlos bemalt (kein weißer Hintergrund).
- Erhöhte 3/4-Ansicht von schräg oben (ca. 35° von oben), wie die Stadt in AFK Arena.
- Eine Waldlichtung mit dem Dorfgelände: erdige Wege, die NEUN leere, flache, deutlich sichtbare Bauplätze verbinden (je ein runder oder ovaler Platz aus festgetretener Erde mit ein paar Pflastersteinen, alle ähnlich groß, ca. 15–20 % der Bildbreite). Die Bauplätze sind gleichmäßig über das Bild verteilt (nicht im obersten 12 % und nicht im untersten 15 % des Bildes, dort nur Wald/Wiese, weil da UI liegt), keiner überdeckt einen anderen.
- AUF den Bauplätzen stehen KEINE Gebäude (die werden später einzeln darüber gesetzt). Keine Figuren.
- Ein kleiner Bach mit Holzbrücke, Zäune, Blumen, Laternenpfähle, ein paar Kisten am Wegrand; Spuren der Katastrophe: einzelne verkohlte Baumstümpfe am Rand.
- Oben im Bild hinter den Bäumen: der Mossveil-Wald, durch die Bäume schimmert schwach violettes Licht des Rifts (klein, als Andeutung, nicht dominant).
- Stimmung: früher Morgen, warm, hoffnungsvoll, satte Grün- und Erdtöne.
"""

WORLD = """BILD: Weltkarte für den Abenteuer-Bildschirm (wie die Karte in 7DS Idle Adventure oder der Dark Forest in AFK Arena).
- Hochformat 1024x1536, randlos bemalt, gemalte Fantasy-Landschaft von schräg oben (leichte Vogelperspektive, keine flache Pergamentkarte), lebendige Farben.
- Unten Mitte: ein kleines Dorf in einer Waldlichtung (Dawnhollow). Darum dichter grüner Wald (Mossveil). Mitte links im Wald: eine Lichtung mit einem alten Schrein und einem violett leuchtenden Erdriss (das Rift).
- Mitte rechts: eine Jagdgegend – Hügel, Wiesen, eine Holzhütte am Waldrand, Lagerfeuer.
- Oben links: eine Felslandschaft mit einem alten Steinbogen-Portal, aus dem blaues und violettes Licht strömt.
- Oben rechts: eine steinerne Arena-Ruine (rundes Kolosseum) auf einem Plateau.
- Ganz oben Mitte: in der Ferne zerbrochene, schwebende Landmassen mit violetten Rissen am Himmel (die Shattered Realms), dunkler und bedrohlicher.
- Rechts unten: Meeresküste mit einer Bucht.
- Die Orte sind klar voneinander getrennt und gut lesbar, mit Wegen verbunden; dazwischen genug ruhige Fläche (Wald, Wiesen) für Markierungen, die das Spiel darüber legt.
- KEINE Schrift, keine Ortsnamen, keine Pins oder Symbole – die setzt das Spiel selbst.
"""

RUNNER = """#!/usr/bin/env bash
# Generates every town/world-map job with Codex, 5 in parallel. Logs per folder: codex.log
cd "$(dirname "$0")"
run() {
  dir="$1"
  if [ -f "$dir/master.png" ]; then echo "skip $dir"; return; fi
  codex exec --skip-git-repo-check -m gpt-6-sol -C "$(cygpath -w "$PWD/$dir")" -i style_reference.png - < "$dir/run_prompt.txt" > "$dir/codex.log" 2>&1
  echo "done $dir rc=$? master=$([ -f "$dir/master.png" ] && echo yes || echo no)" >> _batch.log
}
export -f run
sed 's/\\r$//' _jobs.txt | xargs -P 5 -I{} bash -c 'run {}'
echo "ALL DONE" >> _batch.log
"""


def main():
    out = Path(sys.argv[1])
    style = Path(sys.argv[2])
    out.mkdir(parents=True, exist_ok=True)
    jobs = [("00_town_background", TOWN), ("11_world_map", WORLD)]
    for i, (id, name, text) in enumerate(BUILDINGS + [RUIN], start=1):
        body = f"""GEBÄUDE – "{name}"
- {text}
- {CAMERA}
{BUILDING_RULES}"""
        jobs.append((f"{i:02d}_{id}", body))
    for folder, body in sorted(jobs):
        d = out / folder
        d.mkdir(exist_ok=True)
        shutil.copy(style, d / "style_reference.png")
        (d / "run_prompt.txt").write_text(HEADER.format(folder=str(d.resolve())) + "\n" + body, encoding="utf-8")
    (out / "_jobs.txt").write_text("\n".join(sorted(f for f, _ in jobs)) + "\n", encoding="utf-8", newline="\n")
    (out / "run_jobs.sh").write_text(RUNNER, encoding="utf-8", newline="\n")
    print(len(jobs), "jobs in", out)


if __name__ == "__main__":
    main()
