"""Copies each hero's ultimate cut-in art (cutin_character.png from the art packages) into its content folder.

Writes Assets/Mythwake/Content/Heroes/<id>/<id>_cutin.png at 1024x512. Later packages override earlier ones,
so pass the newest package root last.

usage: py -3 import_cutins.py <package root> [<package root> ...]
"""
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
HEROES = ROOT / "Assets" / "Mythwake" / "Content" / "Heroes"


def candidates(package):
    for name in ("delivery/cutin_character.png", "cutin_character.png"):
        p = package / name
        if p.exists():
            return p
    return None


def main():
    ids = sorted(p.name for p in HEROES.iterdir() if p.is_dir())
    found = {}
    for root in map(Path, sys.argv[1:]):
        for package in sorted(p for p in root.iterdir() if p.is_dir()):
            key = package.name.lower().replace("ä", "ae").replace("ö", "oe").replace("ü", "ue")
            hero = next((i for i in ids if key.endswith(i)), None)
            art = candidates(package)
            if hero and art:
                found[hero] = art
    for hero, art in sorted(found.items()):
        im = Image.open(art).convert("RGBA")
        im = im.resize((1024, im.height * 1024 // im.width), Image.LANCZOS)
        im.save(HEROES / hero / f"{hero}_cutin.png", optimize=True)
    missing = [i for i in ids if i not in found]
    print(f"cut-ins: {len(found)}  missing: {', '.join(missing)}")


if __name__ == "__main__":
    main()
