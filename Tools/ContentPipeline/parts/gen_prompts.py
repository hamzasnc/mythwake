"""One set of prompts for generating a hero's rig parts one by one, shared by Codex and the local image model.

usage: py -3 gen_prompts.py <out dir> <reference png>
Writes <out>/PROMPTS.md (all prompts, for people) and <out>/codex/<NN_name>/ jobs + run_jobs.sh (Codex image
generation, gpt-6-luna, reasoning high, normal tier). local_gen.py reads PROMPTS from here for the local model.
"""
import shutil
import sys
from pathlib import Path

STYLE = ("same character as the reference, chibi game character, about 2.5 heads tall, clean crisp 2D anime line art, "
         "dark outlines, flat cel shading with two shadow tones, light from top left, mobile idle RPG character art, "
         "plain pure white background, no ground shadow, no text, no effects")

PART = ("a single isolated body part of Dorn for 2D skeletal animation: {}. Only this one piece, complete, nothing else "
        "visible; both joint ends fully painted and rounded like a capsule so it can rotate under the neighbouring part; "
        "no cut edges, no other body parts, no props")

DESIGN = ("tousled short medium brown hair with copper highlights, dark red headband with small white fang charms, amber "
          "eyes, small pale bandage over the nose, red scarf, brown leather armor with pale shoulder fur and bronze buckles "
          "over a white apron with a red flame-fang mark, short red armband, dark loose trousers with iron knee guards, "
          "brown boots")

# (number, file name, aspect w:h, prompt without the style block)
PROMPTS = [
    (1, "master_apose", (2, 3), "full body of Dorn standing straight in a neutral A-pose, front three-quarter view, arms "
     "held slightly away from the body and slightly bent, both hands empty and relaxed open, legs straight about shoulder "
     "width apart, nothing in the hands, no shield, no sword; " + DESIGN),
    (2, "sword", (1, 2), "a single simple short iron sword of Dorn, isolated object, straight vertical, blade pointing "
     "down, handle at the top, leather wrapped grip, small bronze crossguard, side view, game item sprite"),
    (3, "shield", (1, 1), "a single small round wooden shield of Dorn, isolated object, front view, wooden planks, iron "
     "rim with rivets, iron boss in the center, game item sprite"),
    (4, "head_bare", (1, 1), "only the bare head of Dorn for a 2D rig: bald head skin shape with both ears and the small "
     "nose bandage, completely without hair, without eyes, without eyebrows, without mouth, smooth blank face, the neck "
     "stub at the bottom ends in a round painted cap"),
    (5, "near_upperarm", (1, 2), PART.format("his left upper arm from shoulder to elbow, pale shoulder fur on top, short "
     "red armband with white flame mark, bare skin below, shown vertical, shoulder at the top")),
    (6, "near_forearm", (1, 2), PART.format("his left forearm from elbow to wrist, brown leather bracer with bronze "
     "buckles, shown vertical, elbow at the top")),
    (7, "near_hand", (1, 1), PART.format("his left hand in a fingerless brown leather glove, open relaxed, back of the "
     "hand visible, wrist at the top")),
    (8, "far_upperarm", (1, 2), PART.format("his right upper arm from shoulder to elbow, pale shoulder fur on top, white "
     "shirt sleeve, bare skin below, shown vertical, shoulder at the top")),
    (9, "far_forearm", (1, 2), PART.format("his right forearm from elbow to wrist, brown leather bracer with bronze "
     "buckles, shown vertical, elbow at the top")),
    (10, "far_hand_grip", (1, 1), PART.format("his right hand in a fingerless brown leather glove, closed as a fist "
     "gripping an invisible handle, wrist at the top")),
    (11, "torso", (3, 4), PART.format("his upper body only, without head, without neck, without arms: brown leather "
     "armor with bronze buckles and crossing straps over a white shirt, pale shoulder fur pads, belt at the bottom; "
     "shoulder and waist ends rounded")),
    (12, "pelvis", (4, 3), PART.format("his hips only: wide brown belt with bronze buckle and leather pouch, top of dark "
     "loose trousers, without legs")),
    (13, "thigh", (1, 2), PART.format("one upper leg from hip to knee in dark loose trousers, shown vertical, hip at the "
     "top")),
    (14, "shin", (1, 2), PART.format("one lower leg from knee to ankle: iron knee guard on leather strap, dark trousers, "
     "top of brown boot, shown vertical, knee at the top")),
    (15, "foot", (1, 1), PART.format("one brown leather boot with buckled straps, side three-quarter view, ankle at the "
     "top")),
    (16, "scarf_front", (4, 3), "only Dorn's red scarf wrapped around the neck, front part, without the body, isolated "
     "costume piece"),
    (17, "apron", (2, 3), "only Dorn's white smith apron with a red flame-fang mark and torn bottom edge, without the "
     "body, isolated costume piece"),
    (18, "eyes_open", (2, 1), "only Dorn's two amber anime eyes as a pair, open, determined look, nothing else"),
    (19, "eyes_closed", (2, 1), "only Dorn's two eyes as a pair, closed, curved lash lines, nothing else"),
    (20, "brows", (2, 1), "only Dorn's two dark brown eyebrows as a pair, determined, nothing else"),
    (21, "mouth_open", (1, 1), "only Dorn's mouth, open shouting, nothing else"),
    (22, "hair", (1, 1), "only Dorn's hair as a separate piece: tousled short medium brown hair with copper highlights "
     "and the dark red headband with small white fang charms, without head and face, front view"),
]

CODEX = """ARBEITSORDNER: {folder}
Angehängt: reference.png (auch im Arbeitsordner) – der freigegebene Held Dorn (Chibi-Stil, handgemalt).

AUFGABE: Erzeuge mit deinem Bildgenerierungs-Tool GENAU EIN neues Bild nach diesem Prompt. reference.png ist die
Charakter- und Stil-Referenz: gleicher Charakter, gleiche Farben, gleiche Linien und Schattierung. Seitenverhältnis
{aspect} (Hochformat/Querformat entsprechend). Ändere nichts am Prompt-Inhalt, erfinde keine zusätzlichen Teile.

PROMPT:
{prompt}, {style}

Dieser Lauf ist nicht-interaktiv: kopiere das fertige Bild als result.png in den Arbeitsordner (generierte Bilder landen
sonst nur unter ~/.codex/generated_images). Prüfe kurz: nur das verlangte Motiv, weißer Hintergrund, kein Text. Wenn
nicht, neu generieren. Danach beenden.
"""

RUNNER = """#!/usr/bin/env bash
# Generates every prompt with Codex image generation, 5 in parallel. Logs per folder: codex.log
cd "$(dirname "$0")"
run() {
  dir="$1"
  if [ -f "$dir/result.png" ]; then echo "skip $dir"; return; fi
  codex exec --skip-git-repo-check -m gpt-6-luna -c 'model_reasoning_effort="high"' -c 'service_tier="default"' -C "$(cygpath -w "$PWD/$dir")" -i reference.png - < "$dir/run_prompt.txt" > "$dir/codex.log" 2>&1
  # Codex' Windows sandbox may be read-only: take the session's newest generated image instead
  sid=$(grep -m1 -o 'session id: [0-9a-f-]*' "$dir/codex.log" | cut -d' ' -f3)
  img=$(ls -t "$HOME/.codex/generated_images/$sid/"*.png 2>/dev/null | head -1)
  if [ ! -f "$dir/result.png" ] && [ -n "$img" ]; then cp "$img" "$dir/result.png"; fi
  echo "done $dir result=$([ -f "$dir/result.png" ] && echo yes || echo no)" >> _batch.log
}
export -f run
sed 's/\\r$//' _jobs.txt | xargs -P 5 -I{} bash -c 'run {}'
echo "ALL DONE" >> _batch.log
"""


def main():
    out, ref = Path(sys.argv[1]), Path(sys.argv[2])
    codex = out / "codex"
    codex.mkdir(parents=True, exist_ok=True)
    jobs = []
    for n, name, (w, h), text in PROMPTS:
        d = codex / f"{n:02d}_{name}"
        d.mkdir(exist_ok=True)
        shutil.copy(ref, d / "reference.png")
        (d / "run_prompt.txt").write_text(CODEX.format(folder=str(d.resolve()), aspect=f"{w}:{h}", prompt=text,
                                                       style=STYLE), encoding="utf-8")
        jobs.append(d.name)
    (codex / "_jobs.txt").write_text("\n".join(jobs) + "\n", encoding="utf-8", newline="\n")
    (codex / "run_jobs.sh").write_text(RUNNER, encoding="utf-8", newline="\n")
    lines = ["# Dorn – Teile einzeln generieren: lokale KI vs. Codex", "",
             "Gleiche Prompts für beide. Referenz: `referenz_dorn_master.png`. Ergebnisse: `lokal/` und `codex/`.", "",
             f"Stil-Block (an jeden Prompt angehängt): `{STYLE}`", "", "| Nr | Datei | Seiten | Prompt |", "|---|---|---|---|"]
    lines += [f"| {n:02d} | {name} | {w}:{h} | {text} |" for n, name, (w, h), text in PROMPTS]
    (out / "PROMPTS.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(len(jobs), "codex jobs in", codex)


if __name__ == "__main__":
    main()
