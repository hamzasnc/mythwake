"""Builds the Spine rigs of every monster whose Codex peel package is finished and puts them into the game.

usage: py -3 build_all.py <masters dir> [<masters dir> ...] [--only id,id]

For each <NN>_<id>/layers.json found: build_creature.py -> Assets/Mythwake/Content/Monsters/<id>/<id>.json/.atlas.txt/.png
(the height comes from the monster's body type, matching the sprite sizes the battle used before). Unity imports the
files as a Spine skeleton; the content catalog then prefers the rig over the cut-out sprite.
"""
import json
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
MONSTERS = ROOT / "Assets" / "Mythwake" / "Content" / "Monsters"
HEIGHT = {"quadruped": 160, "flying": 210, "floating": 210, "humanoid": 240, "huge": 270}


def main():
    args = sys.argv[1:]
    only = None
    if "--only" in args:
        i = args.index("--only")
        only = set(args[i + 1].split(","))
        args = args[:i] + args[i + 2:]
    roster = {m["id"]: m for m in json.loads((HERE.parent / "roster.json").read_text(encoding="utf-8"))["monsters"]}
    built = []
    for d in args:
        for folder in sorted(Path(d).iterdir()):
            if not folder.is_dir() or "_" not in folder.name:
                continue
            mid = folder.name.split("_", 1)[1]
            if mid not in roster or (only and mid not in only) or not (folder / "layers.json").exists():
                continue
            out = MONSTERS / mid
            height = HEIGHT.get(roster[mid]["body"], 240)
            monster_json = out / f"{mid}.monster.json"
            r = subprocess.run([sys.executable, str(HERE / "build_creature.py"), str(folder), str(out), mid, str(height), str(monster_json)],
                               capture_output=True, text=True, encoding="utf-8")
            print(r.stdout.strip() or r.stderr.strip()[-800:])
            if r.returncode == 0:
                (out / "_setup_check.png").replace(folder / "_setup_check.png")
                built.append(mid)
    print(f"built {len(built)}: {', '.join(built)}")


if __name__ == "__main__":
    main()
