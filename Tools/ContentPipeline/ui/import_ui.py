"""Imports the UI kit made by make_ui_jobs.py into Assets/Mythwake/Content/UI:

  icons       -> UI/Icons/<name>.png       (cut out, square, max 256 px)
  chrome      -> UI/Chrome/<name>.png      (cut out, enclosed white = transparent, max 768 px wide)
  background  -> UI/Backgrounds/<name>.png (1024x1536)

Sheets are split by the grid in cells.json: every painted blob (after joining nearby parts) goes to the cell its centre
lies in, so a motif that pokes a little over its cell border stays whole. A contact sheet _import_check.png is
written next to the jobs for review.

usage: py -3 import_ui.py <jobs dir>
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "monsters"))
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "town"))
from make_cutouts import background_mask  # noqa: E402
from import_icons import clear_holes  # noqa: E402

OUT = Path(__file__).resolve().parents[3] / "Assets" / "Mythwake" / "Content" / "UI"


def cut_full(master):
    """Like make_cutouts.cut, without cropping: white connected to the border becomes transparent, edges un-blended."""
    rgb = np.asarray(master.convert("RGB")).astype(np.float32)
    bg = background_mask(rgb.astype(np.uint8))
    alpha = np.where(bg, 0.0, 1.0)
    padded = np.pad(bg, 1)
    near_bg = padded[:-2, 1:-1] | padded[2:, 1:-1] | padded[1:-1, :-2] | padded[1:-1, 2:]
    edge = near_bg & ~bg
    coverage = np.clip((255.0 - rgb.min(axis=2)) / 110.0, 0.0, 1.0)
    alpha = np.where(edge, coverage, alpha)
    safe = np.maximum(alpha, 1e-3)[..., None]
    unblended = np.clip((rgb - (1.0 - alpha[..., None]) * 255.0) / safe, 0, 255)
    rgb = np.where(edge[..., None], unblended, rgb)
    return Image.fromarray(np.dstack([rgb, alpha * 255.0]).astype(np.uint8), "RGBA")


def split(image, cols, rows):
    """Bounding box per grid cell of all blobs whose centre lies in that cell."""
    a = np.asarray(image)[..., 3] > 20
    h, w = a.shape
    joined = ndimage.binary_dilation(a, iterations=max(4, w // 160))
    labels, count = ndimage.label(joined)
    boxes = {}
    for i, sl in enumerate(ndimage.find_objects(labels), start=1):
        if sl is None:
            continue
        ys, xs = sl
        part = (labels[sl] == i) & a[sl]
        if part.sum() < (w * h) * 0.0004:     # specks
            continue
        cy, cx = (ys.start + ys.stop) / 2, (xs.start + xs.stop) / 2
        cell = min(rows - 1, int(cy / h * rows)) * cols + min(cols - 1, int(cx / w * cols))
        x0, y0, x1, y1 = xs.start, ys.start, xs.stop, ys.stop
        if cell in boxes:
            b = boxes[cell]
            boxes[cell] = (min(b[0], x0), min(b[1], y0), max(b[2], x1), max(b[3], y1))
        else:
            boxes[cell] = (x0, y0, x1, y1)
    return boxes


def clear_window(img, min_area=0.08):
    """Frames with a light inner rim keep their white window after clear_holes: any big enclosed white area is a window."""
    rgba = np.asarray(img).copy()
    white = (rgba[..., :3].min(axis=2) >= 240) & (rgba[..., 3] > 0)
    labels, count = ndimage.label(white)
    if count == 0:
        return img
    areas = ndimage.sum(white, labels, range(1, count + 1))
    window = np.isin(labels, 1 + np.flatnonzero(areas >= min_area * white.size))
    if not window.any():
        return img
    rim = ndimage.binary_dilation(window, iterations=2) & ~window
    light = rgba[..., :3].min(axis=2).astype(np.float32)
    rgba[window, 3] = 0
    rgba[rim, 3] = np.minimum(rgba[rim, 3], np.clip((255 - light[rim]) / 60 * 255, 0, 255)).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA")


def save_icon(img, path, size=256):
    box = img.getbbox()
    img = img.crop(box) if box else img
    side = max(img.size)
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.alpha_composite(img, ((side - img.width) // 2, (side - img.height) // 2))
    canvas.resize((size, size), Image.LANCZOS).save(path, optimize=True)


def save_chrome(img, path, max_width=768):
    box = img.getbbox()
    img = img.crop(box) if box else img
    if img.width > max_width:
        img = img.resize((max_width, round(img.height * max_width / img.width)), Image.LANCZOS)
    img.save(path, optimize=True)


def main():
    jobs = Path(sys.argv[1])
    previews = []
    for folder in sorted(p for p in jobs.iterdir() if p.is_dir()):
        master, cells = folder / "master.png", folder / "cells.json"
        if not master.exists() or not cells.exists():
            continue
        spec = json.loads(cells.read_text(encoding="utf-8"))
        kind, names = spec["kind"], spec["names"]
        if kind == "appicon":
            Image.open(master).convert("RGB").resize((1024, 1024), Image.LANCZOS).save(OUT / "AppIcon.png", optimize=True)
            previews.append(("app_icon", Image.open(OUT / "AppIcon.png").convert("RGBA")))
            continue
        if kind == "background":
            (OUT / "Backgrounds").mkdir(parents=True, exist_ok=True)
            target = OUT / "Backgrounds" / f"{names[0]}.png"
            Image.open(master).convert("RGB").resize((1024, 1536), Image.LANCZOS).save(target, optimize=True)
            previews.append((names[0], Image.open(target).convert("RGBA")))
            continue
        sheet = cut_full(Image.open(master))
        boxes = split(sheet, spec["cols"], spec["rows"])
        sub = "Icons" if kind == "icons" else "Chrome"
        (OUT / sub).mkdir(parents=True, exist_ok=True)
        for i, name in enumerate(names):
            if i not in boxes:
                print(f"  {folder.name}: no motif in cell {i + 1} ({name})")
                continue
            part = sheet.crop(boxes[i])
            if kind == "chrome":
                part = clear_window(clear_holes(part))
                save_chrome(part, OUT / sub / f"{name}.png", 1200 if name == "logo" else 768)
            else:
                save_icon(clear_holes(part), OUT / sub / f"{name}.png")
            previews.append((name, Image.open(OUT / sub / f"{name}.png").convert("RGBA")))
        extra = sorted(set(boxes) - set(range(len(names))))
        if extra:
            print(f"  {folder.name}: extra blobs in cells {[c + 1 for c in extra]}")
        print(f"{folder.name}: {len([i for i in range(len(names)) if i in boxes])}/{len(names)}")

    # review sheet: every imported part on a dark background with its name
    cell = 160
    cols = 10
    sheet = Image.new("RGBA", (cols * cell, ((len(previews) + cols - 1) // cols) * (cell + 18)), (34, 38, 52, 255))
    draw = ImageDraw.Draw(sheet)
    for i, (name, img) in enumerate(previews):
        thumb = img.copy()
        thumb.thumbnail((cell - 8, cell - 8))
        x, y = (i % cols) * cell, (i // cols) * (cell + 18)
        sheet.alpha_composite(thumb, (x + (cell - thumb.width) // 2, y + 18 + (cell - thumb.height) // 2))
        draw.text((x + 3, y + 3), name[:22], fill="white")
    sheet.convert("RGB").save(jobs / "_import_check.png")
    print(f"imported {len(previews)} parts, check {jobs / '_import_check.png'}")
    write_icon_classes()


USS = OUT.parents[1] / "Runtime" / "UI" / "Styles" / "Mythwake.uss"
BEGIN, END = "/* @generated ui-icons begin (import_ui.py) */", "/* @generated ui-icons end */"


def write_icon_classes():
    """One class per icon, .ui-icon--<name>, so code can show any icon by name (UiKit.Icon)."""
    names = sorted(p.stem for p in (OUT / "Icons").glob("*.png"))
    block = [BEGIN] + [f'.ui-icon--{n} {{ background-image: url("project://database/Assets/Mythwake/Content/UI/Icons/{n}.png"); }}'
                       for n in names] + [END]
    text = USS.read_text(encoding="utf-8")
    if BEGIN in text:
        text = text[:text.index(BEGIN)] + "\n".join(block) + text[text.index(END) + len(END):]
    else:
        text = text.rstrip("\n") + "\n\n" + "\n".join(block) + "\n"
    USS.write_text(text, encoding="utf-8")
    print(f"{len(names)} icon classes in {USS.name}")


if __name__ == "__main__":
    main()
