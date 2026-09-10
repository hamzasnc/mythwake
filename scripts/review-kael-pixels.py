"""Unretouched Unity-frame contact sheets and native-pixel comparison crops.

This tool only crops, labels and arranges existing renders. It does not repair art.
"""
from pathlib import Path
import argparse
import csv
import json
from PIL import Image, ImageDraw, ImageFont

parser = argparse.ArgumentParser()
parser.add_argument('source', type=Path)
parser.add_argument('output', type=Path)
parser.add_argument('--motion', action='store_true')
parser.add_argument('--before', type=Path)
args = parser.parse_args()
args.output.mkdir(parents=True, exist_ok=True)
font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 16)
background = (33, 43, 59)
if args.motion:
    with (args.source/'bounds.csv').open(newline='') as stream:
        rows = list(csv.DictReader(stream))
    groups = {}
    for row in rows:
        groups.setdefault(row['clip'], []).append((
            args.source/row['clip']/f"frame-{int(row['frame']):04d}.png",
            f"{row['clip']} #{row['frame']} / {row['time']} s"))
else:
    groups = {'poses': [(p, p.stem) for p in sorted(args.source.glob('*.png'))]}

journal = []
for clip, entries in groups.items():
    # Every captured frame is included, in order. No hand-picked best poses.
    for offset in range(0, len(entries), 6):
        sheet = Image.new('RGB', (1536, 1136), background)
        draw = ImageDraw.Draw(sheet)
        for index, (path, label) in enumerate(entries[offset:offset+6]):
            original = Image.open(path).convert('RGB')
            assert original.size == (1024, 1024), path
            # Fixed framing for every frame, rather than a moving flattering crop.
            tile = original.resize((512, 512), Image.Resampling.LANCZOS)
            x, y = index % 3 * 512, index // 3 * 568
            draw.text((x+8, y+8), label, fill='white', font=font)
            sheet.paste(tile, (x, y+36))
        output = args.output/f'{clip}-{offset//6:02d}.png'
        sheet.save(output)
        journal.append({'sheet':output.name, 'frames':[str(p.resolve()) for p,_ in entries[offset:offset+6]],
                        'renderScale':0.5, 'retouched':False})

if args.before:
    for name in ['01-idle-0.00', '05-attack_cross-0.24', '07-attack_cross-0.52',
                 '21-neutral-front', '24-neutral-side', '27-neutral-rear']:
        sheet = Image.new('RGB', (1000, 650), background)
        draw = ImageDraw.Draw(sheet)
        for index, (folder, label) in enumerate([(args.before, 'VORHER'), (args.source, 'AKTUELL')]):
            path = folder/(name+'.png')
            image = Image.open(path).convert('RGB')
            crop = image.crop((300, 350, 800, 950))
            sheet.paste(crop, (index*500, 42))
            draw.text((index*500+14, 12), label+' / '+name, fill='white', font=font)
        sheet.save(args.output/(name+'-comparison.png'))

(args.output/'sheets.json').write_text(json.dumps(journal, indent=2)+'\n')
print(f'{sum(len(v) for v in groups.values())} unretouched frames in {len(journal)} sheets: {args.output}')
