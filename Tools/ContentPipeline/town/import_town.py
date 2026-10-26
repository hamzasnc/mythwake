"""Imports the Codex town art (see make_town_jobs.py) into Assets/Mythwake/Content/Town.

  00_town_background/master.png -> town_background.png
  11_world_map/master.png       -> world_map.png
  10_ruin/master.png            -> ruin.png             (white background removed, cropped)
  NN_<building>/master.png      -> Buildings/<id>.png    (id = buildings.json id, e.g. hunters_hut -> huntersHut)

Hotspot positions live in Content/Town/town.json and are edited by hand against the pictures.

usage: py -3 import_town.py <jobs dir>
"""
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "monsters"))
from make_cutouts import cut  # noqa: E402

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "Assets" / "Mythwake" / "Content" / "Town"
MAX_BUILDING = 768


def camel(snake):
    head, *rest = snake.split("_")
    return head + "".join(p.capitalize() for p in rest)


def cut_building(master, dest):
    sprite = cut(Image.open(master))
    if max(sprite.size) > MAX_BUILDING:
        scale = MAX_BUILDING / max(sprite.size)
        sprite = sprite.resize((round(sprite.width * scale), round(sprite.height * scale)), Image.LANCZOS)
    sprite.save(dest, optimize=True)


def main():
    jobs = Path(sys.argv[1])
    (OUT / "Buildings").mkdir(parents=True, exist_ok=True)
    done = []
    for folder in sorted(p for p in jobs.iterdir() if p.is_dir()):
        master = folder / "master.png"
        if not master.exists():
            continue
        name = folder.name.split("_", 1)[1]
        if name == "town_background":
            Image.open(master).convert("RGB").save(OUT / "town_background.png", optimize=True)
        elif name == "world_map":
            Image.open(master).convert("RGB").save(OUT / "world_map.png", optimize=True)
        elif name == "ruin":
            cut_building(master, OUT / "ruin.png")
        else:
            cut_building(master, OUT / "Buildings" / f"{camel(name)}.png")
        done.append(name)
    print(f"imported {len(done)}: {', '.join(done)}")


if __name__ == "__main__":
    main()
