using Spine.Unity;
using UnityEngine;

public sealed class MythwakeSpineHeroCombatView : MonoBehaviour
{
    public enum Clip
    {
        Idle,
        Run,
        Attack,
        Skill,
        Death
    }

    private const string LioraHeroId = "hero_liora";
    private const string ResourceRoot = "Mythwake/Art/Spine/Liora/";
    private const string SkeletonResourceName = ResourceRoot + "hero_liora_spine";
    private const string AtlasTextResourceName = ResourceRoot + "hero_liora_spine.atlas";
    private const string AtlasTextureResourceName = ResourceRoot + "hero_liora_spine_atlas";
    private const float TopToGroundOffset = 188f;
    private const float MinScale = 0.08f;

    private static SkeletonDataAsset cachedSkeletonDataAsset;
    private static Material cachedMaterial;
    private static bool attemptedAssetLoad;

    private RectTransform root;
    private SkeletonGraphic skeletonGraphic;
    private string currentAnimation;
    private bool currentLoop;
    private float baseScale = 1f;
    private float previewFacing = 1f;
    private float previewScale = 1f;
    private Vector2 previewTopPosition;
    private bool previewMode;

    public static bool SupportsHero(string heroId)
    {
        return heroId == LioraHeroId;
    }

    public static MythwakeSpineHeroCombatView Create(Transform parent, string name, Vector2 topPosition, float scale)
    {
        var viewObject = new GameObject(name, typeof(RectTransform), typeof(MythwakeSpineHeroCombatView));
        viewObject.transform.SetParent(parent, false);

        var view = viewObject.GetComponent<MythwakeSpineHeroCombatView>();
        view.Initialize(topPosition, scale);
        return view;
    }

    public void Initialize(Vector2 topPosition, float scale)
    {
        root = GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(440f, 440f);
        baseScale = Mathf.Max(MinScale, scale);
        SetTopPosition(topPosition);

        if (TryEnsureSkeletonGraphic())
        {
            PlayAnimation("idle", true);
        }

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
        bool visible)
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

        ApplyPose(clip, topPosition, facingScale, scaleMultiplier, tint);
    }

    public void HideTransientEffects()
    {
        // Spine slot timelines own transient visibility; avoid restarting active clips every combat frame.
    }

    private void Update()
    {
        if (!previewMode)
        {
            return;
        }

        ApplyPose(Clip.Idle, previewTopPosition, previewFacing, previewScale, Color.white);
    }

    private void ApplyPose(Clip clip, Vector2 topPosition, float facingScale, float scaleMultiplier, Color tint)
    {
        if (!TryEnsureSkeletonGraphic())
        {
            Hide();
            return;
        }

        SetTopPosition(topPosition);
        var clampedScale = Mathf.Max(MinScale, scaleMultiplier);
        root.localScale = new Vector3(facingScale * baseScale * clampedScale, baseScale * clampedScale, 1f);
        skeletonGraphic.color = tint;

        switch (clip)
        {
            case Clip.Run:
                PlayAnimation("run", true);
                break;
            case Clip.Attack:
                PlayAnimation("attack", false);
                break;
            case Clip.Skill:
                PlayAnimation("skill", false);
                break;
            case Clip.Death:
                PlayAnimation("death", false);
                break;
            default:
                PlayAnimation("idle", true);
                break;
        }
    }

    private void SetTopPosition(Vector2 topPosition)
    {
        root.anchoredPosition = topPosition + new Vector2(0f, -TopToGroundOffset);
    }

    private void PlayAnimation(string animationName, bool loop)
    {
        if (skeletonGraphic == null || !skeletonGraphic.IsValid)
        {
            return;
        }

        if (currentAnimation == animationName && currentLoop == loop)
        {
            return;
        }

        var skeletonData = skeletonGraphic.SkeletonDataAsset.GetSkeletonData(true);
        var animation = skeletonData == null ? null : skeletonData.FindAnimation(animationName);
        if (animation == null)
        {
            return;
        }

        skeletonGraphic.AnimationState.SetAnimation(0, animation, loop);
        currentAnimation = animationName;
        currentLoop = loop;
    }

    private bool TryEnsureSkeletonGraphic()
    {
        if (skeletonGraphic != null && skeletonGraphic.IsValid)
        {
            return true;
        }

        var skeletonDataAsset = GetOrCreateSkeletonDataAsset();
        if (skeletonDataAsset == null || cachedMaterial == null)
        {
            return false;
        }

        skeletonGraphic = gameObject.GetComponent<SkeletonGraphic>();
        if (skeletonGraphic == null)
        {
            skeletonGraphic = gameObject.AddComponent<SkeletonGraphic>();
        }

        skeletonGraphic.raycastTarget = false;
        skeletonGraphic.material = cachedMaterial;
        skeletonGraphic.skeletonDataAsset = skeletonDataAsset;
        skeletonGraphic.startingAnimation = "idle";
        skeletonGraphic.startingLoop = true;
        skeletonGraphic.initialFlipX = false;
        skeletonGraphic.initialFlipY = false;
        skeletonGraphic.timeScale = 1f;
        skeletonGraphic.Initialize(true);
        currentAnimation = null;
        currentLoop = false;
        return skeletonGraphic.IsValid;
    }

    private static SkeletonDataAsset GetOrCreateSkeletonDataAsset()
    {
        if (cachedSkeletonDataAsset != null)
        {
            return cachedSkeletonDataAsset;
        }

        if (attemptedAssetLoad)
        {
            return null;
        }

        attemptedAssetLoad = true;
        var skeletonJson = Resources.Load<TextAsset>(SkeletonResourceName);
        var atlasText = Resources.Load<TextAsset>(AtlasTextResourceName);
        var atlasTexture = Resources.Load<Texture2D>(AtlasTextureResourceName);
        if (skeletonJson == null || atlasText == null || atlasTexture == null)
        {
            Debug.LogWarning("Liora Spine resources are missing. Expected JSON, atlas text, and atlas texture in Resources/Mythwake/Art/Spine/Liora.");
            return null;
        }

        atlasTexture.name = "hero_liora_spine_atlas";
        atlasTexture.filterMode = FilterMode.Bilinear;

        var shader = Shader.Find("Spine/SkeletonGraphic");
        if (shader == null)
        {
            shader = Shader.Find("Spine/Skeleton");
        }
        if (shader == null)
        {
            shader = Shader.Find("UI/Default");
        }

        try
        {
            var materialSource = new Material(shader);
            var atlasAsset = SpineAtlasAsset.CreateRuntimeInstance(atlasText, new[] { atlasTexture }, materialSource, true, renameMaterial: true);
            cachedMaterial = atlasAsset.PrimaryMaterial;
            cachedSkeletonDataAsset = SkeletonDataAsset.CreateRuntimeInstance(skeletonJson, atlasAsset, true, 1f);
            cachedSkeletonDataAsset.name = "hero_liora_runtime_skeleton";
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"Failed to initialize Liora Spine runtime assets: {exception.Message}");
            cachedSkeletonDataAsset = null;
            cachedMaterial = null;
        }

        return cachedSkeletonDataAsset;
    }
}
