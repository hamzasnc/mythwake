using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A quiet arena dim and Kael's purpose-made action illustration.</summary>
public sealed class KaelUltimateBackdrop : MaskableGraphic
{
    static readonly Color Ink = new Color(.026f, .019f, .026f, 1);
    static readonly Color Ivory = new Color(.96f, .92f, .84f, 1);
    const float ArenaTop = -110, ArenaBottom = -980;
    KaelUltimatePortraitGraphic portrait;
    TMP_Text skillLabel;
    Vector2 focus;
    float progress, opacity;

    public float Opacity => opacity;
    public float Progress => progress;
    public Vector2 FocusPosition => focus;
    public int LastVertexCount { get; private set; }
    public bool PortraitVisible => portrait != null && portrait.Texture != null && opacity > 0;
    public Rect OverlayBounds
    {
        get
        {
            var parent = transform.parent as RectTransform;
            var width = parent != null && parent.rect.width > 0 ? Mathf.Min(960, parent.rect.width) : 960;
            return new Rect(-width * .5f, ArenaBottom, width, ArenaTop - ArenaBottom);
        }
    }

    public static KaelUltimateBackdrop Create(Transform parent)
    {
        var go = new GameObject("Kael ultimate action cut-in", typeof(RectTransform), typeof(CanvasRenderer), typeof(KaelUltimateBackdrop));
        go.transform.SetParent(parent, false);
        var backdrop = go.GetComponent<KaelUltimateBackdrop>();
        var rect = backdrop.rectTransform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, 1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        backdrop.raycastTarget = false;
        backdrop.CreatePortraitAndTitle();
        backdrop.Hide();
        return backdrop;
    }

    void CreatePortraitAndTitle()
    {
        var go = new GameObject("Kael dedicated action illustration", typeof(RectTransform), typeof(CanvasRenderer), typeof(KaelUltimatePortraitGraphic));
        go.transform.SetParent(transform, false);
        portrait = go.GetComponent<KaelUltimatePortraitGraphic>();
        portrait.Texture = Resources.Load<Texture2D>("Characters/Kael/ActionPortrait");
        if (portrait.Texture == null)
            throw new InvalidOperationException("Kael: required ActionPortrait texture is missing.");
        portrait.raycastTarget = false;
        portrait.rectTransform.anchorMin = portrait.rectTransform.anchorMax = new Vector2(.5f, 1);
        portrait.rectTransform.pivot = Vector2.zero;
        portrait.rectTransform.sizeDelta = new Vector2(370, 185);

        var label = new GameObject("Localized ultimate name", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(transform, false);
        skillLabel = label.GetComponent<TextMeshProUGUI>();
        skillLabel.fontSize = 24;
        skillLabel.enableAutoSizing = true;
        skillLabel.fontSizeMin = 19;
        skillLabel.fontSizeMax = 24;
        skillLabel.fontStyle = FontStyles.Bold;
        skillLabel.alignment = TextAlignmentOptions.BottomLeft;
        skillLabel.textWrappingMode = TextWrappingModes.NoWrap;
        skillLabel.raycastTarget = false;
        skillLabel.rectTransform.anchorMin = skillLabel.rectTransform.anchorMax = new Vector2(.5f, 1);
        skillLabel.rectTransform.pivot = new Vector2(0, 1);
        skillLabel.rectTransform.sizeDelta = new Vector2(355, 35);
    }

    public void Present(float normalizedProgress, Vector2 heroFocusPosition, float facing, string localizedName)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        progress = Mathf.Clamp01(normalizedProgress);
        focus = heroFocusPosition;
        var t = Mathf.Clamp01(progress / .17f);
        var enter = 1 - (1 - t) * (1 - t) * (1 - t);
        var exit = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.78f, 1, progress));
        opacity = enter * exit;
        var bounds = OverlayBounds;
        var slide = (1 - enter) * 44;
        // Keep the entire entering illustration to the right of End Fight's
        // reserved lower-left control area, including its initial slide offset.
        const float portraitInset = 240;
        portrait.rectTransform.anchoredPosition = new Vector2(bounds.xMin + portraitInset - slide, ArenaBottom + 34);
        portrait.Sample(opacity, 0);
        skillLabel.rectTransform.anchoredPosition = new Vector2(bounds.xMin + portraitInset + 14 - slide * .5f, ArenaBottom + 37);
        skillLabel.text = localizedName ?? string.Empty;
        skillLabel.color = new Color(Ivory.r, Ivory.g, Ivory.b, opacity);
        SetVerticesDirty();
    }

    public void Hide()
    {
        opacity = progress = 0;
        LastVertexCount = 0;
        SetVerticesDirty();
        gameObject.SetActive(false);
    }
    protected override void OnDisable()
    {
        opacity = progress = 0;
        LastVertexCount = 0;
        base.OnDisable();
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (opacity <= .001f) { LastVertexCount = 0; return; }
        var bounds = OverlayBounds;
        var color = new Color(Ink.r, Ink.g, Ink.b, .72f * opacity);
        mesh.AddVert(new Vector2(bounds.xMin, bounds.yMin), color, Vector2.zero);
        mesh.AddVert(new Vector2(bounds.xMin, bounds.yMax), color, Vector2.zero);
        mesh.AddVert(new Vector2(bounds.xMax, bounds.yMax), color, Vector2.zero);
        mesh.AddVert(new Vector2(bounds.xMax, bounds.yMin), color, Vector2.zero);
        mesh.AddTriangle(0, 1, 2); mesh.AddTriangle(2, 3, 0);
        LastVertexCount = mesh.currentVertCount;
    }
}

/// <summary>Unwarped action art; its authored transparent edge defines the cut-in silhouette.</summary>
internal sealed class KaelUltimatePortraitGraphic : MaskableGraphic
{
    public Texture2D Texture;
    float opacity, leftClip;
    public override Texture mainTexture => Texture != null ? Texture : Texture2D.whiteTexture;

    public void Sample(float alpha, float minimumX)
    {
        opacity = alpha; leftClip = Mathf.Max(0, minimumX);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (Texture == null || opacity <= .001f) return;
        var w = rectTransform.rect.width;
        var h = rectTransform.rect.height;
        if (w <= 0 || h <= 0 || leftClip >= w) return;
        var alpha = new Color(1, 1, 1, opacity);
        var u = leftClip / w;
        mesh.AddVert(new Vector2(leftClip, 0), alpha, new Vector2(u, 0));
        mesh.AddVert(new Vector2(leftClip, h), alpha, new Vector2(u, 1));
        mesh.AddVert(new Vector2(w, h), alpha, Vector2.one);
        mesh.AddVert(new Vector2(w, 0), alpha, Vector2.right);
        mesh.AddTriangle(0, 1, 2); mesh.AddTriangle(2, 3, 0);
    }
}
