using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.U2D.IK;

/// <summary>Original Kael rig with editable clips and anatomical constraints; combat owns the clock.</summary>
public sealed class KaelRig : MonoBehaviour
{
    [Serializable] public sealed class DrawingSocket
    {
        public Sprite sprite;
        public Vector2 position;
    }
    [Serializable] public sealed class BodySocket
    {
        public SpriteRenderer drawing;
        public Transform childBone;
        public DrawingSocket[] views;
    }
    public AnimationClip[] clips;
    // Derived by the builder from the actual death clip and assembled hand/weapon.
    public AnimationCurve deathWristAngle, deathHandHeight;
    public Vector2 deathHandToTip;
    public BodySocket[] bodySockets;
    public Transform weaponTip;
    public Transform gripAnchor;
    public Transform vfxAnchor;
    public IKManager2D ik;
    public LineRenderer weaponTrail;
    public Transform shoulderArmor;
    [Range(0, 1)] public float shoulderArmorFollow = .22f;
    [Range(0, 45)] public float shoulderArmorLimit = 18;
    public event Action<string, long> Marker;
    public string CurrentState { get; private set; } = "idle";
    public float CurrentTime { get; private set; }
    PlayableGraph graph;
    AnimationPlayableOutput output;
    sealed class PosePlayback
    {
        public Playable playable;
        public AnimationClip clip;
        public PosePlayback incoming, outgoing;
        public float time, blendAge, blendDuration;
        public bool freezeOutgoing;
    }
    PosePlayback pose, current;
    PosePlayback deathHandover;
    Transform swordHand;
    float deathSourceBladeAngle, deathDestinationBladeAngle, deathBladeDelta;
    bool deathHasAdvanced;
    readonly Dictionary<string, AnimationClip> clipsByName = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
    long sequence = long.MinValue;
    bool initialized;
    readonly HashSet<string> emittedMarkers = new HashSet<string>();
    readonly List<Vector3> trailPoints = new List<Vector3>();
    long markerSequence = long.MinValue;
    bool trailActive;

    void Initialize()
    {
        if (initialized) return;
        if (clips == null || clips.Length == 0) throw new InvalidOperationException("Kael: authored Unity animation clips are required.");
        if (bodySockets == null || bodySockets.Length == 0) throw new InvalidOperationException("Kael: registered body sockets are required. Rebuild the Kael prefab.");
        foreach (var socket in bodySockets)
            if (socket.drawing == null || socket.childBone == null || socket.drawing.transform.parent != socket.childBone.parent)
                throw new InvalidOperationException("Kael: body socket must share the drawing's parent bone.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        clipsByName.Clear();
        foreach (var clip in clips)
        {
            if (clip == null || !names.Add(clip.name)) throw new InvalidOperationException("Kael: missing or duplicate authored clip.");
            clipsByName.Add(clip.name, clip);
        }
        foreach (var required in new[] { "idle", "run", "attack_cross", "attack_spin", "attack_jump", "skill", "hit", "death" })
            if (!names.Contains(required)) throw new InvalidOperationException("Kael: missing authored clip " + required);
        if (weaponTip == null || gripAnchor == null || weaponTip.parent != gripAnchor.parent || weaponTip.parent.parent == null)
            throw new InvalidOperationException("Kael: weapon tip and grip must share the sword on its hand bone.");
        if (deathWristAngle == null || deathWristAngle.length == 0 || deathHandHeight == null ||
            deathHandHeight.length == 0 || deathHandToTip.sqrMagnitude < .000001f)
            throw new InvalidOperationException("Kael: rebuild the prefab's authored death hand profile.");
        swordHand = weaponTip.parent.parent;
        var animator = GetComponent<Animator>();
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        // The prefab's controller is an editable authoring preview. At runtime the
        // combat-clock graph is the sole animation driver; an attached controller
        // would write its default idle pose again during Unity's animation update.
        animator.runtimeAnimatorController = null;
        graph = PlayableGraph.Create("Kael manual animation " + GetInstanceID());
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        output = AnimationPlayableOutput.Create(graph, "Kael", animator);
        graph.Play();
        if (ik != null) ik.enabled = false; // Explicitly evaluated after this instance's graph.
        initialized = true;
    }

    public void Sample(string state, long actionSequence, float age, float delta)
    {
        Initialize();
        if (!clipsByName.TryGetValue(state, out var clip)) throw new InvalidOperationException("Kael: missing animation " + state);
        delta = Mathf.Max(0, delta);
        age = state == "idle" || state == "run" ? Mathf.Repeat(Mathf.Max(0, age), clip.length) : Mathf.Clamp(age, 0, clip.length);
        var changed = current == null || CurrentState != state || sequence != actionSequence;
        var oldAge = changed ? -0.001f : CurrentTime;
        if (changed)
        {
            deathHandover = null;
            deathBladeDelta = 0;
            var hasDeathSource = state == "death" && current != null;
            deathHasAdvanced = !hasDeathSource;
            if (hasDeathSource) ChooseDeathSwordRoute();
            trailActive = false;
            trailPoints.Clear();
            if (weaponTrail != null) weaponTrail.positionCount = 0;
            // A state selected during pause has not contributed a visible pose.
            // Replace that pending destination rather than accumulating zero-weight
            // branches if several commands arrive while the combat clock is stopped.
            if (pose != null && pose.clip == null && pose.blendAge == 0)
            {
                var pending = pose;
                graph.Disconnect(pending.playable, 0);
                graph.Disconnect(pending.playable, 1);
                graph.DestroySubgraph(pending.incoming.playable);
                graph.DestroyPlayable(pending.playable);
                pose = pending.outgoing;
            }
            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);
            playable.SetApplyPlayableIK(false);
            playable.SetSpeed(0);
            current = new PosePlayback { playable = playable, clip = clip, time = age };
            if (pose == null) pose = current;
            else
            {
                var mixer = AnimationMixerPlayable.Create(graph, 2);
                graph.Connect(current.playable, 0, mixer, 0);
                graph.Connect(pose.playable, 0, mixer, 1);
                pose = new PosePlayback { playable = mixer, incoming = current, outgoing = pose,
                    // Death releases the pose that actually received the killing hit.
                    // Let its clip reach the collapse before it fully owns the body;
                    // a 45ms blend first reset an interrupted strike into Guard.
                    freezeOutgoing = state == "death",
                    blendDuration = state == "death" ? DeathHandoverDuration : state == "hit" ? .035f :
                        state == "idle" ? .085f : state == "run" ? .09f : .06f };
                if (state == "death") deathHandover = pose;
            }
            CurrentState = state;
            sequence = actionSequence;
        }
        var previousPose = pose;
        pose = AdvancePose(pose, delta, age);
        if (changed || pose != previousPose) output.SetSourcePlayable(pose.playable);
        graph.Evaluate(0);
        // Sprite tracks switch discretely, while the mixer interpolates transform
        // positions. Anatomical sockets must belong to the displayed drawing even
        // during a rear-to-front blend; otherwise shoulders and hips disconnect.
        // Only attachment positions are constrained. Root motion, body rotation,
        // cloth deformation and animated IK targets retain their authored motion.
        foreach (var socket in bodySockets)
        {
            DrawingSocket visible = null;
            foreach (var candidate in socket.views)
            {
                if (candidate.sprite != socket.drawing.sprite) continue;
                visible = candidate; break;
            }
            if (visible == null) throw new InvalidOperationException("Kael: no anatomical socket for " + socket.drawing.sprite?.name);
            // Work in their shared parent space. Atlas actors live around (10000,
            // 10000); a world-space round trip loses local precision there and can
            // make repeated paused IK samples drift by a pixel.
            var drawing = socket.drawing.transform;
            socket.childBone.localPosition = drawing.localPosition + drawing.localRotation *
                Vector3.Scale(drawing.localScale, new Vector3(visible.position.x,visible.position.y,0));
        }
        if (ik != null) ik.UpdateManager();
        if (state == "death")
        {
            deathHasAdvanced |= delta > 0;
            if (deathHasAdvanced) KeepDeathSwordAboveGround(age);
        }
        if (shoulderArmor != null)
        {
            // A strapped shoulder cap follows the shoulder joint, not the full
            // humerus rotation. Without this separate rigid attachment an upward
            // arm swing inverted the entire plate into the armpit.
            var armAngle = Mathf.DeltaAngle(0, shoulderArmor.parent.localEulerAngles.z);
            var armorAngle = Mathf.Clamp(armAngle * shoulderArmorFollow, -shoulderArmorLimit, shoulderArmorLimit);
            shoulderArmor.localRotation = Quaternion.Euler(0, 0, armorAngle - armAngle);
        }
        CurrentTime = age;
        if (sequence > markerSequence) { markerSequence = sequence; emittedMarkers.Clear(); }
        // Read the authored AnimationEvents with our clock. They never mutate battle state.
        var markers = clip.events;
        for (var markerIndex = 0; markerIndex < markers.Length; markerIndex++)
        {
            var marker = markers[markerIndex];
            if (marker.functionName == nameof(OnKaelAnimationMarker) && marker.time > oldAge && marker.time <= age)
            {
                // Repeated trail windows belong to one action. Identity is the authored
                // marker, not its label, so the second cut is neither lost nor repeated.
                var key = sequence + ":" + state + ":" + markerIndex;
                if (sequence >= markerSequence && emittedMarkers.Add(key))
                {
                    if (marker.stringParameter == "weapon_trail") trailActive = true;
                    if (marker.stringParameter == "weapon_trail_end") trailActive = false;
                    Marker?.Invoke(marker.stringParameter, sequence);
                }
            }
        }
        if (weaponTrail != null)
        {
            if (!trailActive) { trailPoints.Clear(); weaponTrail.positionCount = 0; }
            else if (delta > 0 && weaponTip != null)
            {
                trailPoints.Add(transform.InverseTransformPoint(weaponTip.position));
                if (trailPoints.Count > 9) trailPoints.RemoveAt(0);
                weaponTrail.positionCount = trailPoints.Count;
                weaponTrail.SetPositions(trailPoints.ToArray());
            }
        }
    }

    Matrix4x4 ToAncestor(Transform bone, Transform ancestor)
    {
        var result = Matrix4x4.identity;
        while (bone != ancestor)
        {
            result = Matrix4x4.TRS(bone.localPosition, bone.localRotation, bone.localScale) * result;
            bone = bone.parent;
        }
        return result;
    }

    const float DeathHandoverDuration = .20f, DeathSwordFloor = .04f;

    float AuthoredDeathBladeAngle(float age)
        => deathWristAngle.Evaluate(age) + Mathf.Atan2(deathHandToTip.y, deathHandToTip.x) * Mathf.Rad2Deg;

    void ChooseDeathSwordRoute()
    {
        var handToActor = ToAncestor(swordHand, transform);
        var tipInHand = ToAncestor(weaponTip, swordHand).MultiplyPoint3x4(Vector3.zero);
        var direction = handToActor.MultiplyVector(tipInHand);
        deathSourceBladeAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        deathDestinationBladeAngle = AuthoredDeathBladeAngle(DeathHandoverDuration);
        var shortest = Mathf.DeltaAngle(deathSourceBladeAngle, deathDestinationBladeAngle);
        var other = shortest > 0 ? shortest - 360 : shortest + 360;
        var sourceHeight = handToActor.MultiplyPoint3x4(Vector3.zero).y;
        var shortClearance = PredictedDeathSwordClearance(shortest, sourceHeight, direction.magnitude);
        var otherClearance = PredictedDeathSwordClearance(other, sourceHeight, direction.magnitude);
        // This decision depends on the frozen source and the authored destination,
        // never on the first rendered delta or the quaternion mixer's early turn.
        // Keep safe short paths; raised weapons do not automatically take a long arc.
        deathBladeDelta = shortClearance < DeathSwordFloor + .02f && otherClearance > shortClearance + .001f
            ? other : shortest;
    }

    float PredictedDeathSwordClearance(float route, float sourceHeight, float sourceLength)
    {
        var clearance = float.PositiveInfinity;
        for (var sample = 0; sample <= 24; sample++)
        {
            var phase = sample / 24f;
            var age = phase * DeathHandoverDuration;
            var weight = Mathf.SmoothStep(0, 1, phase);
            var angle = deathSourceBladeAngle + weight * (route +
                Mathf.DeltaAngle(deathDestinationBladeAngle, AuthoredDeathBladeAngle(age)));
            var height = Mathf.Lerp(sourceHeight, deathHandHeight.Evaluate(age), weight);
            var length = Mathf.Lerp(sourceLength, deathHandToTip.magnitude, weight);
            clearance = Mathf.Min(clearance, height + Mathf.Sin(angle * Mathf.Deg2Rad) * length);
        }
        return clearance;
    }

    void KeepDeathSwordAboveGround(float age)
    {
        // Work below the mirrored actor transform. Local TRS accumulation avoids
        // losing subpixel precision at the shared atlas's (10000, 10000) origin.
        var parentToActor = ToAncestor(swordHand.parent, transform);
        var tipInHand = ToAncestor(weaponTip, swordHand).MultiplyPoint3x4(Vector3.zero);
        var tipOffset = swordHand.localRotation * Vector3.Scale(swordHand.localScale, tipInHand);
        var direction = parentToActor.MultiplyVector(tipOffset);
        if (deathHandover != null && deathHandover.blendAge >= deathHandover.blendDuration)
        {
            deathHandover = null;
            deathBladeDelta = 0;
        }
        if (deathHandover != null && deathHandover.blendAge < deathHandover.blendDuration)
        {
            var weight = Mathf.SmoothStep(0, 1, deathHandover.blendAge / deathHandover.blendDuration);
            // Interpolate the physical blade direction along the chosen continuous
            // winding. A raw source/destination quaternion can change hemisphere
            // while the destination itself moves, producing different 1x/2x paths.
            var desired = (deathSourceBladeAngle + weight * (deathBladeDelta +
                Mathf.DeltaAngle(deathDestinationBladeAngle, AuthoredDeathBladeAngle(age)))) * Mathf.Deg2Rad;
            var inParent = parentToActor.inverse.MultiplyVector(new Vector3(Mathf.Cos(desired), Mathf.Sin(desired), 0));
            var turn = Mathf.DeltaAngle(Mathf.Atan2(tipOffset.y, tipOffset.x) * Mathf.Rad2Deg,
                Mathf.Atan2(inParent.y, inParent.x) * Mathf.Rad2Deg);
            swordHand.localRotation = Quaternion.AngleAxis(turn, Vector3.forward) * swordHand.localRotation;
            tipOffset = swordHand.localRotation * Vector3.Scale(swordHand.localScale, tipInHand);
            direction = parentToActor.MultiplyVector(tipOffset);
        }
        var pivot = parentToActor.MultiplyPoint3x4(swordHand.localPosition);
        if (pivot.y + direction.y >= DeathSwordFloor) return;
        // Rotating the hand preserves the authored grip and all sword/VFX anchors.
        // Solve y(theta) = pivotY + A*cos(theta) + B*sin(theta) at floor contact.
        var a = direction.y;
        var b = parentToActor.MultiplyVector(new Vector3(-tipOffset.y, tipOffset.x, 0)).y;
        var radius = Mathf.Sqrt(a * a + b * b);
        if (radius < .000001f) return;
        var contact = Mathf.Asin(Mathf.Clamp((DeathSwordFloor - pivot.y) / radius, -1, 1)) * Mathf.Rad2Deg;
        var phase = Mathf.Atan2(a, b) * Mathf.Rad2Deg;
        var first = Mathf.DeltaAngle(0, contact - phase);
        var second = Mathf.DeltaAngle(0, 180 - contact - phase);
        var correction = Mathf.Abs(first) <= Mathf.Abs(second) ? first : second;
        swordHand.localRotation = Quaternion.AngleAxis(correction, Vector3.forward) * swordHand.localRotation;
    }

    PosePlayback AdvancePose(PosePlayback value, float delta, float actionAge)
    {
        if (value.clip != null)
        {
            // Outgoing clips keep their momentum during the short handover. Their
            // markers are never consumed; only the current action owns events.
            value.time = value == current ? actionAge : value.time + delta;
            var looping = value.clip.name == "idle" || value.clip.name == "run";
            value.time = looping ? Mathf.Repeat(value.time, value.clip.length) : Mathf.Min(value.time, value.clip.length);
            value.playable.SetTime(value.time);
            return value;
        }
        // Freeze the entire visible source mixture for death, including older blend
        // weights and drawing times. Advancing an interrupted spin otherwise turned
        // its back to the viewer before snapping into the front-facing collapse.
        var outgoing = value.freezeOutgoing ? value.outgoing : AdvancePose(value.outgoing, delta, actionAge);
        if (outgoing != value.outgoing)
        {
            graph.Disconnect(value.playable, 1);
            graph.Connect(outgoing.playable, 0, value.playable, 1);
            value.outgoing = outgoing;
        }
        value.incoming = AdvancePose(value.incoming, delta, actionAge);
        value.blendAge += delta;
        if (value.blendAge >= value.blendDuration)
        {
            // Finish older transitions even when a newer transition interrupts them.
            // This preserves the visible mixture without retaining a growing graph.
            graph.Disconnect(value.playable, 0);
            graph.Disconnect(value.playable, 1);
            graph.DestroySubgraph(value.outgoing.playable);
            graph.DestroyPlayable(value.playable);
            return value.incoming;
        }
        var blend = Mathf.SmoothStep(0, 1, value.blendAge / value.blendDuration);
        value.playable.SetInputWeight(0, blend);
        value.playable.SetInputWeight(1, 1 - blend);
        return value;
    }

    public void OnKaelAnimationMarker(string marker) { /* Automatic event evaluation is deliberately inert. */ }
    public void ResetPlayback()
    {
        sequence = long.MinValue;
        CurrentTime = 0;
        if (graph.IsValid()) graph.Destroy();
        initialized = false;
        clipsByName.Clear();
        emittedMarkers.Clear();
        markerSequence = long.MinValue;
        trailPoints.Clear(); trailActive = false;
        if (weaponTrail != null) weaponTrail.positionCount = 0;
        pose = current = null;
        deathHandover = null;
        deathHasAdvanced = false;
        deathBladeDelta = 0;
    }
    void OnDestroy() { Marker = null; ResetPlayback(); }
}
