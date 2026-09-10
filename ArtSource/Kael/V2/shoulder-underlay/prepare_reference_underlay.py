"""Technical registration preview only; built-in imagegen supplies all painted pixels.

Writes only this source folder. Does not alter game assets or the main generator.
Retains the existing visible arm artwork on top of the new cloth underdrawing.
"""
from pathlib import Path
import hashlib,json
import numpy as np
from PIL import Image,ImageDraw

HERE=Path(__file__).resolve().parent
V2=HERE.parent
SOURCE=HERE/'front-cloth-source.png'
SRC_ATTACH=np.array((760.,335.))
SRC_END=np.array((415.,980.))
TARGET_ATTACH=np.array((73.025,72.85474860335195))
TARGET_END=np.array((46.56666666666667,157.3240223463687))
SIZE=(127,189)

def run():
    original=Image.open(SOURCE).convert('RGBA')
    a=SRC_END-SRC_ATTACH;b=TARGET_END-TARGET_ATTACH
    c=float(np.dot(a,b)/np.dot(a,a));s=float((a[0]*b[1]-a[1]*b[0])/np.dot(a,a))
    forward=np.array(((c,-s),(s,c)))
    inverse=np.linalg.inv(forward)
    offset=SRC_ATTACH-inverse@TARGET_ATTACH
    coefficients=(inverse[0,0],inverse[0,1],offset[0],inverse[1,0],inverse[1,1],offset[1])
    underlay=original.transform(SIZE,Image.Transform.AFFINE,coefficients,Image.Resampling.BICUBIC)
    underlay.save(HERE/'front-cloth-registered-full.png')
    pixels=np.asarray(underlay).copy()
    y,x=np.indices((SIZE[1],SIZE[0]))
    longitudinal=((x-TARGET_ATTACH[0])*b[0]+(y-TARGET_ATTACH[1])*b[1])/np.dot(b,b)
    # Source preparation trim: hidden shoulder cloth blends out beneath the
    # original visible lower sleeve. No new colors or synthetic anatomy.
    keep=np.clip((.94-longitudinal)/.20,0,1)
    pixels[:,:,3]=np.rint(pixels[:,:,3]*keep).astype('uint8')
    pixels[pixels[:,:,3]==0,:3]=0
    trimmed=Image.fromarray(pixels)
    trimmed.save(HERE/'front-cloth-proximal-underlay.png')
    old=Image.open(V2/'parts'/'UpperArmNear.png').convert('RGBA')
    if old.size!=SIZE:raise ValueError('Existing arm dimensions changed; review target registration before rerun.')
    old.save(HERE/'existing-front-arm-reference.png')
    composed=Image.alpha_composite(trimmed,old)
    composed.save(HERE/'front-arm-layered-preview.png')
    opacity=np.asarray(trimmed)[:,:,3]
    yy,xx=np.nonzero(opacity<240)
    radius=float(np.sqrt((xx-TARGET_ATTACH[0])**2+(yy-TARGET_ATTACH[1])**2).min())
    report=dict(kind='Technical registration candidate, requires visual Unity review',
        generator='built-in imagegen',source='front-cloth-source.png',
        source_size=list(original.size),source_sha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
        alpha='Generated RGBA retained. No matte extraction or new painted colors.',
        source_landmarks_top_left=dict(attach=SRC_ATTACH.tolist(),end=SRC_END.tolist()),
        target_size=list(SIZE),target_landmarks_top_left=dict(attach=TARGET_ATTACH.tolist(),end=TARGET_END.tolist()),
        transform='Uniform similarity transform only, followed by proximal overlap alpha trim.',
        source_to_target_matrix=forward.tolist(),uniform_scale=float(np.linalg.norm(forward[:,0])),
        opaque_shoulder_radius_texels=radius,
        notes=['The end source landmark locates the internal cloth elbow underlap; source cuff beyond it is excluded from proximal underlay.',
               'Use below existing visible original lower sleeve and below original unchanged pauldron.',
               'Side/rear were not regenerated: they already contain a rounded shoulder. Their smaller seams need separate pixel cleanup.',
               'The layered-preview includes an unchanged snapshot of the current original arm; main prepare_assets.py was not edited.'])
    (HERE/'front-underlay-registration.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf8')
    review=Image.new('RGB',(1530,810),(36,44,57));draw=ImageDraw.Draw(review)
    for i,(name,im) in enumerate([('Existing missing shoulder',old),('New painted underlayer',trimmed),('Layered candidate',composed)]):
        im=im.resize((SIZE[0]*4,SIZE[1]*4),Image.Resampling.NEAREST)
        review.paste(im,(i*510,35),im);draw.text((i*510+10,10),name,fill='white')
        cx=i*510+TARGET_ATTACH[0]*4;cy=35+TARGET_ATTACH[1]*4
        draw.ellipse((cx-5,cy-5,cx+5,cy+5),outline='magenta',width=2)
    review.save(HERE/'front-underlay-review.png')
    print(json.dumps(dict(opaque_shoulder_radius_texels=radius,output=str(HERE/'front-arm-layered-preview.png'))))

if __name__=='__main__':run()
