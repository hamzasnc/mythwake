"""Creates one Codex work folder per monster with the master prompt (step 1 only) and style references.

Output: <out>/<NN>_<id>/run_prompt.txt, style_reference.png
Then run run_masters.sh to generate the masters in parallel.

usage: py -3 make_master_jobs.py <out dir> <style reference sheet>
"""
import json
import shutil
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
GROUPS = {
    "wildBeast": "Wild Beasts – normale Tiere, frühe Gegner",
    "corruptedBeast": "Corrupted Beasts – Tiere, die Fragment-Energie verändert hat",
    "hollowed": "Hollowed – Menschen, Kreaturen oder Helden, die Fragmente verdorben haben",
    "crimsonWraith": "Crimson Wraiths – mächtige verdorbene Magier und Elite-Gegner",
    "riftborn": "Riftborn – Kreaturen aus den Shattered Realms",
    "fragmentConstruct": "Fragment Constructs – magische Wesen aus instabiler Fragment-Energie",
    "corruptedGuardian": "Corrupted Guardians – große Bosse, verbunden mit den verlorenen Guardians",
}
ASPECTS = {
    "ravak": "Ravak (Feuer, stärker bei wenig HP) – Leuchtfarbe Glutrot/Orange",
    "gorath": "Gorath (regeneriert, wächst mit der Kampfdauer) – Leuchtfarbe Bernstein/Fleischrot",
    "somnara": "Somnara (verlangsamt, Schlaf) – Leuchtfarbe Mondblau/Violett",
    "veloria": "Veloria (Bezauberung, Verwirrung) – Leuchtfarbe Rosa/Magenta",
    "envyra": "Envyra (stiehlt Buffs, kopiert) – Leuchtfarbe Türkis/Giftgrün",
}
POSE = {
    "quadruped": """- Vierbeiner in 3/4-Seitenansicht nach rechts, ruhige Stand-Pose (kein Sprung, kein Angriff).
- Alle vier Beine vollständig sichtbar, voneinander getrennt, mit weißem Raum zwischen den Beinen; nahe und ferne Beine klar unterscheidbar.
- Kopf frei vor dem Körper, Maul geschlossen oder nur leicht geöffnet, Schwanz hinten frei, nichts überdeckt die Beine.""",
    "humanoid": """- 3/4-Ansicht nach rechts, neutrale Rig-Pose, ganzer Körper mit Rand.
- Beine schulterbreit, von Hüfte bis Fuß sichtbar. Arme vom Körper weg, kreuzen weder Torso noch einander.
- Waffen im normalen Griff (Klinge kommt auf der Daumenseite aus der Faust). Umhänge und Stoff hängen hinter dem Körper.""",
    "flying": """- Schwebend/fliegend in 3/4-Ansicht nach rechts, Flügel weit ausgebreitet und vollständig im Bild, keine Überlappung von Flügel und Körper.
- Krallen/Füße sichtbar und getrennt, Kopf frei.""",
    "floating": """- Schwebend in 3/4-Ansicht nach rechts, ruhige Pose, der untere Teil (Rauch, Schleier, Lichtfahnen) endet vollständig im Bild.
- Arme/Hände vom Körper weg, nichts kreuzt, schwebende Teile mit weißem Abstand zum Körper.""",
    "huge": """- Riesige Boss-Gestalt in 3/4-Ansicht nach rechts, ruhige, imposante Stand-Pose, ganzer Körper mit Rand im Bild.
- Gliedmaßen (Beine, Arme, Tentakel, Flügel) klar voneinander getrennt und vollständig sichtbar, nichts überdeckt den Kern des Körpers.""",
}
TIER = {
    "normal": "Normaler Gegner: einfache, klar lesbare Silhouette, wenige Details, mittlere Größe.",
    "boss": "BOSS: deutlich größer und detailreicher als normale Gegner, imposante, unverwechselbare Silhouette, mehr Leuchtakzente – aber dieselbe Linienstärke und dasselbe Cel-Shading wie die Referenz.",
}

TEMPLATE = """ARBEITSORDNER: {folder}
Angehängt ist style_reference.png (liegt auch im Arbeitsordner): vier Helden aus demselben Spiel. Sie zeigen NUR den Zeichenstil (Linienstärke, Cel-Shading, Farbauftrag, Highlights) – NICHT Anatomie, Material oder Farben für dieses Monster.
Dieser Lauf ist nicht-interaktiv: Mach jetzt NUR SCHRITT 1. Generiere den Master mit deinem Bildgenerierungs-Tool, kopiere das fertige Bild als master.png in den Arbeitsordner (generierte Bilder landen sonst nur unter ~/.codex/generated_images) und speichere deinen verwendeten Bild-Prompt als master-prompt.txt daneben. Prüfe das Ergebnis selbst gegen die Pose-Regeln (ganzer Körper, Gliedmaßen frei, nichts kreuzt, weißer Hintergrund) und generiere neu, falls etwas klar verletzt ist. Danach beenden – mein OK kommt in einer Folgenachricht.

Neues MONSTER (Gegner, kein spielbarer Held) für mein Mobile-Idle-RPG Mythwake. Du machst NUR die Grafik (kein Rig, kein Spine-JSON, keine Animation).
Stil: Rendering exakt wie die Referenz – kräftige dunkle Outlines in gleicher Stärke, sauberes Cel-Shading, handgemalte Highlights. Proportionen CHIBI wie die Monster in AFK Arena / 7DS Idle Adventure: großer, ausdrucksstarker Kopf, kompakter, gedrungener Körper, klare große Formen, eher "niedlich-gefährlich" als realistisch. Kein realistisches Fell-/Federgewimmel, keine Detailüberladung.
VIELFALT (sehr wichtig): Dieses Monster hat eine komplett eigene Silhouette, eigene Farbpalette und eigene Materialien – es darf NICHT wie die Referenzfiguren aussehen (keine Steinhaut, keine Golem-/Felsoptik, außer die Beschreibung verlangt sie). Die Grundfarben kommen aus seiner Natur (z. B. Wolf grau-braun, Baum grün-braun, Drache gold-himmelblau, Gewölbe-Konstrukt schwarz-gold). Fragment-Violett nur als kleiner Akzent (Risse, Augen, ein paar Kristalle), nie als Hauptfarbe – außer bei reinen Rift-Wesen.
Originales Design, keine Ähnlichkeit zu existierenden Franchise-Figuren.

WELT: Atheris ist zerbrochen. Die alten Königreiche sind gefallen, die Guardians verschwunden, und die Fragmente – Überreste vergessener Macht – haben Narben in die Welt gerissen. Aus diesen Wunden bluten die Shattered Realms ins Land: verdorbene Bestien, verlorene Magie, violettes Licht. Im Grenzdorf Dawnhollow erwacht unter dem Mossveil Shrine ein Rift.

MONSTER – "{name}"
- Gruppe: {group}
- Fragment-Aspekt: {aspect}
- Rolle im Kampf: {role} · Reihe: {row}
- {tier}
- Aussehen: {art}
- Muss auf kleiner Bildschirmgröße sofort als dieses Monster erkennbar sein; Gruppe und Aspekt ablesbar.

SCHRITT 1 – MASTER (ein Bild, mindestens 1254 px hoch, reiner weißer Hintergrund #FFFFFF, KEIN gemaltes Karomuster, kein Boden, kein Schatten, keine Szene, kein Text):
{pose}
- Kleine Leuchteffekte nah am Körper, keine ausufernden Effekte.

(Später, NICHT jetzt: Schritt 2 – Ebenen durch Schälen aus dem Master für ein Spine-Rig.)
"""

ROLES = {"dps": "Schadensverursacher", "tank": "Tank", "support": "Unterstützer", "debuffer": "Schwächer"}
ROWS = {"front": "vorne", "back": "hinten"}


def main():
    out, ref1 = Path(sys.argv[1]), Path(sys.argv[2])
    roster = json.loads((HERE / "roster.json").read_text(encoding="utf-8"))["monsters"]
    out.mkdir(parents=True, exist_ok=True)
    jobs = []
    for i, m in enumerate(roster, 1):
        folder = out / f"{i:02d}_{m['id']}"
        folder.mkdir(exist_ok=True)
        shutil.copyfile(ref1, folder / "style_reference.png")
        prompt = TEMPLATE.format(
            folder=str(folder.resolve()), name=m["name"], group=GROUPS[m["group"]], aspect=ASPECTS[m["aspect"]],
            role=ROLES[m["role"]], row=ROWS[m["row"]], tier=TIER[m["tier"]], art=m["art"], pose=POSE[m["body"]])
        (folder / "run_prompt.txt").write_text(prompt, encoding="utf-8", newline="\n")
        jobs.append(folder.name)
    (out / "_jobs.txt").write_text("\n".join(jobs) + "\n", encoding="utf-8", newline="\n")
    print(f"{len(jobs)} jobs in {out}")


if __name__ == "__main__":
    main()
