"""Imports the Codex cut-ins made by make_cutin_jobs.py: NN_<id>/master.png (white background) ->
Assets/Mythwake/Content/Heroes/<id>/<id>_cutin.png, cut out and fitted into 1024x512 (bottom-aligned, centred).

usage: py -3 import_cutin_jobs.py <jobs dir>
Then Mythwake ▸ Rebuild Content Catalog.
"""
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent / "monsters"))
from make_cutouts import cut  # noqa: E402

HEROES = Path(__file__).resolve().parents[2] / "Assets" / "Mythwake" / "Content" / "Heroes"
W, H = 1024, 512


def main():
    done = []
    for folder in sorted(p for p in Path(sys.argv[1]).iterdir() if p.is_dir()):
        master = folder / "master.png"
        id = folder.name.split("_", 1)[1]
        if not master.exists() or not (HEROES / id).is_dir():
            continue
        figure = cut(Image.open(master))
        scale = min(W / figure.width, H / figure.height)
        figure = figure.resize((max(1, round(figure.width * scale)), max(1, round(figure.height * scale))), Image.LANCZOS)
        canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        canvas.alpha_composite(figure, ((W - figure.width) // 2, H - figure.height))
        canvas.save(HEROES / id / f"{id}_cutin.png", optimize=True)
        done.append(id)
    print(f"imported {len(done)}: {', '.join(done)}")


if __name__ == "__main__":
    main()
