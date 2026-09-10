using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

/// <summary>
/// Inspection only: loads the current prefab into a temporary preview scene, updates its
/// actual SpriteSkins, and records body assembly evidence. Never rebuilds assets or opens/saves gameplay scenes.
/// </summary>
public static class KaelBodyAssemblyReview
{
    const string PrefabPath = "Assets/_Mythwake/Resources/Characters/Kael/Kael.prefab";
    const string LayoutPath = "Assets/_Mythwake/Resources/Characters/Kael/KaelAtlasLayout.json";
    const string AtlasPath = "Assets/_Mythwake/Resources/Characters/Kael/KaelAtlas.png";
    const int Size = 1024;

    [Serializable] sealed class ReviewManifest
    {
        public string prefab, unityVersion, utc, folder;
        public string landmarkSource = LayoutPath;
        public string landmarks = "Layout landmarks are sprite-local world units relative to the pivot. Renderer and rigid SpriteSkin bind transforms are reported independently. Endpoint gaps are measurements only, with no acceptance tolerance. Nonuniform skin weights are explicitly marked unsupported for exact landmark deformation.";
        public string captureStatus = "running", error;
        public string purpose = "Diagnostic renders and pose data only; no visual acceptance or gameplay evidence is implied.";
        public string method = "Fresh prefab instance per pose in a temporary Unity preview scene; authored poses use manual KaelRig.Sample; neutral poses are registered directly without sampling animation. All renders use SpriteSkin.OnPreviewUpdate then Camera.Render.";
        public string neutralAssembly = "Neutral front/side/rear use layout.joints with viewJoints overrides, zero transform rotations and actual available view sprites (explicit front fallback when a part has no requested variant). Armored near arm stays in front, far arm behind, coat tails below pelvis/belt, legs below torso. Head-only -15/+15 degree nods follow registration with torso stationary. No combat pose is relabelled as neutral.";
        public string matrices = "All 16-value matrices use row-major storage: index = row * 4 + column. Positions and bounds are Unity world units; sprite rect and pivot are pixels.";
        public string bindPose = "00-bind-pose is the instantiated prefab before any animation or IK sampling. Its SpriteSkin preview deformation is updated for rendering.";
        public string isolation = "No PlayerPrefs, gameplay scene open/save, asset rebuilding, AssetDatabase.Refresh or asset import. Only artifact PNG/JSON files are written.";
        public int imageWidth = Size, imageHeight = Size;
        public Vector3 cameraPosition = new Vector3(0, 2.25f, -10);
        public float orthographicSize = 3;
        public List<PoseEntry> poses = new List<PoseEntry>();
    }

    [Serializable] sealed class PoseEntry
    {
        public string image, data, clip;
        public float time;
        public bool unanimatedBindPose;
        public bool neutralAssembly;
        public string neutralView;
        public float headNodDegrees;
        public string transitionSequence, sourceClip;
        public float sourceTime, stepSeconds;
        public int transitionFrame, actorInstanceId;
    }

    [Serializable] sealed class PoseData
    {
        public string clip, prefab;
        public float requestedTime, sampledTime;
        public bool unanimatedBindPose;
        public bool neutralAssembly;
        public string neutralView;
        public float headNodDegrees;
        public string[] neutralRegistrationNotes;
        public string transitionSequence, sourceClip;
        public float sourceTime, stepSeconds;
        public int transitionFrame, actorInstanceId;
        public TransformData[] hierarchy;
        public RendererData[] renderers;
        public EndpointGap[] endpointGaps;
        public Vector3 rendererBoundsMin, rendererBoundsMax;
    }

    [Serializable] sealed class TransformData
    {
        public string path, parentPath, name;
        public bool activeSelf, activeInHierarchy;
        public int siblingIndex, layer;
        public Vector3 localPosition, localEulerAngles, localScale, worldPosition, worldEulerAngles, lossyScale;
        public float[] localToWorldMatrix;
    }

    [Serializable] sealed class RendererData
    {
        public string path, spriteName, spriteAsset, materialName, shaderName, sortingLayer, rootBonePath;
        public string layoutBone, layoutVariant, landmarkStatus;
        public bool enabled, activeInHierarchy, forceRenderingOff, flipX, flipY;
        public bool hasSpriteSkin, skinEnabled, sourceAlwaysUpdate, previewUpdateCalled, hasCurrentDeformedVertices;
        public int sortingOrder, sortingLayerId, layer;
        public Color color;
        public Rect spriteRectPixels;
        public Vector2 spritePivotPixels, spritePivotNormalized, spriteSizeWorldUnits;
        public float pixelsPerUnit;
        public Vector3 spriteBoundsCenter, spriteBoundsSize, rendererBoundsMin, rendererBoundsMax;
        public float[] localToWorldMatrix;
        public Vector2[] undeformedSpriteVertices;
        public MatrixData[] spriteBindPoses;
        public BoneData[] bones;
        public LandmarkData[] landmarks;
    }

    [Serializable] sealed class AtlasLayout { public LayoutPart[] parts; public LayoutJoint[] joints, viewJoints; }
    [Serializable] sealed class LayoutJoint { public string name, view; public float x, y; }
    [Serializable] sealed class LayoutPart { public string name, bone, variant; public LayoutLandmark[] landmarks; }
    [Serializable] sealed class LayoutLandmark { public string name; public float x, y; }
    [Serializable] sealed class LandmarkData
    {
        public string name, skinTransformMethod, skinTransformIssue;
        public Vector3 spriteLocal, rendererWorld, skinnedWorld;
        public bool skinnedWorldAvailable;
        public float skinWeightSum;
    }
    [Serializable] sealed class EndpointGap
    {
        public string name, attachmentA, landmarkA, attachmentB, landmarkB, status;
        public bool optional, rendererMeasurementAvailable, skinnedMeasurementAvailable;
        public string rendererA, rendererB, spriteA, spriteB, authoredLandmarkA, authoredLandmarkB;
        public Vector3 rendererEndpointA, rendererEndpointB, rendererDelta, skinnedEndpointA, skinnedEndpointB, skinnedDelta;
        public float rendererDistanceWorld, skinnedDistanceWorld, rendererDistancePixels, skinnedDistancePixels;
    }

    [Serializable] sealed class MatrixData { public float[] values; }
    [Serializable] sealed class BoneData
    {
        public int index;
        public string path, name;
        public bool missing;
        public Vector3 worldPosition, localPosition, localScale;
        public float[] localToWorldMatrix;
    }

    [MenuItem("Mythwake/Kael/Inspect Current Body Assembly")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Body assembly inspection requires Edit Mode.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) throw new FileNotFoundException("Current Kael prefab is missing; this inspector does not rebuild it.", PrefabPath);
        var suffix = Argument("-kaelBodyReviewFolder") ?? "current";
        if (suffix == "." || suffix == ".." || string.IsNullOrWhiteSpace(suffix) ||
            suffix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || suffix.Contains("/") || suffix.Contains("\\"))
            throw new ArgumentException("-kaelBodyReviewFolder accepts a single folder name, for example before-fix or current.");
        var folder = Path.GetFullPath(Path.Combine("artifacts/kael/body-fix-review", suffix));
        Directory.CreateDirectory(folder);
        var manifest = new ReviewManifest { prefab = PrefabPath, unityVersion = Application.unityVersion,
            utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), folder = folder };
        var layout = File.Exists(LayoutPath) ? JsonUtility.FromJson<AtlasLayout>(File.ReadAllText(LayoutPath)) : null;
        var preview = new PreviewRenderUtility();
        var camera = preview.camera;
        camera.transform.position = manifest.cameraPosition;
        camera.transform.rotation = Quaternion.identity;
        camera.orthographic = true; camera.orthographicSize = manifest.orthographicSize;
        camera.aspect = 1;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.13f, .17f, .23f, 1);
        camera.cullingMask = -1; // The isolated preview scene contains only this actor and the utility's camera/lights.
        camera.allowHDR = false; camera.allowMSAA = false;
        var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        var oldActive = RenderTexture.active;
        var oldEvent = Event.current;
        camera.targetTexture = target;
        try
        {
            File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonUtility.ToJson(manifest, true));
            Capture(prefab, camera, target, folder, manifest, layout, "bind-pose", 0, true);
            var clips = new[] { "idle", "idle", "run", "attack_cross", "attack_cross", "attack_cross", "attack_cross",
                "attack_spin", "attack_spin", "attack_spin", "attack_jump", "attack_jump", "skill", "skill", "skill",
                "skill", "skill", "skill", "hit", "death" };
            var times = new[] { 0f, 2f, .16f, .16f, .24f, .45f, .52f, .22f, .36f, .42f, .27f, .46f,
                .18f, .28f, .40f, .69f, .92f, 1.32f, .05f, .90f };
            for (var i = 0; i < clips.Length; i++) Capture(prefab, camera, target, folder, manifest, layout, clips[i], times[i], false);
            foreach (var view in new[] { "front", "side", "rear" })
            {
                Capture(prefab, camera, target, folder, manifest, layout, "neutral-" + view, 0, false, view);
                Capture(prefab, camera, target, folder, manifest, layout, "neutral-" + view + "-head-nod-minus15", 0, false, view, -15);
                Capture(prefab, camera, target, folder, manifest, layout, "neutral-" + view + "-head-nod-plus15", 0, false, view, 15);
            }
            manifest.captureStatus = "complete";
            File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonUtility.ToJson(manifest, true));
            Debug.Log("KAEL_BODY_ASSEMBLY_REVIEW_OK: " + folder + "; " + manifest.poses.Count +
                " actual 1024x1024 renders with hierarchy/sprite/bone JSON. Diagnostic evidence only; no visual approval asserted.");
            CaptureTransitions(prefab, camera, target, folder + "-transitions", layout);
        }
        catch (Exception ex)
        {
            if (manifest.captureStatus != "complete")
            {
                manifest.captureStatus = "failed"; manifest.error = ex.ToString();
                File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonUtility.ToJson(manifest, true));
            }
            throw;
        }
        finally
        {
            Event.current = oldEvent; RenderTexture.active = oldActive;
            camera.targetTexture = null;
            target.Release(); UnityEngine.Object.DestroyImmediate(target);
            preview.Cleanup();
        }
    }

    static void Capture(GameObject prefab, Camera camera, RenderTexture target, string folder,
        ReviewManifest manifest, AtlasLayout layout, string clip, float time, bool bindPose, string neutralView = null, float headNod = 0)
    {
        // Instantiate directly in the utility's preview scene; never add objects to a user's gameplay scene.
        var actor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, camera.gameObject.scene);
        try
        {
            actor.hideFlags = HideFlags.HideAndDontSave;
            actor.SetActive(true);
            var rig = actor.GetComponent<KaelRig>();
            if (rig == null) throw new InvalidOperationException("Current prefab has no KaelRig.");
            foreach (var line in actor.GetComponentsInChildren<LineRenderer>(true)) line.enabled = false;
            var animator = actor.GetComponent<Animator>();
            var registrationNotes = Array.Empty<string>();
            if (bindPose || neutralView != null)
            {
                if (animator != null) animator.enabled = false;
                if (rig.ik != null) rig.ik.enabled = false;
                if (neutralView != null) registrationNotes = RegisterNeutralAssembly(actor, layout, neutralView, headNod);
            }
            else rig.Sample(clip, manifest.poses.Count + 1L, time, 1f);

            var stem = manifest.poses.Count.ToString("00", CultureInfo.InvariantCulture) + "-" + clip +
                (bindPose || neutralView != null ? "" : "-" + time.ToString("0.00", CultureInfo.InvariantCulture));
            var data = CaptureActorPose(actor, camera, target, folder, stem, layout, clip, time, bindPose);
            data.neutralAssembly = neutralView != null; data.neutralView = neutralView;
            data.headNodDegrees = headNod; data.neutralRegistrationNotes = registrationNotes;
            File.WriteAllText(Path.Combine(folder, stem + ".json"), JsonUtility.ToJson(data, true));
            manifest.poses.Add(new PoseEntry { image = stem + ".png", data = stem + ".json", clip = clip,
                time = time, unanimatedBindPose = bindPose, neutralAssembly = neutralView != null,
                neutralView = neutralView, headNodDegrees = headNod });
        }
        finally
        {
            if (actor != null)
            {
                var rig = actor.GetComponent<KaelRig>();
                if (rig != null) rig.ResetPlayback();
                UnityEngine.Object.DestroyImmediate(actor);
            }
        }
    }

    static PoseData CaptureActorPose(GameObject actor, Camera camera, RenderTexture target, string folder,
        string stem, AtlasLayout layout, string clip, float time, bool bindPose)
    {
        Texture2D png = null;
        var sourceAlwaysUpdate = new Dictionary<SpriteSkin, bool>();
        try
        {
            Event.current = new Event { type = EventType.Repaint };
            foreach (var skin in actor.GetComponentsInChildren<SpriteSkin>(true))
            {
                sourceAlwaysUpdate[skin] = skin.alwaysUpdate;
                // Preview deformation must run even before the camera has established visibility.
                skin.alwaysUpdate = true;
                if (skin.isActiveAndEnabled) skin.OnPreviewUpdate();
            }
            camera.Render();
            RenderTexture.active = target;
            png = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            png.ReadPixels(new Rect(0, 0, Size, Size), 0, 0); png.Apply();
            File.WriteAllBytes(Path.Combine(folder, stem + ".png"), png.EncodeToPNG());
            var data = CollectPose(actor, clip, time, bindPose, sourceAlwaysUpdate, layout);
            data.actorInstanceId = actor.GetInstanceID();
            return data;
        }
        finally
        {
            foreach (var entry in sourceAlwaysUpdate) if (entry.Key != null) entry.Key.alwaysUpdate = entry.Value;
            if (png != null) UnityEngine.Object.DestroyImmediate(png);
        }
    }

    static void CaptureTransitions(GameObject prefab, Camera camera, RenderTexture target, string folder, AtlasLayout layout)
    {
        Directory.CreateDirectory(folder);
        var manifest = new ReviewManifest {
            prefab = PrefabPath, unityVersion = Application.unityVersion, folder = folder,
            utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            method = "Four independent sequences, one continuously sampled prefab instance per sequence. " +
                "Capture source pose, then change action/clip without ResetPlayback and call actual KaelRig.Sample " +
                "for six successive frames with delta=1/60 and target age=frame/60. " +
                "No manual blend, pose correction, neutral registration or renderer substitution. " +
                "Every frame updates actual SpriteSkins and uses Camera.Render.",
            bindPose = "No bind snapshot in this folder. Each sequence starts with its actual source clip pose.",
            neutralAssembly = "No neutral poses. These captures test actual runtime mixer transitions on the same instance."
        };
        var sources = new[] { "idle", "idle", "attack_spin", "skill" };
        var sourceTimes = new[] { 0f, 0f, .28f, .69f };
        var destinations = new[] { "run", "attack_cross", "death", "death" };
        const float step = 1f / 60;
        File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonUtility.ToJson(manifest, true));
        try
        {
            for (var sequenceIndex = 0; sequenceIndex < sources.Length; sequenceIndex++)
            {
                var actor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, camera.gameObject.scene);
                try
                {
                    actor.hideFlags = HideFlags.HideAndDontSave;
                    actor.SetActive(true);
                    foreach (var line in actor.GetComponentsInChildren<LineRenderer>(true)) line.enabled = false;
                    var rig = actor.GetComponent<KaelRig>();
                    if (rig == null) throw new InvalidOperationException("Current prefab has no KaelRig.");
                    var sequence = sources[sequenceIndex] + "-to-" + destinations[sequenceIndex];
                    var action = 1000L + sequenceIndex * 2;
                    rig.Sample(sources[sequenceIndex], action, sourceTimes[sequenceIndex], step);
                    for (var frame = 0; frame <= 6; frame++)
                    {
                        var clip = frame == 0 ? sources[sequenceIndex] : destinations[sequenceIndex];
                        var time = frame == 0 ? sourceTimes[sequenceIndex] : frame * step;
                        if (frame > 0) rig.Sample(clip, action + 1, time, step);
                        var stem = sequenceIndex.ToString("00", CultureInfo.InvariantCulture) + "-" + sequence +
                            (frame == 0 ? "-source" : "-frame" + frame.ToString("00", CultureInfo.InvariantCulture));
                        var data = CaptureActorPose(actor, camera, target, folder, stem, layout, clip, time, false);
                        data.transitionSequence = sequence; data.sourceClip = sources[sequenceIndex];
                        data.sourceTime = sourceTimes[sequenceIndex]; data.stepSeconds = step; data.transitionFrame = frame;
                        File.WriteAllText(Path.Combine(folder, stem + ".json"), JsonUtility.ToJson(data, true));
                        manifest.poses.Add(new PoseEntry { image = stem + ".png", data = stem + ".json", clip = clip,
                            time = time, transitionSequence = sequence, sourceClip = sources[sequenceIndex],
                            sourceTime = sourceTimes[sequenceIndex], stepSeconds = step, transitionFrame = frame,
                            actorInstanceId = actor.GetInstanceID() });
                    }
                }
                finally
                {
                    if (actor != null)
                    {
                        var rig = actor.GetComponent<KaelRig>();
                        if (rig != null) rig.ResetPlayback();
                        UnityEngine.Object.DestroyImmediate(actor);
                    }
                }
            }
            manifest.captureStatus = "complete";
            File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonUtility.ToJson(manifest, true));
            Debug.Log("KAEL_BODY_TRANSITION_REVIEW_OK: " + folder + "; " + manifest.poses.Count +
                " source/transition snapshots. Measured mixer evidence only; no visual approval asserted.");
        }
        catch (Exception ex)
        {
            manifest.captureStatus = "failed"; manifest.error = ex.ToString();
            File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonUtility.ToJson(manifest, true));
            throw;
        }
    }

    static string[] RegisterNeutralAssembly(GameObject actor, AtlasLayout layout, string view, float headNod)
    {
        if (layout?.joints == null || layout.joints.Length == 0 || layout.parts == null)
            throw new InvalidOperationException("A neutral assembly requires the actual layout joints and parts.");
        var notes = new List<string> { "No animation, IK evaluation, inferred fighting stance or joint-gap correction applied." };
        var transforms = actor.GetComponentsInChildren<Transform>(true);
        // Use explicit measured standing coordinates, never an idle/fight clip pose.
        foreach (var node in transforms) node.localRotation = Quaternion.identity;
        var measured = new Dictionary<string, Vector3>(StringComparer.Ordinal);
        foreach (var joint in layout.joints) measured[joint.name] = new Vector3(joint.x, joint.y, 0);
        foreach (var joint in layout.viewJoints ?? Array.Empty<LayoutJoint>())
            if (joint.view == view) measured[joint.name] = new Vector3(joint.x, joint.y, 0);
        // Parent-before-child traversal keeps each registered world coordinate intact.
        foreach (var node in transforms)
            if (measured.TryGetValue(node.name, out var world)) node.position = world;

        var sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToDictionary(s => s.name, StringComparer.Ordinal);
        var order = new Dictionary<string, int>(StringComparer.Ordinal) {
            {"ScarfShort",0}, {"ScarfLong",1}, {"UpperArmFar",5}, {"ForearmFar",6}, {"HandFar",7},
            {"ThighFar",10}, {"ShinFar",11}, {"FootFar",12}, {"ThighNear",20}, {"ShinNear",21}, {"FootNear",22},
            {"TorsoBack",27}, {"Hair",28}, {"Head",29}, {"Torso",30}, {"Pelvis",32}, {"Collar",40},
            {"UpperArmNear",50}, {"ForearmNear",51}, {"PauldronNear",52}, {"Sword",55}, {"HandNear",56}
        };
        if (view == "rear")
        {
            foreach (var limb in new[] { "Thigh", "Shin", "Foot" })
            { order[limb + "Near"] -= 10; order[limb + "Far"] += 10; }
            order["ScarfLong"] = order["ScarfShort"] = 31;
            order["Sword"] = 4;
        }
        foreach (var renderer in actor.GetComponentsInChildren<SpriteRenderer>(true))
        {
            var original = renderer.sprite == null ? null : Array.Find(layout.parts, p => p.name == renderer.sprite.name);
            if (original == null)
            {
                notes.Add("No layout part matches renderer " + NodePath(renderer.transform, actor.transform) + "; source drawing/order retained.");
                continue;
            }
            var bone = original.bone;
            var part = Array.Find(layout.parts, p => p.bone == bone && p.variant == view);
            if (part == null)
            {
                part = Array.Find(layout.parts, p => p.bone == bone && (string.IsNullOrEmpty(p.variant) || p.variant == "front"));
                notes.Add(bone + ": no " + view + " drawing; authored front variant used.");
            }
            if (part == null || !sprites.TryGetValue(part.name, out var sprite))
                throw new InvalidOperationException("Neutral " + view + " is missing an imported drawing for " + bone + ".");
            renderer.sprite = sprite;
            if (order.TryGetValue(bone, out var sortingOrder)) renderer.sortingOrder = sortingOrder;
        }
        var head = Array.Find(transforms, t => t.name == "Head");
        var torso = Array.Find(transforms, t => t.name == "Torso");
        if (head == null || torso == null) throw new InvalidOperationException("Neutral head-nod review requires Head and Torso transforms.");
        var torsoBefore = torso.localToWorldMatrix;
        head.localRotation = Quaternion.Euler(0, 0, headNod);
        // This is a measured setup description, not a visual-acceptance assertion.
        var torsoAfter = torso.localToWorldMatrix;
        var torsoDelta = 0f;
        for (var row = 0; row < 4; row++) for (var column = 0; column < 4; column++)
            torsoDelta = Mathf.Max(torsoDelta, Mathf.Abs(torsoBefore[row, column] - torsoAfter[row, column]));
        notes.Add("Head local Z nod=" + headNod.ToString("0.00", CultureInfo.InvariantCulture) +
            " degrees; maximum torso matrix change=" + torsoDelta.ToString("R", CultureInfo.InvariantCulture) + ".");
        return notes.ToArray();
    }

    static PoseData CollectPose(GameObject actor, string clip, float time, bool bindPose, Dictionary<SpriteSkin, bool> originalUpdate, AtlasLayout layout)
    {
        var root = actor.transform;
        var renderers = actor.GetComponentsInChildren<SpriteRenderer>(true);
        var visible = renderers.Where(r => r.enabled && r.gameObject.activeInHierarchy && !r.forceRenderingOff && r.sprite != null).ToArray();
        var bounds = visible.Length > 0 ? visible[0].bounds : new Bounds();
        foreach (var renderer in visible) bounds.Encapsulate(renderer.bounds);
        var result = new PoseData {
            prefab = PrefabPath, clip = clip, requestedTime = time, sampledTime = bindPose ? 0 : actor.GetComponent<KaelRig>().CurrentTime,
            unanimatedBindPose = bindPose, rendererBoundsMin = bounds.min, rendererBoundsMax = bounds.max,
            hierarchy = actor.GetComponentsInChildren<Transform>(true).Select(t => new TransformData {
                path = NodePath(t, root), parentPath = t == root ? "" : NodePath(t.parent, root), name = t.name,
                activeSelf = t.gameObject.activeSelf, activeInHierarchy = t.gameObject.activeInHierarchy,
                siblingIndex = t.GetSiblingIndex(), layer = t.gameObject.layer,
                localPosition = t.localPosition, localEulerAngles = t.localEulerAngles, localScale = t.localScale,
                worldPosition = t.position, worldEulerAngles = t.eulerAngles, lossyScale = t.lossyScale,
                localToWorldMatrix = Matrix(t.localToWorldMatrix) }).ToArray(),
            renderers = renderers.Select(r => CollectRenderer(r, root, originalUpdate, layout)).ToArray()
        };
        result.endpointGaps = CollectEndpointGaps(result.renderers);
        return result;
    }

    static RendererData CollectRenderer(SpriteRenderer renderer, Transform root, Dictionary<SpriteSkin, bool> originalUpdate, AtlasLayout layout)
    {
        var sprite = renderer.sprite;
        var skin = renderer.GetComponent<SpriteSkin>();
        var result = new RendererData {
            path = NodePath(renderer.transform, root), enabled = renderer.enabled, activeInHierarchy = renderer.gameObject.activeInHierarchy,
            forceRenderingOff = renderer.forceRenderingOff, flipX = renderer.flipX, flipY = renderer.flipY, color = renderer.color,
            sortingOrder = renderer.sortingOrder, sortingLayer = renderer.sortingLayerName, sortingLayerId = renderer.sortingLayerID,
            layer = renderer.gameObject.layer, materialName = renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "",
            shaderName = renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null ? renderer.sharedMaterial.shader.name : "",
            localToWorldMatrix = Matrix(renderer.transform.localToWorldMatrix), rendererBoundsMin = renderer.bounds.min, rendererBoundsMax = renderer.bounds.max,
            hasSpriteSkin = skin != null, skinEnabled = skin != null && skin.enabled,
            sourceAlwaysUpdate = skin != null && originalUpdate.TryGetValue(skin, out var original) && original,
            previewUpdateCalled = skin != null && skin.isActiveAndEnabled,
            hasCurrentDeformedVertices = skin != null && skin.HasCurrentDeformedVertices(),
            rootBonePath = skin != null && skin.rootBone != null ? NodePath(skin.rootBone, root) : "",
            bones = skin == null || skin.boneTransforms == null ? Array.Empty<BoneData>() : skin.boneTransforms.Select((bone, i) =>
                bone == null ? new BoneData { index = i, missing = true } : new BoneData { index = i, name = bone.name,
                    path = NodePath(bone, root), worldPosition = bone.position, localPosition = bone.localPosition,
                    localScale = bone.localScale, localToWorldMatrix = Matrix(bone.localToWorldMatrix) }).ToArray()
        };
        if (sprite != null)
        {
            result.spriteName = sprite.name; result.spriteAsset = AssetDatabase.GetAssetPath(sprite);
            result.spriteRectPixels = sprite.rect; result.spritePivotPixels = sprite.pivot;
            result.spritePivotNormalized = new Vector2(sprite.pivot.x / Mathf.Max(1, sprite.rect.width), sprite.pivot.y / Mathf.Max(1, sprite.rect.height));
            result.pixelsPerUnit = sprite.pixelsPerUnit; result.spriteSizeWorldUnits = sprite.rect.size / sprite.pixelsPerUnit;
            result.spriteBoundsCenter = sprite.bounds.center; result.spriteBoundsSize = sprite.bounds.size;
            result.undeformedSpriteVertices = sprite.vertices;
            result.spriteBindPoses = sprite.GetBindPoses().Select(m => new MatrixData { values = Matrix(m) }).ToArray();
            var candidates = layout?.parts?.Where(p => p.name == sprite.name).ToArray() ?? Array.Empty<LayoutPart>();
            result.landmarkStatus = candidates.Length == 0 ? "No layout entry for the exact sampled sprite name." :
                candidates.Length > 1 ? "Duplicate layout entries for this sprite name; no endpoint is selected." : "Layout entry matched by exact sampled sprite name.";
            if (candidates.Length == 1)
            {
                var part = candidates[0]; result.layoutBone = part.bone; result.layoutVariant = part.variant;
                result.landmarks = (part.landmarks ?? Array.Empty<LayoutLandmark>()).Select(p => TransformLandmark(renderer, skin, p)).ToArray();
                if (result.landmarks.Length == 0) result.landmarkStatus = "Matched sprite has no authored landmarks.";
            }
        }
        return result;
    }

    static LandmarkData TransformLandmark(SpriteRenderer renderer, SpriteSkin skin, LayoutLandmark point)
    {
        var local = new Vector3(point.x, point.y, 0);
        var data = new LandmarkData { name = point.name, spriteLocal = local,
            rendererWorld = renderer.transform.TransformPoint(FlipLocal(renderer, local)) };
        if (skin == null || !skin.isActiveAndEnabled)
        {
            data.skinnedWorldAvailable = true; data.skinnedWorld = data.rendererWorld;
            data.skinTransformMethod = "Renderer transform; no active SpriteSkin.";
            return data;
        }
        var sprite = renderer.sprite;
        if (!sprite.HasVertexAttribute(VertexAttribute.BlendWeight))
        { data.skinTransformIssue = "Active SpriteSkin has no blend weights."; return data; }
        var weights = sprite.GetVertexAttribute<BoneWeight>(VertexAttribute.BlendWeight);
        if (weights.Length == 0) { data.skinTransformIssue = "Sprite has no mesh vertices/weights."; return data; }
        var first = weights[0];
        for (var i = 1; i < weights.Length; i++)
            if (!SameWeight(first, weights[i]))
            {
                data.skinTransformMethod = "Renderer-space measurement only.";
                data.skinTransformIssue = "Nonuniform vertex weights require landmark-specific interpolation; no exact deformed endpoint is claimed.";
                return data;
            }
        var bindPoses = sprite.GetBindPoses();
        var bones = skin.boneTransforms;
        var indices = new[] { first.boneIndex0, first.boneIndex1, first.boneIndex2, first.boneIndex3 };
        var values = new[] { first.weight0, first.weight1, first.weight2, first.weight3 };
        var world = Vector3.zero;
        for (var i = 0; i < indices.Length; i++)
        {
            var weight = values[i]; if (weight == 0) continue;
            var index = indices[i];
            if (float.IsNaN(weight) || float.IsInfinity(weight) || bones == null || index < 0 ||
                index >= bones.Length || index >= bindPoses.Length || bones[index] == null)
            { data.skinTransformIssue = "Invalid bone reference, bind pose or weight; no skinned endpoint is claimed."; return data; }
            world += (bones[index].localToWorldMatrix * bindPoses[index]).MultiplyPoint3x4(local) * weight;
            data.skinWeightSum += weight;
        }
        // SpriteRenderer flip is applied in renderer-local space after the skin deformation.
        data.skinnedWorld = renderer.transform.TransformPoint(FlipLocal(renderer, renderer.transform.InverseTransformPoint(world)));
        data.skinnedWorldAvailable = true;
        data.skinTransformMethod = "Uniform mesh weights: sum(weight * bone.localToWorld * sprite.bindPose * landmark), then renderer flip. Weights are not normalized by this diagnostic.";
        return data;
    }

    static Vector3 FlipLocal(SpriteRenderer renderer, Vector3 value) =>
        new Vector3(renderer.flipX ? -value.x : value.x, renderer.flipY ? -value.y : value.y, value.z);

    static bool SameWeight(BoneWeight a, BoneWeight b) => a.boneIndex0 == b.boneIndex0 && a.boneIndex1 == b.boneIndex1 &&
        a.boneIndex2 == b.boneIndex2 && a.boneIndex3 == b.boneIndex3 && a.weight0 == b.weight0 && a.weight1 == b.weight1 &&
        a.weight2 == b.weight2 && a.weight3 == b.weight3;

    static EndpointGap[] CollectEndpointGaps(RendererData[] renderers)
    {
        var gaps = new List<EndpointGap> { MeasureGap(renderers, "neck", "Head", "neck", "Torso", "neck") };
        foreach (var side in new[] { "Near", "Far" })
        {
            gaps.Add(MeasureGap(renderers, "shoulder" + side, "UpperArm" + side, "proximal", "Torso", "shoulder" + side));
            gaps.Add(MeasureGap(renderers, "elbow" + side, "UpperArm" + side, "distal", "Forearm" + side, "proximal"));
            gaps.Add(MeasureGap(renderers, "wrist" + side, "Forearm" + side, "distal", "Hand" + side, "proximal"));
            gaps.Add(MeasureGap(renderers, "hip" + side, "Pelvis", "hip" + side, "Thigh" + side, "proximal", true));
            gaps.Add(MeasureGap(renderers, "knee" + side, "Thigh" + side, "distal", "Shin" + side, "proximal"));
            gaps.Add(MeasureGap(renderers, "ankle" + side, "Shin" + side, "distal", "Foot" + side, "proximal"));
        }
        gaps.Add(MeasureGap(renderers, "swordGrip", "HandNear", "grip", "Sword", "attach", true));
        return gaps.ToArray();
    }

    static EndpointGap MeasureGap(RendererData[] renderers, string name, string attachmentA, string landmarkA,
        string attachmentB, string landmarkB, bool optional = false)
    {
        var gap = new EndpointGap { name = name, attachmentA = attachmentA, landmarkA = landmarkA,
            attachmentB = attachmentB, landmarkB = landmarkB, optional = optional };
        var first = FindLandmark(renderers, attachmentA, landmarkA, out var rendererA, out var issueA);
        var second = FindLandmark(renderers, attachmentB, landmarkB, out var rendererB, out var issueB);
        gap.rendererA = rendererA?.path; gap.rendererB = rendererB?.path;
        gap.spriteA = rendererA?.spriteName; gap.spriteB = rendererB?.spriteName;
        if (first == null || second == null)
        { gap.status = "Unavailable: " + issueA + " " + issueB; return gap; }
        gap.authoredLandmarkA = first.name; gap.authoredLandmarkB = second.name;
        gap.rendererMeasurementAvailable = true;
        gap.rendererEndpointA = first.rendererWorld; gap.rendererEndpointB = second.rendererWorld;
        gap.rendererDelta = second.rendererWorld - first.rendererWorld;
        gap.rendererDistanceWorld = gap.rendererDelta.magnitude;
        gap.rendererDistancePixels = new Vector2(gap.rendererDelta.x, gap.rendererDelta.y).magnitude * (Size / 6f);
        gap.skinnedMeasurementAvailable = first.skinnedWorldAvailable && second.skinnedWorldAvailable;
        if (gap.skinnedMeasurementAvailable)
        {
            gap.skinnedEndpointA = first.skinnedWorld; gap.skinnedEndpointB = second.skinnedWorld;
            gap.skinnedDelta = second.skinnedWorld - first.skinnedWorld;
            gap.skinnedDistanceWorld = gap.skinnedDelta.magnitude;
            gap.skinnedDistancePixels = new Vector2(gap.skinnedDelta.x, gap.skinnedDelta.y).magnitude * (Size / 6f);
            gap.status = "Renderer and skin-bind endpoint measurements available. No acceptance threshold applied.";
        }
        else gap.status = "Renderer-space measurement available; exact skin measurement unavailable: " +
            first.skinTransformIssue + " " + second.skinTransformIssue;
        return gap;
    }

    static LandmarkData FindLandmark(RendererData[] renderers, string attachment, string landmark,
        out RendererData renderer, out string issue)
    {
        renderer = null; issue = "";
        var candidates = renderers.Where(r => r.layoutBone == attachment && r.enabled && r.activeInHierarchy &&
            !r.forceRenderingOff && r.color.a > 0).ToArray();
        if (candidates.Length != 1)
        { issue = attachment + " has " + candidates.Length + " visible exact-layout renderers."; return null; }
        renderer = candidates[0];
        var matches = renderer.landmarks?.Where(p => p.name == landmark).ToArray() ?? Array.Empty<LandmarkData>();
        // Current authored layout calls limb proximal/distal points attach/end;
        // the head's attachment point is its neck. Keep the actual authored name in each result.
        var alias = landmark == "proximal" ? "attach" : landmark == "distal" ? "end" :
            attachment == "Head" && landmark == "neck" ? "attach" : null;
        if (matches.Length == 0 && alias != null)
            matches = renderer.landmarks?.Where(p => p.name == alias).ToArray() ?? Array.Empty<LandmarkData>();
        if (matches.Length != 1)
        { issue = attachment + "/" + landmark + " has " + matches.Length + " authored matches in " + renderer.spriteName + "."; return null; }
        return matches[0];
    }

    static float[] Matrix(Matrix4x4 value)
    {
        var values = new float[16];
        for (var row = 0; row < 4; row++) for (var column = 0; column < 4; column++) values[row * 4 + column] = value[row, column];
        return values;
    }

    static string NodePath(Transform node, Transform root) => node == root ? "." : AnimationUtility.CalculateTransformPath(node, root);

    static string Argument(string option)
    {
        var args = Environment.GetCommandLineArgs(); var index = Array.IndexOf(args, option);
        return index < 0 ? null : index + 1 < args.Length ? args[index + 1] : throw new ArgumentException(option + " requires a folder name.");
    }
}
