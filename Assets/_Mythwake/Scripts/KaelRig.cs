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
    AnimationMixerPlayable mixer;
    AnimationClipPlayable current, previous;
    long sequence = long.MinValue;
    float blendAge;
    float previousTime;
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
        foreach (var clip in clips)
            if (clip == null || !names.Add(clip.name)) throw new InvalidOperationException("Kael: missing or duplicate authored clip.");
        foreach (var required in new[] { "idle", "run", "attack_cross", "attack_spin", "attack_jump", "skill", "hit", "death" })
            if (!names.Contains(required)) throw new InvalidOperationException("Kael: missing authored clip " + required);
        var animator = GetComponent<Animator>();
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        // The prefab's controller is an editable authoring preview. At runtime the
        // combat-clock graph is the sole animation driver; an attached controller
        // would write its default idle pose again during Unity's animation update.
        animator.runtimeAnimatorController = null;
        graph = PlayableGraph.Create("Kael manual animation " + GetInstanceID());
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        mixer = AnimationMixerPlayable.Create(graph, 2);
        AnimationPlayableOutput.Create(graph, "Kael", animator).SetSourcePlayable(mixer);
        graph.Play();
        if (ik != null) ik.enabled = false; // Explicitly evaluated after this instance's graph.
        initialized = true;
    }

    public void Sample(string state, long actionSequence, float age, float delta)
    {
        Initialize();
        var clip = Array.Find(clips, value => value != null && value.name == state);
        if (clip == null) throw new InvalidOperationException("Kael: missing animation " + state);
        delta = Mathf.Max(0, delta);
        age = state == "idle" || state == "run" ? Mathf.Repeat(Mathf.Max(0, age), clip.length) : Mathf.Clamp(age, 0, clip.length);
        var changed = !current.IsValid() || CurrentState != state || sequence != actionSequence;
        var oldAge = changed ? -0.001f : CurrentTime;
        if (changed)
        {
            trailActive = false;
            trailPoints.Clear();
            if (weaponTrail != null) weaponTrail.positionCount = 0;
            if (previous.IsValid()) graph.DestroyPlayable(previous);
            if (current.IsValid())
            {
                graph.Disconnect(mixer, 0);
                previous = current;
                previousTime = CurrentTime;
                graph.Connect(previous, 0, mixer, 1);
            }
            current = AnimationClipPlayable.Create(graph, clip);
            current.SetApplyFootIK(false);
            current.SetApplyPlayableIK(false);
            current.SetSpeed(0);
            graph.Connect(current, 0, mixer, 0);
            CurrentState = state;
            sequence = actionSequence;
            blendAge = 0;
        }
        blendAge += delta;
        var blend = previous.IsValid() ? Mathf.Clamp01(blendAge / (state == "death" ? .045f : .065f)) : 1;
        // A newly shown preview also has a pose on a paused (zero-delta) frame.
        if (changed && delta == 0) { blend = 1; blendAge = .065f; }
        mixer.SetInputWeight(0, blend);
        mixer.SetInputWeight(1, 1 - blend);
        current.SetTime(age);
        if (previous.IsValid()) previous.SetTime(previousTime);
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

    public void OnKaelAnimationMarker(string marker) { /* Automatic event evaluation is deliberately inert. */ }
    public void ResetPlayback()
    {
        sequence = long.MinValue;
        CurrentTime = 0;
        if (graph.IsValid()) graph.Destroy();
        initialized = false;
        emittedMarkers.Clear();
        markerSequence = long.MinValue;
        trailPoints.Clear(); trailActive = false;
        if (weaponTrail != null) weaponTrail.positionCount = 0;
        current = default;
        previous = default;
    }
    void OnDestroy() { Marker = null; ResetPlayback(); }
}
