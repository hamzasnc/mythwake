using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.IK;

/// <summary>Explicitly timed, editable control curves for one front-view strike study.</summary>
public static class KaelStrikeStudyAuthoring
{
    const string SourcePath = "ArtSource/Kael/MotionStudy/strike-study.json";
    const string AssetFolder = "Assets/_Mythwake/ArtStudies/KaelStrike";
    [Serializable] sealed class Study
    {
        public string name;
        public float duration, impact;
        public bool nearArmFlip;
        public float shoulderArmorFollow;
        public Track[] tracks;
        public DrawingOrder[] drawingOrders;
    }
    [Serializable] sealed class Track
    {
        public string bone, property;
        public Key[] keys;
    }
    [Serializable] sealed class Key
    {
        public float time, value, velocity;
    }
    [Serializable] sealed class DrawingOrder
    {
        public string drawing;
        public int order;
    }

    [MenuItem("Mythwake/Kael/Author and Inspect Single Strike Study")]
    public static void Run()
    {
        var study = JsonUtility.FromJson<Study>(File.ReadAllText(SourcePath));
        if (study == null || study.name != "strike_study" || study.duration != .84f || study.impact != .42f)
            throw new InvalidOperationException("The study must preserve the existing single-contact basic timing.");
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, "-kaelMotionReviewFolder");
        var slug = index >= 0 && index + 1 < args.Length ? args[index + 1] : "take01";
        if (!System.Text.RegularExpressions.Regex.IsMatch(slug, "^[a-zA-Z0-9_-]+$"))
            throw new ArgumentException("Use one simple study review folder name.");
        var folder = Path.GetFullPath("artifacts/kael/strike-study/" + slug);
        Directory.CreateDirectory(folder);
        var actor = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Mythwake/Resources/Characters/Kael/Kael.prefab"));
        var clip = new AnimationClip { name = study.name, frameRate = 60 };
        var renderErrors = new List<string>();
        Application.LogCallback observeError = (message, stack, type) => {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                renderErrors.Add(message);
        };
        Application.logMessageReceived += observeError;
        try
        {
            actor.name = "KaelStrikeAuthoring";
            actor.GetComponent<KaelRig>().shoulderArmorFollow = study.shoulderArmorFollow;
            KaelStudyHeadLayers.Prepare(actor, AssetFolder);
            var bones = actor.GetComponentsInChildren<Transform>(true).ToDictionary(t => t.name);
            var drawings = actor.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var item in study.drawingOrders ?? Array.Empty<DrawingOrder>())
            {
                if (drawings.Count(drawing => drawing.name == item.drawing) != 1)
                    throw new InvalidOperationException("Unknown or ambiguous drawing order: " + item.drawing);
            }
            foreach (var bone in bones.Values)
            {
                if (bone == actor.transform) continue;
                var path = AnimationUtility.CalculateTransformPath(bone, actor.transform);
                for (var axis = 0; axis < 3; axis++)
                {
                    Constant(clip, path, "m_LocalPosition." + "xyz"[axis], bone.localPosition[axis], study.duration);
                    Constant(clip, path, "localEulerAnglesRaw." + "xyz"[axis],
                        Mathf.DeltaAngle(0, bone.localEulerAngles[axis]), study.duration);
                    Constant(clip, path, "m_LocalScale." + "xyz"[axis], bone.localScale[axis], study.duration);
                }
            }
            foreach (var renderer in drawings)
            {
                var order = study.drawingOrders?.SingleOrDefault(item => item.drawing == renderer.name);
                if (order != null) renderer.sortingOrder = order.order;
                var path = AnimationUtility.CalculateTransformPath(renderer.transform, actor.transform);
                AnimationUtility.SetObjectReferenceCurve(clip,
                    EditorCurveBinding.PPtrCurve(path, typeof(SpriteRenderer), "m_Sprite"),
                    new[] { new ObjectReferenceKeyframe { time = 0, value = renderer.sprite } });
                clip.SetCurve(path, typeof(SpriteRenderer), "m_SortingOrder",
                    AnimationCurve.Constant(0, study.duration, renderer.sortingOrder));
            }
            foreach (var solver in actor.GetComponentsInChildren<LimbSolver2D>(true))
            {
                if (solver.name == "NearArm") solver.flip = study.nearArmFlip;
                var path = AnimationUtility.CalculateTransformPath(solver.transform, actor.transform);
                clip.SetCurve(path, typeof(LimbSolver2D), "m_Weight", AnimationCurve.Constant(0, study.duration, 1));
                clip.SetCurve(path, typeof(LimbSolver2D), "m_Flip", AnimationCurve.Constant(0, study.duration, solver.flip ? 1 : 0));
            }
            var duplicates = study.tracks.GroupBy(t => t.bone + "/" + t.property).FirstOrDefault(g => g.Count() != 1);
            if (duplicates != null) throw new InvalidOperationException("Duplicate control: " + duplicates.Key);
            foreach (var track in study.tracks)
            {
                if (!bones.TryGetValue(track.bone, out var bone) || track.keys == null || track.keys.Length < 2)
                    throw new InvalidOperationException("Incomplete authored control: " + track.bone);
                if (track.keys[0].time != 0 || track.keys.Last().time != study.duration)
                    throw new InvalidOperationException("Controls must include the exact start and end: " + track.bone);
                var previous = -1f;
                foreach (var key in track.keys)
                {
                    if (key.time <= previous || float.IsNaN(key.value) || float.IsNaN(key.velocity))
                        throw new InvalidOperationException("Invalid key in " + track.bone);
                    previous = key.time;
                }
                // Tangents are supplied by the motion source, never guessed by a
                // global smoothing pass. Different body channels lead and follow.
                var curve = new AnimationCurve(track.keys.Select(k =>
                    new Keyframe(k.time, k.value, k.velocity, k.velocity)).ToArray());
                clip.SetCurve(AnimationUtility.CalculateTransformPath(bone, actor.transform),
                    typeof(Transform), track.property, curve);
            }
            AnimationUtility.SetAnimationEvents(clip, new[] { new AnimationEvent {
                time = study.impact, functionName = "OnKaelAnimationMarker", stringParameter = "impact:0" } });
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.stopTime = study.duration; settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            File.Copy(SourcePath, Path.Combine(folder, "authored-controls.json"), true);
            KaelBakedMotionStudy.Export(actor, clip, AssetFolder, folder);
            if (renderErrors.Count > 0)
            {
                File.WriteAllLines(Path.Combine(folder, "unity-render-errors.txt"), renderErrors);
                throw new InvalidOperationException("Unity reported rendering/import errors; the study is not valid. Inspect unity-render-errors.txt.");
            }
            Debug.Log("KAEL_SINGLE_STRIKE_STUDY_EXPORTED: " + folder +
                "; one front-view animation; visual quality remains a separate review.");
        }
        finally
        {
            Application.logMessageReceived -= observeError;
            if (actor != null) { actor.GetComponent<KaelRig>()?.ResetPlayback(); UnityEngine.Object.DestroyImmediate(actor); }
            UnityEngine.Object.DestroyImmediate(clip);
        }
    }

    static void Constant(AnimationClip clip, string path, string property, float value, float duration)
        => clip.SetCurve(path, typeof(Transform), property, AnimationCurve.Constant(0, duration, value));
}
