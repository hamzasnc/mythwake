using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

/// <summary>
/// Continuous, clock-driven inspection of the shipping rig. Each scenario keeps one live
/// actor and samples every intermediate pose; it never assembles motion from still images.
/// This is neutral rig evidence, deliberately separate from real Canvas/gameplay acceptance.
/// </summary>
public static class KaelMotionContinuityReview
{
    const string PrefabPath = "Assets/_Mythwake/Resources/Characters/Kael/Kael.prefab";
    const int FrameRate = 60;
    const float PoseTolerance = .000025f;
    static readonly string[] ObservedBones = { "Root", "Hip", "Torso", "Head", "UpperArmNear", "ForearmNear",
        "HandNear", "HandFar", "ThighNear", "ShinNear", "FootNear", "ThighFar", "ShinFar", "FootFar", "Sword", "WeaponTip" };

    [Serializable] sealed class Manifest
    {
        public string status = "running", unityVersion, utc, prefab = PrefabPath, captureMode, folder;
        public string method = "A persistent prefab instance per scenario in an isolated PreviewRenderUtility scene. " +
            "KaelRig.Sample advances from a fixed 60 Hz wall clock at 1x/2x; SpriteSkin.OnPreviewUpdate updates actual meshes. " +
            "Every saved PNG is a fresh Camera.Render. No scene, PlayerPrefs, assets or combat outcomes are changed.";
        public string limits = "Technical checks do not approve animation quality. Bone/target movement is diagnostic, not proof of planted soles. " +
            "Drawing swaps require visual inspection; this neutral rig capture is not Canvas or gameplay evidence. " +
            "Run is observational even on failure; RunAndValidate fails after preserving all evidence.";
        public string units = "Positions and distances are world units. Camera spans six world units vertically. Angles are degrees. " +
            "Frame CSV records rendered frame time and requested authoritative clip age separately. Marker latency is bounded by one simulation step.";
        public int fps = FrameRate, size, samples, savedFrames, stressTransitions, stressPausedTransitions,
            maxStressPlayables, maxPausedPlayables, settledPlayables;
        public float maxPausedStressPoseDelta;
        public List<ClipInfo> clips = new List<ClipInfo>();
        public List<ScenarioReport> scenarios = new List<ScenarioReport>();
        public List<DeathCadencePair> deathCadencePairs = new List<DeathCadencePair>();
        public List<string> failures = new List<string>();
    }
    [Serializable] sealed class ClipInfo { public string name, asset, sha256; public float duration; public string[] impacts; public float[] impactTimes; }
    [Serializable] sealed class DeathCadencePair
    {
        public string sourceState;
        public float sourceAge;
        public int facing, sharedAgeSamples;
        public float maxTipDistance, maxGripDistance, maxBladeDirectionDegrees;
        public string method = "Two independent rigs start from the same source pose and death age zero. " +
            "One advances at 1/60 s and one at 1/30 s; actual actor-space grip/tip positions and blade directions " +
            "are compared at common death ages. This checks cadence invariance, not prescribed coordinates or wrist rotations.";
    }
    [Serializable] sealed class ScenarioReport
    {
        public string name, frames, metrics, markerLog;
        public int speed, facing, samples, savedFrames, contacts, expectedContacts, pausedSamples;
        public float maxSocketGap, maxGripGap, maxNearFootStep, maxFarFootStep, maxBoneStep, maxTransitionBoneStep,
            maxPausePoseDelta, maxMirrorDelta, maxDeathHoldDelta, maxPlantedNearFootRange, maxDeathWeaponFloorPenetration;
        public List<string> failures = new List<string>();
    }
    sealed class Phase
    {
        public string state;
        public float duration;
        public Phase(string state, float duration) { this.state = state; this.duration = duration; }
    }
    sealed class Scenario
    {
        public string name;
        public Phase[] phases;
        public float pauseAt = -1;
        public bool pausedStateChange;
        public int pausedPhase = 1;
        public Scenario(string name, params Phase[] phases) { this.name = name; this.phases = phases; }
    }
    sealed class Pose
    {
        public Vector3[] bones;
        public Matrix4x4[] matrices;
        public Sprite[] drawings;
        public int[] orders;
    }
    sealed class ActiveContact { public string state, marker; public long sequence; public float age, time, authoredAge; public int frame; }

    [MenuItem("Mythwake/Kael/Inspect Continuous Motion and Transitions")]
    public static void Run() { Capture(false); }
    public static void RunAndValidate() { Capture(true); }

    static void Capture(bool strict)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Continuity review requires Edit Mode.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) throw new FileNotFoundException("Build the current Kael prefab before inspection.", PrefabPath);
        var slug = Argument("-kaelMotionReviewFolder") ?? "current";
        if (slug == "." || slug == ".." || string.IsNullOrWhiteSpace(slug) || slug.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || slug.Contains("/") || slug.Contains("\\"))
            throw new ArgumentException("-kaelMotionReviewFolder requires one folder name.");
        var capture = Argument("-kaelMotionReviewCapture") ?? "primary";
        if (capture != "primary" && capture != "all" && capture != "none") throw new ArgumentException("Capture must be primary, all or none.");
        var size = int.Parse(Argument("-kaelMotionReviewSize") ?? "512", CultureInfo.InvariantCulture);
        if (size < 128 || size > 2048) throw new ArgumentException("Capture size must be 128..2048.");
        var folder = Path.GetFullPath(Path.Combine("artifacts/kael/motion-continuity", slug));
        Directory.CreateDirectory(folder);
        var manifest = new Manifest { folder = folder, utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            unityVersion = Application.unityVersion, captureMode = capture, size = size };
        ReadClipInfo(prefab.GetComponent<KaelRig>(), manifest);
        var preview = new PreviewRenderUtility();
        var camera = preview.camera;
        camera.transform.position = new Vector3(0, 2.25f, -10); camera.transform.rotation = Quaternion.identity;
        camera.orthographic = true; camera.orthographicSize = 3; camera.aspect = 1;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.13f, .17f, .23f, 1);
        camera.cullingMask = -1; camera.allowHDR = camera.allowMSAA = false;
        var target = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        var readback = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var oldActive = RenderTexture.active; var oldEvent = Event.current;
        camera.targetTexture = target;
        try
        {
            foreach (var scenario in Scenarios())
            foreach (var speed in new[] { 1, 2 })
            {
                List<Pose> right = null;
                foreach (var facing in new[] { 1, -1 })
                {
                    var render = capture == "all" || capture == "primary" && speed == 1 && facing == 1;
                    var poses = CaptureScenario(prefab, camera, target, readback, manifest, scenario, speed, facing, render, right);
                    if (facing == 1) right = poses;
                    File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonUtility.ToJson(manifest, true));
                }
            }
            InspectDeathCadencePairs(prefab, camera, manifest);
            InspectBlendGraphLifetime(prefab, camera, manifest);
            manifest.status = manifest.failures.Count == 0 ? "technical-checks-passed-visual-review-required" : "technical-checks-failed";
        }
        catch (Exception ex) { manifest.status = "capture-failed"; manifest.failures.Add(ex.ToString()); throw; }
        finally
        {
            File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonUtility.ToJson(manifest, true));
            Event.current = oldEvent; RenderTexture.active = oldActive; camera.targetTexture = null;
            target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(readback);
            preview.Cleanup();
        }
        Debug.Log("KAEL_MOTION_CONTINUITY_REVIEW: " + manifest.status + "; samples=" + manifest.samples +
            "; actual PNG renders=" + manifest.savedFrames + "; failures=" + manifest.failures.Count + "; " + folder);
        if (strict && manifest.failures.Count > 0) throw new InvalidOperationException(string.Join("\n", manifest.failures));
    }

    static IEnumerable<Scenario> Scenarios()
    {
        foreach (var basic in new[] { "attack_cross", "attack_spin", "attack_jump" })
        foreach (var loop in new[] { "idle", "run" })
            yield return new Scenario(loop + "-" + basic + "-" + loop, new Phase(loop, .6f), new Phase(basic, .84f), new Phase(loop, .6f));
        yield return new Scenario("followup-cross-spin-jump", new Phase("idle", .3f), new Phase("attack_cross", .84f),
            new Phase("attack_spin", .84f), new Phase("attack_jump", .84f), new Phase("idle", .5f));
        yield return new Scenario("idle-skill-idle", new Phase("idle", .4f), new Phase("skill", 1.32f), new Phase("idle", .6f));
        foreach (var loop in new[] { "idle", "run" })
            yield return new Scenario(loop + "-hit-" + loop, new Phase(loop, .4f), new Phase("hit", .2f), new Phase(loop, .6f));
        yield return new Scenario("hit-interrupted-by-action", new Phase("run", .4f), new Phase("hit", 1f / 60),
            new Phase("attack_cross", .84f), new Phase("idle", .5f));
        foreach (var action in new[] { "attack_cross", "attack_spin", "attack_jump", "skill" })
            yield return new Scenario(action + "-interrupted-by-death", new Phase("idle", .3f), new Phase(action, .3f), new Phase("death", 1.25f));
        yield return new Scenario("skill-turn-interrupted-by-death", new Phase("idle", .3f), new Phase("skill", 1f / 3), new Phase("death", 1.25f));
        yield return new Scenario("skill-airborne-interrupted-by-death", new Phase("idle", .3f), new Phase("skill", .65f), new Phase("death", 1.25f));
        yield return new Scenario("pause-during-action-entry", new Phase("idle", .4f), new Phase("attack_cross", .84f), new Phase("idle", .5f)) { pauseAt = .42f };
        yield return new Scenario("pause-on-contact", new Phase("idle", .4f), new Phase("attack_cross", .84f), new Phase("idle", .5f)) { pauseAt = .65f };
        yield return new Scenario("paused-state-change", new Phase("idle", .4f), new Phase("attack_cross", .84f), new Phase("idle", .5f)) { pausedStateChange = true };
        yield return new Scenario("paused-hit-to-action", new Phase("run", .4f), new Phase("hit", 1f / 60), new Phase("attack_cross", .84f), new Phase("idle", .5f))
            { pausedStateChange = true, pausedPhase = 2 };
        yield return new Scenario("loop-wrap-idle-run-idle", new Phase("idle", 2.65f), new Phase("run", 1.4f), new Phase("idle", .6f));
    }

    static List<Pose> CaptureScenario(GameObject prefab, Camera camera, RenderTexture target, Texture2D readback,
        Manifest manifest, Scenario scenario, int speed, int facing, bool render, List<Pose> right)
    {
        var key = scenario.name + "-" + speed + "x-" + (facing > 0 ? "right" : "left");
        var folder = Path.Combine(manifest.folder, key); Directory.CreateDirectory(folder);
        var report = new ScenarioReport { name = scenario.name, speed = speed, facing = facing,
            metrics = key + "/frames.csv", markerLog = key + "/contacts.csv", frames = render ? key + "/frames" : "not-captured" };
        manifest.scenarios.Add(report);
        if (render) Directory.CreateDirectory(Path.Combine(folder, "frames"));
        var actor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, camera.gameObject.scene);
        var output = new List<Pose>();
        try
        {
            actor.hideFlags = HideFlags.HideAndDontSave; actor.SetActive(true); actor.transform.localScale = new Vector3(facing, 1, 1);
            var rig = actor.GetComponent<KaelRig>(); rig.weaponTrail.enabled = false;
            var transforms = actor.GetComponentsInChildren<Transform>(true);
            var sprites = actor.GetComponentsInChildren<SpriteRenderer>(true);
            var skins = actor.GetComponentsInChildren<SpriteSkin>(true);
            var bones = ObservedBones.Select(name => transforms.Single(t => t.name == name)).ToArray();
            var nearFootTarget = transforms.Single(t => t.name == "NearFootTarget");
            var farFootTarget = transforms.Single(t => t.name == "FarFootTarget");
            var contacts = new List<ActiveContact>();
            var expected = new HashSet<string>();
            var delivered = new HashSet<string>();
            var plantedFootRanges = new Dictionary<string, Vector2>(StringComparer.Ordinal);
            var csv = new StringBuilder("frame,wallTime,combatTime,delta,phase,state,sequence,requestedAge,sampledAge,paused,transition,maxSocketGap,gripGap,nearFootTargetGap,farFootTargetGap,boneStep,transitionStep,deathWeaponFloorPenetration");
            foreach (var name in ObservedBones) csv.Append(',').Append(name).Append("X,").Append(name).Append("Y,").Append(name).Append("Angle");
            csv.AppendLine();
            var step = speed / (float)FrameRate;
            var total = scenario.phases.Sum(p => p.duration);
            var boundaries = new float[scenario.phases.Length];
            for (var i = 1; i < boundaries.Length; i++) boundaries[i] = boundaries[i - 1] + scenario.phases[i - 1].duration;
            var sequenceByPhase = new long[scenario.phases.Length];
            long nextSequence = 0;
            for (var i = 0; i < scenario.phases.Length; i++)
            { if (IsAction(scenario.phases[i].state)) nextSequence++; sequenceByPhase[i] = nextSequence; }
            int frame = 0, phase = 0, pauseRemaining = 0; float clock = 0, wallTime = 0;
            bool pauseUsed = false, zeroChangeUsed = false;
            Pose previous = null, deathEnd = null;
            string sampledState = null; long sampledSequence = -1;
            float requestedAge = 0, lastSampledAge = 0; long activeSequence = 0; string activeState = "idle";
            rig.Marker += (marker, sequence) =>
            {
                if (!marker.StartsWith("impact:", StringComparison.Ordinal)) return;
                var contactKey = activeState + ":" + sequence + ":" + marker;
                if (!delivered.Add(contactKey)) Fail(report, "Duplicate contact " + contactKey);
                if (!expected.Contains(contactKey)) Fail(report, "Unexpected contact " + contactKey);
                var authored = rig.clips.Single(c => c.name == activeState).events.Single(e =>
                    e.functionName == nameof(KaelRig.OnKaelAnimationMarker) && e.stringParameter == marker).time;
                if (requestedAge < authored - .00001f || requestedAge > authored + step + .00001f)
                    Fail(report, "Contact fell outside its authoritative sample window: " + contactKey);
                contacts.Add(new ActiveContact { state = activeState, sequence = sequence, marker = marker, age = requestedAge, authoredAge = authored, time = clock, frame = frame });
            };
            while (clock <= total + .00001f)
            {
                var lastPhase = phase;
                while (phase + 1 < boundaries.Length && clock >= boundaries[phase + 1] - .00001f) phase++;
                activeState = scenario.phases[phase].state; activeSequence = sequenceByPhase[phase];
                requestedAge = Mathf.Max(0, clock - boundaries[phase]);
                var changed = sampledState != activeState || sampledSequence != activeSequence;
                var paused = pauseRemaining > 0;
                var delta = frame == 0 || paused ? 0 : step;
                if (scenario.pausedStateChange && phase == scenario.pausedPhase && lastPhase < phase && !zeroChangeUsed)
                { delta = 0; paused = true; zeroChangeUsed = true; requestedAge = 0; }
                if (paused && !changed) requestedAge = lastSampledAge;
                var clip = rig.clips.Single(c => c.name == activeState);
                var sampleAge = activeState == "idle" || activeState == "run" ? Mathf.Repeat(requestedAge, clip.length) : Mathf.Min(requestedAge, clip.length);
                foreach (var marker in clip.events)
                    if (marker.functionName == nameof(KaelRig.OnKaelAnimationMarker) && marker.stringParameter.StartsWith("impact:", StringComparison.Ordinal) && marker.time <= sampleAge + .000001f)
                        expected.Add(activeState + ":" + activeSequence + ":" + marker.stringParameter);
                rig.Sample(activeState, activeSequence, requestedAge, delta);
                Event.current = new Event { type = EventType.Repaint };
                foreach (var skin in skins) skin.OnPreviewUpdate();
                var pose = Snapshot(transforms, sprites, bones);
                output.Add(pose);
                if (rig.CurrentState != activeState || Mathf.Abs(rig.CurrentTime - sampleAge) > .00001f) Fail(report, "State/clock differs from request at frame " + frame);
                float socketGap = 0;
                foreach (var socket in rig.bodySockets)
                {
                    var visible = socket.views.Single(v => v.sprite == socket.drawing.sprite);
                    socketGap = Mathf.Max(socketGap, Vector3.Distance(socket.childBone.position, socket.drawing.transform.TransformPoint(visible.position)));
                }
                var gripGap = Vector3.Distance(rig.gripAnchor.position, bones[Array.IndexOf(ObservedBones, "Sword")].position);
                report.maxSocketGap = Mathf.Max(report.maxSocketGap, socketGap); report.maxGripGap = Mathf.Max(report.maxGripGap, gripGap);
                // Observe the actual authored blade-tip anchor in actor space, not
                // a sprite quad/bounds corner. Facing cannot change the floor and
                // no particular wrist angle, root position or weapon scale is imposed.
                var deathTipPenetration = activeState == "death" ?
                    Mathf.Max(0, -rig.transform.InverseTransformPoint(rig.weaponTip.position).y) : 0;
                report.maxDeathWeaponFloorPenetration = Mathf.Max(report.maxDeathWeaponFloorPenetration, deathTipPenetration);
                if (activeState == "death" && delta > 0 && deathTipPenetration > .01f)
                    Fail(report, "The actual blade tip passed below the floor during an advancing death sample.");
                // During these authored support phases the near foot is the planted
                // pivot. Perspective changes may exchange drawings and hip sockets,
                // but cannot teleport that contact across the floor. Start after the
                // entry blend, and compare range rather than an authored coordinate.
                if (activeState == "attack_spin" && requestedAge >= .12f ||
                    activeState == "skill" && requestedAge >= .12f && requestedAge <= .34f)
                {
                    var supportKey = activeState + ":" + activeSequence;
                    var x = bones[10].position.x;
                    if (!plantedFootRanges.TryGetValue(supportKey, out var range)) range = new Vector2(x, x);
                    range.x = Mathf.Min(range.x, x); range.y = Mathf.Max(range.y, x); plantedFootRanges[supportKey] = range;
                    report.maxPlantedNearFootRange = Mathf.Max(report.maxPlantedNearFootRange, range.y - range.x);
                }
                var boneStep = previous == null ? 0 : PointDelta(previous, pose, false);
                report.maxBoneStep = Mathf.Max(report.maxBoneStep, boneStep);
                if (changed) report.maxTransitionBoneStep = Mathf.Max(report.maxTransitionBoneStep, boneStep);
                if (previous != null)
                {
                    report.maxNearFootStep = Mathf.Max(report.maxNearFootStep, Vector3.Distance(previous.bones[10], pose.bones[10]));
                    report.maxFarFootStep = Mathf.Max(report.maxFarFootStep, Vector3.Distance(previous.bones[13], pose.bones[13]));
                    if (paused)
                    {
                        var pauseDelta = PoseDelta(previous, pose); report.maxPausePoseDelta = Mathf.Max(report.maxPausePoseDelta, pauseDelta);
                        if (pauseDelta > PoseTolerance) Fail(report, "Paused pose changed at frame " + frame + ", delta=" + F(pauseDelta));
                        report.pausedSamples++;
                    }
                }
                if (activeState == "death" && requestedAge >= clip.length + .12f)
                {
                    if (deathEnd == null) deathEnd = pose;
                    var hold = PoseDelta(deathEnd, pose); report.maxDeathHoldDelta = Mathf.Max(report.maxDeathHoldDelta, hold);
                    if (hold > PoseTolerance) Fail(report, "Death end pose changed at frame " + frame);
                }
                if (right != null && frame < right.Count) report.maxMirrorDelta = Mathf.Max(report.maxMirrorDelta, PointDelta(right[frame], pose, true));
                csv.Append(frame).Append(',').Append(F(wallTime)).Append(',').Append(F(clock)).Append(',').Append(F(delta)).Append(',').Append(phase)
                    .Append(',').Append(activeState).Append(',').Append(activeSequence).Append(',').Append(F(requestedAge)).Append(',').Append(F(rig.CurrentTime))
                    .Append(',').Append(paused ? 1 : 0).Append(',').Append(changed ? 1 : 0).Append(',').Append(F(socketGap)).Append(',').Append(F(gripGap))
                    .Append(',').Append(F(Vector3.Distance(bones[10].position, nearFootTarget.position))).Append(',').Append(F(Vector3.Distance(bones[13].position, farFootTarget.position)))
                    .Append(',').Append(F(boneStep)).Append(',').Append(F(changed ? boneStep : 0)).Append(',').Append(F(deathTipPenetration));
                for (var i = 0; i < bones.Length; i++) csv.Append(',').Append(F(pose.bones[i].x)).Append(',').Append(F(pose.bones[i].y)).Append(',').Append(F(Mathf.DeltaAngle(0, bones[i].localEulerAngles.z)));
                csv.AppendLine();
                if (render)
                {
                    camera.Render(); RenderTexture.active = target;
                    readback.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); readback.Apply();
                    File.WriteAllBytes(Path.Combine(folder, "frames", frame.ToString("00000", CultureInfo.InvariantCulture) + ".png"), readback.EncodeToPNG());
                    report.savedFrames++; manifest.savedFrames++;
                }
                previous = pose; sampledState = activeState; sampledSequence = activeSequence; lastSampledAge = requestedAge;
                report.samples++; manifest.samples++; frame++; wallTime += 1f / FrameRate;
                if (scenario.pauseAt >= 0 && clock >= scenario.pauseAt && !pauseUsed)
                { pauseUsed = true; pauseRemaining = 8; }
                else if (pauseRemaining > 0)
                { pauseRemaining--; if (pauseRemaining == 0) clock += step; }
                else clock += step;
            }
            if (report.maxSocketGap > .0001f || report.maxGripGap > .0001f) Fail(report, "Anatomical/weapon socket detached.");
            if (report.maxPlantedNearFootRange > .015f)
                Fail(report, "A planted near foot moved during spin/supporting skill phase; horizontal range=" + F(report.maxPlantedNearFootRange));
            if (right != null && (right.Count != output.Count || report.maxMirrorDelta > .0001f)) Fail(report, "Mirrored playback differs from the corresponding right-facing sample.");
            foreach (var missing in expected.Except(delivered)) Fail(report, "Missing contact " + missing);
            report.contacts = contacts.Count; report.expectedContacts = expected.Count;
            File.WriteAllText(Path.Combine(folder, "frames.csv"), csv.ToString());
            var markerCsv = new StringBuilder("frame,combatTime,state,sequence,marker,requestedAge,authoredAge\n");
            foreach (var contact in contacts) markerCsv.Append(contact.frame).Append(',').Append(F(contact.time)).Append(',').Append(contact.state).Append(',').Append(contact.sequence)
                .Append(',').Append(contact.marker).Append(',').Append(F(contact.age)).Append(',').Append(F(contact.authoredAge)).AppendLine();
            File.WriteAllText(Path.Combine(folder, "contacts.csv"), markerCsv.ToString());
            foreach (var issue in report.failures) manifest.failures.Add(key + ": " + issue);
        }
        finally { actor.GetComponent<KaelRig>().ResetPlayback(); UnityEngine.Object.DestroyImmediate(actor); }
        return output;
    }

    static Pose Snapshot(Transform[] transforms, SpriteRenderer[] sprites, Transform[] bones)
    {
        var pose = new Pose { bones = bones.Select(b => b.position).ToArray(), matrices = transforms.Select(t => Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale)).ToArray(),
            drawings = sprites.Select(s => s.sprite).ToArray(), orders = sprites.Select(s => s.sortingOrder).ToArray() };
        foreach (var matrix in pose.matrices) for (var i = 0; i < 16; i++)
            if (float.IsNaN(matrix[i]) || float.IsInfinity(matrix[i])) throw new InvalidOperationException("Rig produced a non-finite transform.");
        return pose;
    }
    static float PoseDelta(Pose a, Pose b)
    {
        var delta = 0f;
        for (var i = 0; i < a.matrices.Length; i++) for (var j = 0; j < 16; j++) delta = Mathf.Max(delta, Mathf.Abs(a.matrices[i][j] - b.matrices[i][j]));
        for (var i = 0; i < a.drawings.Length; i++) if (a.drawings[i] != b.drawings[i] || a.orders[i] != b.orders[i]) delta = Mathf.Max(delta, 1);
        return delta;
    }
    static float PointDelta(Pose a, Pose b, bool mirrored)
    {
        var delta = 0f;
        for (var i = 0; i < a.bones.Length; i++)
        { var point = a.bones[i]; if (mirrored) point.x = -point.x; delta = Mathf.Max(delta, Vector3.Distance(point, b.bones[i])); }
        return delta;
    }
    static bool IsAction(string state) => state.StartsWith("attack", StringComparison.Ordinal) || state == "skill";

    static void InspectDeathCadencePairs(GameObject prefab, Camera camera, Manifest manifest)
    {
        var states = new[] { "skill", "skill", "skill", "attack_spin" };
        var ages = new[] { 16f / 60, 1f / 3, .65f, 16f / 60 };
        for (var source = 0; source < states.Length; source++)
        foreach (var facing in new[] { 1, -1 })
        {
            var report = new DeathCadencePair { sourceState = states[source], sourceAge = ages[source], facing = facing };
            manifest.deathCadencePairs.Add(report);
            GameObject first = null, second = null;
            try
            {
                first = (GameObject)PrefabUtility.InstantiatePrefab(prefab, camera.gameObject.scene);
                second = (GameObject)PrefabUtility.InstantiatePrefab(prefab, camera.gameObject.scene);
                foreach (var actor in new[] { first, second })
                {
                    actor.hideFlags = HideFlags.HideAndDontSave; actor.SetActive(true);
                    actor.transform.localScale = new Vector3(facing, 1, 1);
                    var rig = actor.GetComponent<KaelRig>(); rig.weaponTrail.enabled = false;
                    // A fully evaluated common source isolates clock-step differences
                    // from the naturally different last frames of the longer scenarios.
                    rig.Sample(states[source], 1, ages[source], 1);
                    rig.Sample("death", 2, 0, 0);
                }
                var once = first.GetComponent<KaelRig>(); var twice = second.GetComponent<KaelRig>();
                CompareDeathCadence(once, twice, report);
                for (var frame = 1; frame <= 54; frame++)
                {
                    var age = frame / 60f;
                    once.Sample("death", 2, age, 1f / 60);
                    if (frame % 2 != 0) continue;
                    twice.Sample("death", 2, age, 1f / 30);
                    CompareDeathCadence(once, twice, report);
                }
                // Allow small numerical differences from repeated IK evaluation.
                // Opposite winding choices produce much larger errors and cannot
                // hide behind both paths eventually reaching the same end pose.
                if (report.maxTipDistance > .002f || report.maxGripDistance > .002f || report.maxBladeDirectionDegrees > .25f)
                    manifest.failures.Add("Death cadence changed the actual weapon path from " + states[source] + " at " + F(ages[source]) +
                        "s, facing=" + facing + ": tip=" + F(report.maxTipDistance) + "WU, direction=" + F(report.maxBladeDirectionDegrees) + "deg.");
            }
            finally
            {
                foreach (var actor in new[] { first, second })
                    if (actor != null) { actor.GetComponent<KaelRig>().ResetPlayback(); UnityEngine.Object.DestroyImmediate(actor); }
            }
        }
    }

    static void CompareDeathCadence(KaelRig once, KaelRig twice, DeathCadencePair report)
    {
        var tipA = once.transform.InverseTransformPoint(once.weaponTip.position);
        var tipB = twice.transform.InverseTransformPoint(twice.weaponTip.position);
        var gripA = once.transform.InverseTransformPoint(once.gripAnchor.position);
        var gripB = twice.transform.InverseTransformPoint(twice.gripAnchor.position);
        report.maxTipDistance = Mathf.Max(report.maxTipDistance, Vector3.Distance(tipA, tipB));
        report.maxGripDistance = Mathf.Max(report.maxGripDistance, Vector3.Distance(gripA, gripB));
        // atan2(cross,dot) remains stable near zero; acos(dot) amplifies tiny float
        // errors and would make this a brittle comparison of identical directions.
        var a = tipA - gripA; var b = tipB - gripB;
        var turn = Mathf.Abs(Mathf.Atan2(a.x * b.y - a.y * b.x, a.x * b.x + a.y * b.y) * Mathf.Rad2Deg);
        report.maxBladeDirectionDegrees = Mathf.Max(report.maxBladeDirectionDegrees, turn);
        report.sharedAgeSamples++;
    }

    static void InspectBlendGraphLifetime(GameObject prefab, Camera camera, Manifest manifest)
    {
        // A separate lifecycle stress check, not an assertion that combat switches states
        // this often. Rapid legal movement-state changes and paused requests must not
        // retain every old playable. Reflection keeps this diagnostic out of the runtime API.
        var graphField = typeof(KaelRig).GetField("graph", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (graphField == null) { manifest.failures.Add("Cannot inspect the rig's playback graph lifetime."); return; }
        var actor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, camera.gameObject.scene);
        try
        {
            actor.hideFlags = HideFlags.HideAndDontSave; actor.SetActive(true);
            var rig = actor.GetComponent<KaelRig>(); rig.weaponTrail.enabled = false;
            var transforms = actor.GetComponentsInChildren<Transform>(true);
            var sprites = actor.GetComponentsInChildren<SpriteRenderer>(true);
            var bones = ObservedBones.Select(name => transforms.Single(t => t.name == name)).ToArray();
            rig.Sample("idle", 0, .4f, 0);
            for (var i = 0; i < 200; i++)
            {
                rig.Sample(i % 2 == 0 ? "idle" : "run", 0, 1f / FrameRate, 1f / FrameRate);
                var graph = (UnityEngine.Playables.PlayableGraph)graphField.GetValue(rig);
                manifest.maxStressPlayables = Math.Max(manifest.maxStressPlayables, graph.GetPlayableCount());
                manifest.stressTransitions++;
            }
            rig.Sample("run", 0, .4f, .25f);
            manifest.settledPlayables = ((UnityEngine.Playables.PlayableGraph)graphField.GetValue(rig)).GetPlayableCount();
            var paused = Snapshot(transforms, sprites, bones);
            for (var i = 0; i < 100; i++)
            {
                rig.Sample(i % 2 == 0 ? "idle" : "run", 0, 0, 0);
                var graph = (UnityEngine.Playables.PlayableGraph)graphField.GetValue(rig);
                manifest.maxPausedPlayables = Math.Max(manifest.maxPausedPlayables, graph.GetPlayableCount());
                manifest.maxPausedStressPoseDelta = Mathf.Max(manifest.maxPausedStressPoseDelta, PoseDelta(paused, Snapshot(transforms, sprites, bones)));
                manifest.stressPausedTransitions++;
            }
            if (manifest.maxStressPlayables > 16 || manifest.settledPlayables > 2 || manifest.maxPausedPlayables > 4)
                manifest.failures.Add("Transition graph did not remain bounded or release completed transitions.");
            if (manifest.maxPausedStressPoseDelta > PoseTolerance)
                manifest.failures.Add("Repeated paused state changes modified the visible source pose.");
        }
        finally { actor.GetComponent<KaelRig>().ResetPlayback(); UnityEngine.Object.DestroyImmediate(actor); }
    }

    static void Fail(ScenarioReport report, string message) { if (!report.failures.Contains(message)) report.failures.Add(message); }
    static string F(float value) => value.ToString("0.000000", CultureInfo.InvariantCulture);
    static string Argument(string option)
    {
        var args = Environment.GetCommandLineArgs(); var index = Array.IndexOf(args, option);
        return index < 0 ? null : index + 1 < args.Length ? args[index + 1] : throw new ArgumentException(option + " requires a value.");
    }
    static void ReadClipInfo(KaelRig rig, Manifest manifest)
    {
        foreach (var clip in rig.clips)
        {
            var path = AssetDatabase.GetAssetPath(clip);
            var contacts = clip.events.Where(e => e.functionName == nameof(KaelRig.OnKaelAnimationMarker) && e.stringParameter.StartsWith("impact:", StringComparison.Ordinal)).ToArray();
            string hash;
            using (var algorithm = SHA256.Create()) hash = BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
            manifest.clips.Add(new ClipInfo { name = clip.name, asset = path, sha256 = hash, duration = clip.length,
                impacts = contacts.Select(c => c.stringParameter).ToArray(), impactTimes = contacts.Select(c => c.time).ToArray() });
        }
        CheckContactProfile(manifest, "attack_cross", .84f, .24f, .52f);
        CheckContactProfile(manifest, "attack_spin", .84f, .42f);
        CheckContactProfile(manifest, "attack_jump", .84f, .46f);
        CheckContactProfile(manifest, "skill", 1.32f, .40f, .92f);
        CheckContactProfile(manifest, "attack", .60f, .20f);
        CheckContactProfile(manifest, "skill_legacy", 1f, .40f);
    }
    static void CheckContactProfile(Manifest manifest, string name, float duration, params float[] contacts)
    {
        var clip = manifest.clips.SingleOrDefault(c => c.name == name);
        if (clip == null || Mathf.Abs(clip.duration - duration) > .002f || clip.impactTimes.Length != contacts.Length)
        { manifest.failures.Add(name + " changed its existing authoritative duration/contact count."); return; }
        for (var i = 0; i < contacts.Length; i++)
            if (clip.impacts[i] != "impact:" + i || Mathf.Abs(clip.impactTimes[i] - contacts[i]) > .001f)
                manifest.failures.Add(name + " changed its existing authoritative contact " + i + ".");
    }
}
