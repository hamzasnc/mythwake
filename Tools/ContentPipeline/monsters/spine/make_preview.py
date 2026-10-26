"""Writes a self-contained HTML page that renders the skeleton with spine-webgl 4.2.
Hash parameters: mode=grid|play, anim, times=0,0.1,..., cols, w, h, bg=dark|light|none,
debug=1 (bones), save=<file>.png (POSTs the rendered grid to /save; works while the page is hidden)."""
import base64, sys, os

build, out, N = sys.argv[1], sys.argv[2], sys.argv[3]
j = open(os.path.join(build, N + ".json"), encoding="utf-8").read()
a = open(os.path.join(build, N + ".atlas.txt"), encoding="utf-8").read()
p = base64.b64encode(open(os.path.join(build, N + ".png"), "rb").read()).decode()

html = f"""<!doctype html><html><head><meta charset="utf-8"><title>Hero Preview</title>
<style>html,body{{margin:0;background:#2a2e36;color:#ddd;font:12px sans-serif}}
canvas{{display:block;max-width:100vw;max-height:100vh}}#lbl{{position:fixed;left:6px;top:4px}}</style>
<script src="https://cdn.jsdelivr.net/npm/@esotericsoftware/spine-webgl@4.2.119/dist/iife/spine-webgl.min.js"></script>
</head><body><div id="lbl"></div><div id="btns" style="position:fixed;right:8px;top:6px;display:none"></div><canvas id="c"></canvas>
<script>
const J={j!r};
const A={a!r};
const P="data:image/png;base64,{p}";
const H=new URLSearchParams(location.hash.slice(1));
const mode=H.get('mode')||(location.hash?'grid':'play'); let animName=H.get('anim')||'idle';
const times=(H.get('times')||'0').split(',').map(Number);
const cols=+(H.get('cols')||Math.min(times.length,6));
const bg=H.get('bg')||'dark', debug=H.get('debug')==='1';
const cv=document.getElementById('c'); cv.width=+(H.get("w")||(mode==="play"?900:1600)); cv.height=+(H.get("h")||900);
const W=cv.width, Hh=cv.height;
window.__ready=false; window.__saved=false; window.__errors=[];
window.onerror=(m)=>{{window.__errors.push(String(m));}};
const ctx=new spine.ManagedWebGLRenderingContext(cv,{{alpha:bg==='none',preserveDrawingBuffer:true,premultipliedAlpha:true}});
const renderer=new spine.SceneRenderer(cv,ctx,true);
const am=new spine.AssetManager(ctx);
am.setRawDataURI('s.json','data:application/json;base64,'+btoa(unescape(encodeURIComponent(J))));
am.setRawDataURI('s.atlas','data:text/plain;base64,'+btoa(A));
am.setRawDataURI('{N}.png',P);
am.loadTextureAtlas('s.atlas'); am.loadText('s.json');
let data, skels=[], state=null, last=0;
function draw(){{
  const gl=ctx.gl; gl.viewport(0,0,W,Hh);
  if(bg==='none') gl.clearColor(0,0,0,0); else if(bg==='light') gl.clearColor(0.93,0.93,0.95,1); else gl.clearColor(0.165,0.18,0.21,1);
  gl.clear(gl.COLOR_BUFFER_BIT);
  const cam=renderer.camera; cam.setViewport(W,Hh); cam.position.x=W/2; cam.position.y=Hh/2; cam.zoom=1; cam.update();
  renderer.begin();
  for(const s of skels){{ renderer.drawSkeleton(s,false); if(debug) renderer.drawSkeletonDebug(s,false); }}
  renderer.end();
}}
function start(){{
  const sj=new spine.SkeletonJson(new spine.AtlasAttachmentLoader(am.require('s.atlas')));
  data=sj.readSkeletonData(JSON.parse(am.require('s.json')));
  if(mode==='grid'){{
    const rows=Math.ceil(times.length/cols), cw=W/cols, ch=Hh/rows;
    const sc=Math.min(cw/(+(H.get('vw')||330)), ch/(+(H.get('vh')||290)));
    times.forEach((t,i)=>{{
      const s=new spine.Skeleton(data); s.setToSetupPose(); s.scaleX=s.scaleY=sc;
      s.x=cw*(i%cols)+cw*(+(H.get('cx')||0.45)); s.y=Hh-(ch*(Math.floor(i/cols)+1))+ch*0.08;
      const an=data.findAnimation(animName);
      if(an) an.apply(s,0,t,false,[],1,spine.MixBlend.setup,spine.MixDirection.mixIn);
      for(const h of (H.get('hide')||'').split(',').filter(Boolean)){{ const sl=s.findSlot(h); if(sl) sl.setAttachment(null); }}
      const only=(H.get('only')||'').split(',').filter(Boolean);
      if(only.length) for(const sl of s.slots) if(!only.includes(sl.data.name)) sl.setAttachment(null);
      s.updateWorldTransform(spine.Physics.update);
      skels.push(s);
    }});
    document.getElementById('lbl').textContent=animName+' @ '+times.join(', ');
    draw(); window.__ready=true;
    if(H.get('save')) fetch('/save?name='+H.get('save'),{{method:'POST',body:cv.toDataURL('image/png')}}).then(()=>window.__saved=true);
  }} else if(mode==='seq'){{
    // renders an animation frame by frame and POSTs <prefix>_NNN.png for GIF assembly
    const s=new spine.Skeleton(data); const sc=+(H.get('scale')||Hh/300); s.scaleX=s.scaleY=sc;
    s.x=W*(+(H.get('cx')||0.45)); s.y=Hh*0.06; skels.push(s);
    const an=data.findAnimation(animName), fps=+(H.get('fps')||30), n=Math.round(an.duration*fps)+(H.get('loop')==='1'?0:1);
    const prefix=H.get('prefix')||animName;
    (async()=>{{ for(let i=0;i<n;i++){{ s.setToSetupPose(); an.apply(s,0,i/fps,false,[],1,spine.MixBlend.setup,spine.MixDirection.mixIn);
        s.updateWorldTransform(spine.Physics.update); draw();
        await fetch('/save?name='+prefix+'_'+String(i).padStart(3,'0')+'.png',{{method:'POST',body:cv.toDataURL('image/png')}}); }}
      window.__saved=true; }})();
    window.__ready=true;
  }} else {{
    const s=new spine.Skeleton(data); s.scaleX=s.scaleY=Hh/300; s.x=W*0.5; s.y=Hh*0.08; skels.push(s);
    state=new spine.AnimationState(new spine.AnimationStateData(data)); state.setAnimation(0,animName,true);
    const b=document.getElementById('btns'); b.style.display='block';
    for(const a of data.animations){{ const e=document.createElement('button'); e.textContent=a.name; e.style.margin='2px';
      e.onclick=()=>{{ state.setAnimation(0,a.name,a.name==='idle'); if(a.name!=='idle'&&a.name!=='death') state.addAnimation(0,'idle',true,0); }}; b.appendChild(e); }}
    state.data.defaultMix=0.12;
    window.__ready=true; last=performance.now(); requestAnimationFrame(loop);
  }}
}}
function loop(now){{ const d=Math.min(0.1,(now-last)/1000); last=now; state.update(d); state.apply(skels[0]);
  skels[0].update(d); skels[0].updateWorldTransform(spine.Physics.update); draw(); requestAnimationFrame(loop); }}
(function wait(){{ if(am.isLoadingComplete()) {{ try{{start();}}catch(e){{window.__errors.push(String(e&&e.stack||e));}} }} else setTimeout(wait,30); }})();
</script></body></html>"""
open(out, "w", encoding="utf-8").write(html)
print("wrote", out, len(html) // 1024, "KB")
