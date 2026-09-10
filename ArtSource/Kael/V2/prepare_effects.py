"""Prepare Kael V2 painted effects from the approved generated source images.

Run with the project's Python (Pillow + NumPy), from any working directory:
    python ArtSource/Kael/V2/prepare_effects.py

No Unity, downloads, preferences, scenes, or source images are modified.
Outputs use straight alpha. Existing genuine source alpha is preserved.
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageOps

SOURCE = Path(__file__).resolve().parent
PROJECT = SOURCE.parents[2]
OUTPUT = PROJECT / "Assets/_Mythwake/Resources/Characters/Kael"
QA = PROJECT / "artifacts/kael"
TILES = (
    "0 / thin blade trail",
    "1 / broad curved trail",
    "2 / blade charge (up)",
    "3 / opening / basic impact",
    "4 / final skill impact",
    "5 / directional after-stroke",
    "6 / flying ember fragments",
    "7 / optional ink wake",
)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def exterior_connected(mask: np.ndarray, negative_space_seeds: tuple[tuple[int, int], ...] = ()) -> np.ndarray:
    """Flood candidate pixels connected to the boundary or inspected negative space."""
    pixels = Image.fromarray(np.where(mask, 255, 0).astype(np.uint8), "L")
    expanded = ImageOps.expand(pixels, border=1, fill=255)
    ImageDraw.floodfill(expanded, (0, 0), 128, thresh=0)
    for x, y in negative_space_seeds:
        if expanded.getpixel((x + 1, y + 1)) == 255:
            ImageDraw.floodfill(expanded, (x + 1, y + 1), 128, thresh=0)
    return np.asarray(expanded)[1:-1, 1:-1] == 128


def resize_rgba(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    """Filter coverage and premultiplied colour together, then store straight alpha."""
    return image.convert("RGBa").resize(size, Image.Resampling.LANCZOS).convert("RGBA")


def prepare_vfx_source(image: Image.Image) -> tuple[Image.Image, str]:
    rgba = np.asarray(image.convert("RGBA")).copy()
    rgb = rgba[:, :, :3]
    alpha = rgba[:, :, 3]
    # The actual approved VFX source has detailed alpha and opaque dark ink.
    # Keying its dark RGB would remove intentional material, so use native coverage.
    native_alpha = image.mode == "RGBA" and np.mean(alpha < 250) > 0.03 and np.mean(alpha < 4) > 0.01
    if native_alpha:
        exterior = exterior_connected(alpha <= 3)
        rgba[exterior, 3] = 0
        method = "native source alpha; boundary-connected residual coverage <=3 removed"
    else:
        # Reproducible support for a future opaque black-background export:
        # preserve internal dark ink, remove only near-black exterior, and unmatte
        # its immediate edge. This branch is not used for the approved RGBA source.
        exterior = exterior_connected(rgb.max(axis=2) <= 6)
        near = np.asarray(Image.fromarray((exterior * 255).astype(np.uint8), "L").filter(ImageFilter.MaxFilter(5))) > 0
        edge = near & ~exterior
        coverage = np.ones(alpha.shape, dtype=np.float32)
        coverage[exterior] = 0
        coverage[edge] = np.maximum(rgb.max(axis=2)[edge].astype(np.float32) / 255, 1 / 255)
        straight = np.clip(rgb.astype(np.float32) / np.maximum(coverage[..., None], 1 / 255), 0, 255)
        rgba[:, :, :3] = np.rint(straight).astype(np.uint8)
        rgba[:, :, 3] = np.rint(coverage * alpha).astype(np.uint8)
        method = "boundary-connected black matte removed; immediate edge unmatted"
    rgba[rgba[:, :, 3] == 0, :3] = 0
    return Image.fromarray(rgba, "RGBA"), method


def prepare_portrait(image: Image.Image) -> tuple[Image.Image, dict]:
    rgb = np.asarray(image.convert("RGB")).astype(np.float32)
    minimum = rgb.min(axis=2)
    maximum = rgb.max(axis=2)
    # Ivory hair is enclosed by its dark contour. Only neutral near-white pixels
    # connected to the outer background are removed, not all bright image pixels.
    candidate = (minimum >= 238) & ((maximum - minimum) <= 22)
    # White negative-space islands enclosed by the crimson strokes are background,
    # too. These inspected source-space seeds do not intersect hair, skin or metal.
    negative_space_seeds = ((900, 420), (1000, 650))
    exterior = exterior_connected(candidate, negative_space_seeds)
    near = np.asarray(Image.fromarray((exterior * 255).astype(np.uint8), "L").filter(ImageFilter.MaxFilter(5))) > 0
    edge = near & ~exterior
    coverage = np.ones(minimum.shape, dtype=np.float32)
    coverage[exterior] = 0
    coverage[edge] = np.clip(1 - minimum[edge] / 255, 1 / 255, 1)
    # Remove white spill from the narrow antialiased boundary only. Interior skin,
    # ivory hair, bright metal and other painted colours remain their source RGB.
    straight = rgb.copy()
    a = coverage[edge, None]
    straight[edge] = np.clip((rgb[edge] - (1 - a) * 255) / a, 0, 255)
    rgba = np.empty((*minimum.shape, 4), dtype=np.uint8)
    rgba[:, :, :3] = np.rint(straight).astype(np.uint8)
    rgba[:, :, 3] = np.rint(coverage * 255).astype(np.uint8)
    rgba[exterior, :3] = 0
    result = resize_rgba(Image.fromarray(rgba, "RGBA"), (1024, 512))
    return result, {
        "method": "boundary-connected neutral white >=238; two-pixel edge white unmatting",
        "source_background_pixels": int(exterior.sum()),
        "inspected_negative_space_seeds_xy": negative_space_seeds,
        "source_edge_pixels": int(edge.sum()),
        "interior_bright_pixels_preserved": int(((minimum >= 238) & ~near).sum()),
    }


def make_atlas(source: Image.Image) -> tuple[Image.Image, list[dict]]:
    cleaned, method = prepare_vfx_source(source)
    atlas = Image.new("RGBA", (2048, 1024), (0, 0, 0, 0))
    w, h = cleaned.size
    report = []
    for index, name in enumerate(TILES):
        col, row = index % 4, index // 4
        # Rounded source-grid boundaries avoid dropped/duplicated columns when
        # the generated image has non-divisible dimensions (1774 x 887).
        crop = (round(col * w / 4), round(row * h / 2),
                round((col + 1) * w / 4), round((row + 1) * h / 2))
        cell = cleaned.crop(crop)
        painted = resize_rgba(cell, (488, 488))
        atlas.alpha_composite(painted, (col * 512 + 12, row * 512 + 12))
        pixels = np.asarray(painted)
        report.append({
            "index": index, "name": name, "source_crop_xyxy": crop,
            "atlas_cell_xywh": [col * 512, row * 512, 512, 512],
            "paint_inset_px": 12, "alpha_method": method,
            "covered_pixels": int((pixels[:, :, 3] > 8).sum()),
            "dark_ink_pixels": int(((pixels[:, :, :3].max(axis=2) < 50) & (pixels[:, :, 3] > 96)).sum()),
        })
    return atlas, report


def load_font(size: int) -> ImageFont.ImageFont:
    for font in (Path("C:/Windows/Fonts/arial.ttf"), Path("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf")):
        if font.exists():
            return ImageFont.truetype(str(font), size)
    return ImageFont.load_default()


def composite(image: Image.Image, background: tuple[int, int, int], size: tuple[int, int]) -> Image.Image:
    base = Image.new("RGBA", size, (*background, 255))
    base.alpha_composite(resize_rgba(image, size))
    return base.convert("RGB")


def make_qa(atlas: Image.Image, portrait: Image.Image) -> Image.Image:
    sheet = Image.new("RGB", (1100, 1310), (28, 28, 32))
    draw = ImageDraw.Draw(sheet)
    font = load_font(19)
    title_font = load_font(25)
    for half, background in enumerate(((24, 25, 32), (236, 232, 223))):
        y = half * 650
        draw.text((20, y + 12), "Kael V2 / " + ("dark arena" if half == 0 else "light edge review"),
                  font=title_font, fill=(240, 234, 224))
        for index, name in enumerate(TILES):
            col, row = index % 4, index // 4
            x, tile_y = 20 + col * 270, y + 53 + row * 202
            tile = atlas.crop((col * 512, row * 512, (col + 1) * 512, (row + 1) * 512))
            sheet.paste(composite(tile, background, (250, 175)), (x, tile_y))
            draw.text((x, tile_y + 178), name, font=font, fill=(234, 224, 216))
        # Portrait at 2x its intended Canvas strip size: unwarped 2:1 art.
        sheet.paste(composite(portrait, background, (370, 185)), (20, y + 457))
        draw.text((414, y + 468), "Dedicated action cut-in, 370 x 185", font=font, fill=(234, 224, 216))
        draw.text((414, y + 496), "1024 x 512 source; straight alpha", font=font, fill=(234, 224, 216))
        draw.text((414, y + 524), "Hair and dark ink preserved", font=font, fill=(234, 224, 216))
        draw.text((414, y + 552), "Atlas: 4 x 2 cells; 12 px transparent gutters", font=font, fill=(234, 224, 216))
    return sheet


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    QA.mkdir(parents=True, exist_ok=True)
    vfx_path = SOURCE / "vfx-source.png"
    portrait_path = SOURCE / "action-portrait-source.png"
    vfx_source = Image.open(vfx_path)
    portrait_source = Image.open(portrait_path)
    atlas, tile_report = make_atlas(vfx_source)
    portrait, portrait_report = prepare_portrait(portrait_source)
    atlas_path = OUTPUT / "VfxAtlas.png"
    action_path = OUTPUT / "ActionPortrait.png"
    atlas.save(atlas_path, compress_level=9)
    portrait.save(action_path, compress_level=9)
    atlas_pixels = np.asarray(atlas)
    for index in range(8):
        col, row = index % 4, index // 4
        cell_alpha = atlas_pixels[row * 512:(row + 1) * 512, col * 512:(col + 1) * 512, 3]
        assert not cell_alpha[:12].any() and not cell_alpha[-12:].any()
        assert not cell_alpha[:, :12].any() and not cell_alpha[:, -12:].any()
        assert (cell_alpha > 8).any(), "Missing painted atlas region"
    assert atlas.mode == portrait.mode == "RGBA"
    assert atlas.size == (2048, 1024) and portrait.size == (1024, 512)
    make_qa(atlas, portrait).save(QA / "v2-effects-dark-light-qa.png", compress_level=9)
    report = {
        "source_files": {str(p.relative_to(PROJECT)): {"sha256": sha256(p), "size": list(Image.open(p).size)} for p in (vfx_path, portrait_path)},
        "outputs": {str(p.relative_to(PROJECT)): {"sha256": sha256(p), "size": list(Image.open(p).size), "mode": Image.open(p).mode} for p in (atlas_path, action_path)},
        "atlas_tiles": tile_report, "portrait": portrait_report,
        "runtime_uv_contract": "KaelCombatVfx.Region: index%4 columns, index//4 rows from top; half-texel UV inset",
        "source_images_unchanged": True,
    }
    (QA / "v2-effects-preparation.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report["outputs"], indent=2))


if __name__ == "__main__":
    main()
