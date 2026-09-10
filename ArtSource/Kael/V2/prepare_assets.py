"""Reproducible technical preparation of Kael V2's original painted sources.

Python matte extraction, crops, resizing and packing explicitly authorized by the
user on 2026-09-10. No anatomical parts are painted/synthesized by this script.
Requirements: Pillow, numpy. Coordinates in source boxes are top-left; Unity
atlas rectangles and attachment pivots are bottom-left.
"""
from pathlib import Path
from collections import deque
import json, hashlib
import numpy as np
from PIL import Image, ImageDraw, ImageOps

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[2]
OUT = PROJECT / 'Assets/_Mythwake/Resources/Characters/Kael'
PART_DIR = HERE / 'parts'
PPU = 256

# name, source rectangle, world height, normalized attachment pivot, flip x
PARTS = [
 ('Head',(0,0,346,293),.90,.48,.10,False),
 ('Torso',(345,19,594,293),.82,.50,.12,False),
 ('Pelvis',(588,65,852,292),.54,.50,.90,False),
 ('Collar',(885,85,1115,217),.30,.45,.57,False),
 ('UpperArmNear',(64,293,275,566),.74,.58,.62,False),
 ('ForearmNear',(376,301,520,571),.59,.45,.78,False),
 ('HandNear',(625,342,794,552),.30,.54,.72,False),
 ('Sword',(929,209,1067,650),2.05,.49,.145,False),
 ('UpperArmFar',(74,584,226,825),.61,.42,.90,True),
 ('ForearmFar',(378,584,523,825),.57,.34,.78,False),
 ('HandFar',(625,585,797,829),.29,.65,.72,False),
 ('Hair',(940,661,1069,852),.22,.30,.96,False),
 ('ThighNear',(91,829,250,1097),.69,.55,.94,True),
 ('ShinNear',(385,829,506,1097),.63,.525,.82,False),
 ('FootNear',(602,915,821,1092),.29,.32,.83,False),
 ('ScarfLong',(885,827,1118,1112),1.15,.90,.97,True),
 ('ThighFar',(90,1097,245,1374),.69,.47,.94,True),
 ('ShinFar',(385,1098,506,1375),.63,.525,.82,False),
 ('FootFar',(573,1172,805,1358),.29,.31,.83,True),
 ('ScarfShort',(928,1107,1108,1401),.92,.38,.97,False),
]
HIP = (0.0, 1.40)
SWAP_BOXES = [
 [(20,0,405,349),(452,0,775,348),(785,32,1200,362),(1220,90,1540,304),(1570,70,1810,315),(1870,0,2110,360)],
 [(20,352,405,724),(450,350,775,710),(795,385,1200,715),(1235,447,1530,650),(1570,428,1810,672),(1870,352,2110,724)],
]

# Measured on the previous transparent review parts, in TOP-LEFT pixel space.
# These coordinates are normalized against their stated reference size; the
# source is then scaled UNIFORMLY. No variant inherits another view's rectangle
# or pivot, and no cross-section-only width stretching is performed.
# attach = shared anatomical joint. end = the SAME joint used by its child.
# Both lie in retained painted overlap, never at a hollow construction endcap.
MEASURE = {
 'Head':((271,256),dict(attach=(110,236),hair=(45,162))),
 'Torso':((190,230),dict(attach=(100,211),neck=(84,50),shoulderNear=(21,86),shoulderFar=(180,86))),
 'Pelvis':((158,138),dict(attach=(79,18),hipNear=(42,50),hipFar=(109,50))),
 'UpperArmNear':((120,179),dict(attach=(69,69),end=(44,149))),
 'ForearmNear':((82,123),dict(attach=(36,33),end=(58,100))),
 'HandNear':((78,77),dict(attach=(44,25),grip=(42,53))),
 'UpperArmFar':((67,133),dict(attach=(32,30),end=(39,109))),
 'ForearmFar':((64,118),dict(attach=(25,36),end=(48,97))),
 'HandFar':((69,74),dict(attach=(43,24))),
 'ThighNear':((85,159),dict(attach=(45,11),end=(27,136))),
 'ShinNear':((58,148),dict(attach=(30,18),end=(30,132))),
 'FootNear':((95,74),dict(attach=(31,19))),
 'ThighFar':((77,159),dict(attach=(35,11),end=(49,136))),
 'ShinFar':((58,148),dict(attach=(30,18),end=(30,132))),
 'FootFar':((97,74),dict(attach=(31,19))),
 'Head_Rear':((271,256),dict(attach=(146,240),hair=(86,177))),
 'Head_Side':((271,256),dict(attach=(126,236),hair=(55,172))),
 'Head_Action':((271,256),dict(attach=(111,240),hair=(44,163))),
 'Head_Blink':((271,256),dict(attach=(106,239),hair=(43,164))),
 'Torso_Rear':((190,230),dict(attach=(96,211),neck=(95,51),shoulderNear=(168,95),shoulderFar=(13,95))),
 'Torso_Side':((190,230),dict(attach=(107,211),neck=(77,54),shoulderNear=(55,95),shoulderFar=(140,90))),
 'Pelvis_Rear':((158,138),dict(attach=(81,17),hipNear=(112,50),hipFar=(43,50))),
 'Pelvis_Side':((158,138),dict(attach=(80,18),hipNear=(60,50),hipFar=(94,50))),
 'UpperArmNear_Rear':((120,179),dict(attach=(36,92),end=(61,166))),
 'UpperArmNear_Side':((120,179),dict(attach=(36,92),end=(61,166))),
}
# Only disconnected technical cap pixels are removed. No anatomy is painted.
# Endpoints precede the cut by several pixels, so the next part covers the seam.
CAP_CUTS = {
 'UpperArmNear':(0,.87),'ForearmNear':(.11,.865),'HandNear':(.18,1),
 'UpperArmFar':(0,.865),'ForearmFar':(.15,.865),'HandFar':(.16,1),
 'ThighNear':(0,.90),'ThighFar':(0,.90),
 'ShinNear':(.105,.925),'ShinFar':(.105,.925),
 'FootNear':(.14,1),'FootFar':(.14,1),
}

# The original drawing combines a rigid pauldron and the moving upper arm.
# Separate their existing pixels, retaining a dark painted seam underlap.
# Coordinates are normalized to the unchanged canvas, not a new crop/pivot.
PAULDRON_OUTLINES = {
 'front':[(0,0),(1,0),(1,.52),(.86,.57),(.66,.59),(.46,.63),(.25,.71),(0,.77)],
 'rear':[(0,0),(1,0),(1,1),(.62,.79),(.46,.71),(.34,.62),(.31,.53),(.22,.44),(.11,.43),(0,.58)],
 'side':[(0,0),(1,0),(1,1),(.62,.79),(.46,.71),(.34,.62),(.31,.53),(.22,.44),(.11,.43),(0,.58)],
}

# These are masks of EXISTING collar pixels in each measured torso canvas.
# The forward collar remains in front of the neck. The rear rim is a separate
# attachment on the very same torso transform, drawn behind the head. The tan
# closed construction disk in the front source is removed, not repainted skin.
COLLAR_MASKS = {
 'front': dict(reference=(174,210),
     back=[(45,0),(174,0),(174,48),(103,47),(89,35),(100,19),(79,8),(48,5)],
     remove=[(49,5),(57,2),(80,3),(98,7),(109,12),(106,21),(102,36),(93,37),(82,23),(65,13)]),
 'rear': dict(reference=(182,210),
     back=[(61,0),(123,0),(123,10),(111,7),(86,6),(62,9)], remove=[]),
 'side': dict(reference=(151,210),
     back=[(34,0),(151,0),(151,68),(111,59),(102,53),(99,46),(104,22)],
     remove=[]),
}

# New genuine rear drawings, authored with the built-in image tool. Measurements
# refer to the tight border-matted original PNG, before uniform scaling. The
# front source, pivots and segment lengths are intentionally unaffected.
REAR_LOWER = {
 'Pelvis_Rear':(.54,(1165,947),dict(attach=(582,123),hipNear=(850,350),hipFar=(370,350))),
 'ThighNear_Rear':(.69,(659,1371),dict(attach=(301,115),end=(370,1190))),
 'ThighFar_Rear':(.69,(530,1273),dict(attach=(305,115),end=(325,1143))),
 'ShinNear_Rear':(.63,(402,1108),dict(attach=(182,108),end=(205,1020))),
 'ShinFar_Rear':(.63,(422,1220),dict(attach=(205,119),end=(200,1110))),
 'FootNear_Rear':(.29,(708,822),dict(attach=(262,152))),
 'FootFar_Rear':(.29,(677,734),dict(attach=(235,139))),
}

def matte(image, kind='white', largest=True):
    rgb = np.asarray(image.convert('RGB')).copy()
    lo, hi = rgb.min(axis=2), rgb.max(axis=2)
    candidate = ((lo > 108) & ((hi.astype(int)-lo)<20)) if kind=='checker' else ((lo>222)&((hi.astype(int)-lo)<28))
    # Only remove light neutral pixels CONNECTED TO THE OUTSIDE. Enclosed ivory
    # hair, armor reflections and eye highlights retain full opacity.
    mask = Image.fromarray(np.where(candidate,0,255).astype('uint8'))
    pad = ImageOps.expand(mask,1,0)
    ImageDraw.floodfill(pad,(0,0),128)
    keep = np.asarray(pad)[1:-1,1:-1] != 128
    if largest:
        # Tight source boxes may contain the edge of the neighbouring drawing.
        visited = keep.copy(); best=[]; h,w=keep.shape
        for sy,sx in zip(*np.nonzero(keep)):
            if not visited[sy,sx]: continue
            queue=deque([(int(sy),int(sx))]); visited[sy,sx]=False; component=[]
            while queue:
                y,x=queue.popleft(); component.append((y,x))
                for ny,nx in ((y-1,x),(y+1,x),(y,x-1),(y,x+1)):
                    if 0<=ny<h and 0<=nx<w and visited[ny,nx]:
                        visited[ny,nx]=False; queue.append((ny,nx))
            if len(component)>len(best): best=component
        keep[:]=False
        for y,x in best: keep[y,x]=True
    rgba=np.dstack([rgb,keep.astype('uint8')*255]); rgba[~keep,:3]=0
    result=Image.fromarray(rgba)
    bounds=result.getchannel('A').getbbox()
    if not bounds: raise ValueError('Empty painted part')
    return result.crop(bounds)

def fit_height(part,height):
    h=round(height*PPU)
    return part.resize((round(part.width*h/part.height),h),Image.Resampling.LANCZOS)

def register(name,bone,variant,part,px,py):
    if name in MEASURE:
        (rw,rh), points=MEASURE[name]
        ax,ay=points['attach']; px,py=ax/rw,1-ay/rh
        landmarks=[dict(name=key,x=(x/rw-px)*part.width/PPU,
                        y=(1-y/rh-py)*part.height/PPU) for key,(x,y) in points.items()]
    else:
        landmarks=[dict(name='attach',x=0.,y=0.)]
    return dict(name=name,bone=bone,variant=variant,image=part,pivotX=px,pivotY=py,landmarks=landmarks)

def point(record,name):
    p=next(p for p in record['landmarks'] if p['name']==name)
    return np.array((p['x'],p['y']))

def clear_caps(name,part):
    if name in CAP_CUTS:
        top,bottom=CAP_CUTS[name]; draw=ImageDraw.Draw(part)
        if top: draw.rectangle((0,0,part.width,round(part.height*top)),fill=(0,0,0,0))
        if bottom<1: draw.rectangle((0,round(part.height*bottom),part.width,part.height),fill=(0,0,0,0))
    if name=='UpperArmFar':
        # Retain the rounded outer cloth shoulder. The former top=.14 cut made
        # its silhouette a straight horizontal shelf. Remove only the slanted
        # hollow construction opening inside the torso overlap.
        contour=[(8,21),(15,11),(31,1),(42,0),(49,4),(49,12),(44,21),(29,29),(17,31),(9,27)]
        mask=Image.new('L',part.size)
        ImageDraw.Draw(mask).polygon([(round(x*part.width/78),round(y*part.height/156)) for x,y in contour],fill=255)
        # The source is an open tube, so erasing its disk alone leaves a hole.
        # Reuse the already-generated closed black-cloth shoulder painting as
        # an original-art underdrawing inside the original outer silhouette.
        # No procedural cloth/skin or synthetic fill color is introduced.
        reference=Image.open(HERE/'shoulder-underlay/front-cloth-source.png').convert('RGBA')
        source_anchor=np.array((760.,335.)); target_anchor=np.array((37.,22.))
        source_axis=np.array((-345.,645.)); source_axis/=np.linalg.norm(source_axis)
        target_axis=np.array((8.,93.)); target_axis/=np.linalg.norm(target_axis)
        cosine=float(source_axis@target_axis)
        sine=float(source_axis[0]*target_axis[1]-source_axis[1]*target_axis[0])
        matrix=.20*np.array(((cosine,-sine),(sine,cosine)))
        inverse=np.linalg.inv(matrix); offset=source_anchor-inverse@target_anchor
        patch=reference.transform(part.size,Image.Transform.AFFINE,
            (float(inverse[0,0]),float(inverse[0,1]),float(offset[0]),
             float(inverse[1,0]),float(inverse[1,1]),float(offset[1])),resample=Image.Resampling.BICUBIC)
        pixels=np.asarray(part).copy(); cloth=np.asarray(patch)
        selected=(np.asarray(mask)>0)&(pixels[:,:,3]>0)
        if np.any(cloth[:,:,3][selected]<245): raise ValueError('Far shoulder underdrawing does not cover original cap')
        pixels[selected,:3]=cloth[selected,:3]
        part=Image.fromarray(pixels)
    return part

def trim_side_neck_underlap(part):
    # Original side-head art has an unfinished broad neck cut behind the nape.
    # Only that excess underdrawing is trimmed back inside the standing collar;
    # face, hair silhouette, attachment and head rotation remain unchanged.
    contour=[(95,175),(98,186),(108,211),(112,230),(55,230),(54,201),
             (61,190),(71,185),(76,181),(80,177),(84,175),(89,171),(92,169)]
    ImageDraw.Draw(part).polygon([(round(x*part.width/240),round(y*part.height/230)) for x,y in contour],fill=(0,0,0,0))
    return part

def full_side_pelvis(part,px,py,height):
    # The old 70% rectangular crop cut straight across the painted red sash
    # and ivory edging. Retain the complete original contour at exactly the
    # scale of that earlier upper canvas; neither belt nor hip anchors shrink.
    old_h=round(part.height*.70)
    old_size=(round(part.width*round(height*PPU)/old_h),round(height*PPU))
    full_size=(old_size[0],round(part.height*old_size[1]/old_h))
    full=part.resize(full_size,Image.Resampling.LANCZOS)
    name='Pelvis_Side'
    (rw,rh),points=MEASURE[name]
    ax,ay=points['attach']
    pivot_x=ax/rw; pivot_y=1-(ay/rh*old_size[1])/full.height
    landmarks=[dict(name=key,x=(x-ax)/rw*old_size[0]/PPU,
                    y=(ay-y)/rh*old_size[1]/PPU) for key,(x,y) in points.items()]
    return dict(name=name,bone='Pelvis',variant='side',image=full,
                pivotX=pivot_x,pivotY=pivot_y,landmarks=landmarks)

def split_torso_collars(records):
    additions=[]
    for record in records:
        if record['bone']!='Torso': continue
        original=record['image']; w,h=original.size
        setting=COLLAR_MASKS[record['variant']]; rw,rh=setting['reference']
        def selection(points):
            mask=Image.new('L',(w,h))
            if points: ImageDraw.Draw(mask).polygon([(round(x*w/rw),round(y*h/rh)) for x,y in points],fill=255)
            return np.asarray(mask)>0
        rear=selection(setting['back']); removed=selection(setting['remove'])
        pixels=np.asarray(original).copy()
        foreground=pixels.copy(); foreground[rear|removed]=0
        background=pixels.copy(); background[~rear|removed]=0
        record['image']=Image.fromarray(foreground)
        suffix='' if record['variant']=='front' else '_'+record['variant'].title()
        additions.append(dict(record,name='TorsoBack'+suffix,bone='TorsoBack',
            image=Image.fromarray(background),landmarks=[dict(p) for p in record['landmarks']]))
        # All surviving pixels are exact original pixels. Only the documented
        # construction-cap mask is subtracted from the original source union.
        expected=pixels[:,:,3].copy(); expected[removed]=0
        if not np.array_equal(np.maximum(foreground[:,:,3],background[:,:,3]),expected):
            raise ValueError('Collar split changed pixels outside the construction cap')
    records.extend(additions)

def calibrate_shins(records):
    # Solve segment length from the actual painted ankle and floor anchors.
    # The whole shin is scaled uniformly, rather than translating the character
    # or compensating a broken foot socket with a root/render offset.
    by_name={p['name']:p for p in records}
    for side in ('Near','Far'):
        pelvis=by_name['Pelvis']; thigh=by_name['Thigh'+side]
        shin=by_name['Shin'+side]; foot=by_name['Foot'+side]
        knee_y=HIP[1]+point(pelvis,'hip'+side)[1]+point(thigh,'end')[1]
        ankle_y=foot['image'].height/PPU*foot['pivotY']
        desired=ankle_y-knee_y
        factor=desired/point(shin,'end')[1]
        if not .80<factor<1.20: raise ValueError('Measured shin length exceeds proportion tolerance: '+side)
        old=shin['image']; scaled=fit_height(old,old.height/PPU*factor)
        sx,sy=scaled.width/old.width,scaled.height/old.height
        for landmark in shin['landmarks']: landmark['x']*=sx; landmark['y']*=sy
        shin['image']=scaled

def add_rear_lower(records):
    for name,(height,reference,landmarks) in REAR_LOWER.items():
        source=Image.open(HERE/'rear-lower'/(name+'-source.png'))
        # Preserve real generated transparency if present. Current source exports
        # contain a neutral checkerboard, so only its outside-connected matte is
        # removed; painted ivory edges enclosed by the silhouette stay intact.
        if source.mode=='RGBA' and source.getchannel('A').getextrema()[0]<255:
            bounds=source.getchannel('A').getbbox(); source=source.crop(bounds)
        else: source=matte(source,'checker')
        if source.size!=reference:
            raise ValueError('Rear drawing measurement changed: '+name+' '+str(source.size))
        MEASURE[name]=(reference,landmarks)
        record=register(name,name.split('_')[0],'rear',fit_height(source,height),.5,.5)
        old=next((i for i,p in enumerate(records) if p['name']==name),None)
        if old is None: records.append(record)
        else: records[old]=record

    by_name={p['name']:p for p in records}
    for side in ('Near','Far'):
        pelvis=by_name['Pelvis_Rear']; thigh=by_name['Thigh'+side+'_Rear']
        shin=by_name['Shin'+side+'_Rear']; foot=by_name['Foot'+side+'_Rear']
        knee_y=HIP[1]+point(pelvis,'hip'+side)[1]+point(thigh,'end')[1]
        ankle_y=foot['image'].height/PPU*foot['pivotY']
        factor=(ankle_y-knee_y)/point(shin,'end')[1]
        if not .80<factor<1.20: raise ValueError('Rear shin measurement outside proportion tolerance: '+side)
        old=shin['image']; scaled=fit_height(old,old.height/PPU*factor)
        sx,sy=scaled.width/old.width,scaled.height/old.height
        for landmark in shin['landmarks']: landmark['x']*=sx; landmark['y']*=sy
        shin['image']=scaled

        # Raster height is integral. Absorb its subpixel rounding at the measured
        # rear ankle registration, so the heel rests at Y=0 without moving the
        # rig root or changing any front-view geometry.
        resolved_ankle=knee_y+point(shin,'end')[1]
        if abs(resolved_ankle-ankle_y) > 1/PPU:
            raise ValueError('Rear ankle rounding exceeds one texel: '+side)
        foot['pivotY']=resolved_ankle*PPU/foot['image'].height

def skeleton(records,view):
    by_name={(p['bone'],p['variant']):p for p in records}
    def piece(name): return by_name.get((name,view),by_name[(name,'front')])
    joints={'Hip':np.array(HIP),'Torso':np.array(HIP)}
    torso=piece('Torso'); pelvis=piece('Pelvis')
    joints['Neck']=joints['Torso']+point(torso,'neck')
    joints['Head']=joints['Neck'].copy()
    for side in ('Near','Far'):
        upper='UpperArm'+side; fore='Forearm'+side; hand='Hand'+side
        joints[upper]=joints['Torso']+point(torso,'shoulder'+side)
        joints[fore]=joints[upper]+point(piece(upper),'end')
        joints[hand]=joints[fore]+point(piece(fore),'end')
        thigh='Thigh'+side; shin='Shin'+side; foot='Foot'+side
        joints[thigh]=joints['Hip']+point(pelvis,'hip'+side)
        joints[shin]=joints[thigh]+point(piece(thigh),'end')
        joints[foot]=joints[shin]+point(piece(shin),'end')
    joints['Hair']=joints['Head']+point(piece('Head'),'hair')
    joints['ScarfLong']=np.array((-.14,HIP[1]+.01))
    joints['ScarfShort']=np.array((.12,HIP[1]+.01))
    return joints

def split_pauldrons(records):
    additions=[]
    for record in records:
        if record['bone']!='UpperArmNear': continue
        original=record['image']; w,h=original.size
        outline=PAULDRON_OUTLINES[record['variant']]
        mask=Image.new('L',(w,h)); ImageDraw.Draw(mask).polygon([(round(x*w),round(y*h)) for x,y in outline],fill=255)
        selected=np.asarray(mask)>0
        pixels=np.asarray(original).copy()
        # An earlier 'dark rim' overlap retained detached black armor-tip pixels
        # on the moving arm. Armor ownership follows the traced mask exactly;
        # a registered original-art underdrawing supplies the actual joint.
        arm=pixels.copy(); arm[selected]=0
        plate=pixels.copy(); plate[~selected]=0
        record['image']=Image.fromarray(arm)
        suffix='' if record['variant']=='front' else '_'+record['variant'].title()
        cap=dict(record,name='PauldronNear'+suffix,bone='PauldronNear',image=Image.fromarray(plate),
                 landmarks=[dict(p) for p in record['landmarks']])
        additions.append(cap)
        # Verify exact source coverage before rig rotation. Registration and the
        # existing opaque source colors are identical in both output layers.
        union=np.maximum(arm[:,:,3],plate[:,:,3])
        if not np.array_equal(union,pixels[:,:,3]): raise ValueError('Pauldron split lost source pixels')
    records.extend(additions)

def add_front_shoulder_underlay(records):
    record=next(p for p in records if p['name']=='UpperArmNear')
    folder=HERE/'shoulder-underlay'
    registration=json.loads((folder/'front-underlay-registration.json').read_text())
    underlay=Image.open(folder/'front-cloth-proximal-underlay.png').convert('RGBA')
    if list(record['image'].size)!=registration['target_size'] or underlay.size!=record['image'].size:
        raise ValueError('Front shoulder underdrawing canvas differs from measured registration')
    for key in ('attach','end'):
        local=point(record,key)
        actual=np.array((record['pivotX']*underlay.width+local[0]*PPU,
                         (1-record['pivotY'])*underlay.height-local[1]*PPU))
        expected=np.array(registration['target_landmarks_top_left'][key])
        if np.linalg.norm(actual-expected)>1e-5:
            raise ValueError('Front shoulder underdrawing landmark drift: '+key)
    record['image']=Image.alpha_composite(underlay,record['image'])

def transfer_skin_from_pauldrons(records):
    # Three small painted skin regions were inside the coarse armor split and
    # followed the rigid plate. Keep every metallic pixel in place; return only
    # the warm, saturated skin texels in these traced regions to the moving arm.
    regions=[[(28,80),(35,81),(45,91),(48,103),(51,112),(48,116),(42,105),(33,91)],
             [(51,114),(60,114),(67,124),(69,131),(60,128),(54,122)],
             [(78,143),(86,143),(95,148),(101,153),(101,158),(90,154),(84,152)]]
    # Follow the actual lower metal edge including its dark outline. Everything
    # below it is the source arm underdrawing, not a rigid armor-tip feature.
    # A saturation cutoff left pale skin and its black outline protruding here.
    underhang=[(49,115),(58,123),(68,128),(80,134),(90,140),(100,147),
               (111,154),(147,180),(147,189),(0,189),(0,115)]
    for view in ('Rear','Side'):
        plate=next(p for p in records if p['name']=='PauldronNear_'+view)
        arm=next(p for p in records if p['name']=='UpperArmNear_'+view)
        pixels=np.asarray(plate['image']).copy(); h,w=pixels.shape[:2]
        mask=Image.new('L',(w,h));draw=ImageDraw.Draw(mask)
        for region in regions:
            draw.polygon([(round(x*w/147),round(y*h/189)) for x,y in region],fill=255)
        rgb=pixels[:,:,:3].astype(float)
        skin=(rgb[:,:,0]-rgb[:,:,2]>.30*rgb[:,:,0])&(rgb[:,:,0]-rgb[:,:,1]>18)&(rgb[:,:,1]-rgb[:,:,2]>8)
        lower=Image.new('L',(w,h))
        if view=='Rear':
            ImageDraw.Draw(lower).polygon([(round(x*w/147),round(y*h/189)) for x,y in underhang],fill=255)
        anatomical=(np.asarray(mask)>0)&skin&(pixels[:,:,3]>0)
        if view=='Side':
            # Native inspection identified this detached pale skin island in
            # the lower circular opening. Its low saturation escaped the skin
            # heuristic. Trace each exact row; the actual metal rim at y124/125
            # and the already clean outer blade tip remain completely intact.
            if (w,h)!=(142,189): raise ValueError('Side plate pixel island requires remeasuring')
            for row,left,right in [(117,55,55),(118,54,56),(119,53,56),
                                   (120,52,57),(121,52,57),(122,53,58),(123,54,55),
                                   (118,49,50),(119,50,50)]:
                anatomical[row,left:right+1]=pixels[row,left:right+1,3]>0
        selected=(anatomical|(np.asarray(lower)>0))&(pixels[:,:,3]>0)
        # The real painted skin already transferred in the previous repair is
        # retained. The additional redundant overlap skirt includes external
        # pale matte/outline remnants: discard those rather than moving a new
        # detached gray stripe onto the otherwise continuous arm silhouette.
        moving=pixels.copy();moving[~anatomical]=0
        pixels[selected]=0
        plate['image']=Image.fromarray(pixels)
        arm['image']=Image.alpha_composite(Image.fromarray(moving),arm['image'])

def move_side_shoulder_skin(records):
    # The side torso source contains a second, fixed shoulder-skin disk. Use
    # those existing painted pixels as the moving proximal arm underdrawing.
    # Its garment armhole/rim stays on the torso, so it does not turn into a
    # second deltoid when the arm rotates. No pixels are synthesized.
    torso=next(p for p in records if p['name']=='Torso_Side')
    arm=next(p for p in records if p['name']=='UpperArmNear_Side')
    original=torso['image']; w,h=original.size
    mask=Image.new('L',original.size)
    contour=[(15,66),(27,55),(43,52),(59,61),(70,76),(75,94),
             (71,108),(62,120),(45,126),(28,121),(15,110),(10,95),(11,80)]
    ImageDraw.Draw(mask).polygon([(round(x*w/151),round(y*h/210)) for x,y in contour],fill=255)
    selected=np.asarray(mask)>0
    pixels=np.asarray(original).copy(); extracted=pixels.copy(); extracted[~selected]=0
    foreground=pixels.copy(); foreground[selected]=0
    torso['image']=Image.fromarray(foreground)
    socket=point(torso,'shoulderNear')*PPU
    torso_socket=np.array((torso['pivotX']*w+socket[0],(1-torso['pivotY'])*h-socket[1]))
    arm_attach=np.array((arm['pivotX']*arm['image'].width,(1-arm['pivotY'])*arm['image'].height))
    shift=arm_attach-torso_socket
    underlay=Image.fromarray(extracted).transform(arm['image'].size,Image.Transform.AFFINE,
        (1,0,-float(shift[0]),0,1,-float(shift[1])),resample=Image.Resampling.BICUBIC)
    # The small original deltoid outline lies inside that larger connected
    # painted shoulder. Replace its proximal region, keeping original lower
    # arm anatomy and the diagonal black arm band intact.
    moving=np.asarray(arm['image']).copy()
    yy,xx=np.indices(moving.shape[:2])
    moving[(yy<129)&(xx<77)]=0
    arm['image']=Image.alpha_composite(Image.fromarray(moving),underlay)

def build():
    OUT.mkdir(parents=True,exist_ok=True); PART_DIR.mkdir(exist_ok=True)
    source=Image.open(HERE/'parts-front-source.png'); swaps=Image.open(HERE/'parts-swaps-source.png')
    expressions=Image.open(HERE/'expressions-source.png')
    parts=[]; bases={}
    for name,box,height,px,py,flip in PARTS:
        part=matte(source.crop(box),'checker')
        if flip: part=ImageOps.mirror(part)
        part=fit_height(part,height)
        part=clear_caps(name,part)
        bases[name]=(part,px,py,height)
        parts.append(register(name,name,'front',part,px,py))
    for row,variant in enumerate(('rear','side')):
        for col,bone in enumerate(('Head','Torso','Pelvis','Collar','Hair','UpperArmNear')):
            part=matte(swaps.crop(SWAP_BOXES[row][col]))
            base,px,py,height=bases[bone]
            if bone=='UpperArmNear':
                # Source includes a wrist; cut at the elbow, hidden under gauntlet.
                part=part.crop((0,0,part.width,round(part.height*.70)))
            if bone=='Pelvis' and variant=='side':
                parts.append(full_side_pelvis(part,px,py,height))
                continue
            if bone=='Pelvis':
                # Keep the belt/hip region; the independent skinned coat tails
                # already provide the moving lower silhouette.
                part=part.crop((0,0,part.width,round(part.height*.70)))
            part=fit_height(part,height)
            if bone=='Head' and variant=='side': part=trim_side_neck_underlap(part)
            name=bone+'_'+variant.title()
            parts.append(register(name,bone,variant,part,px,py))
    for variant,box in [('action',(0,0,586,793)),('blink',(582,0,1139,793))]:
        part=matte(expressions.crop(box))
        base,px,py,height=bases['Head']; part=fit_height(part,height)
        parts.append(register('Head_'+variant.title(),'Head',variant,part,px,py))
    calibrate_shins(parts)
    add_rear_lower(parts)
    views={view:skeleton(parts,view) for view in ('front','rear','side')}
    split_pauldrons(parts)
    transfer_skin_from_pauldrons(parts)
    add_front_shoulder_underlay(parts)
    move_side_shoulder_skin(parts)
    split_torso_collars(parts)
    atlas=Image.new('RGBA',(2048,2048)); entries=[]; x=y=16; row_h=0
    for record in parts:
        name,bone,variant=(record[k] for k in ('name','bone','variant'))
        part=record['image']; px,py=record['pivotX'],record['pivotY']
        if x+part.width+16>atlas.width: x=16; y+=row_h+24; row_h=0
        if y+part.height+16>atlas.height: raise ValueError('Atlas overflow')
        atlas.paste(part,(x,y)); part.save(PART_DIR/(name+'.png'))
        item=dict(name=name,bone=bone,variant=variant,x=x,y=atlas.height-y-part.height,
                  width=part.width,height=part.height,pivotX=px,pivotY=py,
                  landmarks=record['landmarks'])
        if bone=='ScarfLong': item.update(softX=-.26*PPU,softY=-.40*PPU)
        if bone=='ScarfShort': item.update(softX=.06*PPU,softY=-.34*PPU)
        if bone=='Hair': item.update(softX=.025*PPU,softY=-.10*PPU)
        entries.append(item); x+=part.width+24; row_h=max(row_h,part.height)
    atlas.save(OUT/'KaelAtlas.png')
    layout=dict(version=4,pixelsPerUnit=PPU,weaponLength=1.75,
        joints=[dict(name=k,x=float(v[0]),y=float(v[1])) for k,v in views['front'].items()],
        viewJoints=[dict(view=view,name=k,x=float(v[0]),y=float(v[1])) for view,joints in views.items() for k,v in joints.items()],
        parts=entries)
    (OUT/'KaelAtlasLayout.json').write_text(json.dumps(layout,indent=2)+'\n',encoding='utf-8')
    for source_name,dest,size in [('portrait-source.png','Portrait',512),('skill-icon-source.png','SkillIcon',256)]:
        Image.open(HERE/source_name).convert('RGBA').resize((size,size),Image.Resampling.LANCZOS).save(OUT/(dest+'.png'))
    review=Image.new('RGB',(1400,((len(parts)+6)//7)*230),(36,44,57)); draw=ImageDraw.Draw(review)
    for i,record in enumerate(parts):
        name=record['name']; part=record['image']
        ox=i%7*200; oy=i//7*230; draw.text((ox+8,oy+8),name,fill='white')
        thumbnail=part.copy(); thumbnail.thumbnail((185,190),Image.Resampling.LANCZOS)
        review.paste(thumbnail,(ox+(200-thumbnail.width)//2,oy+28),thumbnail)
    review.save(HERE/'parts-review-dark.png')
    light=Image.new('RGB',review.size,(233,234,238)); draw=ImageDraw.Draw(light)
    for i,record in enumerate(parts):
        name=record['name']; part=record['image']
        ox=i%7*200; oy=i//7*230; draw.text((ox+8,oy+8),name,fill='black')
        thumbnail=part.copy(); thumbnail.thumbnail((185,190),Image.Resampling.LANCZOS)
        light.paste(thumbnail,(ox+(200-thumbnail.width)//2,oy+28),thumbnail)
    light.save(HERE/'parts-review-light.png')
    checks={p.relative_to(HERE).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()
            for p in sorted(HERE.rglob('*source.png'))}
    (HERE/'asset-build-manifest.json').write_text(json.dumps(dict(version=4,source_sha256=checks,
        output_parts=len(parts),pixelsPerUnit=PPU,atlas=[2048,2048],alpha='straight',
        cap_cuts=CAP_CUTS,measurement_reference=MEASURE,pauldron_outlines=PAULDRON_OUTLINES,
        collar_masks=COLLAR_MASKS,side_pelvis='complete natural contour at original belt scale; stable hip landmarks',
        rear_lower_measurements=REAR_LOWER,
        extraction='border-connected neutral matte; measured anatomical anchors; aspect-preserving uniform scale; explicit view joints; Lanczos resize'),indent=2)+'\n')
    print(f'KAEL_V2_ART_OK: {len(parts)} parts, RGBA atlas, used {y+row_h}/2048 px')

if __name__=='__main__': build()
