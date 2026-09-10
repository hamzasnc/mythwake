using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D.Animation;

/// <summary>Opt-in capture of actual live render resources, never a replacement battle renderer.</summary>
[InitializeOnLoad]
public static class KaelRenderDiagnostics
{
    static bool captured;
    static double readyAt;
    static int probeStage;
    static double probeAt;
    static RawImage probeSurface;
    static Material originalMaterial;
    static Rect originalUV;
    static Texture originalTexture;
    static Texture2D atlasCopy;
    static KaelRenderDiagnostics() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if (!Application.isPlaying || !MythwakePreferences.IsTestProfileActive || !Environment.GetCommandLineArgs().Contains("-mythwakeGameplayAcceptance")) return;
        if(captured) { Probe();return; }
        var views = UnityEngine.Object.FindObjectsByType<KaelAnimationView>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
        var combat = views.FirstOrDefault(v=>v.name.Contains("Combat"));
        if(combat==null) {readyAt=EditorApplication.timeSinceStartup+1;return;}
        if(EditorApplication.timeSinceStartup<readyAt) return;
        captured=true;
        var folder=Path.GetFullPath("artifacts/kael/render-diagnostics");Directory.CreateDirectory(folder);
        var text=new StringBuilder();
        foreach(var view in views)
        {
            var image=view.GetComponent<RawImage>();
            text.AppendLine($"VIEW {view.name} active={view.isActiveAndEnabled} pos={image.rectTransform.anchoredPosition} scale={image.rectTransform.lossyScale} size={image.rectTransform.rect} uv={image.uvRect} enabled={image.enabled} color={image.color} depth={image.depth} cull={image.canvasRenderer.cull} mat={image.material.name}");
            var mesh=image.canvasRenderer.GetMesh();
            text.AppendLine($" SURFACE texelSize={image.texture.texelSize} alpha={image.canvasRenderer.GetInheritedAlpha()} shader={image.materialForRendering.shader.name} keywords={string.Join(",",image.materialForRendering.shaderKeywords)} meshUV={string.Join(";",mesh.uv.Select(v=>v.ToString()))}");
            text.AppendLine($" MESH vertices={string.Join(";",mesh.vertices.Select(v=>v.ToString()))} colors={string.Join(";",mesh.colors32.Select(v=>v.ToString()))} triangles={string.Join(",",mesh.triangles)} renderColor={image.canvasRenderer.GetColor()} materials={image.canvasRenderer.materialCount}");
            var actor=view.Rig;
            text.AppendLine($"ACTOR active={actor.gameObject.activeInHierarchy} pos={actor.transform.position} local={actor.transform.localPosition} scale={actor.transform.lossyScale} state={actor.CurrentState} time={actor.CurrentTime}");
            foreach(var sprite in actor.GetComponentsInChildren<SpriteRenderer>(true))
                text.AppendLine($" SPRITE {sprite.name} enabled={sprite.enabled} forceOff={sprite.forceRenderingOff} layer={sprite.gameObject.layer} bounds={sprite.bounds} visible={sprite.isVisible} deformed={sprite.GetComponent<SpriteSkin>().HasCurrentDeformedVertices()}");
        }
        foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            text.AppendLine($"CAMERA {camera.name} enabled={camera.enabled} position={camera.transform.position} rotation={camera.transform.rotation} ortho={camera.orthographicSize} aspect={camera.aspect} rect={camera.rect} mask={camera.cullingMask} target={camera.targetTexture?.name} near={camera.nearClipPlane} far={camera.farClipPlane}");
        foreach(var graphic in combat.transform.parent.GetComponentsInChildren<Graphic>())
        {
            var corners=new Vector3[4];graphic.rectTransform.GetWorldCorners(corners);
            text.AppendLine($"UI {graphic.name} depth={graphic.depth} enabled={graphic.enabled} corners={string.Join(";",corners.Select(v=>v.ToString()))} shader={graphic.materialForRendering.shader.name}");
        }
        File.WriteAllText(Path.Combine(folder,"live-state.txt"),text.ToString());
        var rt=combat.GetComponent<RawImage>().texture as RenderTexture;
        if(rt!=null)
        {
            var old=RenderTexture.active; RenderTexture.active=rt;
            var png=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
            png.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);png.Apply();
            File.WriteAllBytes(Path.Combine(folder,"live-atlas.png"),png.EncodeToPNG());
            RenderTexture.active=old;
            if(Environment.GetCommandLineArgs().Contains("-mythwakeRenderProbe")) atlasCopy=png;
            else UnityEngine.Object.DestroyImmediate(png);
        }
        Debug.Log("KAEL_LIVE_RENDER_DIAGNOSTICS: "+folder);
        if(Environment.GetCommandLineArgs().Contains("-mythwakeRenderProbe"))
        {
            probeSurface=combat.GetComponent<RawImage>(); originalMaterial=probeSurface.material;originalUV=probeSurface.uvRect;originalTexture=probeSurface.texture;
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"probe-0-original.png"));
            probeStage=1;probeAt=EditorApplication.timeSinceStartup+1;
        }
    }
    static void Probe()
    {
        if(probeStage==0 || probeSurface==null || EditorApplication.timeSinceStartup<probeAt) return;
        var folder=Path.GetFullPath("artifacts/kael/render-diagnostics");
        if(probeStage==1) probeSurface.material=null;
        if(probeStage==2) ScreenCapture.CaptureScreenshot(Path.Combine(folder,"probe-1-default-ui.png"));
        if(probeStage==3) {probeSurface.material=originalMaterial;probeSurface.uvRect=new Rect(originalUV.x,1-originalUV.y,originalUV.width,-originalUV.height);}
        if(probeStage==4) ScreenCapture.CaptureScreenshot(Path.Combine(folder,"probe-2-flipped-uv.png"));
        if(probeStage==5) probeSurface.uvRect=new Rect(0,0,1,1);
        if(probeStage==6) ScreenCapture.CaptureScreenshot(Path.Combine(folder,"probe-3-full-atlas.png"));
        if(probeStage==7) {probeSurface.uvRect=originalUV;probeSurface.material=null;probeSurface.texture=atlasCopy;}
        if(probeStage==8) ScreenCapture.CaptureScreenshot(Path.Combine(folder,"probe-4-copied-texture.png"));
        if(probeStage==9) {probeSurface.texture=Texture2D.whiteTexture;probeSurface.color=Color.magenta;}
        if(probeStage==10) ScreenCapture.CaptureScreenshot(Path.Combine(folder,"probe-5-solid-quad.png"));
        if(probeStage==11) {probeSurface.texture=originalTexture;probeSurface.material=originalMaterial;probeSurface.color=Color.white;probeStage=0;UnityEngine.Object.DestroyImmediate(atlasCopy);return;}
        probeStage++;probeAt=EditorApplication.timeSinceStartup+2;
    }
}
