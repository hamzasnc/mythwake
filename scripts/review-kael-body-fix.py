"""Compare actual Unity renders before/after anatomical registration repairs."""
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

project = Path(__file__).resolve().parents[1]
review = project / 'artifacts/kael/body-fix-review'

def actor(path, height):
    frame = Image.open(path).convert('RGB')
    pixels = np.asarray(frame).astype(int)
    changed = np.max(abs(pixels - pixels[0, 0]), axis=2) > 10
    ys, xs = np.nonzero(changed)
    bounds = (max(0, int(xs.min())-4), max(0, int(ys.min())-4),
              min(frame.width, int(xs.max())+5), min(frame.height, int(ys.max())+5))
    frame = frame.crop(bounds)
    return frame.resize((round(frame.width*height/frame.height), height), Image.Resampling.LANCZOS)

sheet = Image.new('RGB', (1100, 670), (33, 43, 59))
draw = ImageDraw.Draw(sheet)
font_path = Path('C:/Windows/Fonts/segoeui.ttf')
title_font = ImageFont.truetype(str(font_path), 21) if font_path.is_file() else ImageFont.load_default()
note_font = ImageFont.truetype(str(font_path), 16) if font_path.is_file() else ImageFont.load_default()
for column, (label, path) in enumerate([
    ('VORHER / fehlerhafte Montage', review/'before/00-idle.png'),
    ('KORRIGIERT / tatsaechlicher Unity-Rig', review/'current/01-idle-0.00.png')]):
    image = actor(path, 510)
    sheet.paste(image, (275+column*550-image.width//2, 100))
    draw.text((28+column*550, 30), label, fill='white', font=title_font)
draw.text((28, 636), 'Gleiche Figurhoehe zum Vergleichen. Beide Bilder stammen direkt aus Unity.', fill=(200, 209, 220), font=note_font)
sheet.save(review/'kael-body-before-after.png')
print(review/'kael-body-before-after.png')
