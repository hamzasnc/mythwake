using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Painted blade trails and contact effects, sampled by the authoritative combat clock.</summary>
public sealed class KaelCombatVfx : MaskableGraphic
{
    struct Burst
    {
        public Vector2 position;
        public float time, facing;
        public bool skill, finalContact;
        public int contactIndex;
        public string actionVariant;
    }
    struct TrailPoint { public Vector2 tip, grip; public float time; }
    readonly List<Burst> bursts = new List<Burst>(6);
    readonly List<TrailPoint> trail = new List<TrailPoint>(32);
    readonly HashSet<int> resolvedContacts = new HashSet<int>();
    Texture2D paintedAtlas;
    Vector2 tip, grip;
    float clock, age, facing = 1;
    string state = "idle";
    long lastImpactSequence = -1, trailSequence = -1;
    bool drawingTrail, lifecycleClosed;

    public int ImpactCount { get; private set; }
    public int SkillImpactCount { get; private set; }
    public int FinalSkillImpactCount { get; private set; }
    public int ActiveImpactCount => bursts.Count;
    public float EffectClock => clock;
    public bool HasCharge => state == "skill" && age >= 0 && age < .38f;
    public override Texture mainTexture => paintedAtlas != null ? paintedAtlas : Texture2D.whiteTexture;

    public static KaelCombatVfx Create(Transform parent, Rect? clippingBounds = null)
    {
        var parentRect = parent as RectTransform;
        var width = parentRect != null && parentRect.rect.width > 0 ? Mathf.Min(960, parentRect.rect.width) : 960;
        var bounds = clippingBounds ?? new Rect(-width * .5f, -980, width, 870);
        var viewport = new GameObject("Kael painted effects viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(parent, false);
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = viewportRect.anchorMax = new Vector2(.5f, 1);
        viewportRect.pivot = new Vector2(.5f, 1);
        viewportRect.anchoredPosition = new Vector2(bounds.center.x, bounds.yMax);
        viewportRect.sizeDelta = bounds.size;
        viewport.GetComponent<RectMask2D>().softness = new Vector2Int(5, 5);
        var go = new GameObject("Kael painted sword and ultimate effects", typeof(RectTransform), typeof(CanvasRenderer), typeof(KaelCombatVfx));
        go.transform.SetParent(viewport.transform, false);
        var effect = go.GetComponent<KaelCombatVfx>();
        effect.paintedAtlas = Resources.Load<Texture2D>("Characters/Kael/VfxAtlas");
        if (effect.paintedAtlas == null)
            throw new InvalidOperationException("Kael: required painted VfxAtlas texture is missing.");
        var rect = effect.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
        rect.pivot = new Vector2(.5f, 1);
        // Keep the established fight-root top-origin coordinates, while the parent
        // clips only effects. Target points and projected blade anchors stay exact.
        rect.anchoredPosition = new Vector2(-bounds.center.x, -bounds.yMax);
        rect.sizeDelta = new Vector2(width, parentRect != null ? parentRect.rect.height : 1200);
        effect.raycastTarget = false;
        return effect;
    }

    public void AnimationMarker(string marker, long sequence)
    {
        if (lifecycleClosed || sequence < trailSequence || string.IsNullOrEmpty(marker)) return;
        if (marker.StartsWith("weapon_trail_end", StringComparison.Ordinal))
        {
            if (sequence == trailSequence) drawingTrail = false;
        }
        else if (marker.StartsWith("weapon_trail", StringComparison.Ordinal))
        {
            // Never bridge a new contact's swing to the preceding swing.
            trail.Clear();
            trailSequence = sequence;
            drawingTrail = true;
        }
    }

    public void SetFrame(KaelAnimationView view, string action, long sequence, float actionAge,
        float combatTime, float direction, float focusStrength = 0)
    {
        if (action == "death") { lifecycleClosed = true; ClearVisuals(); clock = combatTime; return; }
        var oldClock = clock;
        if (facing != (direction < 0 ? -1 : 1)) trail.Clear();
        clock = combatTime; age = actionAge; state = action; facing = direction < 0 ? -1 : 1;
        tip = rectTransform.InverseTransformPoint(view.ProjectRigPoint(view.Rig.weaponTip.position));
        grip = rectTransform.InverseTransformPoint(view.ProjectRigPoint(view.Rig.gripAnchor.position));
        if (!IsAttack(action) && action != "skill") drawingTrail = false;
        if (sequence != trailSequence) { drawingTrail = false; trail.Clear(); }
        var lifetime = action == "skill" ? .16f : .105f;
        for (var i = trail.Count - 1; i >= 0; i--)
            if (clock - trail[i].time > lifetime) trail.RemoveAt(i);
        if (drawingTrail && clock > oldClock)
        {
            if (trail.Count >= 28) trail.RemoveAt(0);
            trail.Add(new TrailPoint { tip = tip, grip = grip, time = clock });
        }
        for (var i = bursts.Count - 1; i >= 0; i--)
            if (clock - bursts[i].time >= BurstDuration(bursts[i])) bursts.RemoveAt(i);
        SetVerticesDirty();
    }

    public void ResolvedImpact(Vector2 target, bool skill, long sequence, int contactIndex,
        bool finalContact, float combatTime, float direction, string actionVariant = null)
    {
        // Every contact has an identity; replay delivery cannot duplicate its visual.
        if (lifecycleClosed || sequence < lastImpactSequence || contactIndex < 0) return;
        if (sequence > lastImpactSequence)
        {
            lastImpactSequence = sequence;
            resolvedContacts.Clear();
        }
        if (!resolvedContacts.Add(contactIndex)) return;
        ImpactCount++;
        if (skill) SkillImpactCount++;
        if (skill && finalContact) FinalSkillImpactCount++;
        if (bursts.Count >= 6) bursts.RemoveAt(0);
        bursts.Add(new Burst { position = target, skill = skill, finalContact = finalContact,
            contactIndex = contactIndex, time = combatTime, facing = direction < 0 ? -1 : 1,
            actionVariant = actionVariant ?? (skill ? "skill" : "attack") });
        SetVerticesDirty();
    }

    void ClearVisuals()
    {
        state = "idle"; drawingTrail = false; trail.Clear(); bursts.Clear(); SetVerticesDirty();
    }
    public void ResetEffects()
    {
        ClearVisuals(); clock = age = 0; lastImpactSequence = trailSequence = -1;
        resolvedContacts.Clear();
        lifecycleClosed = false;
        ImpactCount = SkillImpactCount = FinalSkillImpactCount = 0;
    }
    protected override void OnDisable()
    {
        // Disabling only this renderer is the animation-review mode: logical
        // contact identity and counters must survive while frames keep arriving.
        if (gameObject.activeInHierarchy) ClearVisuals();
        else ResetEffects();
        base.OnDisable();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (paintedAtlas == null) return;
        if (HasCharge) DrawCharge(mesh);
        DrawWeaponTrail(mesh);
        foreach (var burst in bursts) DrawImpact(mesh, burst);
    }

    void DrawCharge(VertexHelper mesh)
    {
        var t = Mathf.Clamp01(age / .38f);
        var blade = tip - grip;
        // Paint stays on the blade; no aura or ground platform.
        Stamp(mesh, 2, Vector2.Lerp(grip, tip, .58f),
            new Vector2(24 + 18 * t, Mathf.Max(36, blade.magnitude * (1 + .12f * t))),
            Mathf.Atan2(blade.y, blade.x) * Mathf.Rad2Deg - 90,
            Mathf.SmoothStep(0, .82f, t), facing < 0);
    }

    void DrawWeaponTrail(VertexHelper mesh)
    {
        if (trail.Count < 2) return;
        var life = state == "skill" ? .16f : .105f;
        var region = Region(state == "attack_spin" || state == "skill" ? 1 : 0);
        for (var i = 1; i < trail.Count; i++)
        {
            var a = trail[i - 1]; var b = trail[i];
            if ((a.tip - b.tip).sqrMagnitude < .01f) continue;
            var alphaA = Mathf.Clamp01(1 - (clock - a.time) / life);
            var alphaB = Mathf.Clamp01(1 - (clock - b.time) / life);
            var u0 = Mathf.Lerp(region.xMin, region.xMax, (i - 1f) / (trail.Count - 1));
            var u1 = Mathf.Lerp(region.xMin, region.xMax, i / (float)(trail.Count - 1));
            var index = mesh.currentVertCount;
            Add(mesh, Vector2.Lerp(a.grip, a.tip, .28f), new Vector2(u0, region.yMin), alphaA * .5f);
            Add(mesh, a.tip, new Vector2(u0, region.yMax), alphaA * .95f);
            Add(mesh, b.tip, new Vector2(u1, region.yMax), alphaB * .95f);
            Add(mesh, Vector2.Lerp(b.grip, b.tip, .28f), new Vector2(u1, region.yMin), alphaB * .5f);
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index + 2, index + 3, index);
        }
    }

    void DrawImpact(VertexHelper mesh, Burst burst)
    {
        var t = Mathf.Clamp01((clock - burst.time) / BurstDuration(burst));
        var strong = burst.skill && burst.finalContact;
        var direction = ContactDirection(burst.actionVariant, burst.contactIndex, burst.facing);
        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        var fade = Mathf.Pow(1 - t, 1.45f);
        var size = strong ? new Vector2(265, 196) : burst.skill ? new Vector2(125, 104) : new Vector2(76, 68);
        Stamp(mesh, strong ? 4 : 3, burst.position, size * (1 + .14f * Mathf.Sqrt(t)),
            angle, fade, burst.facing < 0);
        if (strong)
        {
            Stamp(mesh, 5, burst.position + direction * (15 + t * 42),
                new Vector2(220 + 55 * t, 120 - 35 * t), angle, .78f * fade, burst.facing < 0);
            Stamp(mesh, 6, burst.position + direction * (32 + 95 * t) + Vector2.down * (t * t * 27),
                new Vector2(100 + 35 * t, 72 + 28 * t), angle - burst.facing * 12,
                .65f * fade, burst.facing < 0);
        }
    }

    static bool IsAttack(string action) => action == "attack" || (action != null && action.StartsWith("attack_", StringComparison.Ordinal));
    static Vector2 ContactDirection(string action, int contactIndex, float facing)
    {
        if (action == "attack_spin") return new Vector2(facing, 0);
        var rises = action == "skill_legacy" || (action == "skill" && contactIndex == 0) ||
            (action == "attack_cross" && contactIndex == 1);
        return new Vector2(.7f * facing, rises ? .72f : -.72f).normalized;
    }
    static float BurstDuration(Burst burst) => burst.skill && burst.finalContact ? .42f : burst.skill ? .23f : .17f;

    Rect Region(int index)
    {
        // Top-left to bottom-right; inset prevents neighbour-cell bilinear bleeding.
        var padX = .5f / paintedAtlas.width;
        var padY = .5f / paintedAtlas.height;
        return new Rect(index % 4 * .25f + padX, (1 - index / 4) * .5f + padY,
            .25f - 2 * padX, .5f - 2 * padY);
    }
    void Stamp(VertexHelper mesh, int regionIndex, Vector2 center, Vector2 size, float angle, float alpha, bool flipY)
    {
        if (alpha <= .001f || size.x <= 0 || size.y <= 0) return;
        var radians = angle * Mathf.Deg2Rad;
        var x = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * size.x * .5f;
        var y = new Vector2(-Mathf.Sin(radians), Mathf.Cos(radians)) * size.y * .5f;
        var region = Region(regionIndex);
        var bottom = flipY ? region.yMax : region.yMin;
        var top = flipY ? region.yMin : region.yMax;
        var index = mesh.currentVertCount;
        Add(mesh, center - x - y, new Vector2(region.xMin, bottom), alpha);
        Add(mesh, center - x + y, new Vector2(region.xMin, top), alpha);
        Add(mesh, center + x + y, new Vector2(region.xMax, top), alpha);
        Add(mesh, center + x - y, new Vector2(region.xMax, bottom), alpha);
        mesh.AddTriangle(index, index + 1, index + 2);
        mesh.AddTriangle(index + 2, index + 3, index);
    }
    static void Add(VertexHelper mesh, Vector2 position, Vector2 uv, float alpha)
        => mesh.AddVert(position, new Color(1, 1, 1, Mathf.Clamp01(alpha)), uv);
}
