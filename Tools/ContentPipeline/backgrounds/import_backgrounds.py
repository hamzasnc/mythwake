"""Copies finished Codex masters into the game: battle backgrounds to Content/Backgrounds/<id>.png, intro pictures to
Content/Story/<id>.png (the id is the job folder name without its number).

usage: py -3 import_backgrounds.py <jobs dir> [<jobs dir> ...]
Then Mythwake ▸ Rebuild Content Catalog.
"""
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
CONTENT = ROOT / "Assets" / "Mythwake" / "Content"
SIZE = (1024, 1536)


def main():
    copied = 0
    for jobs in map(Path, sys.argv[1:]):
        for folder in sorted(jobs.iterdir()):
            master = folder / "master.png"
            if not folder.is_dir() or "_" not in folder.name or not master.exists():
                continue
            id = folder.name.split("_", 1)[1]
            target = CONTENT / ("Story" if id.startswith("story_") else "Backgrounds") / f"{id}.png"
            target.parent.mkdir(parents=True, exist_ok=True)
            image = Image.open(master).convert("RGB")
            if image.size != SIZE:
                print(f"{id}: {image.size} -> {SIZE}")
                image = image.resize(SIZE, Image.LANCZOS)
            image.save(target, optimize=True)
            copied += 1
    print(f"imported {copied} pictures")


if __name__ == "__main__":
    main()
