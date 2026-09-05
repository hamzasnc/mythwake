using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Reusable login artwork slices and nine-sliced controls; account logic stays in the controller.</summary>
public static class MythwakeLoginUI
{
    private static TMP_FontAsset headingFont;
    private static Sprite buttonSprite;
    public static TMP_FontAsset HeadingFont
    {
        get
        {
            if (headingFont == null)
            {
                var font = Resources.Load<Font>("Mythwake/UI/Fonts/Cinzel-Bold");
                if (font != null) headingFont = TMP_FontAsset.CreateFontAsset(font);
            }
            return headingFont;
        }
    }
    public static RectTransform CreatePresentation(RectTransform overlay)
    {
        var content = new GameObject("Login Portrait Layout", typeof(RectTransform), typeof(AspectRatioFitter)).GetComponent<RectTransform>();
        content.SetParent(overlay, false);
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, .5f);
        var fitter = content.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1080f / 1920f;
        // A fixed design canvas inside the fitted portrait keeps text and hit areas together.
        var design = new GameObject("Login Design", typeof(RectTransform), typeof(LoginDesignScale)).GetComponent<RectTransform>();
        design.SetParent(content, false);
        design.anchorMin = design.anchorMax = design.pivot = new Vector2(.5f, .5f);
        design.sizeDelta = new Vector2(1080, 1920);
        var texture = Resources.Load<Texture2D>("Mythwake/UI/Login/login_background");
        if (texture != null)
        {
            AddSlice(design, texture, "Login Crest Slice", 0, 720);
            AddSlice(design, texture, "Login Body Slice", 720, 920);
            AddSlice(design, texture, "Login Footer Slice", 1640, 280);
        }
        return design;
    }

    private static void AddSlice(RectTransform parent, Texture2D texture, string name, float top, float height)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
        image.transform.SetParent(parent, false);
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
        rect.anchoredPosition = new Vector2(0, -top);
        rect.sizeDelta = new Vector2(1080, height);
        image.texture = texture;
        image.uvRect = new Rect(0, 1 - (top + height) / 1920, 1, height / 1920);
        image.raycastTarget = false;
    }

    public static void StylePanel(Image image)
    {
        image.sprite = null;
        image.color = new Color(.012f, .032f, .045f, 1f);
        var outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(.65f, .46f, .19f, 1f);
        outline.effectDistance = new Vector2(2, -2);
    }

    public static void StyleButton(Button button, bool primary)
    {
        ApplyLoginButton(button.GetComponent<Image>());
        button.GetComponent<Image>().color = primary ? Color.white : new Color(.8f, .88f, .92f);
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, .94f, .76f);
        colors.pressedColor = new Color(.65f, .8f, .85f);
        colors.disabledColor = new Color(.48f, .48f, .48f, .7f);
        button.colors = colors;
        var label = button.GetComponentInChildren<TMP_Text>();
        label.color = new Color(1f, .91f, .69f);
        label.fontStyle = FontStyles.Bold;
        if (HeadingFont != null) label.font = HeadingFont;
        label.fontSizeMin = 22;
        label.fontSizeMax = 30;
    }

    public static void StyleInput(TMP_InputField input)
    {
        ApplyLoginButton(input.GetComponent<Image>());
        input.GetComponent<Image>().color = new Color(.6f, .75f, .85f);
        input.textComponent.color = new Color(.9f, .96f, 1f);
        if (input.placeholder is TMP_Text placeholder) placeholder.color = new Color(.62f, .75f, .8f);
        if (input.textViewport != null)
        {
            input.textViewport.offsetMin = new Vector2(72, 6);
            input.textViewport.offsetMax = new Vector2(-72, -6);
        }
    }

    private static void ApplyLoginButton(Image image)
    {
        if (buttonSprite == null)
        {
            var texture = Resources.Load<Texture2D>("Mythwake/UI/Login/login_button");
            if (texture == null) return;
            // Slice the painted button out of the transparent source canvas.
            var x = texture.width / 2172f;
            var y = texture.height / 724f;
            buttonSprite = Sprite.Create(texture, new Rect(8*x, (724-522)*y, 2156*x, 340*y), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(290*x, 55*y, 290*x, 55*y));
        }
        image.sprite = buttonSprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 3.6f;
    }
}
