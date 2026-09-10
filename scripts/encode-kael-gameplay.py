"""Encode actual Game View frames using the recorded wall-clock intervals."""
import argparse
import csv
import pathlib
import statistics
import subprocess
from PIL import Image


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("capture", type=pathlib.Path)
    parser.add_argument("output", type=pathlib.Path)
    parser.add_argument("--ffmpeg", required=True)
    args = parser.parse_args()
    capture = args.capture.resolve()
    with (capture / "frames.csv").open(encoding="utf-8-sig", newline="") as stream:
        rows = list(csv.DictReader(stream))
    if len(rows) < 2:
        raise ValueError("At least two captured frames are required")
    sizes = set()
    paths = []
    for row in rows:
        path = capture / f"frame-{int(row['index']):05}.png"
        with Image.open(path) as frame:
            sizes.add(frame.size)
        paths.append(path)
    if len(sizes) != 1 or next(iter(sizes)) != (540, 960):
        raise ValueError(f"Unexpected/mixed live capture dimensions: {sizes}")
    times = [float(row["realSeconds"]) for row in rows]
    intervals = [b-a for a, b in zip(times, times[1:])]
    if min(intervals) <= 0:
        raise ValueError("Recorded frame clock must advance")
    intervals.append(statistics.median(intervals))
    manifest = capture / "live-frames.ffconcat"
    lines = ["ffconcat version 1.0"]
    for path, duration in zip(paths, intervals):
        safe = path.as_posix().replace("'", "'\\''")
        lines.extend([f"file '{safe}'", f"duration {duration:.6f}"])
    lines.append(lines[-2])
    manifest.write_text("\n".join(lines) + "\n", encoding="utf-8")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    subprocess.run([args.ffmpeg, "-hide_banner", "-y", "-f", "concat", "-safe", "0",
                    "-i", str(manifest), "-c:v", "libx264", "-threads", "2", "-crf", "20", "-preset", "medium",
                    "-pix_fmt", "yuv420p", "-vf", "fps=30", "-movflags", "+faststart",
                    str(args.output.resolve())], check=True)
    print(f"Encoded {len(rows)} actual frames, {sum(intervals):.3f}s, {next(iter(sizes))}")


if __name__ == "__main__":
    main()
