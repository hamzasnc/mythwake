"""Compose a review from actual Unity study frames; never invent motion frames."""
import argparse
import json
import subprocess
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("capture", type=Path)
    parser.add_argument("--ffmpeg", type=Path, required=True)
    args = parser.parse_args()
    capture = args.capture.resolve()
    manifest = json.loads((capture / "bake-manifest.json").read_text())
    if (capture / "unity-render-errors.txt").exists():
        raise SystemExit("Resolve the Unity rendering/import errors before composing a review.")
    if not manifest["comparison"]["passed"]:
        raise SystemExit("The stored clip must first pass source/bake correspondence.")
    frames = [Image.open(path).convert("RGB") for path in sorted((capture / "frames").glob("*.png"))]
    if len(frames) != manifest["renderedFrames"]:
        raise SystemExit("The capture and manifest do not match.")
    bg = frames[0].getpixel((0, 0))
    boxes = [ImageChops.difference(frame, Image.new("RGB", frame.size, bg)).getbbox() for frame in frames]
    box = (min(b[0] for b in boxes) - 16, min(b[1] for b in boxes) - 16,
           max(b[2] for b in boxes) + 16, max(b[3] for b in boxes) + 16)
    if box[0] < 0 or box[1] < 0 or box[2] > frames[0].width or box[3] > frames[0].height:
        raise SystemExit("The full motion needs a larger capture, not a cropped review.")
    font_path = "C:/Windows/Fonts/segoeui.ttf"
    fonts = {size: ImageFont.truetype(font_path, size) for size in [16, 18, 24, 28]}
    bounds_w, bounds_h = box[2] - box[0], box[3] - box[1]
    large_scale = min(830 / bounds_w, 550 / bounds_h)
    large_size = (round(bounds_w * large_scale), round(bounds_h * large_scale))
    # Same world-to-Canvas projection as KaelAnimationView / KaelRenderAtlas.
    # The 132 px parameter is not a claim of exactly 132 painted body pixels.
    character_world_height = 3.1
    pixels_per_world = manifest["resolution"] / (manifest["cameraOrthographicSize"] * 2)
    views = [(108, "Formation"), (118, "Home"), (132, "Kampf")]
    prepared = []
    for index, source in enumerate(frames):
        out = Image.new("RGB", (1280, 720), (25, 31, 43))
        draw = ImageDraw.Draw(out)
        draw.text((32, 15), "Kael · ein Schwerthieb", font=fonts[28], fill=(236, 237, 242))
        draw.text((32, 56), "Normalgeschwindigkeit · durchgehender Clip · ohne Effekte", font=fonts[18], fill=(176, 189, 208))
        draw.text((32, 99), "Vergrößert", font=fonts[18], fill=(221, 226, 236))
        draw.line((893, 103, 893, 676), fill=(62, 75, 91), width=1)
        draw.text((918, 100), "Canvas-Maßstab", font=fonts[18], fill=(221, 226, 236))
        image = source.crop(box)
        out.paste(image.resize(large_size, Image.Resampling.LANCZOS),
                  (32 + (830 - large_size[0]) // 2, 129 + 550 - large_size[1]))
        for row, (height, label) in enumerate(views):
            scale = height / character_world_height / pixels_per_world
            size = (round(bounds_w * scale), round(bounds_h * scale))
            top = 129 + row * 184
            draw.text((918, top), f"{height} px · {label}", font=fonts[16], fill=(192, 204, 222))
            out.paste(image.resize(size, Image.Resampling.BILINEAR),
                      (918 + (326 - size[0]) // 2, top + 22))
        time = min(index / manifest["renderRate"], manifest["clipDuration"])
        progress_x = 32 + int(800 * time / manifest["clipDuration"])
        draw.line((32, 698, 862, 698), fill=(69, 81, 101), width=2)
        draw.line((32, 698, progress_x, 698), fill=(205, 212, 229), width=3)
        impact_x = 32 + int(800 * manifest["contacts"][0]["time"] / manifest["clipDuration"])
        draw.line((impact_x, 692, impact_x, 704), fill=(185, 118, 118), width=2)
        prepared.append(out)
    # Five complete normal-speed repetitions. Only the neutral start pose is
    # held for 0.2s before each clip; every actual animation frame is preserved.
    sequence = ([0] * 12 + list(range(len(prepared)))) * 5
    output = capture / "Kael-Einzelhieb.mp4"
    command = [str(args.ffmpeg), "-hide_banner", "-loglevel", "error", "-y", "-f", "rawvideo",
               "-pixel_format", "rgb24", "-video_size", "1280x720", "-framerate", "60", "-i", "pipe:0",
               "-an", "-c:v", "libx264", "-threads", "2", "-preset", "medium", "-crf", "17",
               "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(output)]
    process = subprocess.Popen(command, stdin=subprocess.PIPE)
    try:
        for index in sequence:
            process.stdin.write(prepared[index].tobytes())
    finally:
        process.stdin.close()
    if process.wait() != 0:
        raise SystemExit("Video encoding failed.")
    prepared[0].save(capture / "video-layout.png")
    ids = [0, 6, 12, 18, 20, 22, 24, 25, 27, 30, 34, 36, 39, 43, 47, 51]
    width = 384
    tile_height = round(bounds_h * width / bounds_w)
    sheet = Image.new("RGB", (width * 4, (tile_height + 32) * 4), (25, 31, 43))
    draw = ImageDraw.Draw(sheet)
    for n, index in enumerate(ids):
        x, y = n % 4 * width, n // 4 * (tile_height + 32)
        sheet.paste(frames[index].crop(box).resize((width, tile_height), Image.Resampling.LANCZOS), (x, y + 32))
        draw.text((x + 8, y + 4), f"{index:02d} | {index/60:.3f} s", font=fonts[18], fill=(230, 230, 235))
    sheet.save(capture / "pose-sheet.jpg", quality=94)
    (capture / "video-manifest.json").write_text(json.dumps(dict(
        source="Actual Unity 60 Hz renders of reloaded baked-only prefab", sourceFrames=len(frames),
        outputFrames=len(sequence), outputFps=60, repeats=5, initialHoldFrames=12,
        frameMap=sequence, crop=box, characterWorldHeight=character_world_height,
        canvasHeightParameters=[height for height, _ in views],
        limit="Study projection, not an in-game recording. No temporal interpolation or retiming."
    ), indent=2) + "\n")
    print(output)


if __name__ == "__main__":
    main()
