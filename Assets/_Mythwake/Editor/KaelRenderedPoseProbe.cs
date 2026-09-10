using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Read-only observation of bones at the actual camera-render boundary.</summary>
[InitializeOnLoad]
public static class KaelRenderedPoseProbe
{
    static string folder;
    static bool captured;
    static KaelRenderedPoseProbe() { RenderPipelineManager.endCameraRendering += Observe; }
    static void Observe(ScriptableRenderContext context, Camera camera)
    {
        if (captured || !Application.isPlaying || !MythwakePreferences.IsTestProfileActive ||
            !Environment.GetCommandLineArgs().Contains("-mythwakeQuickMotion") ||
            camera.name != "Kael shared canvas render atlas") return;
        var view = UnityEngine.Object.FindObjectsByType<KaelAnimationView>(FindObjectsInactive.Exclude).FirstOrDefault(v=>v.name.Contains("Combat"));
        if (view == null || view.Rig.CurrentState != "death" || view.Rig.CurrentTime < .899f) return;
        captured = true;
        var args=Environment.GetCommandLineArgs();
        var index=Array.IndexOf(args,"-mythwakeCaptureDir");
        folder=Path.GetFullPath(args[index+1]); Directory.CreateDirectory(folder);
        var rig=view.Rig;
        var root=rig.transform.Find("Root");
        var pose=$"At endCameraRendering: state={rig.CurrentState}, clipTime={rig.CurrentTime}, authoredRootRoll=82, actualRootRoll={Mathf.DeltaAngle(0,root.localEulerAngles.z)}, rootPosition={root.localPosition}, controller={rig.GetComponent<Animator>().runtimeAnimatorController?.name}";
        File.WriteAllText(Path.Combine(folder,"rendered-pose.txt"),pose);
        var rt=camera.targetTexture;var previous=RenderTexture.active;RenderTexture.active=rt;
        var pixels=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        pixels.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);pixels.Apply();
        File.WriteAllBytes(Path.Combine(folder,"rendered-death-atlas.png"),pixels.EncodeToPNG());
        RenderTexture.active=previous;UnityEngine.Object.Destroy(pixels);
        Debug.Log("KAEL_RENDERED_POSE: "+pose);
    }
}
