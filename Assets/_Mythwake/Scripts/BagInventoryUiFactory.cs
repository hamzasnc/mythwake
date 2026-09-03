using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BagInventoryUiRefs
{
    public RectTransform[] slotRoots;
    public Button[] slotButtons;
    public Image[] slotFrames;
    public Image[] slotInnerFrames;
    public Image[] slotHighlightFrames;
    public RawImage[] slotIcons;
    public RawImage detailIcon;
    public Image detailFrame;
    public TMP_Text[] rewardTexts;
    public Image[] rewardFrames;
    public RawImage[] rewardIcons;
    public TMP_Text[] slotCountTexts;
    public TMP_Text[] slotNameTexts;
    public TMP_Text[] slotDetailTexts;
    public RectTransform detailRoot;
    public RectTransform useRoot;
    public TMP_Text detailTitleText;
    public TMP_Text detailDescriptionText;
    public TMP_Text detailStatsText;
    public TMP_Text useTitleText;
    public TMP_Text useHintText;
    public TMP_InputField useAmountInput;
    public Button detailCloseButton;
    public Button useOneButton;
    public Button useMinusButton;
    public Button usePlusButton;
    public Button useAmountButton;
    public Button useAllButton;
    public RectTransform rewardPopupRoot;
    public TMP_Text rewardTitleText;
    public TMP_Text rewardSummaryText;
    public Button rewardCloseButton;
    public Button rewardXButton;
}

public static class BagInventoryUiFactory
{
    public static BagInventoryUiRefs Create(
        RectTransform popupRoot,
        RectTransform gridRoot,
        int slotCount,
        int heroCount,
        Func<Transform, string, string, Vector2, Vector2, RectTransform> createPanel,
        Func<Transform, string, string, int, Vector2, Vector2, TMP_Text> createText,
        Func<Transform, string, string, string, float, float, float, float, Button> createButton,
        Func<Transform, string, Texture, Vector2, Vector2, Vector2, RawImage> createRawImage,
        Func<Transform, string, string, float, float, float, float, bool, TMP_InputField> createInput,
        Action<Image, string> applySprite,
        Action<int> selectItem,
        Func<string, string> localize)
    {
        var refs = new BagInventoryUiRefs();
        refs.slotRoots = new RectTransform[slotCount];
        refs.slotButtons = new Button[slotCount];
        refs.slotFrames = new Image[slotCount];
        refs.slotInnerFrames = new Image[slotCount];
        refs.slotHighlightFrames = new Image[slotCount];
        refs.slotIcons = new RawImage[slotCount];
        refs.slotCountTexts = new TMP_Text[slotCount];
        refs.slotNameTexts = new TMP_Text[slotCount];
        refs.slotDetailTexts = new TMP_Text[slotCount];

        const int columns = 5;
        const float startX = -304f;
        const float startY = -28f;
        const float spacingX = 152f;
        const float spacingY = 132f;

        for (var i = 0; i < slotCount; i++)
        {
            var column = i % columns;
            var row = i / columns;
            var slotRoot = createPanel(
                gridRoot,
                $"Inventory Slot {i + 1}",
                "bag_slot_filled",
                new Vector2(startX + (column * spacingX), startY - (row * spacingY)),
                new Vector2(126, 112));
            refs.slotRoots[i] = slotRoot;
            refs.slotFrames[i] = slotRoot.GetComponent<Image>();
            refs.slotFrames[i].raycastTarget = true;

            var slotButton = slotRoot.gameObject.AddComponent<Button>();
            var capturedSlot = i;
            slotButton.targetGraphic = refs.slotFrames[i];
            slotButton.onClick.AddListener(() => selectItem(capturedSlot));
            refs.slotButtons[i] = slotButton;

            var highlight = createPanel(slotRoot, "Selected Highlight", "bag_slot_selected", Vector2.zero, new Vector2(126, 112));
            refs.slotHighlightFrames[i] = highlight.GetComponent<Image>();
            refs.slotHighlightFrames[i].raycastTarget = false;
            highlight.gameObject.SetActive(false);

            refs.slotIcons[i] = createRawImage(slotRoot, "Icon", null, new Vector2(0, -14), new Vector2(86, 76), new Vector2(0.5f, 1f));
            refs.slotIcons[i].raycastTarget = false;

            var countBack = createPanel(slotRoot, "Count Back", "bag_slot_amount_badge", new Vector2(38, -78), new Vector2(58, 28));
            refs.slotCountTexts[i] = createText(countBack, "Count", string.Empty, 17, Vector2.zero, new Vector2(48, 24));
            refs.slotCountTexts[i].fontStyle = FontStyles.Bold;
            refs.slotCountTexts[i].enableAutoSizing = true;
            refs.slotCountTexts[i].fontSizeMin = 10;
            refs.slotCountTexts[i].fontSizeMax = 17;
            refs.slotCountTexts[i].color = new Color(1f, 0.9f, 0.54f);
            refs.slotCountTexts[i].textWrappingMode = TextWrappingModes.NoWrap;

            refs.slotNameTexts[i] = createText(slotRoot, "Name", string.Empty, 14, new Vector2(0, -92), new Vector2(104, 22));
            refs.slotNameTexts[i].fontStyle = FontStyles.Bold;
            refs.slotNameTexts[i].enableAutoSizing = true;
            refs.slotNameTexts[i].fontSizeMin = 9;
            refs.slotNameTexts[i].fontSizeMax = 14;
            refs.slotNameTexts[i].color = new Color(0.16f, 0.08f, 0.025f);
            refs.slotNameTexts[i].textWrappingMode = TextWrappingModes.NoWrap;
            refs.slotNameTexts[i].gameObject.SetActive(false);

            refs.slotDetailTexts[i] = createText(slotRoot, "Detail", string.Empty, 11, new Vector2(0, -112), new Vector2(106, 18));
            refs.slotDetailTexts[i].enableAutoSizing = true;
            refs.slotDetailTexts[i].fontSizeMin = 8;
            refs.slotDetailTexts[i].fontSizeMax = 11;
            refs.slotDetailTexts[i].color = new Color(0.28f, 0.17f, 0.07f);
            refs.slotDetailTexts[i].textWrappingMode = TextWrappingModes.NoWrap;
            refs.slotDetailTexts[i].gameObject.SetActive(false);
            slotRoot.gameObject.SetActive(true);
        }

        refs.detailRoot = createPanel(popupRoot, "Inventory Detail Panel", "bag_detail_panel", new Vector2(0, -494), new Vector2(790, 180));
        refs.detailRoot.GetComponent<Image>().raycastTarget = true;
        refs.detailFrame = createPanel(refs.detailRoot, "Detail Icon Frame", "bag_icon_frame", new Vector2(-302, -40), new Vector2(128, 112)).GetComponent<Image>();
        refs.detailIcon = createRawImage(refs.detailFrame.transform, "Icon", null, new Vector2(0, -16), new Vector2(100, 82), new Vector2(0.5f, 1f));
        refs.detailIcon.raycastTarget = false;

        refs.detailTitleText = createText(refs.detailRoot, "Detail Title", string.Empty, 25, new Vector2(82, -32), new Vector2(500, 34));
        refs.detailTitleText.alignment = TextAlignmentOptions.Left;
        refs.detailTitleText.fontStyle = FontStyles.Bold;
        refs.detailTitleText.color = new Color(0.17f, 0.085f, 0.025f);
        refs.detailTitleText.textWrappingMode = TextWrappingModes.NoWrap;
        refs.detailTitleText.enableAutoSizing = true;
        refs.detailTitleText.fontSizeMin = 18;
        refs.detailTitleText.fontSizeMax = 26;

        refs.detailDescriptionText = createText(refs.detailRoot, "Detail Description", string.Empty, 18, new Vector2(82, -70), new Vector2(500, 48));
        refs.detailDescriptionText.alignment = TextAlignmentOptions.TopLeft;
        refs.detailDescriptionText.color = new Color(0.24f, 0.12f, 0.04f);
        refs.detailDescriptionText.textWrappingMode = TextWrappingModes.Normal;
        refs.detailDescriptionText.enableAutoSizing = true;
        refs.detailDescriptionText.fontSizeMin = 12;
        refs.detailDescriptionText.fontSizeMax = 18;

        refs.detailStatsText = createText(refs.detailRoot, "Detail Stats", string.Empty, 17, new Vector2(82, -122), new Vector2(500, 40));
        refs.detailStatsText.alignment = TextAlignmentOptions.TopLeft;
        refs.detailStatsText.fontStyle = FontStyles.Bold;
        refs.detailStatsText.color = new Color(0.13f, 0.07f, 0.03f);
        refs.detailStatsText.textWrappingMode = TextWrappingModes.Normal;
        refs.detailStatsText.enableAutoSizing = true;
        refs.detailStatsText.fontSizeMin = 12;
        refs.detailStatsText.fontSizeMax = 18;
        refs.detailCloseButton = null;

        refs.useRoot = createPanel(popupRoot, "Inventory Use Panel", "bag_use_panel", new Vector2(0, -700), new Vector2(790, 244));
        refs.useRoot.GetComponent<Image>().raycastTarget = true;
        createPanel(refs.useRoot, "Use Header Plaque", "bag_header", new Vector2(0, -18), new Vector2(260, 50));
        var useHeader = createText(refs.useRoot, "Use Header", localize("ui.inventory.action.use"), 24, new Vector2(0, -28), new Vector2(230, 34));
        useHeader.fontStyle = FontStyles.Bold;
        useHeader.color = new Color(1f, 0.9f, 0.66f);
        useHeader.textWrappingMode = TextWrappingModes.NoWrap;
        useHeader.outlineColor = new Color(0.03f, 0.08f, 0.08f, 0.96f);
        useHeader.outlineWidth = 0.14f;

        refs.useTitleText = createText(refs.useRoot, "Use Title", string.Empty, 21, new Vector2(0, -74), new Vector2(620, 34));
        refs.useTitleText.alignment = TextAlignmentOptions.Center;
        refs.useTitleText.fontStyle = FontStyles.Bold;
        refs.useTitleText.color = new Color(0.17f, 0.085f, 0.025f);
        refs.useTitleText.enableAutoSizing = true;
        refs.useTitleText.fontSizeMin = 14;
        refs.useTitleText.fontSizeMax = 21;
        refs.useTitleText.textWrappingMode = TextWrappingModes.NoWrap;

        refs.useHintText = createText(refs.useRoot, "Use Hint", localize("ui.inventory.use.status"), 16, new Vector2(0, -104), new Vector2(620, 28));
        refs.useHintText.alignment = TextAlignmentOptions.Center;
        refs.useHintText.color = new Color(0.23f, 0.12f, 0.045f);
        refs.useHintText.enableAutoSizing = true;
        refs.useHintText.fontSizeMin = 12;
        refs.useHintText.fontSizeMax = 16;
        refs.useHintText.textWrappingMode = TextWrappingModes.NoWrap;

        refs.useOneButton = createButton(refs.useRoot, "Detail Use One Button", localize("ui.inventory.action.use_one"), "bag_button_normal", -238, -122, 102, 42);
        refs.useMinusButton = createButton(refs.useRoot, "Detail Use Minus Button", "-", "bag_button_normal", -116, -122, 52, 42);
        refs.useAmountInput = createInput(refs.useRoot, "Detail Use Amount Input", "1", -40, -122, 88, 42, false);
        refs.useAmountInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        refs.useAmountInput.characterLimit = 3;
        refs.useAmountInput.text = "1";
        applySprite(refs.useAmountInput.targetGraphic as Image, "bag_button_normal");
        refs.usePlusButton = createButton(refs.useRoot, "Detail Use Plus Button", "+", "bag_button_normal", 36, -122, 52, 42);
        refs.useAllButton = createButton(refs.useRoot, "Detail Use All Button", localize("ui.inventory.action.all"), "bag_button_normal", 156, -122, 102, 42);
        refs.useAmountButton = createButton(refs.useRoot, "Detail Use Amount Button", localize("ui.inventory.action.use"), "bag_ok_button", 0, -178, 330, 56);
        refs.detailRoot.gameObject.SetActive(false);
        refs.useRoot.gameObject.SetActive(false);

        refs.rewardPopupRoot = createPanel(popupRoot, "Inventory Reward Popup", "bag_reward_popup_frame", new Vector2(0, -566), new Vector2(760, 350));
        var popupImage = refs.rewardPopupRoot.GetComponent<Image>();
        if (popupImage != null)
        {
            popupImage.raycastTarget = true;
        }

        createPanel(refs.rewardPopupRoot, "Reward Header Plaque", "bag_header", new Vector2(0, -18), new Vector2(300, 58));
        refs.rewardTitleText = createText(refs.rewardPopupRoot, "Title", localize("ui.inventory.reward.title"), 30, new Vector2(0, -27), new Vector2(260, 38));
        refs.rewardTitleText.fontStyle = FontStyles.Bold;
        refs.rewardTitleText.color = new Color(1f, 0.9f, 0.66f);
        refs.rewardTitleText.textWrappingMode = TextWrappingModes.NoWrap;
        refs.rewardTitleText.outlineColor = new Color(0.09f, 0.035f, 0.01f, 0.96f);
        refs.rewardTitleText.outlineWidth = 0.16f;

        refs.rewardXButton = createButton(refs.rewardPopupRoot, "Inventory Reward X Button", "X", "bag_close_button", 334, -22, 48, 48);
        createPanel(refs.rewardPopupRoot, "Reward Inner Glow", "bag_reward_inner", new Vector2(0, -88), new Vector2(700, 132));
        refs.rewardSummaryText = createText(refs.rewardPopupRoot, "Reward Summary", string.Empty, 22, new Vector2(0, -202), new Vector2(640, 42));
        refs.rewardSummaryText.fontStyle = FontStyles.Bold;
        refs.rewardSummaryText.color = new Color(0.43f, 0.97f, 0.88f);
        refs.rewardSummaryText.enableAutoSizing = true;
        refs.rewardSummaryText.fontSizeMin = 14;
        refs.rewardSummaryText.fontSizeMax = 22;
        refs.rewardSummaryText.textWrappingMode = TextWrappingModes.NoWrap;
        refs.rewardSummaryText.outlineColor = new Color(0.02f, 0.04f, 0.035f, 0.96f);
        refs.rewardSummaryText.outlineWidth = 0.12f;

        refs.rewardFrames = new Image[heroCount];
        refs.rewardIcons = new RawImage[heroCount];
        refs.rewardTexts = new TMP_Text[heroCount];
        for (var i = 0; i < heroCount; i++)
        {
            var frame = createPanel(refs.rewardPopupRoot, $"Reward Slot {i + 1}", "bag_reward_slot", new Vector2(-288f + i * 96f, -76f), new Vector2(80, 88));
            refs.rewardFrames[i] = frame.GetComponent<Image>();
            refs.rewardIcons[i] = createRawImage(frame, "Icon", null, new Vector2(0, -8), new Vector2(62, 62), new Vector2(0.5f, 1f));
            refs.rewardIcons[i].raycastTarget = false;
            refs.rewardTexts[i] = createText(frame, "Text", string.Empty, 13, new Vector2(0, -65), new Vector2(74, 20));
            refs.rewardTexts[i].alignment = TextAlignmentOptions.Center;
            refs.rewardTexts[i].fontStyle = FontStyles.Bold;
            refs.rewardTexts[i].enableAutoSizing = true;
            refs.rewardTexts[i].fontSizeMin = 9;
            refs.rewardTexts[i].fontSizeMax = 13;
            refs.rewardTexts[i].color = Color.white;
            refs.rewardTexts[i].textWrappingMode = TextWrappingModes.NoWrap;
            frame.gameObject.SetActive(false);
        }

        refs.rewardCloseButton = createButton(refs.rewardPopupRoot, "Inventory Reward Close Button", "OK", "bag_ok_button", 0, -278, 330, 58);
        refs.rewardPopupRoot.gameObject.SetActive(false);
        return refs;
    }
}
