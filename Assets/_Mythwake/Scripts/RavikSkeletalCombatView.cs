using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class RavikSkeletalCombatView : MonoBehaviour
{
    public enum Clip
    {
        Idle,
        Run,
        Attack,
        Skill,
        Ultimate,
        Death
    }

    private const string PartResourceRoot = "Mythwake/Art/Skeletal/Ravik/parts/";
    private const float TopToGroundOffset = 178f;
    private const float MinScale = 0.08f;

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
    private float baseScale = 0.38f;
    private float previewFacing = 1f;
    private float previewScale = 1f;
    private Vector2 previewTopPosition;
    private bool previewMode;

    public static RavikSkeletalCombatView Create(Transform parent, string name, Vector2 topPosition, float scale)
    {
        var viewObject = new GameObject(name, typeof(RectTransform), typeof(RavikSkeletalCombatView));
        viewObject.transform.SetParent(parent, false);

        var view = viewObject.GetComponent<RavikSkeletalCombatView>();
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
        SetVisible("projectile_fireball", false);
        SetVisible("fx_flame_burst", false);
        SetVisible("hand_left_flame", false);
        SetVisible("hand_right_flame", false);
    }

    private void Update()
    {
        if (!previewMode)
        {
            return;
        }

        ApplyPose(Clip.Idle, previewTopPosition, Time.unscaledTime, -99f, previewFacing, previewScale, Color.white, false, Vector2.zero, 0f);
    }

    private void BuildRig()
    {
        parts.Clear();

        AddPart("shadow_fire_ring", new Vector2(0f, -8f), 0f, 0.7f);
        AddPart("cloak_left", new Vector2(-46f, 92f), -4f, 0.46f);
        AddPart("cloak_mid", new Vector2(-6f, 94f), 0f, 0.46f);
        AddPart("cloak_right", new Vector2(44f, 94f), 4f, 0.44f);
        AddPart("leg_left_boot", new Vector2(-22f, 50f), -3f, 0.42f);
        AddPart("leg_right_boot", new Vector2(24f, 49f), 3f, 0.42f);
        AddPart("leg_left_lower", new Vector2(-16f, 71f), -2f, 0.42f);
        AddPart("leg_right_lower", new Vector2(22f, 72f), 2f, 0.42f);
        AddPart("torso", new Vector2(2f, 134f), 0f, 0.52f);
        AddPart("belt_vials", new Vector2(4f, 95f), 0f, 0.4f);
        AddPart("arm_left_bent", new Vector2(-44f, 138f), 5f, 0.43f);
        AddPart("arm_right_open", new Vector2(46f, 140f), -5f, 0.43f);
        AddPart("collar", new Vector2(0f, 139f), 0f, 0.42f);
        AddPart("hair_back", new Vector2(0f, 209f), 0f, 0.46f);
        AddPart("head_smirk", new Vector2(0f, 195f), 0f, 0.45f);
        AddPart("head_neutral", new Vector2(0f, 195f), 0f, 0.45f);
        AddPart("head_shout", new Vector2(0f, 195f), 0f, 0.45f);
        AddPart("hair_front", new Vector2(0f, 216f), 0f, 0.45f);
        AddPart("projectile_fireball", new Vector2(106f, 108f), 0f, 0.36f);
        AddPart("fx_flame_burst", new Vector2(145f, 2f), 0f, 0.52f);
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
        SetVisible("projectile_fireball", false);
        SetVisible("fx_flame_burst", false);
        SetVisible("hand_left_flame", false);
        SetVisible("hand_right_flame", false);

        switch (clip)
        {
            case Clip.Run:
                ApplyRun(timer);
                break;
            case Clip.Attack:
                ApplyAttack(Mathf.Clamp01(actionAge / 0.72f), hasTarget, targetPosition, clampedScale);
                break;
            case Clip.Skill:
                ApplySkill(Mathf.Clamp01(actionAge / 0.9f), timer);
                break;
            case Clip.Ultimate:
                ApplyUltimate(Mathf.Clamp01(ultimateProgress), hasTarget, targetPosition, clampedScale);
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

        SetVisible("head_neutral", false);
        SetVisible("head_shout", false);
        SetVisible("head_smirk", true);
    }

    private void ApplyIdle(float timer)
    {
        var breath = Mathf.Sin(timer * 5.4f);
        var slow = Mathf.Sin(timer * 2.2f);
        Offset("torso", new Vector2(0f, breath * 2.6f), slow * 1.4f, 1f);
        Offset("collar", new Vector2(0f, breath * 2.3f), slow * 1.2f, 1f);
        Offset("belt_vials", new Vector2(0f, breath * 1.8f), slow * 1.1f, 1f);
        Offset("head_smirk", new Vector2(0f, -breath * 1.8f), -slow * 1.5f, 1f);
        Offset("hair_front", new Vector2(slow * 1.8f, -breath * 1.4f), -slow * 2.2f, 1f);
        Offset("hair_back", new Vector2(slow * 1.1f, -breath * 1.1f), -slow * 1.6f, 1f);
        Offset("arm_left_bent", new Vector2(-slow * 1.8f, breath * 1.4f), slow * 2.8f, 1f);
        Offset("arm_right_open", new Vector2(slow * 1.8f, breath * 1.4f), -slow * 2.8f, 1f);
        Offset("leg_left_boot", new Vector2(0f, breath * 0.8f), slow * 0.8f, 1f);
        Offset("leg_right_boot", new Vector2(0f, breath * 0.7f), -slow * 0.8f, 1f);
        Offset("leg_left_lower", new Vector2(0f, breath * 0.9f), slow * 0.9f, 1f);
        Offset("leg_right_lower", new Vector2(0f, breath * 0.8f), -slow * 0.9f, 1f);
        Offset("cloak_left", new Vector2(-slow * 2f, -breath * 1.2f), -slow * 3.5f, 1f);
        Offset("cloak_mid", new Vector2(-slow * 1.2f, -breath * 0.8f), -slow * 2f, 1f);
        Offset("cloak_right", new Vector2(slow * 2f, -breath * 1.2f), slow * 3.5f, 1f);
        Offset("shadow_fire_ring", Vector2.zero, 0f, 1f + Mathf.Sin(timer * 3.4f) * 0.025f);
    }

    private void ApplyRun(float timer)
    {
        ApplyIdle(timer);
        var stride = Mathf.Sin(timer * 15f);
        var counter = Mathf.Sin(timer * 15f + Mathf.PI);
        Offset("leg_left_boot", new Vector2(0f, Mathf.Abs(stride) * 4f), stride * 10f, 1f);
        Offset("leg_right_boot", new Vector2(0f, Mathf.Abs(counter) * 4f), counter * 10f, 1f);
        Offset("leg_left_lower", new Vector2(stride * 3f, Mathf.Abs(stride) * 3f), stride * 8f, 1f);
        Offset("leg_right_lower", new Vector2(counter * 3f, Mathf.Abs(counter) * 3f), counter * 8f, 1f);
        Offset("cloak_left", new Vector2(-12f + stride * 2f, 4f), -14f + stride * 4f, 1f);
        Offset("cloak_mid", new Vector2(-6f + stride * 1.5f, 3f), -8f + stride * 3f, 1f);
        Offset("cloak_right", new Vector2(-4f + stride * 1.2f, 3f), -4f + stride * 2f, 1f);
        Offset("torso", new Vector2(0f, Mathf.Abs(stride) * 3.5f), -5f, 1f);
        Offset("collar", new Vector2(0f, Mathf.Abs(stride) * 3.3f), -4.5f, 1f);
        Offset("head_smirk", new Vector2(0f, Mathf.Abs(stride) * 2.5f), 4f, 1f);
        Offset("hair_front", new Vector2(-3f, Mathf.Abs(stride) * 2.5f), 6f, 1f);
        Offset("hair_back", new Vector2(-3f, Mathf.Abs(stride) * 2.5f), 5f, 1f);
    }

    private void ApplyAttack(float phase, bool hasTarget, Vector2 targetPosition, float scaleMultiplier)
    {
        var windup = EaseOut(Mathf.Clamp01(phase / 0.28f));
        var release = EaseOut(Mathf.Clamp01((phase - 0.22f) / 0.38f));
        var recoil = Mathf.Sin(Mathf.Clamp01((phase - 0.58f) / 0.42f) * Mathf.PI);
        SetExpression("head_shout");

        Offset("torso", new Vector2(-7f * windup + 13f * release, 6f * release), -6f + 11f * release, 1f);
        Offset("collar", new Vector2(-6f * windup + 11f * release, 6f * release), -5f + 9f * release, 1f);
        Offset("head_shout", new Vector2(-4f * windup + 8f * release, 2f * release), 3f + 3f * release, 1f);
        Offset("hair_front", new Vector2(-5f * windup + 10f * release, 3f * release), 6f + 4f * release, 1f);
        Offset("hair_back", new Vector2(-4f * windup + 8f * release, 2f * release), 4f + 3f * release, 1f);
        Offset("arm_right_open", new Vector2(20f * release - 6f * windup, 5f * release), -10f - 13f * release + 7f * windup, 1f);
        Offset("arm_left_bent", new Vector2(-12f * windup + 5f * release, 6f * windup), 14f * windup - 5f * release, 1f);
        Offset("cloak_left", new Vector2(-12f * release, -2f), -12f * release, 1f);
        Offset("cloak_mid", new Vector2(-8f * release, -1f), -8f * release, 1f);
        Offset("cloak_right", new Vector2(-6f * release, -1f), -5f * release, 1f);

        if (phase >= 0.22f && phase <= 0.9f)
        {
            AnimateProjectile(phase, hasTarget, targetPosition, scaleMultiplier);
        }

        if (recoil > 0f)
        {
            Offset("torso", new Vector2(-recoil * 6f, 0f), -recoil * 5f, 1f);
        }
    }

    private void ApplySkill(float phase, float timer)
    {
        var pulse = Mathf.Sin(phase * Mathf.PI);
        SetExpression("head_shout");
        Offset("torso", new Vector2(0f, pulse * 8f), 0f, 1f + pulse * 0.05f);
        Offset("collar", new Vector2(0f, pulse * 7.5f), 0f, 1f + pulse * 0.04f);
        Offset("arm_left_bent", new Vector2(-14f * pulse, 18f * pulse), 16f * pulse + Mathf.Sin(timer * 12f) * 2f, 1f);
        Offset("arm_right_open", new Vector2(16f * pulse, 19f * pulse), -16f * pulse + Mathf.Sin(timer * 12f + 1.4f) * 2f, 1f);
        Offset("cloak_left", new Vector2(-22f * pulse, 0f), -18f * pulse, 1f);
        Offset("cloak_mid", new Vector2(-8f * pulse, 0f), -8f * pulse, 1f);
        Offset("cloak_right", new Vector2(24f * pulse, 0f), 18f * pulse, 1f);
    }

    private void ApplyUltimate(float phase, bool hasTarget, Vector2 targetPosition, float scaleMultiplier)
    {
        var pulse = Mathf.Sin(phase * Mathf.PI);
        var snap = EaseOut(Mathf.Clamp01(phase / 0.45f));
        SetExpression("head_shout");

        root.localScale *= 1f + pulse * 0.12f;
        Offset("torso", new Vector2(0f, 14f * pulse), -5f + 9f * snap, 1f + pulse * 0.08f);
        Offset("collar", new Vector2(0f, 13f * pulse), -4f + 8f * snap, 1f + pulse * 0.06f);
        Offset("head_shout", new Vector2(0f, 10f * pulse), 5f * snap, 1f);
        Offset("hair_front", new Vector2(-6f * pulse, 13f * pulse), 10f * snap, 1f + pulse * 0.03f);
        Offset("hair_back", new Vector2(-5f * pulse, 11f * pulse), 8f * snap, 1f + pulse * 0.03f);
        Offset("arm_left_bent", new Vector2(-26f * snap, 34f * snap), 30f * snap, 1f);
        Offset("arm_right_open", new Vector2(30f * snap, 36f * snap), -30f * snap, 1f);
        Offset("cloak_left", new Vector2(-40f * pulse, 10f * pulse), -30f * pulse, 1f + pulse * 0.04f);
        Offset("cloak_mid", new Vector2(-16f * pulse, 10f * pulse), -14f * pulse, 1f + pulse * 0.03f);
        Offset("cloak_right", new Vector2(44f * pulse, 10f * pulse), 30f * pulse, 1f + pulse * 0.04f);

        var burst = Get("fx_flame_burst");
        if (burst != null)
        {
            var targetLocal = hasTarget ? ToLocal(targetPosition, scaleMultiplier) : new Vector2(210f, 6f);
            burst.rect.anchoredPosition = Vector2.Lerp(new Vector2(155f, -8f), targetLocal, 0.85f);
            burst.rect.localScale = Vector3.one * burst.setupScale * (0.55f + pulse * 0.72f + phase * 0.12f);
            burst.rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * Mathf.PI * 2f) * 4f);
            burst.image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(pulse * 1.15f));
            burst.image.gameObject.SetActive(phase > 0.08f && phase < 0.98f);
        }
    }

    private void ApplyDeath(float timer)
    {
        SetExpression("head_neutral");
        var fade = 0.58f + Mathf.Sin(timer * 2.5f) * 0.08f;
        foreach (var part in parts.Values)
        {
            part.image.color = new Color(0.82f, 0.62f, 0.55f, fade);
        }

        Offset("torso", new Vector2(18f, -58f), -72f, 0.98f);
        Offset("collar", new Vector2(18f, -58f), -72f, 0.98f);
        Offset("head_neutral", new Vector2(52f, -82f), -82f, 0.95f);
        Offset("hair_front", new Vector2(50f, -80f), -82f, 0.95f);
        Offset("hair_back", new Vector2(48f, -82f), -80f, 0.95f);
        Offset("arm_left_bent", new Vector2(2f, -78f), -92f, 1f);
        Offset("arm_right_open", new Vector2(54f, -74f), -116f, 1f);
        Offset("cloak_left", new Vector2(34f, -82f), -86f, 0.96f);
        Offset("cloak_mid", new Vector2(40f, -84f), -86f, 0.96f);
        Offset("cloak_right", new Vector2(44f, -84f), -86f, 0.96f);
        SetVisible("projectile_fireball", false);
        SetVisible("fx_flame_burst", false);
    }

    private void AnimateProjectile(float phase, bool hasTarget, Vector2 targetPosition, float scaleMultiplier)
    {
        var projectile = Get("projectile_fireball");
        if (projectile == null)
        {
            return;
        }

        var t = EaseInOut(Mathf.InverseLerp(0.22f, 0.9f, phase));
        var start = new Vector2(82f, 118f);
        var end = hasTarget ? ToLocal(targetPosition, scaleMultiplier) : new Vector2(300f, 74f);
        var arc = Mathf.Sin(t * Mathf.PI) * 44f;
        var position = Vector2.Lerp(start, end, t) + new Vector2(0f, arc);

        projectile.rect.anchoredPosition = position;
        projectile.rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-6f, 10f, t));
        projectile.rect.localScale = Vector3.one * projectile.setupScale * (0.48f + Mathf.Sin(t * Mathf.PI) * 0.2f);
        projectile.image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Sin(t * Mathf.PI) * 1.2f));
        projectile.image.gameObject.SetActive(true);
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

    private void SetExpression(string expressionPart)
    {
        SetVisible("head_smirk", expressionPart == "head_smirk");
        SetVisible("head_neutral", expressionPart == "head_neutral");
        SetVisible("head_shout", expressionPart == "head_shout");
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
}
