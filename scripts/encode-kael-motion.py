"""Encode a labelled rig-only review from real Unity samples (not gameplay)."""
from pathlib import Path
import argparse, subprocess, csv, json
from PIL import Image, ImageDraw
parser=argparse.ArgumentParser(); parser.add_argument('--ffmpeg',required=True)
parser.add_argument('--output', help='Optional review output path; leaves earlier review videos intact.')
parser.add_argument('--source', help='Unity motion capture directory, including bounds.csv and capture-settings.json.')
args=parser.parse_args()
project=Path(__file__).resolve().parents[1]
source=Path(args.source).resolve() if args.source else project/'artifacts/kael/v2-motion-final'
settings=source/'capture-settings.json'
fps=json.loads(settings.read_text())['fps'] if settings.is_file() else 30
assert isinstance(fps,int) and 1 <= fps <= 120
frames=source/'labelled-review'; frames.mkdir(exist_ok=True)
order=[('idle',1),('run',3),('attack_cross',3),('attack_spin',3),('attack_jump',3),('skill',3),('hit',2),('death',1)]
index=0
with (source/'bounds.csv').open(newline='') as stream:
    samples=list(csv.DictReader(stream))
for clip,repeats in order:
    # The numerical capture journal is authoritative, never filename lexicography.
    clip_samples=sorted((r for r in samples if r['clip']==clip),key=lambda r:int(r['frame']))
    assert [int(r['frame']) for r in clip_samples]==list(range(len(clip_samples))),clip
    paths=[source/clip/f"frame-{int(r['frame']):04d}.png" for r in clip_samples]
    assert paths,clip
    assert all(p.is_file() for p in paths),clip
    for _ in range(repeats):
        for path in paths:
            image=Image.open(path).convert('RGB'); draw=ImageDraw.Draw(image)
            draw.text((18,18),'KAEL V2 / '+clip,fill='white')
            draw.text((18,40),'Unity rig review - no VFX - not gameplay capture',fill=(191,202,216))
            image.save(frames/f'frame-{index:05}.png'); index+=1
output=Path(args.output).resolve() if args.output else project/'artifacts/kael/Kael-V2-Bewegung-ohne-Effekte.mp4'
output.parent.mkdir(parents=True, exist_ok=True)
subprocess.run([args.ffmpeg,'-hide_banner','-loglevel','error','-y','-framerate',str(fps),'-i',str(frames/'frame-%05d.png'),
    '-frames:v',str(index),'-c:v','libx264','-threads','2','-crf','18','-preset','medium','-pix_fmt','yuv420p','-movflags','+faststart',str(output)],check=True)
print(f'{index} authored review frames at {fps} fps: {output}')
