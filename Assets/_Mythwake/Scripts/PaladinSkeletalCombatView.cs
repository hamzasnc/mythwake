using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class PaladinSkeletalCombatView : MonoBehaviour
{
    public enum Clip
    {
        Idle,
        Wait,
        Walk,
        Run,
        Attack1,
        Attack2,
        Death
    }

    private const string PartResourceRoot = "Mythwake/Art/Skeletal/Paladin/parts/";
    private const float TopToGroundOffset = 132f;
    private const float MinScale = 0.08f;
    public const float Attack1TotalSeconds = 1.02f;
    public const float Attack2TotalSeconds = 1.14f;

    private sealed class Part
    {
        public string name;
        public RectTransform rect;
        public RawImage image;
        public Vector2 setupPosition;
        public float setupRotation;
        public float setupScale = 1f;
    }

    private readonly Dictionary<string, Part> parts = new Dictionary<string, Part>();
    private RectTransform root;
    private float baseScale = 0.58f;
    private float previewFacing = 1f;
    private float previewScale = 1f;
    private Vector2 previewTopPosition;
    private bool previewMode;

    public static PaladinSkeletalCombatView Create(Transform parent, string name, Vector2 topPosition, float scale)
    {
        var viewObject = new GameObject(name, typeof(RectTransform), typeof(PaladinSkeletalCombatView));
        viewObject.transform.SetParent(parent, false);

        var view = viewObject.GetComponent<PaladinSkeletalCombatView>();
        view.Initialize(topPosition, scale);
        return view;
    }

    public void Initialize(Vector2 topPosition, float scale)
    {
        root = GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = Vector2.zero;
        baseScale = Mathf.Max(MinScale, scale);
        BuildRig();
        SetTopPosition(topPosition);
        gameObject.SetActive(false);
    }

    public void ShowPreview(Vector2 topPosition, float facingScale, float scaleMultiplier)
    {
        previewMode = true;
        previewTopPosition = topPosition;
        previewFacing = facingScale;
        previewScale = Mathf.Max(MinScale, scaleMultiplier);
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        previewMode = false;
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    public void ApplyCombatPose(
        Clip clip,
        Vector2 topPosition,
        float timer,
        float actionAge,
        float facingScale,
        float scaleMultiplier,
        Color tint,
        bool visible,
        bool hasTarget,
        Vector2 targetPosition,
        float ultimateProgress)
    {
        previewMode = false;
        if (!visible)
        {
            Hide();
            return;
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        ApplyPose(clip, topPosition, timer, actionAge, facingScale, scaleMultiplier, tint, hasTarget, targetPosition, ultimateProgress);
    }

    public void HideTransientEffects()
    {
        SetVisible("fx_sword_slash", false);
        SetVisible("fx_shield_flash", false);
        SetVisible("fx_holy_barrier", false);
    }

    private void Update()
    {
        if (!previewMode)
        {
            return;
        }

        ApplyPose(Clip.Wait, previewTopPosition, Time.unscaledTime, -99f, previewFacing, previewScale, Color.white, false, Vector2.zero, 0f);
    }

    private void BuildRig()
    {
        parts.Clear();

        AddPart("shadow_holy_ring", new Vector2(-1f, 12f), 0f, 0.92f);
        AddPart("cape_back", new Vector2(-34f, 78f), -3f, 1f);
        AddPart("leg_left", new Vector2(-25f, 43f), -3f, 1f);
        AddPart("leg_right", new Vector2(17f, 43f), 3f, 1f);
        AddPart("torso_armor", new Vector2(0f, 106f), 0f, 1f);
        AddPart("belt_gem", new Vector2(1f, 73f), 0f, 1f);
        AddPart("arm_sword", new Vector2(-18f, 96f), -8f, 0.96f);
        AddPart("sword", new Vector2(58f, 128f), -26f, 0.9f);
        AddPart("shield", new Vector2(64f, 78f), 4f, 1f);
        AddPart("head_helmet", new Vector2(4f, 157f), 0f, 1f);
        AddPart("fx_sword_slash", new Vector2(96f, 103f), -20f, 0.82f);
        AddPart("fx_shield_flash", new Vector2(78f, 96f), 0f, 0.8f);
        AddPart("fx_holy_barrier", new Vector2(0f, 100f), 0f, 0.78f);
    }

    private void AddPart(string partName, Vector2 position, float rotation, float scale)
    {
        var texture = Resources.Load<Texture2D>(PartResourceRoot + partName);
        if (texture == null)
        {
            return;
        }

        texture.filterMode = FilterMode.Bilinear;
        var partObject = new GameObject(partName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        partObject.transform.SetParent(root, false);

        var rect = partObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(texture.width, texture.height);
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        rect.localScale = Vector3.one * scale;

        var image = partObject.GetComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = false;
        image.color = Color.white;

        parts[partName] = new Part
        {
            name = partName,
            rect = rect,
            image = image,
            setupPosition = position,
            setupRotation = rotation,
            setupScale = scale
        };
    }

    private void ApplyPose(
        Clip clip,
        Vector2 topPosition,
        float timer,
        float actionAge,
        float facingScale,
        float scaleMultiplier,
        Color tint,
        bool hasTarget,
        Vector2 targetPosition,
        float ultimateProgress)
    {
        var clampedScale = Mathf.Max(MinScale, scaleMultiplier);
        SetTopPosition(topPosition);
        root.localScale = new Vector3(facingScale * baseScale * clampedScale, baseScale * clampedScale, 1f);

        ResetParts(tint);
        HideTransientEffects();

        switch (clip)
        {
            case Clip.Wait:
                ApplyWait(timer);
                break;
            case Clip.Walk:
                ApplyWalk(timer);
                break;
            case Clip.Run:
                ApplyRun(timer);
                break;
            case Clip.Attack1:
                ApplyAttack1(Mathf.Clamp01(Mathf.Max(0f, actionAge) / Attack1TotalSeconds), hasTarget, targetPosition, clampedScale);
                break;
            case Clip.Attack2:
                ApplyAttack2(Mathf.Clamp01(Mathf.Max(0f, actionAge) / Attack2TotalSeconds), hasTarget, targetPosition, clampedScale, ultimateProgress);
                break;
            case Clip.Death:
                ApplyDeath(timer);
                break;
            default:
                ApplyIdle(timer);
                break;
        }
    }

    private void SetTopPosition(Vector2 topPosition)
    {
        root.anchoredPosition = topPosition + new Vector2(0f, -TopToGroundOffset);
    }

    private void ResetParts(Color tint)
    {
        foreach (var part in parts.Values)
        {
            part.rect.anchoredPosition = part.setupPosition;
            part.rect.localRotation = Quaternion.Euler(0f, 0f, part.setupRotation);
            part.rect.localScale = Vector3.one * part.setupScale;
            part.image.color = tint;
            part.image.gameObject.SetActive(true);
        }
    }

    private void ApplyIdle(float timer)
    {
        var breath = Mathf.Sin(timer * 5.1f);
        var slow = Mathf.Sin(timer * 2.1f);
        Offset("torso_armor", new Vector2(0f, breath * 1.9f), slow * 0.9f, 1f);
        Offset("belt_gem", new Vector2(0f, breath * 1.2f), slow * 0.7f, 1f);
        Offset("head_helmet", new Vector2(slow * 0.8f, -breath * 1.4f), -slow * 1.1f, 1f);
        Offset("arm_sword", new Vector2(-slow * 1.4f, breath * 1.2f), slow * 2.2f, 1f);
        Offset("sword", new Vector2(-slow * 1.8f, breath * 1.2f), slow * 3f, 1f);
        Offset("shield", new Vector2(slow * 1.6f, breath * 0.8f), -slow * 2.2f, 1f);
        Offset("cape_back", new Vector2(-slow * 2.2f, -breath * 0.8f), -slow * 2.8f, 1f);
        Offset("shadow_holy_ring", Vector2.zero, 0f, 1f + Mathf.Sin(timer * 3.2f) * 0.02f);
    }

    private void ApplyWait(float timer)
    {
        ApplyIdle(timer);
        var pulse = Mathf.Clamp01(Mathf.Sin(timer * 1.35f) * 0.5f + 0.5f);
        var guard = SmoothStep(pulse);
        Offset("shield", new Vector2(guard * 5f, guard * 5f), -5f - guard * 4f, 1f + guard * 0.03f);
        Offset("sword", new Vector2(-guard * 4f, guard * 3f), -15f + guard * 5f, 1f);
        Offset("head_helmet", new Vector2(guard * 1.8f, 0f), -guard * 2.2f, 1f);
        Offset("fx_shield_flash", new Vector2(78f, 96f), 0f, 0.52f + guard * 0.14f);
        SetPartAlpha("fx_shield_flash", Mathf.Clamp01((pulse - 0.72f) * 3.2f) * 0.25f);
        SetVisible("fx_shield_flash", pulse > 0.72f);
    }

    private void ApplyWalk(float timer)
    {
        ApplyIdle(timer);
        var stride = Mathf.Sin(timer * 8.6f);
        var counter = Mathf.Sin(timer * 8.6f + Mathf.PI);
        var lift = Mathf.Abs(stride);
        var counterLift = Mathf.Abs(counter);
        Offset("leg_left", new Vector2(stride * 4f, lift * 2.5f), stride * 7f, 1f);
        Offset("leg_right", new Vector2(counter * 4f, counterLift * 2.5f), counter * 7f, 1f);
        Offset("torso_armor", new Vector2(0f, Mathf.Sin(timer * 17.2f) * 1.4f), -stride * 1.8f, 1f);
        Offset("belt_gem", new Vector2(0f, Mathf.Sin(timer * 17.2f) * 1.2f), -stride * 1.6f, 1f);
        Offset("cape_back", new Vector2(-7f + stride * 1.2f, 1f), -8f + stride * 2.5f, 1f);
        Offset("shield", new Vector2(3f, 2f), -4f + stride * 2f, 1f);
        Offset("sword", new Vector2(-3f, 2f), -16f + stride * 2.5f, 1f);
    }

    private void ApplyRun(float timer)
    {
        ApplyIdle(timer);
        var stride = Mathf.Sin(timer * 14.4f);
        var counter = Mathf.Sin(timer * 14.4f + Mathf.PI);
        Offset("leg_left", new Vector2(stride * 8f, Mathf.Abs(stride) * 5.5f), stride * 12f, 1f);
        Offset("leg_right", new Vector2(counter * 8f, Mathf.Abs(counter) * 5.5f), counter * 12f, 1f);
        Offset("torso_armor", new Vector2(3f, Mathf.Abs(stride) * 4f), -6f, 1f);
        Offset("belt_gem", new Vector2(3f, Mathf.Abs(stride) * 3.5f), -5f, 1f);
        Offset("head_helmet", new Vector2(2f, Mathf.Abs(stride) * 3f), 5f, 1f);
        Offset("cape_back", new Vector2(-15f + stride * 1.8f, 5f), -15f + stride * 3f, 1f);
        Offset("arm_sword", new Vector2(-6f, 3f), 8f + stride * 2f, 1f);
        Offset("sword", new Vector2(-9f, 5f), -24f + stride * 3f, 1f);
        Offset("shield", new Vector2(8f, 5f), -10f + stride * 2f, 1f);
    }

    private void ApplyAttack1(float phase, bool hasTarget, Vector2 targetPosition, float scaleMultiplier)
    {
        var windup = EaseOut(Mathf.InverseLerp(0f, 0.26f, phase)) * (1f - SmoothStep(Mathf.InverseLerp(0.26f, 0.48f, phase)));
        var slashWeight = Mathf.Sin(Mathf.InverseLerp(0.24f, 0.58f, phase) * Mathf.PI);
        var follow = SmoothStep(Mathf.InverseLerp(0.42f, 0.68f, phase)) * (1f - SmoothStep(Mathf.InverseLerp(0.68f, 1f, phase)));

        Offset("torso_armor", new Vector2(-7f * windup + 13f * slashWeight + 5f * follow, 2f * slashWeight), -6f * windup + 10f * slashWeight + 4f * follow, 1f);
        Offset("belt_gem", new Vector2(-6f * windup + 11f * slashWeight + 4f * follow, 1.6f * slashWeight), -5f * windup + 8f * slashWeight + 3f * follow, 1f);
        Offset("head_helmet", new Vector2(-3f * windup + 4f * slashWeight + 2f * follow, 1f * slashWeight), -2.5f * windup + 2.8f * slashWeight, 1f);
        Offset("arm_sword", new Vector2(-14f * windup + 24f * slashWeight + 10f * follow, -4f * windup + 4f * slashWeight), -18f * windup + 34f * slashWeight + 10f * follow, 1f);
        Offset("sword", new Vector2(-28f * windup + 54f * slashWeight + 16f * follow, 10f * windup - 8f * slashWeight - 3f * follow), -42f * windup + 88f * slashWeight + 26f * follow, 1f);
        Offset("shield", new Vector2(-3f * windup + 6f * slashWeight, 1f * slashWeight), -4f * windup - 4f * slashWeight, 1f);
        Offset("cape_back", new Vector2(-9f * slashWeight - 4f * follow, -1f * slashWeight), -8f * slashWeight - 3f * follow, 1f);

        var slashPart = Get("fx_sword_slash");
        if (slashPart != null)
        {
            var targetLocal = hasTarget ? ToLocal(targetPosition, scaleMultiplier) : new Vector2(145f, 74f);
            var t = EaseInOut(Mathf.InverseLerp(0.28f, 0.7f, phase));
            var alpha = Mathf.Sin(t * Mathf.PI);
            slashPart.rect.anchoredPosition = Vector2.Lerp(new Vector2(74f, 116f), targetLocal, 0.56f + t * 0.22f);
            slashPart.rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-35f, 22f, t));
            slashPart.rect.localScale = Vector3.one * slashPart.setupScale * (0.56f + alpha * 0.34f);
            slashPart.image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha * 1.08f));
            slashPart.image.gameObject.SetActive(phase >= 0.28f && phase <= 0.78f);
        }
    }

    private void ApplyAttack2(float phase, bool hasTarget, Vector2 targetPosition, float scaleMultiplier, float ultimateProgress)
    {
        var brace = EaseOut(Mathf.InverseLerp(0f, 0.24f, phase)) * (1f - SmoothStep(Mathf.InverseLerp(0.24f, 0.46f, phase)));
        var bash = Mathf.Sin(Mathf.InverseLerp(0.22f, 0.6f, phase) * Mathf.PI);
        var settle = SmoothStep(Mathf.InverseLerp(0.48f, 0.72f, phase)) * (1f - SmoothStep(Mathf.InverseLerp(0.72f, 1f, phase)));
        var glow = Mathf.Max(Mathf.Sin(Mathf.InverseLerp(0.08f, 0.9f, phase) * Mathf.PI), Mathf.Sin(ultimateProgress * Mathf.PI));

        Offset("torso_armor", new Vector2(-8f * brace + 11f * bash + 4f * settle, 4f * glow), -7f * brace + 8f * bash + 2f * settle, 1f + glow * 0.025f);
        Offset("belt_gem", new Vector2(-7f * brace + 10f * bash + 3f * settle, 3f * glow), -6f * brace + 7f * bash + 2f * settle, 1f + glow * 0.02f);
        Offset("head_helmet", new Vector2(-3f * brace + 3f * bash, 2f * glow), -2.5f * brace + 2.5f * bash, 1f);
        Offset("leg_left", new Vector2(-4f * brace + 2f * settle, 0f), -6f * brace + 3f * settle, 1f);
        Offset("leg_right", new Vector2(5f * brace - 2f * settle, 1f * bash), 6f * brace - 2f * settle, 1f);
        Offset("shield", new Vector2(-7f * brace + 28f * bash + 8f * settle, 8f * brace + 4f * bash), -16f * brace + 34f * bash + 6f * settle, 1f + glow * 0.04f);
        Offset("arm_sword", new Vector2(-8f * brace + 6f * bash, -2f * brace + 2f * bash), -12f * brace + 10f * bash, 1f);
        Offset("sword", new Vector2(-16f * brace + 12f * bash, 6f * brace - 4f * bash), -30f * brace + 24f * bash, 1f);
        Offset("cape_back", new Vector2(-13f * glow - 4f * settle, 3f * glow), -13f * glow - 4f * settle, 1f + glow * 0.025f);

        var flash = Get("fx_shield_flash");
        if (flash != null)
        {
            var t = Mathf.Clamp01(Mathf.InverseLerp(0.18f, 0.66f, phase));
            flash.rect.anchoredPosition = new Vector2(72f + 28f * bash, 94f + 6f * brace);
            flash.rect.localScale = Vector3.one * flash.setupScale * (0.68f + Mathf.Sin(t * Mathf.PI) * 0.44f);
            flash.rect.localRotation = Quaternion.Euler(0f, 0f, -12f + 28f * bash);
            flash.image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Sin(t * Mathf.PI) * 1.2f));
            flash.image.gameObject.SetActive(phase >= 0.18f && phase <= 0.74f);
        }

        var barrier = Get("fx_holy_barrier");
        if (barrier != null)
        {
            var targetLocal = hasTarget ? ToLocal(targetPosition, scaleMultiplier) : new Vector2(118f, 86f);
            barrier.rect.anchoredPosition = Vector2.Lerp(new Vector2(24f, 100f), targetLocal, 0.25f * bash);
            barrier.rect.localScale = Vector3.one * barrier.setupScale * (0.74f + glow * 0.28f);
            barrier.image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(glow * 0.7f));
            barrier.image.gameObject.SetActive(phase > 0.05f && phase < 0.9f);
        }
    }

    private void ApplyDeath(float timer)
    {
        var fade = 0.58f + Mathf.Sin(timer * 2.5f) * 0.08f;
        foreach (var part in parts.Values)
        {
            part.image.color = new Color(0.76f, 0.72f, 0.66f, fade);
        }

        Offset("torso_armor", new Vector2(26f, -64f), -82f, 0.98f);
        Offset("belt_gem", new Vector2(27f, -66f), -82f, 0.96f);
        Offset("head_helmet", new Vector2(62f, -94f), -92f, 0.96f);
        Offset("arm_sword", new Vector2(16f, -76f), -108f, 1f);
        Offset("sword", new Vector2(58f, -102f), -122f, 0.96f);
        Offset("shield", new Vector2(36f, -88f), -88f, 0.96f);
        Offset("leg_left", new Vector2(16f, -32f), -70f, 0.98f);
        Offset("leg_right", new Vector2(48f, -30f), -78f, 0.98f);
        Offset("cape_back", new Vector2(32f, -88f), -84f, 0.96f);
        HideTransientEffects();
    }

    private Vector2 ToLocal(Vector2 worldPosition, float scaleMultiplier)
    {
        var fallbackScale = baseScale * Mathf.Max(MinScale, scaleMultiplier);
        var currentScale = root.localScale;
        var scaleX = Mathf.Abs(currentScale.x) > 0.0001f ? currentScale.x : fallbackScale;
        var scaleY = Mathf.Abs(currentScale.y) > 0.0001f ? currentScale.y : fallbackScale;
        return new Vector2(
            (worldPosition.x - root.anchoredPosition.x) / scaleX,
            (worldPosition.y - root.anchoredPosition.y) / scaleY);
    }

    private Part Get(string partName)
    {
        return parts.TryGetValue(partName, out var part) ? part : null;
    }

    private void Offset(string partName, Vector2 offset, float rotationOffset, float scaleMultiplier)
    {
        var part = Get(partName);
        if (part == null)
        {
            return;
        }

        part.rect.anchoredPosition = part.setupPosition + offset;
        part.rect.localRotation = Quaternion.Euler(0f, 0f, part.setupRotation + rotationOffset);
        part.rect.localScale = Vector3.one * part.setupScale * Mathf.Max(0.05f, scaleMultiplier);
    }

    private void SetPartAlpha(string partName, float alpha)
    {
        var part = Get(partName);
        if (part == null)
        {
            return;
        }

        var color = part.image.color;
        color.a = alpha;
        part.image.color = color;
    }

    private void SetVisible(string partName, bool isVisible)
    {
        var part = Get(partName);
        if (part != null)
        {
            part.image.gameObject.SetActive(isVisible);
        }
    }

    private static float EaseOut(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t);
    }

    private static float EaseInOut(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private static float SmoothStep(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}
