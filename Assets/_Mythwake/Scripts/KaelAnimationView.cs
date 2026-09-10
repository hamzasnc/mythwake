using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Canvas projection of an independently clocked Unity 2D Animation actor.</summary>
public sealed class KaelAnimationView : MonoBehaviour
{
    public KaelRig Rig { get; private set; }
    public event Action<string, long> Marker;
    RawImage surface;
    int cell = -1;
    float idleAge, runAge;
    Vector2 lastPosition;
    bool hadPosition;
    float characterHeight;
    long effectSequence = -1;

    public static KaelAnimationView Create(Transform parent, string name, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(KaelAnimationView));
        go.transform.SetParent(parent, false);
        var view = go.GetComponent<KaelAnimationView>();
        view.surface = go.GetComponent<RawImage>();
        view.surface.raycastTarget = false;
        view.characterHeight = height;
        var rect = view.surface.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
        rect.pivot = new Vector2(.5f, KaelRenderAtlas.FootMargin / KaelRenderAtlas.CellWorldSize);
        rect.sizeDelta = Vector2.one * (height * KaelRenderAtlas.CellWorldSize / KaelRenderAtlas.CharacterWorldHeight);
        view.cell = KaelRenderAtlas.Instance.Acquire(view.surface, out var actor);
        view.Rig = actor;
        actor.Marker += view.OnMarker;
        actor.gameObject.SetActive(view.gameObject.activeInHierarchy);
        return view;
    }

    public void Present(Vector2 topPosition, string state, long sequence, float actionAge, float delta, float facing = 1)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (Rig == null) throw new InvalidOperationException("Kael: required rig/prefab is unavailable.");
        delta = Mathf.Max(0, delta);
        idleAge += delta;
        if (state == "run" && delta > 0 && hadPosition)
        {
            var speed = Vector2.Distance(topPosition, lastPosition) / delta;
            runAge += delta * Mathf.Clamp(speed / 285f, .1f, 3);
        }
        lastPosition = topPosition;
        hadPosition = true;
        surface.rectTransform.localScale = Vector3.one;
        surface.rectTransform.anchoredPosition = topPosition + Vector2.down * characterHeight;
        Rig.transform.localScale = new Vector3(facing < 0 ? -1 : 1, 1, 1);
        Rig.Sample(state, sequence, state == "idle" ? idleAge : state == "run" ? runAge : actionAge, delta);
    }

    public void SetCinematicProjection(Vector2 focusFoot, float strength)
    {
        // A camera-style Canvas projection; the actor and its body bones keep their
        // real combat position/animation. All weapon anchors use this same transform.
        strength = Mathf.Clamp01(strength);
        var rect = surface.rectTransform;
        var foot = lastPosition + Vector2.down * characterHeight;
        rect.anchoredPosition = Vector2.Lerp(foot, focusFoot, strength);
        rect.localScale = Vector3.one * Mathf.Lerp(1, 1.35f, strength);
    }

    public void ResolvedImpact(long sequence, int contactIndex)
    {
        // Used to compare authoritative impact and the clip marker without applying damage twice.
        effectSequence = sequence;
    }
    public Vector3 ProjectRigPoint(Vector3 worldPosition)
    {
        // Rig world coordinates already include facing. Project through the same
        // foot pivot and scale as the atlas image, so VFX stay attached to the blade.
        var offset = worldPosition - Rig.transform.position;
        var scale = characterHeight / KaelRenderAtlas.CharacterWorldHeight;
        return surface.rectTransform.TransformPoint(new Vector3(offset.x * scale, offset.y * scale, 0));
    }
    void OnMarker(string marker, long sequence) { Marker?.Invoke(marker, sequence); }
    public void ResetView()
    {
        idleAge = runAge = 0;
        effectSequence = -1;
        hadPosition = false;
        surface.rectTransform.localScale = Vector3.one;
        if (Rig != null) Rig.ResetPlayback();
        gameObject.SetActive(false);
    }
    void OnEnable() { if (Rig != null) Rig.gameObject.SetActive(true); }
    void OnDisable() { if (Rig != null) Rig.gameObject.SetActive(false); }
    void OnDestroy()
    {
        Marker = null;
        if (Rig != null) Rig.Marker -= OnMarker;
        if (KaelRenderAtlas.Existing != null) KaelRenderAtlas.Existing.Release(cell);
    }
}

/// <summary>One transparent camera/atlas for up to eight visible instances. No camera per hero.</summary>
public sealed class KaelRenderAtlas : MonoBehaviour
{
    public const float CellWorldSize = 6f, FootMargin = .75f, CharacterWorldHeight = 3.1f;
    public static KaelRenderAtlas Existing { get; private set; }
    public static KaelRenderAtlas Instance => Existing != null ? Existing : Create();
    const int Columns = 4, Rows = 2, Resolution = 512;
    readonly KaelRig[] actors = new KaelRig[Columns * Rows];
    RenderTexture atlas;
    Camera renderCamera;
    Material canvasMaterial;
    GameObject prefab;

    static KaelRenderAtlas Create()
    {
        var go = new GameObject("Kael shared canvas render atlas");
        Existing = go.AddComponent<KaelRenderAtlas>();
        try { Existing.Initialize(); }
        catch { Existing = null; Destroy(go); throw; }
        return Existing;
    }
    void Initialize()
    {
        prefab = Resources.Load<GameObject>("Characters/Kael/Kael");
        if (prefab == null) throw new InvalidOperationException("Kael: Characters/Kael/Kael.prefab missing. Run Mythwake > Kael > Rebuild Original Rig.");
        canvasMaterial = Resources.Load<Material>("Characters/Kael/KaelCanvas");
        if (canvasMaterial == null) throw new InvalidOperationException("Kael: premultiplied Canvas material missing.");
        // URP 2D's render graph requires a depth attachment even for unlit sprite output.
        atlas = new RenderTexture(Columns * Resolution, Rows * Resolution, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
        { name = "Kael shared RGBA atlas", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, useMipMap = false, antiAliasing = 1 };
        atlas.Create();
        renderCamera = gameObject.AddComponent<Camera>();
        renderCamera.orthographic = true;
        renderCamera.orthographicSize = Rows * CellWorldSize / 2;
        renderCamera.aspect = (float)Columns / Rows;
        renderCamera.transform.position = new Vector3(10000, 10000, -10);
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = Color.clear;
        renderCamera.cullingMask = 1 << 31;
        renderCamera.nearClipPlane = .1f;
        renderCamera.farClipPlane = 30;
        renderCamera.allowHDR = renderCamera.allowMSAA = false;
        renderCamera.targetTexture = atlas;
        renderCamera.depth = -50;
    }
    public int Acquire(RawImage surface, out KaelRig actor)
    {
        for (var i = 0; i < actors.Length; i++)
        {
            if (actors[i] != null) continue;
            var go = Instantiate(prefab, transform);
            go.name = "Kael actor " + i;
            var x = i % Columns;
            var y = i / Columns;
            go.transform.position = new Vector3(10000 + (x + .5f - Columns / 2f) * CellWorldSize, 10000 + (y - Rows / 2f) * CellWorldSize + FootMargin, 0);
            foreach (var child in go.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            actor = actors[i] = go.GetComponent<KaelRig>();
            if (actor == null) throw new InvalidOperationException("Kael prefab does not contain KaelRig.");
            // Live sword paint is drawn by the Canvas VFX view. The authoring
            // LineRenderer must not leak a second trail into the actor atlas.
            if (actor.weaponTrail != null) actor.weaponTrail.enabled = false;
            surface.texture = atlas;
            surface.material = canvasMaterial;
            surface.uvRect = new Rect(x / (float)Columns, y / (float)Rows, 1f / Columns, 1f / Rows);
            return i;
        }
        throw new InvalidOperationException("Kael: shared atlas capacity exceeded (8 independent instances).");
    }
    public void Release(int cell)
    {
        if (cell < 0 || cell >= actors.Length || actors[cell] == null) return;
        actors[cell].gameObject.SetActive(false);
        Destroy(actors[cell].gameObject);
        actors[cell] = null;
    }
    void LateUpdate()
    {
        var anyVisible = false;
        foreach (var actor in actors) anyVisible |= actor != null && actor.gameObject.activeInHierarchy;
        renderCamera.enabled = anyVisible;
    }
    void OnDestroy()
    {
        if (Existing == this) Existing = null;
        if (renderCamera != null) renderCamera.targetTexture = null;
        if (atlas != null) { atlas.Release(); Destroy(atlas); }
    }
}
