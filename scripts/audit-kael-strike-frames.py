"""Inspect every captured study image and prepare native-pixel joint review pages."""
import argparse
import csv
import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("capture", type=Path)
    args = parser.parse_args()
    capture = args.capture.resolve()
    manifest = json.loads((capture / "bake-manifest.json").read_text())
    if not manifest["comparison"]["passed"] or (capture / "unity-render-errors.txt").exists():
        raise SystemExit("Resolve export errors before reviewing the capture.")
    paths = sorted((capture / "frames").glob("*.png"))
    rows = list(csv.DictReader((capture / "frames.csv").open()))
    if len(paths) != len(rows) or len(rows) != manifest["renderedFrames"]:
        raise SystemExit("Frame files, coordinates and manifest do not agree.")
    output = capture / "pixel-review"
    output.mkdir(exist_ok=True)
    frames = [Image.open(path).convert("RGB") for path in paths]
    background = Image.new("RGB", frames[0].size, frames[0].getpixel((0, 0)))
    checks = []
    for index, frame in enumerate(frames):
        delta = ImageChops.difference(frame, background)
        box = delta.getbbox()
        checks.append(dict(frame=index, time=float(rows[index]["time"]), bounds=box,
                           cropped=box is None or box[0] == 0 or box[1] == 0
                           or box[2] == frame.width or box[3] == frame.height,
                           imageSha256=hashlib.sha256(frame.tobytes()).hexdigest()))
    # Crops contain original renderer pixels, with no scaling, smoothing or redraw.
    ppw = manifest["resolution"] / (manifest["cameraOrthographicSize"] * 2)
    def screen(x, y):
        return (frames[0].width / 2 + (x - manifest["cameraPosition"]["x"]) * ppw,
                frames[0].height / 2 - (y - manifest["cameraPosition"]["y"]) * ppw)
    def bounds(names, padding):
        points = [screen(float(row[name + "X"]), float(row[name + "Y"]))
                  for row in rows for name in names]
        left, top, right, bottom = padding
        return (max(0, math.floor(min(x for x, y in points) - left * ppw)),
                max(0, math.floor(min(y for x, y in points) - top * ppw)),
                min(frames[0].width, math.ceil(max(x for x, y in points) + right * ppw)),
                min(frames[0].height, math.ceil(max(y for x, y in points) + bottom * ppw)))
    regions = {
        "upper": bounds(["Head", "UpperArmNear", "ForearmNear", "HandNear",
                         "UpperArmFar", "ForearmFar", "HandFar"], (.4, .9, .7, .35)),
        "lower": bounds(["Hip", "ThighNear", "ShinNear", "FootNear",
                         "ThighFar", "ShinFar", "FootFar"], (.8, .1, .4, .3))}
    font = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 18)
    # Include every actual motion frame and the first held endpoint. The complete
    # 67-frame pixel audit still checks all remaining duplicate hold frames.
    active = list(range(min(len(rows), math.ceil(manifest["clipDuration"] * manifest["renderRate"]) + 1)))
    for name, box in regions.items():
        width, height = box[2] - box[0], box[3] - box[1]
        for start in range(0, len(active), 6):
            ids = active[start:start + 6]
            sheet = Image.new("RGB", (width * 2, (height + 28) * 3), (20, 26, 36))
            draw = ImageDraw.Draw(sheet)
            for slot, index in enumerate(ids):
                x, y = slot % 2 * width, slot // 2 * (height + 28)
                sheet.paste(frames[index].crop(box), (x, y + 28))
                draw.text((x + 8, y + 2), f"{index:02d} | {float(rows[index]['time']):.3f} s | 1:1",
                          fill=(235, 236, 239), font=font)
            sheet.save(output / f"{name}-{start:02d}-{ids[-1]:02d}.png")
    foot_keys = [bone + axis for bone in ("FootNear", "FootFar") for axis in "XY"]
    summary = dict(capture=str(capture), totalFrames=len(frames), motionFramesOnPages=len(active),
                   allFramesFit=not any(item["cropped"] for item in checks),
                   startEndPixelIdentical=ImageChops.difference(frames[0], frames[-1]).getbbox() is None,
                   footCoordinateRanges={key: max(float(r[key]) for r in rows) - min(float(r[key]) for r in rows)
                                         for key in foot_keys},
                   cropRegions=regions, frames=checks,
                   scope="Pixel measurements and native-resolution review pages; aesthetic assessment is separate.")
    summary["handPixelContribution"] = {}
    for name, filename in [("weaponHand", "pixel-audit.csv"), ("freeHand", "pixel-audit-free-hand.csv")]:
        path = capture / filename
        if not path.exists():
            continue
        samples = list(csv.DictReader(path.open()))
        if len(samples) != len(frames):
            raise SystemExit(f"Incomplete hand pixel audit: {filename}")
        ratios = [float(row["handVisibilityRatio"]) for row in samples if row["handVisibilityRatio"]]
        summary["handPixelContribution"][name] = dict(
            minimumRatio=min(ratios) if ratios else None,
            zeroContributionFrames=[int(row["frame"]) for row in samples if int(row["handContributingPixels"]) == 0],
            borderContactFrames=[int(row["frame"]) for row in samples if int(row["touchesFrameBorder"]) != 0],
            method="RGB contribution above 3/255 versus isolated hand footprint; similar underlying colors also reduce the ratio.")
    (output / "pixel-audit.json").write_text(json.dumps(summary, indent=2) + "\n")
    print(json.dumps({key: value for key, value in summary.items() if key not in ("frames", "cropRegions")}, indent=2))


if __name__ == "__main__":
    main()
