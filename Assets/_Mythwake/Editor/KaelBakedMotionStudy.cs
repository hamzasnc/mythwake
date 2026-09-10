using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D.Animation;
using UnityEngine.U2D.IK;

/// <summary>Editor-only final-pose bake and an independent playback/render check of a single study clip.</summary>
public static class KaelBakedMotionStudy
{
    const int BakeRate = 120, RenderRate = 60, Resolution = 1024, MaxBakeRefinements = 5;
    static readonly string[] Observed = { "Hip", "Torso", "Head", "UpperArmNear", "ForearmNear", "HandNear",
        "UpperArmFar", "ForearmFar", "HandFar", "ThighNear", "ShinNear", "FootNear", "ThighFar", "ShinFar", "FootFar", "WeaponTip", "GripAnchor" };

    [Serializable] sealed class FileHash { public string path, sha256; }
    [Serializable] sealed class HashManifest { public FileHash[] files; }
    [Serializable] sealed class Contact { public float time; public string marker; }
    [Serializable] sealed class Comparison
    {
        public int samples, spriteMismatches, sortingMismatches;
        public float maxLocalPositionError, maxLocalRotationDegrees, maxLocalScaleError, maxWorldPositionError, maxGripGap;
        public bool passed;
    }
    [Serializable] sealed class Manifest
    {
        public string status = "running", utc, unityVersion, sourceClip, bakedClip, prefab;
        public string method = "Offline 120Hz base capture after source socket/IK/armor evaluation, with at most five midpoint refinement passes for curved motion; standalone baked prefab replay has no KaelRig or IK components. All output PNGs render that reloaded prefab.";
        public string limits = "A single animation study, not gameplay evidence or visual approval. Transform interpolation is measured at intermediate times; SpriteSkin remains active to render weighted meshes.";
        public int bakeRate = BakeRate, renderRate = RenderRate, resolution = Resolution, bakeSamples, renderedFrames, transforms, renderers;
        public int bakeRefinements, unresolvedInterpolationIntervals;
        public bool pixelAuditEnabled;
        public string pixelAuditMethod;
        public float clipDuration, endHold = .25f;
        public Vector3 cameraPosition = new Vector3(0, 2.25f, -10);
        public float cameraOrthographicSize = 3;
        public Contact[] contacts;
        public Comparison comparison;
        public FileHash[] generated;
    }
    sealed class Pose
    {
        public float time;
        public Vector3[] position, scale, world;
        public float[] angle;
        public Sprite[] sprites;
        public int[] order;
    }

    public static void Export(GameObject source, AnimationClip controlClip, string assetFolder, string captureFolder)
    {
        if (source == null || controlClip == null || controlClip.length <= 0) throw new ArgumentException("A source actor and a non-empty control clip are required.");
        assetFolder = ValidateAssetFolder(assetFolder);
        captureFolder = Path.GetFullPath(captureFolder);
        var framesFolder = Path.Combine(captureFolder, "frames");
        if (Directory.Exists(framesFolder) && Directory.EnumerateFiles(framesFolder, "*.png").Any())
            throw new InvalidOperationException("Use a fresh study capture folder so old frames cannot be mistaken for the new animation.");
        Directory.CreateDirectory(assetFolder); Directory.CreateDirectory(captureFolder);
        var clipPath = assetFolder + "/strike_study_baked.anim";
        var prefabPath = assetFolder + "/KaelStrikeStudy.prefab";
        var hashesPath = assetFolder + "/bake-fingerprints.json";
        ProtectExisting(hashesPath, clipPath, prefabPath);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var manifest = new Manifest { utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), unityVersion = Application.unityVersion,
            sourceClip = AssetDatabase.GetAssetPath(controlClip), bakedClip = clipPath, prefab = prefabPath, clipDuration = controlClip.length,
            contacts = controlClip.events.Where(e => e.functionName == nameof(KaelRig.OnKaelAnimationMarker))
                .Select(e => new Contact { time = e.time, marker = e.stringParameter }).ToArray() };
        var preview = new PreviewRenderUtility();
        GameObject working = null, template = null, bakedActor = null;
        var oldEvent = Event.current;
        try
        {
            working = CloneIntoPreview(source, preview);
            var rig = working.GetComponent<KaelRig>();
            if (rig == null) throw new InvalidOperationException("The editor source requires KaelRig for its one-time solved-pose bake.");
            rig.clips = rig.clips.Where(c => c != null && c.name != controlClip.name).Concat(new[] { controlClip }).ToArray();
            if (rig.weaponTrail != null) rig.weaponTrail.enabled = false;
            rig.ResetPlayback();
            var transforms = OrderedTransforms(working);
            var renderers = OrderedRenderers(working);
            manifest.transforms = transforms.Length; manifest.renderers = renderers.Length;
            var times = BakeTimes(controlClip);
            var poses = RefineBake(rig, controlClip, transforms, renderers, times, manifest);
            manifest.bakeSamples = poses.Count;
            var baked = WriteClip(clipPath, working, transforms, renderers, poses, controlClip.events);

            template = CloneIntoPreview(source, preview);
            foreach (var oldRig in template.GetComponentsInChildren<KaelRig>(true)) UnityEngine.Object.DestroyImmediate(oldRig);
            foreach (var manager in template.GetComponentsInChildren<IKManager2D>(true)) UnityEngine.Object.DestroyImmediate(manager);
            foreach (var solver in template.GetComponentsInChildren<Solver2D>(true)) UnityEngine.Object.DestroyImmediate(solver);
            foreach (var line in template.GetComponentsInChildren<LineRenderer>(true)) UnityEngine.Object.DestroyImmediate(line);
            foreach (var animator in template.GetComponentsInChildren<Animator>(true))
            { animator.runtimeAnimatorController = null; animator.applyRootMotion = false; animator.fireEvents = false; }
            foreach (var transform in template.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.hideFlags = HideFlags.None;
                foreach (var component in transform.GetComponents<Component>()) if (component != null) component.hideFlags = HideFlags.None;
            }
            var player = template.AddComponent<KaelBakedClipPlayer>(); player.clip = baked;
            template.name = "Kael Strike Study - baked bones";
            PrefabUtility.SaveAsPrefabAsset(template, prefabPath);
            UnityEngine.Object.DestroyImmediate(template); template = null;
            AssetDatabase.SaveAssets();
            manifest.generated = WriteHashes(hashesPath, clipPath, prefabPath);
            // Reload the saved prefab: comparing an unsaved clone would miss lost bindings.
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            bakedActor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview.camera.gameObject.scene);
            bakedActor.hideFlags = HideFlags.HideAndDontSave;
            bakedActor.GetComponent<KaelBakedClipPlayer>().playAutomatically = false;
            RequirePurePlayback(bakedActor);
            manifest.comparison = Compare(working, controlClip, bakedActor, times, captureFolder);
            working.SetActive(false);
            RenderBaked(bakedActor, preview.camera, captureFolder, manifest);
            manifest.status = manifest.comparison.passed ? "technical-bake-checks-passed-visual-review-required" : "source-baked-comparison-failed";
            WriteManifest(captureFolder, manifest);
            AssetDatabase.ImportAsset(hashesPath, ImportAssetOptions.ForceSynchronousImport);
            if (!manifest.comparison.passed) throw new InvalidOperationException("Kael study bake differs from its solved source; inspect comparison.csv and bake-manifest.json. No visual approval is asserted.");
            Debug.Log("KAEL_BAKED_STUDY_OK: " + prefabPath + "; " + manifest.renderedFrames + " actual baked-only renders; " + manifest.comparison.samples + " source/baked comparisons. " + captureFolder);
        }
        catch
        {
            if (manifest.status == "running") manifest.status = "export-failed";
            WriteManifest(captureFolder, manifest);
            throw;
        }
        finally
        {
            Event.current = oldEvent;
            if (working != null) { working.GetComponent<KaelRig>()?.ResetPlayback(); UnityEngine.Object.DestroyImmediate(working); }
            if (template != null) UnityEngine.Object.DestroyImmediate(template);
            if (bakedActor != null) { bakedActor.GetComponent<KaelBakedClipPlayer>()?.ResetPlayback(); UnityEngine.Object.DestroyImmediate(bakedActor); }
            preview.Cleanup();
        }
    }

    static GameObject CloneIntoPreview(GameObject source, PreviewRenderUtility preview)
    {
        var clone = UnityEngine.Object.Instantiate(source);
        SceneManager.MoveGameObjectToScene(clone, preview.camera.gameObject.scene);
        clone.hideFlags = HideFlags.HideAndDontSave;
        clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        clone.transform.localScale = Vector3.one;
        clone.SetActive(true);
        return clone;
    }
    static Transform[] OrderedTransforms(GameObject actor) => actor.GetComponentsInChildren<Transform>(true)
        .Where(t => t != actor.transform).OrderBy(t => AnimationUtility.CalculateTransformPath(t, actor.transform), StringComparer.Ordinal).ToArray();
    static SpriteRenderer[] OrderedRenderers(GameObject actor) => actor.GetComponentsInChildren<SpriteRenderer>(true)
        .OrderBy(r => AnimationUtility.CalculateTransformPath(r.transform, actor.transform), StringComparer.Ordinal).ToArray();
    static Pose Capture(Transform[] bones, SpriteRenderer[] renderers, float time)
    {
        foreach (var bone in bones)
            if (Mathf.Abs(Mathf.DeltaAngle(0, bone.localEulerAngles.x)) > .001f || Mathf.Abs(Mathf.DeltaAngle(0, bone.localEulerAngles.y)) > .001f)
                throw new InvalidOperationException("This 2D study bake requires planar rotations; non-planar bone: " + bone.name);
        return new Pose { time = time,
            position = bones.Select(t => t.localPosition).ToArray(), scale = bones.Select(t => t.localScale).ToArray(),
            world = bones.Select(t => t.position).ToArray(),
            angle = bones.Select(t => Mathf.DeltaAngle(0, t.localEulerAngles.z)).ToArray(),
            sprites = renderers.Select(r => r.sprite).ToArray(), order = renderers.Select(r => r.sortingOrder).ToArray() };
    }

    static List<Pose> RefineBake(KaelRig rig, AnimationClip clip, Transform[] bones, SpriteRenderer[] renderers,
        SortedSet<float> times, Manifest manifest)
    {
        var indices = bones.Select((bone, index) => new { bone, index }).ToDictionary(item => item.bone, item => item.index);
        var parents = bones.Select(bone => indices.TryGetValue(bone.parent, out var index) ? index : -1).ToArray();
        for (var i = 0; i < parents.Length; i++)
            if (parents[i] >= i) throw new InvalidOperationException("Bake transform order must place parents before children.");
        // A fixed 120Hz grid reproduces its keys exactly but can flatten a fast
        // curved sword path between them. Refine only intervals whose solved
        // midpoint cannot be represented accurately by final bone interpolation.
        // This is offline sampling; the exported player remains a plain clip.
        for (var pass = 0; ; pass++)
        {
            var ordered = times.ToArray();
            var probes = new SortedSet<float>(times);
            for (var i = 1; i < ordered.Length; i++) probes.Add((ordered[i - 1] + ordered[i]) * .5f);
            var captured = new Dictionary<float, Pose>();
            rig.ResetPlayback(); var previous = 0f;
            foreach (var time in probes)
            {
                rig.Sample(clip.name, 1, time, time - previous);
                captured.Add(time, Capture(bones, renderers, time)); previous = time;
            }
            var additions = new List<float>();
            for (var i = 1; i < ordered.Length; i++)
            {
                var middle = (ordered[i - 1] + ordered[i]) * .5f;
                if (NeedsRefinement(captured[ordered[i - 1]], captured[ordered[i]], captured[middle], parents)) additions.Add(middle);
            }
            manifest.bakeRefinements = pass;
            if (additions.Count == 0 || pass == MaxBakeRefinements)
            {
                manifest.unresolvedInterpolationIntervals = additions.Count;
                var result = ordered.Select(time => captured[time]).ToList();
                for (var frame = 1; frame < result.Count; frame++)
                    for (var bone = 0; bone < bones.Length; bone++)
                        result[frame].angle[bone] = result[frame - 1].angle[bone] +
                            Mathf.DeltaAngle(result[frame - 1].angle[bone], result[frame].angle[bone]);
                return result;
            }
            foreach (var time in additions) times.Add(time);
        }
    }
    static bool NeedsRefinement(Pose first, Pose last, Pose actual, int[] parents)
    {
        var matrices = new Matrix4x4[parents.Length];
        for (var i = 0; i < parents.Length; i++)
        {
            var position = Vector3.LerpUnclamped(first.position[i], last.position[i], .5f);
            var scale = Vector3.LerpUnclamped(first.scale[i], last.scale[i], .5f);
            var angle = Mathf.LerpAngle(first.angle[i], last.angle[i], .5f);
            if (Vector3.Distance(position, actual.position[i]) > .00075f ||
                Vector3.Distance(scale, actual.scale[i]) > .00025f || Mathf.Abs(Mathf.DeltaAngle(angle, actual.angle[i])) > .1f) return true;
            var local = Matrix4x4.TRS(position, Quaternion.Euler(0, 0, angle), scale);
            matrices[i] = parents[i] < 0 ? local : matrices[parents[i]] * local;
            if (Vector3.Distance(matrices[i].MultiplyPoint3x4(Vector3.zero), actual.world[i]) > .001f) return true;
        }
        return false;
    }

    static SortedSet<float> BakeTimes(AnimationClip clip)
    {
        var times = new SortedSet<float> { 0, clip.length };
        for (var i = 0; i <= Mathf.CeilToInt(clip.length * BakeRate); i++) times.Add(Mathf.Min(clip.length, i / (float)BakeRate));
        foreach (var marker in clip.events) times.Add(Mathf.Clamp(marker.time, 0, clip.length));
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            foreach (var key in AnimationUtility.GetObjectReferenceCurve(clip, binding))
            { times.Add(Mathf.Clamp(key.time, 0, clip.length)); times.Add(Mathf.Clamp(key.time - .00001f, 0, clip.length)); }
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            if (binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_SortingOrder")
                foreach (var key in AnimationUtility.GetEditorCurve(clip, binding).keys)
                { times.Add(Mathf.Clamp(key.time, 0, clip.length)); times.Add(Mathf.Clamp(key.time - .00001f, 0, clip.length)); }
        return times;
    }
    static AnimationCurve Linear(IEnumerable<Keyframe> values)
    {
        var keys = values.ToArray();
        if (keys.Length > 2 && keys.All(key => key.value == keys[0].value)) keys = new[] { keys[0], keys[keys.Length - 1] };
        var curve = new AnimationCurve(keys);
        for (var i = 0; i < curve.length; i++)
        { AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear); }
        return curve;
    }
    static AnimationClip WriteClip(string path, GameObject actor, Transform[] bones, SpriteRenderer[] renderers, List<Pose> poses, AnimationEvent[] events)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        clip.ClearCurves(); clip.name = "strike_study_baked"; clip.frameRate = BakeRate; clip.wrapMode = WrapMode.Once;
        for (var bone = 0; bone < bones.Length; bone++)
        {
            var index = bone; var binding = AnimationUtility.CalculateTransformPath(bones[bone], actor.transform);
            for (var axis = 0; axis < 3; axis++)
            {
                var component = axis;
                clip.SetCurve(binding, typeof(Transform), "m_LocalPosition." + "xyz"[axis], Linear(poses.Select(p => new Keyframe(p.time, p.position[index][component]))));
                clip.SetCurve(binding, typeof(Transform), "m_LocalScale." + "xyz"[axis], Linear(poses.Select(p => new Keyframe(p.time, p.scale[index][component]))));
            }
            clip.SetCurve(binding, typeof(Transform), "localEulerAnglesRaw.z", Linear(poses.Select(p => new Keyframe(p.time, p.angle[index]))));
        }
        for (var renderer = 0; renderer < renderers.Length; renderer++)
        {
            var keys = new List<ObjectReferenceKeyframe>(); var order = new List<Keyframe>();
            for (var i = 0; i < poses.Count; i++)
            {
                if (i == 0 || poses[i].sprites[renderer] != poses[i - 1].sprites[renderer])
                    keys.Add(new ObjectReferenceKeyframe { time = poses[i].time, value = poses[i].sprites[renderer] });
                if (i == 0 || poses[i].order[renderer] != poses[i - 1].order[renderer])
                    order.Add(new Keyframe(poses[i].time, poses[i].order[renderer], float.PositiveInfinity, float.PositiveInfinity));
            }
            var binding = AnimationUtility.CalculateTransformPath(renderers[renderer].transform, actor.transform);
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve(binding, typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
            clip.SetCurve(binding, typeof(SpriteRenderer), "m_SortingOrder", new AnimationCurve(order.ToArray()));
        }
        AnimationUtility.SetAnimationEvents(clip, events);
        EditorUtility.SetDirty(clip); AssetDatabase.SaveAssets();
        if (Mathf.Abs(clip.length - poses[poses.Count - 1].time) > .002f)
            throw new InvalidOperationException("A discrete baked key extended the study clip duration.");
        return clip;
    }

    static Comparison Compare(GameObject source, AnimationClip control, GameObject baked, SortedSet<float> bakeTimes, string folder)
    {
        var rig = source.GetComponent<KaelRig>(); var player = baked.GetComponent<KaelBakedClipPlayer>();
        var first = OrderedTransforms(source); var second = OrderedTransforms(baked);
        var a = OrderedRenderers(source); var b = OrderedRenderers(baked);
        var grip = second.Single(t => t.name == "GripAnchor"); var sword = second.Single(t => t.name == "Sword");
        if (first.Length != second.Length || a.Length != b.Length) throw new InvalidOperationException("Baked hierarchy/renderer count differs from source.");
        for (var i = 0; i < first.Length; i++)
            if (AnimationUtility.CalculateTransformPath(first[i], source.transform) != AnimationUtility.CalculateTransformPath(second[i], baked.transform))
                throw new InvalidOperationException("Baked transform path differs from source.");
        var times = new SortedSet<float>(bakeTimes);
        var ordered = bakeTimes.ToArray();
        for (var i = 1; i < ordered.Length; i++) times.Add((ordered[i - 1] + ordered[i]) * .5f);
        var result = new Comparison(); var csv = new StringBuilder("time,facing,localPositionError,rotationDegrees,scaleError,worldPositionError,spriteMismatches,sortingMismatches,positionBone,rotationBone,worldBone\n");
        foreach (var facing in new[] { 1, -1 })
        {
            source.transform.localScale = baked.transform.localScale = new Vector3(facing, 1, 1);
            rig.ResetPlayback(); player.ResetPlayback(); var previous = 0f;
            foreach (var time in times)
            {
                rig.Sample(control.name, 10, time, time - previous); player.Sample(time); previous = time;
                float position = 0, rotation = 0, scale = 0, world = 0;
                string positionBone = "", rotationBone = "", worldBone = "";
                for (var i = 0; i < first.Length; i++)
                {
                    var localError = Vector3.Distance(first[i].localPosition, second[i].localPosition);
                    var angleError = Quaternion.Angle(first[i].localRotation, second[i].localRotation);
                    var worldError = Vector3.Distance(first[i].position, second[i].position);
                    if (localError > position) { position = localError; positionBone = first[i].name; }
                    if (angleError > rotation) { rotation = angleError; rotationBone = first[i].name; }
                    scale = Mathf.Max(scale, Vector3.Distance(first[i].localScale, second[i].localScale));
                    if (worldError > world) { world = worldError; worldBone = first[i].name; }
                }
                var sprites = 0; var orders = 0;
                for (var i = 0; i < a.Length; i++) { if (a[i].sprite != b[i].sprite) sprites++; if (a[i].sortingOrder != b[i].sortingOrder) orders++; }
                result.samples++; result.spriteMismatches += sprites; result.sortingMismatches += orders;
                result.maxLocalPositionError = Mathf.Max(result.maxLocalPositionError, position);
                result.maxLocalRotationDegrees = Mathf.Max(result.maxLocalRotationDegrees, rotation);
                result.maxLocalScaleError = Mathf.Max(result.maxLocalScaleError, scale);
                result.maxWorldPositionError = Mathf.Max(result.maxWorldPositionError, world);
                result.maxGripGap = Mathf.Max(result.maxGripGap, Vector3.Distance(grip.position, sword.position));
                csv.Append(F(time)).Append(',').Append(facing).Append(',').Append(F(position)).Append(',').Append(F(rotation)).Append(',')
                    .Append(F(scale)).Append(',').Append(F(world)).Append(',').Append(sprites).Append(',').Append(orders)
                    .Append(',').Append(positionBone).Append(',').Append(rotationBone).Append(',').Append(worldBone).AppendLine();
            }
        }
        result.passed = result.maxLocalPositionError <= .005f && result.maxLocalRotationDegrees <= .5f &&
            result.maxLocalScaleError <= .001f && result.maxWorldPositionError <= .005f && result.maxGripGap <= .0001f &&
            result.spriteMismatches == 0 && result.sortingMismatches == 0;
        source.transform.localScale = baked.transform.localScale = Vector3.one;
        File.WriteAllText(Path.Combine(folder, "comparison.csv"), csv.ToString());
        return result;
    }

    static void RenderBaked(GameObject actor, Camera camera, string folder, Manifest manifest)
    {
        var player = actor.GetComponent<KaelBakedClipPlayer>(); player.ResetPlayback();
        var skins = actor.GetComponentsInChildren<SpriteSkin>(true);
        var transforms = actor.GetComponentsInChildren<Transform>(true);
        var bones = Observed.Select(name => transforms.Single(t => t.name == name)).ToArray();
        var frames = Path.Combine(folder, "frames"); Directory.CreateDirectory(frames);
        var csv = new StringBuilder("frame,time,clipTime");
        foreach (var name in Observed) csv.Append(',').Append(name).Append("X,").Append(name).Append("Y,").Append(name).Append("Z,").Append(name).Append("Angle");
        csv.AppendLine();
        camera.transform.SetPositionAndRotation(manifest.cameraPosition, Quaternion.identity);
        camera.orthographic = true; camera.orthographicSize = manifest.cameraOrthographicSize; camera.aspect = 1;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.13f, .17f, .23f, 1);
        camera.cullingMask = -1; camera.allowHDR = camera.allowMSAA = false;
        var target = new RenderTexture(Resolution, Resolution, 24, RenderTextureFormat.ARGB32);
        var readback = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
        var oldActive = RenderTexture.active; camera.targetTexture = target;
        try
        {
            var pixelAudit = Environment.GetCommandLineArgs().Contains("-kaelStudyPixelAudit")
                ? new HandPixelAudit(actor, camera, target, readback, "HandNear Art") : null;
            var freeHandAudit = pixelAudit != null ? new HandPixelAudit(actor, camera, target, readback, "HandFar Art") : null;
            manifest.pixelAuditEnabled = pixelAudit != null;
            if (pixelAudit != null) manifest.pixelAuditMethod = "Per frame for near and far hands: full-minus-no-hand RGB difference >3/255; isolated hand versus actual empty background >3/255. Ratio is contributing/potential pixels, is not clamped, and is blank for zero potential pixels. Border content uses the same threshold. Standard frames are saved before diagnostic renders; the full image is restored between hand measurements.";
            for (var frame = 0; frame <= Mathf.CeilToInt((manifest.clipDuration + manifest.endHold) * RenderRate); frame++)
            {
                var time = frame / (float)RenderRate; player.Sample(time);
                Event.current = new Event { type = EventType.Repaint };
                foreach (var skin in skins) skin.OnPreviewUpdate();
                ReadFrame(camera, target, readback);
                File.WriteAllBytes(Path.Combine(frames, frame.ToString("00000", CultureInfo.InvariantCulture) + ".png"), readback.EncodeToPNG());
                pixelAudit?.Measure(frame, time, player.CurrentTime);
                if (freeHandAudit != null)
                {
                    // Near-hand measurement leaves an isolated-hand readback.
                    // Restore the real full image without advancing the pose.
                    ReadFrame(camera, target, readback);
                    freeHandAudit.Measure(frame, time, player.CurrentTime);
                }
                csv.Append(frame).Append(',').Append(F(time)).Append(',').Append(F(player.CurrentTime));
                foreach (var bone in bones) csv.Append(',').Append(F(bone.position.x)).Append(',').Append(F(bone.position.y)).Append(',').Append(F(bone.position.z))
                    .Append(',').Append(F(Mathf.DeltaAngle(0, bone.localEulerAngles.z)));
                csv.AppendLine(); manifest.renderedFrames++;
            }
            if (pixelAudit != null) File.WriteAllText(Path.Combine(folder, "pixel-audit.csv"), pixelAudit.csv.ToString());
            if (freeHandAudit != null) File.WriteAllText(Path.Combine(folder, "pixel-audit-free-hand.csv"), freeHandAudit.csv.ToString());
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = oldActive;
            target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(readback);
        }
        File.WriteAllText(Path.Combine(folder, "frames.csv"), csv.ToString());
    }

    static void ReadFrame(Camera camera, RenderTexture target, Texture2D readback)
    {
        camera.Render(); RenderTexture.active = target;
        readback.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0); readback.Apply();
    }

    // Diagnostic renders never sample the clip or change any pose/camera value.
    // The one full-image buffer is reused for the entire optional audit.
    sealed class HandPixelAudit
    {
        const int ChannelThreshold = 3;
        readonly Camera camera;
        readonly RenderTexture target;
        readonly Texture2D readback;
        readonly Renderer[] renderers;
        readonly SpriteRenderer hand;
        readonly bool[] enabled;
        readonly Color32[] full = new Color32[Resolution * Resolution];
        readonly Color32 background;
        public readonly StringBuilder csv = new StringBuilder("frame,time,clipTime,handPotentialPixels,handContributingPixels,handVisibilityRatio,handRendererEnabled,contentBorderPixels,touchesFrameBorder\n");

        public HandPixelAudit(GameObject actor, Camera camera, RenderTexture target, Texture2D readback, string handDrawing)
        {
            this.camera = camera; this.target = target; this.readback = readback;
            renderers = actor.GetComponentsInChildren<Renderer>(true);
            hand = actor.GetComponentsInChildren<SpriteRenderer>(true).Single(r => r.name == handDrawing);
            enabled = new bool[renderers.Length];
            SaveVisibility();
            try
            {
                foreach (var renderer in renderers) renderer.enabled = false;
                ReadFrame(camera, target, readback);
                // Read the real GPU background to retain the exact color-space
                // conversion used by the ordinary 1024px render pathway.
                background = readback.GetRawTextureData<Color32>()[0];
            }
            finally { RestoreVisibility(); }
        }

        public void Measure(int frame, float time, float clipTime)
        {
            readback.GetRawTextureData<Color32>().CopyTo(full);
            var contributing = 0; var potential = 0; var border = 0;
            var handEnabled = hand.enabled;
            SaveVisibility();
            try
            {
                hand.enabled = false;
                ReadFrame(camera, target, readback);
                var withoutHand = readback.GetRawTextureData<Color32>();
                for (var i = 0; i < full.Length; i++)
                {
                    if (Different(full[i], withoutHand[i])) contributing++;
                    if ((i < Resolution || i >= full.Length - Resolution || i % Resolution == 0 || i % Resolution == Resolution - 1)
                        && Different(full[i], background)) border++;
                }
                foreach (var renderer in renderers) renderer.enabled = false;
                hand.enabled = true;
                ReadFrame(camera, target, readback);
                var isolated = readback.GetRawTextureData<Color32>();
                for (var i = 0; i < isolated.Length; i++) if (Different(isolated[i], background)) potential++;
            }
            finally { RestoreVisibility(); }
            csv.Append(frame).Append(',').Append(F(time)).Append(',').Append(F(clipTime)).Append(',')
                .Append(potential).Append(',').Append(contributing).Append(',')
                .Append(potential > 0 ? F(contributing / (float)potential) : "").Append(',')
                .Append(handEnabled ? 1 : 0).Append(',').Append(border).Append(',').Append(border > 0 ? 1 : 0).AppendLine();
        }

        void SaveVisibility()
        { for (var i = 0; i < renderers.Length; i++) enabled[i] = renderers[i].enabled; }
        void RestoreVisibility()
        { for (var i = 0; i < renderers.Length; i++) renderers[i].enabled = enabled[i]; }
        static bool Different(Color32 first, Color32 second)
            => Mathf.Abs(first.r - second.r) > ChannelThreshold || Mathf.Abs(first.g - second.g) > ChannelThreshold ||
                Mathf.Abs(first.b - second.b) > ChannelThreshold;
    }

    static void RequirePurePlayback(GameObject actor)
    {
        if (actor.GetComponentsInChildren<KaelRig>(true).Length != 0 || actor.GetComponentsInChildren<IKManager2D>(true).Length != 0 ||
            actor.GetComponentsInChildren<Solver2D>(true).Length != 0 || actor.GetComponentsInChildren<LineRenderer>(true).Length != 0)
            throw new InvalidOperationException("The baked study must not retain live rig/IK/trail components.");
        if (actor.GetComponentsInChildren<SpriteSkin>(true).Length == 0) throw new InvalidOperationException("The baked study lost its weighted sprite renderers.");
    }
    static string ValidateAssetFolder(string folder)
    {
        folder = folder.Replace('\\', '/').TrimEnd('/');
        var absolute = Path.GetFullPath(folder);
        var studyRoot = Path.GetFullPath("Assets/_Mythwake/ArtStudies") + Path.DirectorySeparatorChar;
        if (!absolute.StartsWith(studyRoot, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Bake assets must stay in Assets/_Mythwake/ArtStudies, outside live Resources.");
        var assetsRoot = Path.GetFullPath("Assets") + Path.DirectorySeparatorChar;
        return "Assets/" + absolute.Substring(assetsRoot.Length).Replace('\\', '/');
    }
    static void ProtectExisting(string manifestPath, params string[] paths)
    {
        var known = File.Exists(manifestPath) ? JsonUtility.FromJson<HashManifest>(File.ReadAllText(manifestPath)).files : Array.Empty<FileHash>();
        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;
            var prior = (known ?? Array.Empty<FileHash>()).FirstOrDefault(h => h.path == path);
            if (prior == null || prior.sha256 != Hash(path))
                throw new InvalidOperationException("Study export stopped: " + path + " has no matching generated fingerprint or was edited. Preserve the hand-edited asset before regenerating; it will not be overwritten.");
        }
    }
    static FileHash[] WriteHashes(string path, params string[] assets)
    {
        var files = assets.Select(asset => new FileHash { path = asset, sha256 = Hash(asset) }).ToArray();
        File.WriteAllText(path, JsonUtility.ToJson(new HashManifest { files = files }, true) + Environment.NewLine);
        return files;
    }
    static string Hash(string path)
    { using (var sha = SHA256.Create()) using (var input = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant(); }
    static string F(float value) => value.ToString("0.000000", CultureInfo.InvariantCulture);
    static void WriteManifest(string folder, Manifest value) => File.WriteAllText(Path.Combine(folder, "bake-manifest.json"), JsonUtility.ToJson(value, true) + Environment.NewLine);
}
