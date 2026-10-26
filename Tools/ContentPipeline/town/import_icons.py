"""Imports Codex item icons (see make_fragment_icon_jobs.py) into Assets/Mythwake/Content/Icons.

  NN_<name>/master.png -> Icons/<name>.png   (white background removed, cropped, max 512 px)

usage: py -3 import_icons.py <jobs dir>
"""
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "monsters"))
from make_cutouts import cut  # noqa: E402

OUT = Path(__file__).resolve().parents[3] / "Assets" / "Mythwake" / "Content" / "Icons"
MAX = 512


def main():
    jobs = Path(sys.argv[1])
    OUT.mkdir(parents=True, exist_ok=True)
    done = []
    for folder in sorted(p for p in jobs.iterdir() if p.is_dir()):
        master = folder / "master.png"
        if not master.exists():
            continue
        name = folder.name.split("_", 1)[1]
        icon = cut(Image.open(master))
        # square canvas so every icon sits the same in its box
        side = max(icon.size)
        canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        canvas.alpha_composite(icon, ((side - icon.width) // 2, (side - icon.height) // 2))
        if side > MAX:
            canvas = canvas.resize((MAX, MAX), Image.LANCZOS)
        canvas.save(OUT / f"{name}.png", optimize=True)
        done.append(name)
    print(f"imported {len(done)}: {', '.join(done)}")


if __name__ == "__main__":
    main()
