"""Diagnostic plates from actual Unity renders, not substitute character art."""
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageOps

project=Path(__file__).resolve().parents[1]
out=project/'artifacts/kael'
actor=Image.open(project/'Assets/_Mythwake/Resources/Mythwake/Art/Runtime/hero_kael.png').convert('RGBA')
actor=actor.crop(actor.getchannel('A').getbbox())
sheet=Image.new('RGB',(1040,650),(34,42,54)); draw=ImageDraw.Draw(sheet)
draw.text((24,18),'Kael V2 / actual Unity idle render / design inspection',fill='white')
for column,height in enumerate((108,118,132,264)):
    tile=actor.resize((round(actor.width*height/actor.height),height),Image.Resampling.LANCZOS)
    x=30+column*255
    draw.text((x,54),str(height)+' pixel character height',fill='white')
    sheet.paste(tile,(x,90+264-height),tile)
    gray=ImageOps.grayscale(tile).convert('RGBA'); gray.putalpha(tile.getchannel('A'))
    gray.thumbnail((150,132),Image.Resampling.LANCZOS)
    sheet.paste(gray,(x,385),gray)
    silhouette=Image.new('RGBA',tile.size,(0,0,0,255)); silhouette.putalpha(tile.getchannel('A'))
    silhouette.thumbnail((150,100),Image.Resampling.LANCZOS)
    draw.rectangle((x,535,x+210,635),fill=(230,230,232))
    sheet.paste(silhouette,(x+20,535),silhouette)
sheet.save(out/'v2-design-size-review.png')

def crop_render(path):
    image=Image.open(path).convert('RGB'); pixels=np.array(image).astype(int)
    changed=np.max(abs(pixels-pixels[0,0]),axis=2)>5
    ys,xs=np.nonzero(changed)
    return image.crop((int(xs.min())-5,int(ys.min())-5,int(xs.max())+6,int(ys.max())+6))

comparison=Image.new('RGB',(1000,610),(32,42,58)); draw=ImageDraw.Draw(comparison)
for index,(label,path) in enumerate([
    ('Kael V1 / earlier actual rig',out/'rig-review/00-idle.png'),
    ('Kael V2 / redesigned actual rig',out/'v2-rig-review/00-idle.png')]):
    image=crop_render(path); image=image.resize((round(image.width*460/image.height),460),Image.Resampling.LANCZOS)
    x=250+500*index-image.width//2
    comparison.paste(image,(x,85)); draw.text((30+500*index,30),label,fill='white')
draw.text((30,575),'Equal character height. Rig stills compare design; movement is reviewed separately in real gameplay.',fill='white')
comparison.save(out/'v2-before-after-design.png')
print('Design inspection plates written from real rig renders.')
