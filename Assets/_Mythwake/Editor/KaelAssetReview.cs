using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

public static class KaelAssetReview
{
    [MenuItem("Mythwake/Kael/Rebuild and Inspect Body Assembly")]
    public static void RebuildBodyReview()
    {
        Run();
        KaelBodyAssemblyReview.Run();
        CaptureMotion();
    }

    public static void RebuildPixelReview()
    {
        Run();
        KaelBodyAssemblyReview.Run();
        CapturePixelMotion();
    }

    [MenuItem("Mythwake/Kael/Build and Review Rig")]
    public static void Run()
    {
        KaelAssetBuilder.Build();
        KaelRosterValidation.Run();
        KaelCombatSessionValidation.Run();
        KaelAnimationValidation.Run();
        CapturePoses();
        CaptureRuntimeIcon();
    }

    /// <summary>Flatten the actual authored idle pose for legacy portrait/sprite consumers.</summary>
    public static void CaptureRuntimeIcon()
    {
        const string path = "Assets/_Mythwake/Resources/Mythwake/Art/Runtime/hero_kael.png";
        const int size = 1024;
        var actor = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Characters/Kael/Kael"));
        var cameraObject = new GameObject("Kael runtime icon capture",typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        // A linear target makes the required unpremultiplication explicit. The final
        // PNG is converted to straight-alpha sRGB for ordinary Sprite/UI materials.
        var target = new RenderTexture(size,size,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
        var oldActive = RenderTexture.active; var oldEvent = Event.current;
        Texture2D readback = null, output = null;
        try
        {
            var rig = actor.GetComponent<KaelRig>(); rig.weaponTrail.enabled = false;
            rig.Sample("idle",1,0,0);
            Event.current = new Event { type = EventType.Repaint };
            foreach (var skin in actor.GetComponentsInChildren<SpriteSkin>()) skin.OnPreviewUpdate();
            var renderers = actor.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("Kael icon capture has no rendered body.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            camera.transform.position = new Vector3(bounds.center.x,bounds.center.y,-10);
            camera.orthographic = true; camera.orthographicSize = Mathf.Max(bounds.size.x,bounds.size.y)*.56f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
            camera.cullingMask = 1<<31; camera.allowHDR = camera.allowMSAA = false; camera.targetTexture=target;
            camera.Render(); RenderTexture.active = target;
            readback = new Texture2D(size,size,TextureFormat.RGBA32,false,true);
            readback.ReadPixels(new Rect(0,0,size,size),0,0); readback.Apply();
            var pixels = readback.GetPixels();
            var minX=size; var minY=size; var maxX=-1; var maxY=-1;
            for (var y=0;y<size;y++) for (var x=0;x<size;x++)
            {
                var c = pixels[y*size+x];
                if (c.a > 1/255f) { minX=Mathf.Min(minX,x); minY=Mathf.Min(minY,y); maxX=Mathf.Max(maxX,x); maxY=Mathf.Max(maxY,y); }
                if (c.a > 0)
                {
                    var linear = new Color(Mathf.Clamp01(c.r/c.a),Mathf.Clamp01(c.g/c.a),Mathf.Clamp01(c.b/c.a),c.a);
                    pixels[y*size+x] = QualitySettings.activeColorSpace == ColorSpace.Linear ? linear.gamma : linear;
                }
                else pixels[y*size+x] = Color.clear;
            }
            if (maxX<=minX || maxY<=minY) throw new InvalidOperationException("Kael icon capture rendered no visible character pixels.");
            var padding = Mathf.CeilToInt(Mathf.Max(maxX-minX+1,maxY-minY+1)*.035f);
            var width=maxX-minX+1+padding*2; var height=maxY-minY+1+padding*2;
            var cropped = new Color[width*height];
            for (var y=minY;y<=maxY;y++) for (var x=minX;x<=maxX;x++)
                cropped[(y-minY+padding)*width+x-minX+padding]=pixels[y*size+x];
            output = new Texture2D(width,height,TextureFormat.RGBA32,false);
            output.SetPixels(cropped); output.Apply();
            File.WriteAllBytes(path,output.EncodeToPNG());
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaSource=TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency=true;
            importer.sRGBTexture=true; importer.mipmapEnabled=false; importer.npotScale=TextureImporterNPOTScale.None;
            importer.filterMode=FilterMode.Bilinear; importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.maxTextureSize=1024;
            importer.ClearPlatformTextureSettings("Android"); importer.SaveAndReimport();
            Debug.Log("KAEL_RUNTIME_ICON_OK: " + path + "; actual new-rig idle, straight-alpha sRGB, " + width + "x" + height);
        }
        finally
        {
            Event.current=oldEvent; RenderTexture.active=oldActive;
            actor.GetComponent<KaelRig>().ResetPlayback(); camera.targetTexture=null;
            if (readback != null) UnityEngine.Object.DestroyImmediate(readback);
            if (output != null) UnityEngine.Object.DestroyImmediate(output);
            target.Release(); UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(actor); UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    public static void CapturePoses()
    {
        var folder = Path.GetFullPath("artifacts/kael/v2-rig-review"); Directory.CreateDirectory(folder);
        var actor = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Characters/Kael/Kael"));
        var cameraObject = new GameObject("Kael rig review camera",typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        var target = new RenderTexture(640,640,24,RenderTextureFormat.ARGB32);
        var oldActive = RenderTexture.active;
        var oldEvent = Event.current;
        try
        {
            camera.transform.position = new Vector3(0,2.25f,-10);
            camera.orthographic = true; camera.orthographicSize = 3;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.13f,.17f,.23f,1);
            camera.cullingMask = 1<<31; camera.allowHDR = camera.allowMSAA = false; camera.targetTexture=target;
            var rig=actor.GetComponent<KaelRig>();
            rig.weaponTrail.enabled = false;
            var states=new[]{"idle","idle","run","attack_cross","attack_cross","attack_cross","attack_cross",
                "attack_spin","attack_spin","attack_spin","attack_spin","attack_jump","attack_jump","attack_jump",
                "skill","skill","skill","skill","skill","skill","hit","death"};
            var times=new[]{0f,2f,.16f,.16f,.24f,.45f,.52f,.16f,.28f,.36f,.42f,.17f,.37f,.46f,.18f,.28f,.4f,.69f,.92f,1.075f,.05f,.90f};
            for(var i=0;i<states.Length;i++)
            {
                rig.ResetPlayback(); rig.Sample(states[i],i,times[i],1);
                // Official SpriteSkin preview API; this is a rig review, not gameplay evidence.
                Event.current=new Event {type=EventType.Repaint};
                foreach(var skin in actor.GetComponentsInChildren<SpriteSkin>()) skin.OnPreviewUpdate();
                camera.Render(); RenderTexture.active=target;
                var png=new Texture2D(640,640,TextureFormat.RGBA32,false);
                png.ReadPixels(new Rect(0,0,640,640),0,0); png.Apply();
                File.WriteAllBytes(Path.Combine(folder,$"{i:00}-{states[i]}.png"),png.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(png);
            }
            Debug.Log("KAEL_RIG_REVIEW_OK: "+folder);
        }
        finally
        {
            Event.current=oldEvent; RenderTexture.active=oldActive;
            actor.GetComponent<KaelRig>().ResetPlayback();
            camera.targetTexture=null;
            target.Release(); UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(actor); UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    /// <summary>Authored animation review without VFX; this is distinct from actual combat evidence.</summary>
    public static void CaptureMotion()
    {
        CaptureMotionFrames("artifacts/kael/v2-motion-final", 640, 30);
    }

    public static void CapturePixelMotion()
    {
        CaptureMotionFrames("artifacts/kael/pixel-repair-2/motion-final", 1024, 60);
    }

    static void CaptureMotionFrames(string outputFolder, int resolution, int frameRate)
    {
        var folder = Path.GetFullPath(outputFolder);
        Directory.CreateDirectory(folder);
        var actor = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Characters/Kael/Kael"));
        var cameraObject = new GameObject("Kael v2 motion review camera",typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        var target = new RenderTexture(resolution,resolution,24,RenderTextureFormat.ARGB32);
        var oldActive = RenderTexture.active; var oldEvent = Event.current;
        var bounds = new Bounds(); var hasBounds = false;
        var report = new System.Text.StringBuilder("clip,frame,time,minX,minY,maxX,maxY,minWeaponY,weaponTipY,swordScaleY\n");
        try
        {
            camera.transform.position = new Vector3(0,2.25f,-10);
            camera.orthographic = true; camera.orthographicSize = 3;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.13f,.17f,.23f,1);
            camera.cullingMask = 1<<31; camera.allowHDR = camera.allowMSAA = false; camera.targetTexture=target;
            var rig = actor.GetComponent<KaelRig>(); rig.weaponTrail.enabled = false;
            var sequence = 1L;
            foreach (var clip in rig.clips)
            {
                if (clip.name == "attack") continue;
                var clipFolder = Path.Combine(folder,clip.name); Directory.CreateDirectory(clipFolder);
                rig.ResetPlayback();
                var count = Mathf.CeilToInt(clip.length*frameRate);
                for (var frame=0;frame<=count;frame++)
                {
                    var time = Mathf.Min(clip.length,frame/(float)frameRate);
                    rig.Sample(clip.name,sequence,time,1f/frameRate);
                    Event.current = new Event { type = EventType.Repaint };
                    foreach (var skin in actor.GetComponentsInChildren<SpriteSkin>()) skin.OnPreviewUpdate();
                    var sampleBounds = new Bounds(); var hasSample = false;
                    foreach (var renderer in actor.GetComponentsInChildren<SpriteRenderer>())
                    {
                        if (!hasSample) { sampleBounds = renderer.bounds; hasSample = true; }
                        else sampleBounds.Encapsulate(renderer.bounds);
                    }
                    if (!hasBounds) { bounds = sampleBounds; hasBounds = true; } else bounds.Encapsulate(sampleBounds);
                    var sword = rig.weaponTip.parent.GetComponentInChildren<SpriteRenderer>();
                    report.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2:F4},{3:F4},{4:F4},{5:F4},{6:F4},{7:F4},{8:F4},{9:F4}\n",
                        clip.name,frame,time,sampleBounds.min.x,sampleBounds.min.y,sampleBounds.max.x,sampleBounds.max.y,
                        sword.bounds.min.y,rig.weaponTip.position.y,rig.weaponTip.parent.localScale.y);
                    camera.Render(); RenderTexture.active=target;
                    var png = new Texture2D(resolution,resolution,TextureFormat.RGBA32,false);
                    png.ReadPixels(new Rect(0,0,resolution,resolution),0,0); png.Apply();
                    File.WriteAllBytes(Path.Combine(clipFolder,$"frame-{frame:D4}.png"),png.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(png);
                }
                sequence++;
            }
            File.WriteAllText(Path.Combine(folder,"bounds.csv"),report.ToString());
            File.WriteAllText(Path.Combine(folder,"capture-settings.json"),
                $"{{\"width\":{resolution},\"height\":{resolution},\"fps\":{frameRate},\"effects\":false}}");
            Debug.Log("KAEL_V2_MOTION_WITHOUT_VFX_OK: " + folder + "; renderer bounds " + bounds.min + " to " + bounds.max);
        }
        finally
        {
            Event.current=oldEvent; RenderTexture.active=oldActive;
            actor.GetComponent<KaelRig>().ResetPlayback(); camera.targetTexture=null;
            target.Release(); UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(actor); UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}
