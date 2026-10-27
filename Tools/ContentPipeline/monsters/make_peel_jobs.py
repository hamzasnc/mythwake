"""Writes the Codex "peel" prompt (step 2) for monster masters: layered, rig-ready parts + layers.json for the
generic creature rig builder (spine/build_creature.py).

Same art rules as the heroes (output/Heroes_Basis_*/run_peel_prompt.txt): every hidden area is painted by the image
generator, never filled by code; the parts reassemble exactly into the approved master.

usage: py -3 make_peel_jobs.py <masters dir> <id,id,...>
Writes <dir>/<NN>_<id>/run_peel_prompt.txt and <dir>/_peel_jobs.txt; run_peel.sh runs them (5 in parallel).
"""
import sys
from pathlib import Path

# Parts per monster, back to front as a guide (Codex may merge or split where the master needs it, but keeps the
# naming scheme). "near" = towards the viewer (screen-left side of a body that faces right), "far" = behind.
PARTS = {
    "forest_wolf": "tail (chain), leg_hind_far_upper, leg_hind_far_lower, leg_front_far_upper, leg_front_far_lower, body (Rumpf mit Brustfell), leg_hind_near_upper, leg_hind_near_lower, leg_front_near_upper, leg_front_near_lower, ear_far, head (ohne Unterkiefer), jaw (Unterkiefer), ear_near, eye_near, eye_near_closed",
    "wild_boar": "tail (chain), leg_hind_far_upper, leg_hind_far_lower, leg_front_far_upper, leg_front_far_lower, body (Rumpf mit Rückenborsten), leg_hind_near_upper, leg_hind_near_lower, leg_front_near_upper, leg_front_near_lower, ear_far, head (ohne Unterkiefer, mit Hauern), jaw, ear_near, eye_near, eye_near_closed",
    "corrupted_wolf": "tail (chain), leg_hind_far_upper, leg_hind_far_lower, leg_front_far_upper, leg_front_far_lower, body (Rumpf mit Kristallen und glühenden Rissen), leg_hind_near_upper, leg_hind_near_lower, leg_front_near_upper, leg_front_near_lower, ear_far, head (ohne Unterkiefer), jaw, ear_near, eye_near, eye_near_closed, glow_cracks",
    "corrupted_boar": "tail (chain), leg_hind_far_upper, leg_hind_far_lower, leg_front_far_upper, leg_front_far_lower, body (Rumpf mit Moos und glühenden Rissen), leg_hind_near_upper, leg_hind_near_lower, leg_front_near_upper, leg_front_near_lower, ear_far, head (ohne Unterkiefer, mit Kristallhauern), jaw, ear_near, eye_near, eye_near_closed, glow_cracks",
    "corrupted_crow": "wing_far (chain), tail (chain, Schwanzfedern), leg_far, body (Rumpf), leg_near, head (mit Stirnauge, ohne Unterschnabel), jaw (Unterschnabel), wing_near (chain), eye_near, eye_near_closed",
    "hollowed_soldier": "cape (roter Stoffrest, chain), leg_far_upper, leg_far_lower, arm_far_upper, arm_far_lower, hand_far, weapon (Schwert in der hinteren Hand, prop), body (Rüstungstorso mit Kristallen), leg_near_upper, leg_near_lower, head, arm_near_upper, arm_near_lower, hand_near, shield (Rundschild an der vorderen Hand, prop)",
    "hollowed_archer": "cape (zerrissener Umhang, chain), quiver (Köcher mit Kristallpfeilen, prop), leg_far_upper, leg_far_lower, arm_far_upper, arm_far_lower, hand_far, body (Torso), leg_near_upper, leg_near_lower, head (Kapuze), arm_near_upper, arm_near_lower, hand_near, weapon (Bogen in der vorderen Hand, prop), glow_eyes",
    "crimson_wraith": "robe_back (chain), arm_far_upper, arm_far_lower, hand_far (mit Flamme), body (Kapuzenrobe Oberteil), robe_front (unterer Flammensaum, chain), head (Kapuze mit Gesicht), arm_near_upper, arm_near_lower, hand_near (mit Flamme), glow_flame_near, glow_flame_far, glow_eyes, shards (schwebende Splitter, prop)",
    "riftborn_stalker": "leg_far_upper, leg_far_lower, arm_far_upper, arm_far_lower, hand_far (Klauen), body (Kristalltorso), leg_near_upper, leg_near_lower, head (Kristallkopf mit Zacken), arm_near_upper, arm_near_lower, hand_near (Klauen), glow_veins",
    "riftborn_wisp": "tail (Geisterschweif, chain), body (Kopf und Körper ohne Hut), hat (Kristallhut, prop), hand_far, hand_near, crescent_left (schwebende Sichel, prop), crescent_right (schwebende Sichel, prop), crescent_low (schwebende Sichel, prop), eye_near, eye_near_closed, glow_core",
    "fragment_construct": "leg_far_upper, leg_far_lower, arm_far_upper, arm_far_lower, hand_far (Steinfaust), body (Steintorso mit Smaragdkern), leg_near_upper, leg_near_lower, head (Kristallkopf), arm_near_upper, arm_near_lower, hand_near (Steinfaust), glow_core",
    "hollowtusk_alpha": "tail (chain), leg_hind_far_upper, leg_hind_far_lower, leg_front_far_upper, leg_front_far_lower, body (Rumpf mit Rückenkristallen), leg_hind_near_upper, leg_hind_near_lower, leg_front_near_upper, leg_front_near_lower, ear_far, head (Kopf ohne Unterkiefer, mit Hauern), jaw (Unterkiefer), ear_near, eye_near, eye_near_closed",
    "riftborn_treant": "roots_back (chain), leg_far_upper, leg_far_lower, arm_far_upper, arm_far_lower, hand_far (Krallenäste), body (Stamm mit glühendem Herz), leg_near_upper, leg_near_lower, head (Gesicht im Stamm-Oberteil), crown (Laubkrone, chain), jaw, arm_near_upper, arm_near_lower, hand_near, eye_near, eye_far, eye_near_closed, eye_far_closed, glow_core",
    "crimson_wraith_adept": "robe_back (chain), leg_far_upper, leg_far_lower, arm_far_upper, arm_far_lower, hand_far (mit Flamme), body (Torso + Robe vorne), leg_near_upper, leg_near_lower, head (mit Maske/Kapuze), hair (chain), arm_near_upper, arm_near_lower, hand_near (mit Flamme), robe_front (chain), glow_flame_near, glow_flame_far",
    "hollow_spire_warden": "cape (chain), leg_far_upper, leg_far_lower, arm_far_upper, arm_far_lower, hand_far, halberd (prop in der hinteren Hand), body (Rüstungstorso mit Turmspitzen), leg_near_upper, leg_near_lower, head (Helm), arm_near_upper, arm_near_lower, hand_near, glow_eyes",
    "obsidian_vault_sentinel": "leg_far_upper, leg_far_lower, arm_far_upper, arm_far_lower, hand_far, body (Tresor-Torso mit Smaragd-Schloss), leg_near_upper, leg_near_lower, head (Helm/Kopf), arm_near_upper, arm_near_lower, hand_near, glow_core",
    "fragment_devourer": "tail (chain), leg_hind_far_upper, leg_hind_far_lower, leg_front_far_upper, leg_front_far_lower, body (Rumpf mit Kristallstacheln), leg_hind_near_upper, leg_hind_near_lower, leg_front_near_upper, leg_front_near_lower, head (Oberkiefer + Schädel), jaw (Unterkiefer), eye_near, eye_near_closed, glow_maw",
    "corrupted_guardian_of_the_deep": "tentacle_back_1 (chain), tentacle_back_2 (chain), leg_hind_far, leg_front_far, body (Unterkörper/Bauch), shell (Panzer mit Korallen und Ruinen), leg_hind_near, leg_front_near, neck, head, jaw, tentacle_front_1 (chain), tentacle_front_2 (chain), eye_near, eye_near_closed",
    "dragon_echo_of_the_sky": "wing_far (chain), tail (chain), leg_hind_far_upper, leg_hind_far_lower, arm_far (Vorderbein), body (Rumpf mit Brustkristall), leg_hind_near_upper, leg_hind_near_lower, neck (chain), head (ohne Unterkiefer, mit Hörnern), jaw, arm_near (Vorderbein), wing_near (chain), eye_near, eye_near_closed",
    "false_guardian_construct": "halo (Heiligenschein-Ringe, prop), arm_back_far_upper, arm_back_far_lower, shield_far (Spiegelschild), leg_far_upper, leg_far_lower, arm_far_upper, arm_far_lower, hand_far, body (Marmortorso mit Kern), leg_near_upper, leg_near_lower, head (Maskengesicht), arm_near_upper, arm_near_lower, hand_near, arm_back_near_upper, arm_back_near_lower, shield_near, glow_core",
    "celestial_turtle": "tail (chain), leg_hind_far, leg_front_far, body (Unterkörper), shell (Sternenpanzer mit schwebenden Inseln), leg_hind_near, neck (chain), head (ohne Unterkiefer), jaw, leg_front_near, eye_near, eye_near_closed, glow_cracks",
    "dragon_of_the_sky": "wing_far (chain), tail (chain), leg_hind_far_upper, leg_hind_far_lower, leg_front_far_upper, leg_front_far_lower, body (Rumpf mit Rückenkristallen), leg_hind_near_upper, leg_hind_near_lower, mane (Wolkenmähne, chain), neck (chain), head (ohne Unterkiefer, mit Hörnern), jaw, leg_front_near_upper, leg_front_near_lower, wing_near (chain), eye_near, eye_near_closed, glow_lightning",
    "hollowed_leviathan": "fin_back (chain), tail (chain, hinterer Körperteil bis zur Schwanzflosse), body (chain, mittlerer S-Bogen mit den hohlen Rippen), fin_mid (chain), neck (chain), head (ohne Unterkiefer), jaw, fin_head_near, eye_near, eye_near_closed, glow_lanterns",
    "six_legged_worldbearer": "leg_3_far_upper, leg_3_far_lower, leg_2_far_upper, leg_2_far_lower, leg_1_far_upper, leg_1_far_lower, tail (chain), body (Rumpf mit der Landschaft: Berge, Wald, Wasserfall, Ruinen), leg_3_near_upper, leg_3_near_lower, leg_2_near_upper, leg_2_near_lower, head (mit Hörnern), jaw, leg_1_near_upper, leg_1_near_lower, eye_near, eye_near_closed, glow_crystals",
    "craterborn_guardian": "shards_back (Meteorsplitter hinten, prop), leg_far_upper, leg_far_lower, arm_far_upper, arm_far_lower, hand_far, body (Torso mit Krater), leg_near_upper, leg_near_lower, head (Kraterrand-Helm), arm_near_upper, arm_near_lower, hand_near, shards_front (Meteorsplitter vorne, prop), glow_crater",
}

PROMPT = """ARBEITSORDNER: {folder}
Dein Master (master.png in diesem Ordner) für das Monster "{name}" ist freigegeben. Mach jetzt SCHRITT 2 komplett und ohne Rückfragen: das Monster in rig-fertige Ebenen SCHÄLEN für ein Spine-Rig. Dieser Lauf ist nicht-interaktiv; Entscheidungen in NOTES.md. Du machst NUR die Grafik und layers.json – das Rig, die Animationen und den Spine-Export macht ein Skript danach.

EBENEN (von hinten nach vorne, als Richtwert; zusammenlegen oder teilen nur, wo der Master es verlangt – Namensschema beibehalten):
{parts}

NAMENSSCHEMA: near = zum Betrachter hin (vorne), far = dahinter. Beine/Arme mit Gelenk werden an jedem Gelenk getrennt (_upper/_lower, ggf. Fuß/Hand eigene Ebene). Lange biegsame Teile (Schwanz, Tentakel, Hals, Mähne, Umhang, Flügel, Schlangenkörper, Krone) bleiben EINE Ebene und werden in layers.json als "chain" markiert. Kleine hängende Anhängsel bleiben Teil ihrer Elternebene. Leuchten/Auren als glow_* (nur das Leuchten, additiv gedacht).
Varianten: eye_*_closed (Lid geschlossen, gleicher Stil, gleiche Stelle). Falls ein Maul da ist: jaw = Unterkiefer als eigene Ebene, und der Oberkiefer/Schädel (head) wird so ausgemalt, dass bei geöffnetem Kiefer der Rachen dunkel gemalt sichtbar ist (Zähne/Zunge/Rachen im head unter dem Kiefer weitermalen).

PFLICHTREGELN FÜR DAS SCHÄLEN (wie bei den Helden):
- Jede Ebene ist ein RGBA-PNG in der vollen Master-Größe ({w}x{h}), an derselben Stelle wie im Master, Hintergrund transparent.
- Jede verdeckte Fläche malt der BILDGENERATOR (maskierter Edit, Spenderbild unter work/donors/<schritt>_<ebene>.png), übernommen per Code nur innerhalb der Maske. Verboten als Füllung: flache Farbflächen, Polygone, Verläufe, Weichzeichnen, geklonte Pixel, weiße oder leere Blöcke.
- Schnittkanten an Gelenken: beide Seiten mit runder Überlappung (ca. 30 px) ausmalen, damit beim Drehen keine Lücke entsteht. Der Ansatz eines Beins/Flügels/Halses/Schwanzes reicht ca. 30–40 px unter den Körper.
- Alle sichtbaren Ebenen übereinander (in Reihenfolge) müssen den Master pixelgenau ergeben: prüfe das mit work/composite_check.png (Master | Zusammensetzung | Differenz) und behebe sichtbare Abweichungen.
- Kleine Fehler (Säume, Restpixel) sind KEIN Grund zum Abbrechen: weiterschälen, am Ende ein Reparatur-Durchgang (maskierter Bild-Edit nur auf die Stelle), Rest in NOTES.md notieren. Abbrechen nur, wenn der Bildgenerator blockiert – dann genauer Stand in NOTES.md, keine Code-Füllung.
- Vor dem Abschluss work/layers_contact.png ansehen (jede Ebene einzeln auf mittelgrauem Grund); ungezeichnete Flecken neu malen.

layers.json (exakt dieses Format, Pixel, Ursprung oben links; die // Kommentare hier sind nur Erklärung und kommen NICHT in die Datei – layers.json muss gültiges JSON sein):
{{
  "canvas": {{"width": {w}, "height": {h}}},
  "facing": "right",
  "ground": [x, y],            // Punkt auf dem Boden mittig unter dem Körper (bei schwebenden Wesen: unter dem Körperschwerpunkt, auf Höhe der Unterkante)
  "layers": [
    {{
      "name": "leg_front_near_upper",
      "file": "layers/leg_front_near_upper.png",
      "order": 12,                 // 0 = ganz hinten
      "visible": true,             // Varianten (eye_*_closed) false
      "variant_of": null,          // bei Varianten: Name der Ebene, die sie ersetzen
      "parent": "body",            // Ebene, an der das Teil hängt; null nur für "body" (bzw. das Hauptteil)
      "role": "limb",              // body | head | jaw | limb | chain | eye | glow | prop
      "joints": {{"root": [x, y], "end": [x, y]}},   // root = Drehpunkt am Elternteil (Gelenk), end = freies Ende/nächstes Gelenk
      "spine": [[x, y], [x, y], [x, y], [x, y]]     // NUR bei role "chain": 4–6 Punkte entlang der Mittellinie von root bis end
    }}
  ]
}}
Wichtig für die Gelenkpunkte: root liegt genau dort, wo sich das Teil am Elternteil dreht (Schulter, Hüfte, Knie, Kieferscharnier, Halsansatz, Schwanzansatz, Flügelgelenk). Für "body": root = Körperschwerpunkt, end = Richtung Kopf. Für head: root = Halsansatz/Nacken, end = Schnauzen- bzw. Gesichtsspitze. Für jaw: root = Kieferscharnier, end = Kinnspitze.

LIEFERUNG im Arbeitsordner: layers/*.png, layers.json, work/composite_check.png, work/layers_contact.png, NOTES.md (Ebenenliste, Entscheidungen, Rest-Fehler). Danach beenden.
"""

RUNNER = """#!/usr/bin/env bash
# Runs the peel jobs listed in _peel_jobs.txt with Codex, 5 in parallel. Log per folder: codex_peel.log
cd "$(dirname "$0")"
run() {
  dir="$1"
  if [ -f "$dir/layers.json" ]; then echo "skip $dir"; return; fi
  codex exec --skip-git-repo-check -m gpt-6-luna -c 'model_reasoning_effort="high"' -c 'service_tier="default"' -C "$(cygpath -w "$PWD/$dir")" -i "$(cygpath -w "$PWD/$dir/master.png")" - < "$dir/run_peel_prompt.txt" > "$dir/codex_peel.log" 2>&1
  echo "done $dir rc=$? layers=$([ -f "$dir/layers.json" ] && echo yes || echo no)" >> _peel_batch.log
}
export -f run
sed 's/\\r$//' _peel_jobs.txt | xargs -P 5 -I{} bash -c 'run {}'
echo "ALL DONE" >> _peel_batch.log
"""


def main():
    import json
    from PIL import Image
    root = Path(sys.argv[1])
    ids = sys.argv[2].split(",")
    roster = {m["id"]: m for m in json.loads((Path(__file__).parent / "roster.json").read_text(encoding="utf-8"))["monsters"]}
    jobs = []
    for mid in ids:
        folder = next((f for f in root.iterdir() if f.is_dir() and f.name.split("_", 1)[-1] == mid), None)
        if folder is None or not (folder / "master.png").exists():
            print("missing master:", mid)
            continue
        w, h = Image.open(folder / "master.png").size
        parts = "- " + PARTS[mid].replace(", ", "\n- ")
        text = PROMPT.format(folder=str(folder.resolve()), name=roster[mid]["name"], parts=parts, w=w, h=h)
        (folder / "run_peel_prompt.txt").write_text(text, encoding="utf-8", newline="\n")
        jobs.append(folder.name)
    (root / "_peel_jobs.txt").write_text("\n".join(jobs) + "\n", encoding="utf-8", newline="\n")
    (root / "run_peel.sh").write_text(RUNNER, encoding="utf-8", newline="\n")
    print(len(jobs), "peel jobs in", root)


if __name__ == "__main__":
    main()
