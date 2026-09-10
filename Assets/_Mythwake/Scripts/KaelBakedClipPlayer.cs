using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>Plays a finished bone clip. No IK, socket repair, pose correction or combat side effects.</summary>
public sealed class KaelBakedClipPlayer : MonoBehaviour
{
    public AnimationClip clip;
    public bool playAutomatically = true;
    [Min(0)] public float playbackSpeed = 1;
    public bool loop;
    public float CurrentTime { get; private set; }
    PlayableGraph graph;
    AnimationClipPlayable playback;
    AnimationClip boundClip;
    float elapsed;

    public void Sample(float age)
    {
        if (clip == null) throw new InvalidOperationException("Kael baked study: a finished animation clip is required.");
        if (graph.IsValid() && boundClip != clip) ResetPlayback();
        if (!graph.IsValid())
        {
            var animator = GetComponent<Animator>();
            if (animator == null) throw new InvalidOperationException("Kael baked study: Animator is missing.");
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            animator.fireEvents = false; // Marker data stays in the clip; this study cannot resolve combat.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            graph = PlayableGraph.Create("Kael baked study " + GetInstanceID());
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            playback = AnimationClipPlayable.Create(graph, clip);
            boundClip = clip;
            playback.SetApplyFootIK(false);
            playback.SetApplyPlayableIK(false);
            playback.SetSpeed(0);
            AnimationPlayableOutput.Create(graph, "Baked bones", animator).SetSourcePlayable(playback);
            graph.Play();
        }
        CurrentTime = Mathf.Clamp(age, 0, clip.length);
        playback.SetTime(CurrentTime);
        graph.Evaluate(0);
    }

    void Update()
    {
        if (!playAutomatically || clip == null) return;
        elapsed += Time.deltaTime * Mathf.Max(0, playbackSpeed);
        Sample(loop && clip.length > 0 ? Mathf.Repeat(elapsed, clip.length) : elapsed);
    }

    public void ResetPlayback()
    {
        if (graph.IsValid()) graph.Destroy();
        playback = default;
        boundClip = null;
        elapsed = CurrentTime = 0;
    }
    void OnDisable() { ResetPlayback(); }
    void OnDestroy() { ResetPlayback(); }
}
