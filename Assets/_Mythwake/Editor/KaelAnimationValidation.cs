using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

/// <summary>Loads the real editable Kael asset; no scene, player state or preference changes.</summary>
public static class KaelAnimationValidation
{
    /// <summary>Run in isolated Play Mode before recording; never opens a scene or touches preferences.</summary>
    public static IEnumerator ValidateCanvasRuntime()
    {
        Require(Application.isPlaying, "Canvas lifecycle validation requires Play Mode.");
        var fixture = new GameObject("Kael Canvas lifecycle fixture", typeof(RectTransform), typeof(Canvas));
        var canvas = fixture.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -32000;
        var maskObject = new GameObject("Masked viewport", typeof(RectTransform), typeof(RectMask2D));
        maskObject.transform.SetParent(fixture.transform, false);
        var maskRect = maskObject.GetComponent<RectTransform>();
        maskRect.anchorMin = maskRect.anchorMax = new Vector2(.5f, .5f);
        maskRect.sizeDelta = new Vector2(256, 256);
        KaelAnimationView first = null, second = null, replacement = null;
        try
        {
            first = KaelAnimationView.Create(maskRect, "Independent Canvas actor A", 64);
            second = KaelAnimationView.Create(maskRect, "Independent Canvas actor B", 64);
            Require(first.Rig != second.Rig, "Two Canvas views share the same actor.");
            var firstSurface = first.GetComponent<RawImage>();
            var secondSurface = second.GetComponent<RawImage>();
            Require(firstSurface.texture is RenderTexture && firstSurface.texture == secondSurface.texture, "Views must share a live atlas texture.");
            Require(!firstSurface.uvRect.Overlaps(secondSurface.uvRect), "Independent views use overlapping atlas cells.");
            var firstImpacts = 0;
            var secondImpacts = 0;
            first.Marker += (marker, sequence) => { if (marker.StartsWith("impact:",StringComparison.Ordinal)) firstImpacts++; };
            second.Marker += (marker, sequence) => { if (marker.StartsWith("impact:",StringComparison.Ordinal)) secondImpacts++; };
            var firstPosition = new Vector2(-44, -35);
            var secondPosition = new Vector2(44, -35);
            first.Present(firstPosition, "attack_cross", 11, .23f, .23f, 1);
            second.Present(secondPosition, "attack_cross", 21, .23f, .23f, -1);
            Require(first.Rig.transform.localScale.x == 1 && second.Rig.transform.localScale.x == -1, "Facing must remain independent per actor.");
            yield return NextGameFrame();
            Canvas.ForceUpdateCanvases();
            maskObject.GetComponent<RectMask2D>().PerformClipping();
            ValidateCanvasSurface(first, firstPosition, 64);
            ValidateCanvasSurface(second, secondPosition, 64);
            var secondPose = CapturePose(second.Rig);
            first.Present(firstPosition, "attack_cross", 11, .25f, .02f, 1);
            Require(firstImpacts == 1 && secondImpacts == 0, "Advancing one view advanced the other view's contact marker.");
            EqualPose(second.Rig, secondPose, "Advancing one view changed the other view's pose");
            var firstPose = CapturePose(first.Rig);
            first.Present(firstPosition, "attack_cross", 11, .25f, 0, 1);
            EqualPose(first.Rig, firstPose, "Zero-delta Canvas sampling changed pose");
            Require(firstImpacts == 1, "Paused Canvas view emitted duplicate contact.");
            second.Present(secondPosition, "attack_cross", 21, .25f, .02f, -1);
            Require(secondImpacts == 1, "Second view lost its independent clock.");

            first.gameObject.SetActive(false);
            Require(!first.Rig.gameObject.activeInHierarchy && second.Rig.gameObject.activeInHierarchy, "Hiding one view affected the other actor.");
            first.ResetView();
            first.Present(firstPosition, "attack_cross", 11, .25f, .25f, 1);
            Require(firstImpacts == 2 && first.Rig.gameObject.activeInHierarchy, "Reset/show did not start a fresh marker sequence.");
            var releasedCell = firstSurface.uvRect;
            var releasedActor = first.Rig;
            UnityEngine.Object.Destroy(first.gameObject);
            yield return NextGameFrame();
            Require(releasedActor == null, "Destroyed view leaked its atlas actor.");
            Require(second != null && second.Rig.gameObject.activeInHierarchy, "Destroying one view destroyed the independent view.");
            replacement = KaelAnimationView.Create(maskRect, "Reused Canvas actor A cell", 64);
            Require(replacement.GetComponent<RawImage>().uvRect == releasedCell, "Released atlas cell was not reused.");
            Require(replacement.Rig != second.Rig, "Reusing a cell hijacked a live actor.");
            replacement.Present(firstPosition, "idle", 0, 0, 0, 1);
            yield return NextGameFrame();
            Canvas.ForceUpdateCanvases();
            maskObject.GetComponent<RectMask2D>().PerformClipping();
            ValidateCanvasSurface(replacement, firstPosition, 64);
            Require(second.Rig.transform.localScale.x == -1, "Cell reuse changed the other view's facing.");
            var renderedIdle = CaptureBodyPose(replacement.Rig);
            var standing = CaptureDeathBaseline(replacement.Rig);
            foreach (var action in new[] { "attack", "skill", "death" })
            {
                var age = action == "attack" ? .24f : action == "skill" ? .4f : .9f;
                replacement.Present(firstPosition, action, action == "attack" ? 31 : action == "skill" ? 32 : 33, age, 1, 1);
                RequireDifferentBodyPose(replacement.Rig, renderedIdle, action);
                if (action == "death") RequireDeathPose(replacement.Rig, standing);
                // Observe the real atlas camera after a subsequent PlayerLoop, not the
                // immediate Graph.Evaluate result that another Animator could overwrite.
                yield return ValidatePoseAtNextRender(replacement.Rig, action, standing);
            }
            yield return ValidateCombatEffects(maskRect, replacement, second, firstPosition, secondPosition);
            Debug.Log("Kael Canvas runtime lifecycle passed: two independent live atlas views, real mesh UVs, cell placement, foot-aligned size, mask, facing, paused clock, reset, release, cell reuse and distinct attack/skill/death bone poses persisting through the next real atlas render. No scene or player preferences were changed.");
        }
        finally
        {
            UnityEngine.Object.Destroy(fixture);
        }
        yield return NextGameFrame();
    }

    private static IEnumerator NextGameFrame()
    {
        var frame = Time.frameCount;
        var deadline = Time.realtimeSinceStartup + 5;
        while (Time.frameCount == frame)
        {
            Require(Time.realtimeSinceStartup < deadline, "Play Mode did not advance while testing Canvas lifecycle.");
            yield return null;
        }
    }

    private sealed class EffectMeshSnapshot
    {
        public Vector3[] vertices;
        public Color32[] colors;
        public int[] triangles;
        public Vector2[] uv;
    }

    private static IEnumerator ValidateCombatEffects(RectTransform parent, KaelAnimationView first, KaelAnimationView second,
        Vector2 firstPosition, Vector2 secondPosition)
    {
        var fixtureBounds = new Rect(-parent.rect.width * .5f, -parent.rect.height, parent.rect.width, parent.rect.height);
        var firstEffect = KaelCombatVfx.Create(parent, fixtureBounds);
        var secondEffect = KaelCombatVfx.Create(parent, fixtureBounds);
        try
        {
            Require(firstEffect.mainTexture != Texture2D.whiteTexture && firstEffect.mainTexture == secondEffect.mainTexture,
                "Independent effect instances must use the actual shared painted atlas.");
            Require(firstEffect.mainTexture.width == 2048 && firstEffect.mainTexture.height == 1024,
                "Painted VFX atlas must retain its four-by-two 512-pixel cells.");
            first.Present(firstPosition, "skill", 101, .2f, 1, 1);
            second.Present(secondPosition, "skill", 201, .2f, 1, -1);
            firstEffect.SetFrame(first, "skill", 101, .2f, 1, 1);
            secondEffect.SetFrame(second, "skill", 201, .2f, 1, -1);
            Require(firstEffect.HasCharge && secondEffect.HasCharge, "Independent skill windups must show their charge.");
            RequireMirroredProjection(first, second);
            firstEffect.AnimationMarker("impact:0", 101);
            firstEffect.AnimationMarker("weapon_trail", 101);
            Require(firstEffect.ImpactCount == 0 && firstEffect.ActiveImpactCount == 0 && secondEffect.ImpactCount == 0,
                "Animation markers alone created an authoritative impact.");
            var pausedCharge = CaptureEffectMesh(firstEffect);
            var independentCharge = CaptureEffectMesh(secondEffect);
            Require(pausedCharge.vertices.Length > 0 && independentCharge.vertices.Length > 0, "Skill charge has no actual Canvas geometry.");
            Require(pausedCharge.uv.Length == pausedCharge.vertices.Length && pausedCharge.uv[0] != pausedCharge.uv[1],
                "Charge geometry does not sample the painted texture region.");
            yield return NextGameFrame();
            for (var i = 0; i < 3; i++)
            {
                first.Present(firstPosition, "skill", 101, .2f, 0, 1);
                firstEffect.SetFrame(first, "skill", 101, .2f, 1, 1);
                EqualEffectMesh(CaptureEffectMesh(firstEffect), pausedCharge, "Paused skill charge changed geometry or color");
                Require(firstEffect.EffectClock == 1, "Paused skill charge advanced its clock.");
            }
            EqualEffectMesh(CaptureEffectMesh(secondEffect), independentCharge, "Advancing the first VFX instance changed the second");

            first.Present(firstPosition, "skill", 101, .4f, .2f, 1);
            firstEffect.SetFrame(first, "skill", 101, .4f, 1.2f, 1);
            firstEffect.ResolvedImpact(new Vector2(20, -60), true, 101, 0, false, 1.2f, 1);
            firstEffect.ResolvedImpact(new Vector2(20, -60), true, 101, 0, false, 1.2f, 1);
            firstEffect.ResolvedImpact(new Vector2(20, -60), false, 100, 0, true, 1.2f, 1);
            Require(firstEffect.ImpactCount == 1 && firstEffect.SkillImpactCount == 1 && firstEffect.ActiveImpactCount == 1,
                "Duplicate or older resolution manufactured an additional impact.");
            Require(firstEffect.FinalSkillImpactCount == 0, "The opening contact incorrectly produced the final skill emphasis.");
            Require(secondEffect.ImpactCount == 0 && secondEffect.ActiveImpactCount == 0 && secondEffect.HasCharge,
                "One instance's resolved hit altered another instance's effects.");
            var pausedImpact = CaptureEffectMesh(firstEffect);
            Require(pausedImpact.vertices.Length > 0, "Resolved impact has no Canvas geometry.");
            yield return NextGameFrame();
            first.Present(firstPosition, "skill", 101, .4f, 0, 1);
            firstEffect.SetFrame(first, "skill", 101, .4f, 1.2f, 1);
            EqualEffectMesh(CaptureEffectMesh(firstEffect), pausedImpact, "Paused resolved impact changed geometry or color");

            first.Present(firstPosition, "skill", 101, .92f, .52f, 1);
            firstEffect.SetFrame(first, "skill", 101, .92f, 1.72f, 1);
            firstEffect.ResolvedImpact(new Vector2(20, -60), true, 101, 1, true, 1.72f, 1);
            firstEffect.ResolvedImpact(new Vector2(20, -60), true, 101, 1, true, 1.72f, 1);
            Require(firstEffect.ImpactCount == 2 && firstEffect.SkillImpactCount == 2 && firstEffect.FinalSkillImpactCount == 1,
                "A second distinct contact must resolve once within the same action and receive final-hit emphasis.");
            var pausedFinalImpact = CaptureEffectMesh(firstEffect);
            Require(pausedFinalImpact.vertices.Length > pausedImpact.vertices.Length,
                "The final skill contact has no distinct painted conclusion.");
            first.Present(firstPosition, "skill", 101, .92f, 0, 1);
            firstEffect.SetFrame(first, "skill", 101, .92f, 1.72f, 1);
            EqualEffectMesh(CaptureEffectMesh(firstEffect), pausedFinalImpact, "Final-contact hit stop changed painted geometry or UVs");

            first.Present(firstPosition, "death", 102, .9f, 1, 1);
            firstEffect.SetFrame(first, "death", 102, .9f, 1.8f, 1);
            firstEffect.ResolvedImpact(new Vector2(20, -60), true, 101, 2, true, 1.8f, 1);
            Require(!firstEffect.HasCharge && firstEffect.ActiveImpactCount == 0 && CaptureEffectMesh(firstEffect).vertices.Length == 0,
                "Death left an active charge, trail or impact mesh.");
            EqualEffectMesh(CaptureEffectMesh(secondEffect), independentCharge, "Death cleared another instance's charge");
            firstEffect.ResetEffects();
            Require(firstEffect.ImpactCount == 0 && firstEffect.SkillImpactCount == 0 && firstEffect.FinalSkillImpactCount == 0 && firstEffect.EffectClock == 0,
                "Reset did not clear the VFX lifecycle counters and clock.");
            first.Present(firstPosition, "attack_cross", 101, .24f, 1, 1);
            firstEffect.SetFrame(first, "attack_cross", 101, .24f, 1, 1);
            firstEffect.ResolvedImpact(new Vector2(20, -60), false, 101, 0, false, 1, 1);
            Require(firstEffect.ImpactCount == 1 && firstEffect.SkillImpactCount == 0 && firstEffect.ActiveImpactCount == 1,
                "A reused VFX instance rejected a fresh battle's previously used sequence.");
            firstEffect.enabled = false;
            first.Present(firstPosition, "attack_cross", 101, .52f, .28f, 1);
            firstEffect.SetFrame(first, "attack_cross", 101, .52f, 1.28f, 1);
            firstEffect.ResolvedImpact(new Vector2(20, -60), false, 101, 1, true, 1.28f, 1);
            Require(firstEffect.ImpactCount == 2 && firstEffect.SkillImpactCount == 0,
                "Animation-only presentation lost logical contact counters when its renderer was disabled.");
            firstEffect.enabled = true;
            first.Present(firstPosition, "idle", 102, 0, 1, 1);
            firstEffect.SetFrame(first, "idle", 102, 0, 2, 1);
            Require(firstEffect.ActiveImpactCount == 0 && CaptureEffectMesh(firstEffect).vertices.Length == 0,
                "Expired normal-hit effects remained visible.");
            Debug.Log("Kael painted Canvas VFX validation passed: shared atlas and sampled UVs, independent instances, mirrored projected anchors, paused charge and contacts, two contacts per action with one final emphasis, no late hit after death, reset/reuse and animation-only logical counters.");
        }
        finally
        {
            UnityEngine.Object.Destroy(firstEffect.gameObject);
            UnityEngine.Object.Destroy(secondEffect.gameObject);
        }
    }

    private static void RequireMirroredProjection(KaelAnimationView first, KaelAnimationView second)
    {
        var firstFoot = first.ProjectRigPoint(first.Rig.transform.position);
        var secondFoot = second.ProjectRigPoint(second.Rig.transform.position);
        foreach (var anchors in new[] { new[] { first.Rig.weaponTip, second.Rig.weaponTip }, new[] { first.Rig.gripAnchor, second.Rig.gripAnchor } })
        {
            var a = first.ProjectRigPoint(anchors[0].position) - firstFoot;
            var b = second.ProjectRigPoint(anchors[1].position) - secondFoot;
            Require(Mathf.Abs(a.x + b.x) < .25f && Mathf.Abs(a.y - b.y) < .25f,
                "Mirroring the actor did not mirror its projected weapon bone around the foot anchor.");
        }
    }

    private static EffectMeshSnapshot CaptureEffectMesh(KaelCombatVfx effect)
    {
        Canvas.ForceUpdateCanvases();
        var mesh = effect.canvasRenderer.GetMesh();
        if (mesh == null)
            return new EffectMeshSnapshot { vertices = Array.Empty<Vector3>(), colors = Array.Empty<Color32>(), triangles = Array.Empty<int>(), uv = Array.Empty<Vector2>() };
        return new EffectMeshSnapshot { vertices = mesh.vertices, colors = mesh.colors32, triangles = mesh.triangles, uv = mesh.uv };
    }

    private static void EqualEffectMesh(EffectMeshSnapshot actual, EffectMeshSnapshot expected, string message)
    {
        Require(actual.vertices.Length == expected.vertices.Length && actual.colors.Length == expected.colors.Length &&
            actual.triangles.Length == expected.triangles.Length && actual.uv.Length == expected.uv.Length, message + ": topology changed.");
        for (var i = 0; i < actual.vertices.Length; i++)
            Require(Vector3.SqrMagnitude(actual.vertices[i] - expected.vertices[i]) < .000001f, message + ": vertex " + i);
        for (var i = 0; i < actual.colors.Length; i++)
            Require(actual.colors[i].Equals(expected.colors[i]), message + ": color " + i);
        for (var i = 0; i < actual.triangles.Length; i++)
            Require(actual.triangles[i] == expected.triangles[i], message + ": triangle " + i);
        for (var i = 0; i < actual.uv.Length; i++)
            Require(Vector2.SqrMagnitude(actual.uv[i] - expected.uv[i]) < .000001f, message + ": UV " + i);
    }

    private static IEnumerator ValidatePoseAtNextRender(KaelRig rig, string state, DeathBaseline standing)
    {
        var camera = KaelRenderAtlas.Existing.GetComponent<Camera>();
        var expected = CapturePose(rig);
        var earliestFrame = Time.frameCount + 1;
        Matrix4x4[] rendered = null;
        Action<ScriptableRenderContext, Camera> observer = (context, renderedCamera) =>
        {
            if (renderedCamera == camera && Time.frameCount >= earliestFrame)
                rendered = CapturePose(rig);
        };
        RenderPipelineManager.endCameraRendering += observer;
        try
        {
            var deadline = Time.realtimeSinceStartup + 5;
            while (rendered == null)
            {
                Require(Time.realtimeSinceStartup < deadline, "The live atlas camera did not render the " + state + " test pose.");
                yield return null;
            }
            EqualPose(rendered, expected, state + " bones changed between manual sampling and the next real atlas render");
            EqualPose(rig, expected, state + " pose changed while its manual animation clock was paused");
            if (state == "death") RequireDeathPose(rig, standing);
        }
        finally
        {
            RenderPipelineManager.endCameraRendering -= observer;
        }
    }

    private static void ValidateCanvasSurface(KaelAnimationView view, Vector2 topPosition, float height)
    {
        var surface = view.GetComponent<RawImage>();
        var rect = surface.rectTransform;
        Require(Mathf.Abs(rect.sizeDelta.y * KaelRenderAtlas.CharacterWorldHeight / KaelRenderAtlas.CellWorldSize - height) < .001f,
            "Atlas cell scaling changed visible body height.");
        Require(Mathf.Abs(rect.pivot.y - KaelRenderAtlas.FootMargin / KaelRenderAtlas.CellWorldSize) < .0001f,
            "Atlas projection no longer anchors at the feet.");
        Require(Vector2.Distance(rect.anchoredPosition, topPosition + Vector2.down * height) < .001f, "Canvas body does not align with the requested top position.");
        Require(surface.canvasRenderer.hasRectClipping && !surface.canvasRenderer.cull, "Live viewport mask is missing or incorrectly culls the view.");
        var mesh = surface.canvasRenderer.GetMesh();
        Require(mesh != null && mesh.vertexCount == 4 && mesh.triangles.Length == 6, "Canvas did not build a valid quad.");
        var uv = mesh.uv;
        var expected = surface.uvRect;
        Require(uv.Length == 4, "Canvas quad lacks texture coordinates.");
        foreach (var point in uv)
            Require(point.x >= expected.xMin - .0001f && point.x <= expected.xMax + .0001f &&
                point.y >= expected.yMin - .0001f && point.y <= expected.yMax + .0001f, "Generated Canvas UV samples outside its actor cell.");
        Require(Vector2.Distance(uv[0], new Vector2(expected.xMin, expected.yMin)) < .0001f &&
            Vector2.Distance(uv[2], new Vector2(expected.xMax, expected.yMax)) < .0001f, "Canvas UV orientation or cell extent changed.");
        var camera = KaelRenderAtlas.Existing.GetComponent<Camera>();
        var projectedFeet = camera.WorldToViewportPoint(view.Rig.transform.position);
        var expectedFeet = new Vector2(expected.center.x, expected.yMin + expected.height * KaelRenderAtlas.FootMargin / KaelRenderAtlas.CellWorldSize);
        Require(Vector2.Distance(new Vector2(projectedFeet.x, projectedFeet.y), expectedFeet) < .0002f, "Actor cell placement and Canvas UV no longer agree.");
    }

    [MenuItem("Mythwake/Kael/Validate Animation Lifecycle")]
    public static void Run()
    {
        var prefab = Resources.Load<GameObject>("Characters/Kael/Kael");
        Require(prefab != null, "Kael prefab has not been built.");
        var firstObject = UnityEngine.Object.Instantiate(prefab);
        var secondObject = UnityEngine.Object.Instantiate(prefab);
        firstObject.name = "Kael lifecycle fixture A";
        secondObject.name = "Kael lifecycle fixture B";
        try
        {
            var first = firstObject.GetComponent<KaelRig>();
            var second = secondObject.GetComponent<KaelRig>();
            Require(first != null && second != null, "Prefab lacks KaelRig.");
            foreach (var renderer in firstObject.GetComponentsInChildren<SpriteRenderer>()) renderer.forceRenderingOff = true;
            foreach (var renderer in secondObject.GetComponentsInChildren<SpriteRenderer>()) renderer.forceRenderingOff = true;
            ValidateEditableSources(first);
            ValidateWeightedMeshes(firstObject);
            ValidateManualClock(first, second);
            Debug.Log("Kael animation validation passed: authored basic variants, rear/side exchange drawings, contact markers, skinned mesh bindings and normalized weights, independent instances, paused clock, seamless idle loop, death hold and reset. This validates the real rig; it does not replace a recorded gameplay or device test.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(firstObject);
            UnityEngine.Object.DestroyImmediate(secondObject);
        }
    }

    private static void ValidateEditableSources(KaelRig rig)
    {
        var expected = new[] { "idle", "run", "attack", "attack_cross", "attack_spin", "attack_jump", "skill", "hit", "death" };
        Require(rig.clips != null && rig.clips.Length >= expected.Length, "The authored profile needs base states and all three basic variants.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var clip in rig.clips)
        {
            Require(clip != null && names.Add(clip.name), "Missing or duplicate clip.");
            Require(clip.length > 0 && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(clip)), "Clip must be a persisted editable asset.");
            var animatedPaths = new HashSet<string>();
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                if (!string.IsNullOrEmpty(binding.path)) animatedPaths.Add(binding.path);
            Require(animatedPaths.Count >= 3, clip.name + " must animate multiple body bones, not just the character root.");
        }
        foreach (var name in expected) Require(names.Contains(name), "Missing state " + name);
        ValidateImpactClip(Clip(rig, "attack_cross"), .84f, .24f, .52f);
        ValidateImpactClip(Clip(rig, "attack"), .60f, .20f);
        ValidateImpactClip(Clip(rig, "attack_spin"), .84f, .42f);
        ValidateImpactClip(Clip(rig, "attack_jump"), .84f, .46f);
        ValidateImpactClip(Clip(rig, "skill"), 1.32f, .40f, .92f);
        ValidateImpactClip(Clip(rig, "skill_legacy"), 1f, .40f);
        Require(rig.weaponTip != null && rig.gripAnchor != null && rig.vfxAnchor != null, "Missing weapon or VFX anchors.");
        Require(rig.ik != null && rig.ik.solvers.Count == 4, "The character needs four explicitly bound limb IK chains.");
        foreach (var state in new[] { "attack_spin", "skill" })
        {
            foreach (var part in new[] { "Head Art", "Torso Art", "TorsoBack Art", "Pelvis Art", "ThighNear Art", "ThighFar Art",
                "ShinNear Art", "ShinFar Art", "FootNear Art", "FootFar Art" })
            {
                var rear = false; var side = false;
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(Clip(rig,state)))
                {
                    if (!binding.path.EndsWith(part,StringComparison.Ordinal)) continue;
                    foreach (var key in AnimationUtility.GetObjectReferenceCurve(Clip(rig,state),binding))
                    {
                        var sprite = key.value as Sprite;
                        rear |= sprite != null && sprite.name.IndexOf("rear",StringComparison.OrdinalIgnoreCase) >= 0;
                        side |= sprite != null && sprite.name.IndexOf("side",StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                }
                var sideRequired = part == "Head Art" || part == "Torso Art" || part == "TorsoBack Art" || part == "Pelvis Art";
                Require(rear && (!sideRequired || side), state + " lacks a required perspective drawing for " + part);
            }
        }
        KaelAssetBuilder.ValidateGeneratedFingerprints();
    }

    private static void ValidateImpactClip(AnimationClip clip, float duration, params float[] expected)
    {
        Require(Mathf.Abs(clip.length-duration) < .002f, clip.name + " duration differs from its contact profile.");
        var actual = new List<AnimationEvent>();
        var trailStarts = 0; var trailEnds = 0;
        foreach (var marker in clip.events)
        {
            if (marker.functionName != nameof(KaelRig.OnKaelAnimationMarker)) continue;
            if (marker.stringParameter.StartsWith("impact:",StringComparison.Ordinal)) actual.Add(marker);
            if (marker.stringParameter == "weapon_trail") trailStarts++;
            if (marker.stringParameter == "weapon_trail_end") trailEnds++;
        }
        Require(actual.Count == expected.Length && trailStarts == expected.Length && trailEnds == expected.Length,
            clip.name + " needs one unique contact marker and trail window per contact.");
        for (var i=0;i<expected.Length;i++)
            Require(Mathf.Abs(actual[i].time-expected[i]) < .001f && actual[i].stringParameter == "impact:"+i,
                clip.name + " contact marker is not aligned to its authoritative slot.");
    }

    private static void ValidateWeightedMeshes(GameObject actor)
    {
        var skins = actor.GetComponentsInChildren<SpriteSkin>(true);
        Require(skins.Length > 0, "The actor contains no SpriteSkin meshes.");
        Require(actor.GetComponentsInChildren<SpriteRenderer>(true).Length >= 10, "The body must consist of separate editable parts.");
        var hasBlendedVertex = false;
        var boundBones = new HashSet<Transform>();
        foreach (var skin in skins)
        {
            var renderer = skin.GetComponent<SpriteRenderer>();
            Require(renderer != null && renderer.sprite != null, "Missing renderer sprite on " + skin.name);
            Require(renderer.sharedMaterial != null && renderer.sprite.texture != null, "Missing material or atlas texture on " + skin.name);
            var sprite = renderer.sprite;
            var bones = skin.boneTransforms;
            Require(skin.rootBone != null && bones != null && bones.Length > 0, "Unbound SpriteSkin " + skin.name);
            var bindPoses = sprite.GetBindPoses();
            Require(bindPoses.Length == bones.Length, "Bind-pose/bone count mismatch on " + skin.name);
            foreach (var bone in bones) { Require(bone != null, "Null bone binding."); boundBones.Add(bone); }
            Require(sprite.HasVertexAttribute(VertexAttribute.BlendWeight), "Missing skin weights on " + skin.name);
            var weights = sprite.GetVertexAttribute<BoneWeight>(VertexAttribute.BlendWeight);
            Require(weights.Length > 0, "Empty mesh weights.");
            for (var i = 0; i < weights.Length; i++)
            {
                var w = weights[i];
                var total = w.weight0 + w.weight1 + w.weight2 + w.weight3;
                Require(Mathf.Abs(total - 1f) < .001f, "Non-normalized vertex weights on " + skin.name);
                CheckInfluence(w.boneIndex0, w.weight0, bones.Length);
                CheckInfluence(w.boneIndex1, w.weight1, bones.Length);
                CheckInfluence(w.boneIndex2, w.weight2, bones.Length);
                CheckInfluence(w.boneIndex3, w.weight3, bones.Length);
                hasBlendedVertex |= w.weight0 > .001f && (w.weight1 > .001f || w.weight2 > .001f || w.weight3 > .001f);
            }
        }
        Require(boundBones.Count >= 15, "The skin does not bind a complete body hierarchy.");
        Require(hasBlendedVertex, "No mesh uses weighted deformation across multiple bones.");
    }

    private static void CheckInfluence(int index, float weight, int boneCount)
    {
        Require(weight >= 0 && weight <= 1, "Invalid bone weight.");
        if (weight > 0) Require(index >= 0 && index < boneCount, "Weight references an absent bone.");
    }

    private static void ValidateManualClock(KaelRig first, KaelRig second)
    {
        var firstMarkers = new List<string>(); var secondMarkers = new List<string>();
        var trails = 0;
        first.Marker += (marker, sequence) =>
        {
            if (marker.StartsWith("impact:",StringComparison.Ordinal)) firstMarkers.Add(sequence+":"+marker);
            if (marker == "weapon_trail") trails++;
        };
        second.Marker += (marker, sequence) => { if (marker.StartsWith("impact:",StringComparison.Ordinal)) secondMarkers.Add(sequence+":"+marker); };
        first.Sample("idle",0,0,0); second.Sample("idle",0,0,0);
        var idle = CaptureBodyPose(first); var otherIdle = CapturePose(second);
        var standing = CaptureDeathBaseline(first);
        first.Sample("attack_cross",101,.23f,.23f);
        Require(firstMarkers.Count == 0, "First cross contact occurred before 240ms.");
        first.Sample("attack_cross",101,.25f,.02f);
        Require(firstMarkers.Count == 1 && firstMarkers[0] == "101:impact:0", "Missing first cross contact.");
        RequireDifferentBodyPose(first,idle,"attack_cross");
        var paused = CapturePose(first);
        for (var i=0;i<4;i++) first.Sample("attack_cross",101,.25f,0);
        EqualPose(first,paused,"Paused cross pose moved");
        EqualPose(second,otherIdle,"One actor changed the other actor");
        Require(secondMarkers.Count == 0 && firstMarkers.Count == 1,"Marker leaked across instances or paused frames.");
        first.Sample("attack_cross",101,.53f,.28f);
        Require(firstMarkers.Count == 2 && firstMarkers[1] == "101:impact:1" && trails == 2,
            "Second same-action contact or repeated trail window was deduplicated.");
        first.Sample("attack_cross",101,.23f,0); first.Sample("attack_cross",101,.60f,.37f);
        Require(firstMarkers.Count == 2 && trails == 2,"Re-sampling duplicated contact or trail markers.");
        second.Sample("attack_cross",201,.53f,.53f);
        Require(secondMarkers.Count == 2,"Independent actor lacks its two contacts.");

        first.Sample("attack_spin",102,.28f,1);
        var rearVisible = false;
        foreach (var renderer in first.GetComponentsInChildren<SpriteRenderer>())
            if (renderer.name == "Head Art") rearVisible = renderer.sprite != null && renderer.sprite.name.IndexOf("rear",StringComparison.OrdinalIgnoreCase) >= 0;
        Require(rearVisible,"Manual graph did not apply the rear-view sprite exchange.");
        RequireDifferentBodyPose(first,idle,"attack_spin");
        first.Sample("attack_jump",103,.37f,1);
        var nearFoot = first.transform.Find("Targets/NearFootTarget");
        Require(nearFoot != null && nearFoot.localPosition.y > .7f,"Jump pose has no raised foot target.");
        RequireDifferentBodyPose(first,idle,"attack_jump");

        var count = firstMarkers.Count;
        first.Sample("skill",104,.39f,1);
        Require(firstMarkers.Count == count,"Ultimate hit before 400ms.");
        first.Sample("skill",104,.41f,.02f);
        first.Sample("skill",104,.93f,.52f);
        Require(firstMarkers.Count == count+2 && firstMarkers[count] == "104:impact:0" && firstMarkers[count+1] == "104:impact:1",
            "Ultimate must emit its two uniquely named authored contacts.");
        RequireDifferentBodyPose(first,idle,"skill");
        first.Sample("skill",105,.39f,.1f); first.Sample("death",106,0,.1f);
        first.Sample("death",106,2,1);
        RequireDeathPose(first, standing);
        var dead = CapturePose(first); first.Sample("death",106,8,1);
        EqualPose(first,dead,"Death end pose did not hold");
        Require(firstMarkers.Count == count+2,"Death allowed a pending ultimate contact.");

        first.ResetPlayback(); first.Sample("idle",0,0,0);
        var loopStart = CapturePose(first); first.Sample("idle",0,Clip(first,"idle").length,1);
        EqualPose(first,loopStart,"Idle loop boundary changed pose");
        first.Sample("attack_cross",101,.53f,.53f);
        Require(firstMarkers.Count == count+4,"Reset did not permit a fresh two-contact action.");
        var sampleSequence=500L;
        foreach (var clip in first.clips)
        {
            first.Sample(clip.name,sampleSequence++,clip.length*.5f,1);
            foreach (var matrix in CapturePose(first))
                for (var component=0;component<16;component++)
                    Require(!float.IsNaN(matrix[component])&&!float.IsInfinity(matrix[component]),clip.name+" produced a non-finite pose.");
        }
        first.ResetPlayback(); second.ResetPlayback();
    }

    private static AnimationClip Clip(KaelRig rig, string name) => Array.Find(rig.clips, clip => clip != null && clip.name == name);
    private static Matrix4x4[] CaptureBodyPose(KaelRig rig)
    {
        // Check visible body bones, not just state/time fields, IK targets or VFX.
        var paths = new[] { "Root", "Root/Hip", "Root/Hip/Torso", "Root/Hip/Torso/Neck/Head",
            "Root/Hip/Torso/UpperArmNear", "Root/Hip/Torso/UpperArmNear/ForearmNear",
            "Root/Hip/Torso/UpperArmNear/ForearmNear/HandNear", "Root/Hip/ThighNear", "Root/Hip/ThighNear/ShinNear" };
        var result = new Matrix4x4[paths.Length];
        for (var i = 0; i < paths.Length; i++)
        {
            var bone = rig.transform.Find(paths[i]);
            Require(bone != null, "Missing body bone " + paths[i]);
            result[i] = Matrix4x4.TRS(bone.localPosition, bone.localRotation, bone.localScale);
        }
        return result;
    }
    private static void RequireDifferentBodyPose(KaelRig rig, Matrix4x4[] idle, string state)
    {
        var actual = CaptureBodyPose(rig);
        var changed = 0;
        for (var bone = 0; bone < actual.Length; bone++)
        {
            var difference = 0f;
            for (var component = 0; component < 16; component++)
                difference = Mathf.Max(difference, Mathf.Abs(actual[bone][component] - idle[bone][component]));
            if (difference > .025f) changed++;
        }
        Require(changed >= 3, state + " must visibly move at least three body bones away from idle; changed " + changed + ".");
    }
    private sealed class DeathBaseline
    {
        public float headHeight, hipHeight;
        public Matrix4x4[] body;
    }
    private static DeathBaseline CaptureDeathBaseline(KaelRig rig)
    {
        return new DeathBaseline {
            headHeight = rig.transform.InverseTransformPoint(rig.transform.Find("Root/Hip/Torso/Neck/Head").position).y,
            hipHeight = rig.transform.InverseTransformPoint(rig.transform.Find("Root/Hip").position).y,
            body = CaptureBodyPose(rig) };
    }
    private static void RequireDeathPose(KaelRig rig, DeathBaseline standing)
    {
        // Check the observable collapse, independent of how the artist divides it
        // between Root, pelvis and spine. Rotating a rigid standing body alone must
        // not pass. End-pose stability is checked separately with repeated samples.
        var fallen = CaptureDeathBaseline(rig);
        Require(fallen.headHeight < standing.headHeight * .7f && fallen.hipHeight < standing.hipHeight * .7f,
            "Death must lower both head and pelvis substantially from the sampled standing pose.");
        RequireDifferentBodyPose(rig, standing.body, "death");
        var articulatedJoints = 0;
        // Local joint angles deliberately exclude Root and hip translation: global
        // tipping or lowering the entire illustration does not articulate a body.
        foreach (var index in new[] { 2, 3, 4, 5, 7, 8 })
        {
            var before = Mathf.Atan2(standing.body[index].m10, standing.body[index].m00) * Mathf.Rad2Deg;
            var after = Mathf.Atan2(fallen.body[index].m10, fallen.body[index].m00) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle(before, after)) > 8) articulatedJoints++;
        }
        Require(articulatedJoints >= 3, "Death must change several spine/arm/leg joints, not only tip the character root.");
    }
    private static Matrix4x4[] CapturePose(KaelRig rig)
    {
        var transforms = rig.GetComponentsInChildren<Transform>(true);
        var pose = new Matrix4x4[transforms.Length];
        for (var i = 0; i < transforms.Length; i++) pose[i] = Matrix4x4.TRS(transforms[i].localPosition, transforms[i].localRotation, transforms[i].localScale);
        return pose;
    }
    private static void EqualPose(KaelRig rig, Matrix4x4[] expected, string message)
    {
        EqualPose(CapturePose(rig), expected, message);
    }
    private static void EqualPose(Matrix4x4[] actual, Matrix4x4[] expected, string message)
    {
        Require(actual.Length == expected.Length, message + ": hierarchy changed.");
        for (var i = 0; i < actual.Length; i++)
            for (var component = 0; component < 16; component++)
                Require(Mathf.Abs(actual[i][component] - expected[i][component]) < .0001f, message + ": bone " + i);
    }
    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Kael animation validation: " + message);
    }
}
