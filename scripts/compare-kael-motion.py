"""Compare unretouched Unity captures on the same timeline; this is not gameplay."""
import argparse
import csv
import json
from pathlib import Path
import subprocess
from PIL import Image, ImageDraw, ImageFont

p = argparse.ArgumentParser()
p.add_argument('--before', required=True, type=Path)
p.add_argument('--after', required=True, type=Path)
p.add_argument('--output', required=True, type=Path)
p.add_argument('--ffmpeg', required=True)
a = p.parse_args()
font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 17)

def read_capture(folder):
    settings = json.loads((folder/'capture-settings.json').read_text())
    if settings['fps'] != 60:
        raise ValueError('Comparison requires two genuine 60 fps Unity captures')
    with (folder/'bounds.csv').open(newline='') as f:
        rows = list(csv.DictReader(f))
    return {clip: [r for r in rows if r['clip'] == clip] for clip in {r['clip'] for r in rows}}

before, after = read_capture(a.before), read_capture(a.after)
order = [('idle', 1, 1), ('run', 2, 1), ('attack_cross', 2, 1),
         ('attack_spin', 2, 1), ('attack_jump', 2, 1), ('skill', 2, 1),
         ('hit', 2, 1), ('death', 1, 1),
         ('attack_spin', 1, 4), ('death', 1, 4)]
a.output.parent.mkdir(parents=True, exist_ok=True)
process = subprocess.Popen([a.ffmpeg, '-hide_banner', '-loglevel', 'error', '-y',
    '-f', 'rawvideo', '-pixel_format', 'rgb24', '-video_size', '1024x560', '-framerate', '60',
    '-i', '-', '-an', '-c:v', 'libx264', '-threads', '2', '-preset', 'medium', '-crf', '18',
    '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(a.output)], stdin=subprocess.PIPE)
timeline, count = [], 0
try:
    for clip, repeats, slow in order:
        old, new = before[clip], after[clip]
        if len(old) != len(new) or any(abs(float(x['time'])-float(y['time'])) > .0002 for x,y in zip(old,new)):
            raise ValueError(f'{clip}: capture timelines differ; do not silently retime either actor')
        timeline.append({'clip': clip, 'startSeconds': count/60, 'repeats': repeats,
                         'playbackSpeed': 1/slow, 'sourceFramesPerRepeat': len(old)})
        frames = []
        for x,y in zip(old,new):
            frame = Image.new('RGB', (1024,560), (33,43,59))
            draw = ImageDraw.Draw(frame)
            speed = '1x' if slow == 1 else '0.25x / ZEITLUPE'
            draw.text((14,7), f'VORHER / {clip} / {speed}', font=font, fill='white')
            draw.text((526,7), f'NACHHER / {clip} / {speed}', font=font, fill='white')
            for index,(folder,row) in enumerate(((a.before,x),(a.after,y))):
                path = folder/clip/f"frame-{int(row['frame']):04d}.png"
                with Image.open(path) as source:
                    frame.paste(source.convert('RGB').resize((512,512),Image.Resampling.LANCZOS),(index*512,40))
            frames.append(frame.tobytes())
        for _ in range(repeats):
            for frame in frames:
                for _ in range(slow):
                    process.stdin.write(frame)
                    count += 1
    process.stdin.close()
    if process.wait() != 0:
        raise RuntimeError('ffmpeg failed')
except BaseException:
    process.kill()
    process.wait()
    raise
a.output.with_suffix('.json').write_text(json.dumps({
    'before': str(a.before.resolve()), 'after': str(a.after.resolve()),
    'fps': 60, 'frames': count, 'durationSeconds': count/60, 'timeline': timeline,
    'method': 'Real Unity frames, equal framing and clip times, downsampled equally; no retouching or motion interpolation. '
              'Slow motion repeats captured frames. This neutral rig comparison is separate from gameplay.'
}, indent=2)+'\n')
print(f'{count} frames / {count/60:.3f}s: {a.output}')
