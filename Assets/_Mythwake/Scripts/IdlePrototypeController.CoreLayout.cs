using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class IdlePrototypeController
{
    private bool corePresentationReady;

    private void RefreshReadableCorePresentation()
    {
        if (corePresentationReady) EnsureRuntimeCoreScreenFrames();
    }
    // All measurements are canvas units at a 1080-wide portrait reference.
    // Reserve only the live resource bar and navigation; content owns the rest.
    private void ApplyReadableCoreLayout(Sprite frame, Sprite button, Sprite card, Sprite node)
    {
        var panels = new[] { homePanel, heroesPanel, villagePanel, dungeonsPanel, summonPanel };
        foreach (var panel in panels)
        {
            if (panel == null) continue;
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(0, 224);
            rect.offsetMax = new Vector2(0, -158);
            foreach (Transform child in panel.transform)
            {
                if (!child.name.Contains("Screen Frame")) continue;
                var childRect = child as RectTransform;
                childRect.anchorMin = Vector2.zero;
                childRect.anchorMax = Vector2.one;
                childRect.offsetMin = new Vector2(12, 0);
                childRect.offsetMax = new Vector2(-12, 0);
                var image = child.GetComponent<Image>();
                if (image != null) image.pixelsPerUnitMultiplier = 4f;
            }
        }
        ReadableHeroes(frame);
        ReadableDungeons(frame);
        ReadableSummon(frame, card);
        ReadableMaps();
        ReadableDialogs(frame, card);
        ReadableHeroDetails(frame);
        ReadableTopBar(button, node);
        corePresentationReady = true;
    }

    private static void CoreRect(Component component, float x, float y, float width, float height)
    {
        if (component == null) return;
        var rect = component.transform as RectTransform;
        SetRuntimeRect(rect, new Vector2(x, -y), new Vector2(width, height), new Vector2(.5f, 1f));
    }

    private static void CoreText(TMP_Text text, float size)
    {
        if (text == null) return;
        text.enableAutoSizing = false;
        text.fontSize = size;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.margin = Vector4.zero;
    }

    private static void CoreHide(Transform parent, params string[] names)
    {
        if (parent == null) return;
        foreach (var name in names)
        {
            var child = parent.Find(name);
            if (child != null) child.gameObject.SetActive(false);
        }
    }

    private static void CorePanel(RectTransform panel, Sprite frame)
    {
        if (panel == null) return;
        ApplyRuntimeCorePanelSkin(panel, frame);
        panel.GetComponent<Image>().pixelsPerUnitMultiplier = 4f;
        var fill = panel.Find("Core Inset Fill")?.GetComponent<Image>();
        if (fill == null)
            fill = CreateRuntimePanel(panel, "Core Inset Fill", Vector2.zero, Vector2.one, new Color(.012f, .038f, .047f, 1)).GetComponent<Image>();
        fill.raycastTarget = false;
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = new Vector2(20, 20);
        fill.rectTransform.offsetMax = new Vector2(-20, -20);
        fill.transform.SetAsFirstSibling();
    }

    private static float CoreHeight(GameObject panel)
    {
        return panel == null ? 1538 : Mathf.Max(1450, panel.GetComponent<RectTransform>().rect.height);
    }

    private void ReadableHeroes(Sprite frame)
    {
        if (heroesPanel == null) return;
        CoreHide(heroesPanel.transform, "Selected Hero Card", "Heroes Clean Backdrop", "Equipment Panel");
        var height = CoreHeight(heroesPanel);
        CoreRect(heroRosterFilterRoot, 0, 20, 980, 118);
        CorePanel(heroRosterFilterRoot, frame);
        CoreRect(heroRosterCountText, -350, 30, 200, 56);
        CoreText(heroRosterCountText, 34);
        CoreRect(heroSortToggleButton, -65, 12, 270, 94);
        CoreRect(heroAttackTypeFilterButton, 290, 12, 360, 94);
        CoreText(heroSortToggleText, 32);
        CoreText(heroAttackTypeFilterText, 32);
        CoreRect(heroSubTabRoot, 0, height - 128, 980, 112);
        CorePanel(heroSubTabRoot, frame);
        CoreRect(heroRosterTabButton, -240, 2, 430, 106);
        CoreRect(heroSetTeamTabButton, 240, 2, 430, 106);
        CoreText(heroRosterTabText, 34);
        CoreText(heroSetTeamTabText, 34);
        var team = heroesTabMode == HeroesTabMode.SetTeam;
        CoreRect(heroTeamRoot, 0, 20, 980, 350);
        CorePanel(heroTeamRoot, frame);
        CoreHide(heroTeamRoot, "Team Backplate");
        CoreRect(heroTeamHintText, -100, 16, 700, 66);
        CoreText(heroTeamHintText, 26);
        CoreRect(heroAutoSetTeamButton, 348, 12, 230, 82);
        for (var i = 0; heroTeamSlotButtons != null && i < heroTeamSlotButtons.Length; i++)
        {
            CoreRect(heroTeamSlotButtons[i], (i - 3) * 132, 104, 126, 220);
            CoreRect(heroTeamSlotPortraits[i], 0, 12, 100, 106);
            CoreRect(heroTeamSlotTexts[i], 0, 124, 124, 90);
            CoreText(heroTeamSlotTexts[i], 22);
        }
        var top = team ? 390f : 156f;
        var cardHeight = Mathf.Min(408, (height - top - 150) / 3f - 12);
        if (heroSelectButtons == null) return;
        for (var i = 0; i < heroSelectButtons.Length; i++)
        {
            var b = heroSelectButtons[i];
            CoreHide(b.transform, "Runtime Hero Portrait Matte");
            CoreRect(b, (i % 3 - 1) * 320, top + (i / 3) * (cardHeight + 12), 300, cardHeight);
            var portraitSize = Mathf.Min(220, cardHeight * .51f);
            if (heroCardPortraits != null && i < heroCardPortraits.Length)
                CoreRect(heroCardPortraits[i], 0, 24, portraitSize, portraitSize);
            if (heroCardLevelTexts != null && i < heroCardLevelTexts.Length)
            {
                CoreRect(heroCardLevelTexts[i], 0, cardHeight * .55f, 236, 40);
                CoreText(heroCardLevelTexts[i], 30);
            }
            if (heroCardStarTexts != null && i < heroCardStarTexts.Length)
            {
                CoreRect(heroCardStarTexts[i], 0, cardHeight * .65f, 242, 38);
                CoreText(heroCardStarTexts[i], 27);
            }
            if (heroCardShardFills != null && i < heroCardShardFills.Length && heroCardShardFills[i] != null)
                CoreRect(heroCardShardFills[i].transform.parent, 0, cardHeight * .82f, 214, 40);
            if (heroCardShardTexts != null && i < heroCardShardTexts.Length)
            {
                CoreRect(heroCardShardTexts[i], 0, 0, 206, 40);
                CoreText(heroCardShardTexts[i], 26);
            }
            if (heroCardRoleBadgeTexts != null && i < heroCardRoleBadgeTexts.Length)
            {
                CoreRect(heroCardRoleBadgeTexts[i].transform.parent, -92, 18, 42, 38);
                CoreText(heroCardRoleBadgeTexts[i], 25);
                CoreRect(heroCardRoleBadgeTexts[i], 0, 0, 42, 38);
            }
            if (heroCardTeamBadgeTexts != null && i < heroCardTeamBadgeTexts.Length)
            {
                CoreRect(heroCardTeamBadgeTexts[i].transform.parent, 92, 18, 42, 38);
                CoreText(heroCardTeamBadgeTexts[i], 25);
                CoreRect(heroCardTeamBadgeTexts[i], 0, 0, 42, 38);
            }
        }
    }

    private void ReadableDungeons(Sprite frame)
    {
        if (dungeonsPanel == null || dungeonSelectorPanelRoot == null) return;
        var height = CoreHeight(dungeonsPanel);
        CoreRect(dungeonsHeaderText, 0, 26, 940, 62);
        CoreText(dungeonsHeaderText, 48);
        CoreRect(dungeonsSubtitleText, 0, 90, 940, 60);
        CoreText(dungeonsSubtitleText, 26);
        CoreRect(dungeonSelectorPanelRoot, 0, 162, 1000, height - 182);
        dungeonSelectorPanelRoot.GetComponent<Image>().enabled = false;
        CoreHide(dungeonSelectorPanelRoot, "Dungeon Selector Header Rail", "Dungeon Selector Header Trim");
        SetComponentActive(dungeonSelectorTitleText, false);
        CoreRect(dungeonSelectorCardsRoot, 0, 0, 980, 342);
        var selectors = new[] { goldDungeonButton, essenceDungeonButton, gearDungeonButton, ancientTowerDungeonButton, shardRiftDungeonButton };
        for (var i = 0; i < selectors.Length; i++)
        {
            var b = selectors[i];
            if (b == null) continue;
            CoreRect(b, i < 3 ? (i - 1) * 326 : (i - 3.5f) * 326, i < 3 ? 0 : 170, 310, 158);
            CoreHide(b.transform, "Selector Card Back", "Selector Card Accent", "Selector Banner Shade", "Dungeon Selector Icon", "Dungeon Set Detail");
            var rect = b.GetComponent<RectTransform>();
            SetNamedChildRuntimeRect(rect, "Selector Banner", new Vector2(0, -12), new Vector2(272, 64), new Vector2(.5f, 1));
            SetNamedChildRuntimeRect(rect, "Dungeon Set Title", new Vector2(0, -77), new Vector2(280, 36), new Vector2(.5f, 1));
            SetNamedChildRuntimeRect(rect, "Dungeon Set Progress", new Vector2(0, -113), new Vector2(280, 32), new Vector2(.5f, 1));
            CoreText(b.transform.Find("Dungeon Set Title")?.GetComponent<TMP_Text>(), 29);
            CoreText(b.transform.Find("Dungeon Set Progress")?.GetComponent<TMP_Text>(), 25);
        }
        CoreRect(dungeonDetailRoot, 0, 348, 960, 656);
        CorePanel(dungeonDetailRoot, frame);
        CoreHide(dungeonDetailRoot, "Dungeon Detail Top Trim", "Dungeon Detail Banner Shade", "Dungeon Detail Info Plate");
        CoreRect(dungeonDetailBannerImage, 0, 22, 888, 240);
        CoreRect(dungeonDetailBossImage, 350, 266, 100, 100);
        CoreRect(dungeonDetailTitleText, -45, 276, 740, 54);
        CoreText(dungeonDetailTitleText, 42);
        CoreRect(dungeonDetailMetaText, 0, 338, 840, 86);
        CoreText(dungeonDetailMetaText, 28);
        CoreRect(dungeonDetailRewardsText, 0, 430, 850, 100);
        CoreText(dungeonDetailRewardsText, 26);
        CoreRect(dungeonDetailRunButton, 0, 534, 580, 112);
        CoreText(dungeonDetailRunButtonText, 38);
        CoreRect(dungeonFloorListRoot, 0, 1020, 980, 204);
        dungeonFloorListRoot.GetComponent<Image>().enabled = false;
        CoreHide(dungeonFloorListRoot, "Dungeon Floor Header", "Dungeon Floor List Title");
        CoreRect(towerFloorSectionRoot, 0, 0, 350, 42);
        for (var i = 0; dungeonFloorButtons != null && i < dungeonFloorButtons.Length; i++)
        {
            CoreRect(dungeonFloorButtons[i], (i - 1.5f) * 242, 46, 234, 150);
            var floorImage = dungeonFloorButtons[i].GetComponent<Image>();
            floorImage.sprite = frame;
            floorImage.type = Image.Type.Sliced;
            floorImage.pixelsPerUnitMultiplier = 4;
            CoreHide(dungeonFloorButtons[i].transform, "Floor Accent");
            CoreRect(dungeonFloorTitleTexts[i], 0, 24, 208, 34);
            CoreRect(dungeonFloorStatusTexts[i], 0, 60, 208, 32);
            CoreRect(dungeonFloorActionTexts[i], 0, 96, 208, 32);
            CoreText(dungeonFloorTitleTexts[i], 29);
            CoreText(dungeonFloorStatusTexts[i], 25);
            CoreText(dungeonFloorActionTexts[i], 24);
        }
        CoreRect(dungeonFlowHintRoot, 0, 1236, 940, 86);
        CorePanel(dungeonFlowHintRoot, frame);
        foreach (var text in dungeonFlowHintRoot.GetComponentsInChildren<TMP_Text>())
        {
            CoreRect(text, 0, 12, 880, 64);
            CoreText(text, 25);
        }
        SetComponentActive(runtimeDungeonResultText, false);
    }

    private void ReadableSummon(Sprite frame, Sprite card)
    {
        if (summonPanel == null || summonOfferRoot == null) return;
        CoreHide(summonPanel.transform, "Summon Banner", "Summon Title", "Summon Rates Card");
        var heading = summonPanel.transform.Find("Summon Header")?.GetComponent<TMP_Text>();
        CoreRect(heading, 0, 24, 940, 64);
        CoreText(heading, 48);
        CoreRect(summonOfferRoot, 0, 106, 980, 636);
        CorePanel(summonOfferRoot, frame);
        CoreHide(summonOfferRoot, "Runtime Sky Layer", "Runtime Cloud Layer", "Runtime Mountain Layer", "Runtime Hill Layer", "Runtime Castle Accent", "Runtime Tree Accent", "Summon Offer Top Shade", "Summon Offer Promo Shade", "Summon Offer Bottom Shade", "Summon Offer Border Top", "Summon Offer Border Bottom");
        CoreRect(summonOfferTitleText, 0, 30, 890, 60);
        CoreText(summonOfferTitleText, 44);
        CoreRect(summonOfferPromoText, 0, 96, 870, 74);
        CoreText(summonOfferPromoText, 30);
        for (var i = 0; summonOfferHeroImages != null && i < summonOfferHeroImages.Length; i++)
        {
            var x = (i - 1) * 300;
            var portraitFrame = summonOfferRoot.Find("Featured Portrait Frame " + i)?.GetComponent<Image>();
            if (portraitFrame == null)
                portraitFrame = CreateRuntimeSpriteImage(summonOfferRoot, "Featured Portrait Frame " + i, card, Vector2.zero, new Vector2(286, 420), new Vector2(.5f, 1));
            CoreRect(portraitFrame, x, 188, 286, 412);
            portraitFrame.raycastTarget = false;
            portraitFrame.transform.SetAsFirstSibling();
            CoreRect(summonOfferHeroImages[i], x, 232, 216, 216);
        }
        summonOfferRoot.Find("Core Inset Fill")?.SetAsFirstSibling();
        CoreRect(summonButton, -246, 760, 466, 140);
        CoreRect(summonTenButton, 246, 760, 466, 140);
        CoreText(summonButton?.GetComponentInChildren<TMP_Text>(), 36);
        CoreText(summonTenButton?.GetComponentInChildren<TMP_Text>(), 36);
        CoreRect(summonSingleCostText, -126, 82, 90, 38);
        CoreRect(summonTenCostText, -126, 82, 90, 38);
        CoreRect(summonButton.transform.Find("Gem Icon"), -190, 82, 28, 38);
        CoreRect(summonTenButton.transform.Find("Gem Icon"), -190, 82, 28, 38);
        CoreText(summonSingleCostText, 27);
        CoreText(summonTenCostText, 27);
        CoreRect(summonCarouselRoot, 0, 920, 980, 272);
        CorePanel(summonCarouselRoot, frame);
        for (var i = 0; summonCarouselButtons != null && i < summonCarouselButtons.Length; i++)
        {
            var b = summonCarouselButtons[i];
            CoreRect(b, (i - 1) * 284, 26, 270, 214);
            CoreHide(b.transform, "Runtime Sky Layer", "Runtime Cloud Layer", "Runtime Mountain Layer", "Runtime Hill Layer", "Runtime Castle Accent", "Runtime Tree Accent");
            CoreRect(summonCarouselTitleTexts[i], 0, 12, 248, 64);
            CoreText(summonCarouselTitleTexts[i], 28);
            CoreRect(summonCarouselRateTexts[i], 0, 164, 250, 38);
            CoreText(summonCarouselRateTexts[i], 24);
            CoreRect(summonCarouselHeroImages[i * 2], -56, 74, 88, 88);
            CoreRect(summonCarouselHeroImages[i * 2 + 1], 56, 74, 88, 88);
            ApplyRuntimeCorePanelSkin(b.GetComponent<RectTransform>(), frame);
            b.GetComponent<Image>().pixelsPerUnitMultiplier = 4;
            b.GetComponent<Image>().raycastTarget = true;
            b.GetComponent<Image>().color = i == 1 ? new Color(1, .88f, .55f) : Color.white;
        }
        CoreRect(summonCarouselPreviousButton, -460, 82, 64, 104);
        CoreRect(summonCarouselNextButton, 460, 82, 64, 104);
        ApplyRuntimeCoreMapNodeSkin(new[] { summonCarouselPreviousButton, summonCarouselNextButton }, Resources.Load<Sprite>("Mythwake/UI/Core/ui_map_node"));
        CoreRect(summonResultBoxRoot, 0, 1210, 960, 92);
        CorePanel(summonResultBoxRoot, frame);
        CoreRect(summonResultText, 0, 10, 890, 72);
        CoreText(summonResultText, 28);
        CoreRect(summonCountChipRoot, -340, 1330, 270, 142);
        CorePanel(summonCountChipRoot, frame);
        CoreRect(summonCountText, 0, 40, 242, 62);
        CoreText(summonCountText, 30);
        if (summonCountText != null) summonCountText.alignment = TextAlignmentOptions.Center;
        CoreRect(summonRatesBoxRoot, 150, 1330, 660, 142);
        CorePanel(summonRatesBoxRoot, frame);
        CoreHide(summonRatesBoxRoot, "Rates Box Glow");
        CoreRect(summonRatesText, 0, 18, 608, 106);
        CoreText(summonRatesText, 29);
    }

    private void ReadableMaps()
    {
        LayoutReadableVillageHeader();
        if (homeActionRoot != null)
        {
            var height = CoreHeight(homePanel);
            CoreRect(homeActionRoot, 0, 0, 1080, height);
            CoreHide(homeActionRoot, "Campaign Screen Frame");
            var idleHeight = Mathf.Max(279, height - 1221);
            CoreRect(homeIdleCombatRoot, 0, 1221, 1040, idleHeight);
            SetNamedChildRuntimeRect(homeIdleCombatRoot, "Home Idle Mini Map Background", Vector2.zero, new Vector2(1040, idleHeight), new Vector2(.5f, 1));
            SetNamedChildRuntimeRect(homeIdleCombatRoot, "Idle Combat Map Dim", Vector2.zero, new Vector2(1040, idleHeight), new Vector2(.5f, 1));
            CoreText(homeIdleCombatText, 30);
            CoreRect(homeIdleCombatText, 0, 24, 850, 52);
            CoreText(homeIdleRewardText, 24);
            CoreRect(homeIdleRewardText, 0, 204, 820, 66);
            var rewardProgressBackground = homeIdleRewardFill != null
                ? homeIdleRewardFill.transform.parent as RectTransform
                : null;
            if (rewardProgressBackground != null)
                rewardProgressBackground.sizeDelta = new Vector2(850, 66);
            CoreRect(campaignStagePreviewRoot, 0, 800, 840, 164);
            CoreRect(campaignStagePreviewText, 0, 10, 794, 144);
            CoreText(campaignStagePreviewText, 26);
            var modeBadge = homeActionRoot.Find("Home Stage Mode Badge")?.GetComponent<RawImage>();
            if (modeBadge != null)
                modeBadge.texture = Resources.Load<Sprite>("Mythwake/UI/Core/ui_action_button").texture;
            foreach (var b in campaignStageButtons)
            {
                if (b == null) continue;
                var rect = b.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(144, 152);
                var node = Resources.Load<Sprite>("Mythwake/UI/Core/ui_map_node");
                foreach (var haloName in new[] { "Stage Selected Halo", "Stage Current Halo" })
                {
                    var halo = b.transform.Find(haloName)?.GetComponent<Image>();
                    if (halo == null) continue;
                    halo.sprite = node;
                    CoreRect(halo, 0, -6, 156, 164);
                }
                CoreRect(b.transform.Find("Stage Icon"), 0, 32, 68, 68);
                var label = b.transform.Find("Stage Label")?.GetComponent<TMP_Text>();
                CoreRect(label, 0, 108, 130, 40);
                CoreText(label, 28);
                foreach (var badgeName in new[] { "Stage Locked Badge", "Stage Current Badge", "Stage Cleared Badge" })
                    CoreRect(b.transform.Find(badgeName), 0, 78, 80, 30);
            }
        }
    }

    private void LayoutReadableVillageHeader()
    {
        if (villagePanel == null) return;
        CoreRect(villageHeaderText, 0, 24, 940, 64);
        CoreText(villageHeaderText, 48);
        CoreText(villageHintText, 28);
        var hintHeight = villageHintText == null ? 52 : Mathf.Max(52, villageHintText.GetPreferredValues(villageHintText.text, 940, Mathf.Infinity).y + 12);
        CoreRect(villageHintText, 0, 88, 940, hintHeight);
        var mapTop = 96 + hintHeight;
        CoreRect(villageMapViewportRoot, 0, mapTop, 1040, CoreHeight(villagePanel) - mapTop);
    }

    private void ReadableDialogs(Sprite frame, Sprite card)
    {
        CorePanel(villageBuildPanelRoot, frame);
        CorePanel(villageDemolishPanelRoot, frame);
        CoreRect(villageBuildPanelRoot, 0, 480, 960, 600);
        CoreHide(villageBuildPanelRoot, "Divider");
        CoreRect(villageBuildPanelTitleText, 0, 28, 870, 60);
        CoreText(villageBuildPanelTitleText, 42);
        CoreRect(villageBuildPanelBodyText, 0, 104, 880, 90);
        CoreText(villageBuildPanelBodyText, 30);
        for (var i = 0; villageBuildOptionButtons != null && i < villageBuildOptionButtons.Length; i++)
        {
            var b = villageBuildOptionButtons[i];
            CoreRect(b, (i - 1) * 296, 214, 278, 226);
            CoreRect(b.transform.Find("Icon"), 0, 12, 160, 160);
            CoreRect(villageBuildOptionTexts[i], 0, 172, 254, 48);
            CoreText(villageBuildOptionTexts[i], 28);
        }
        CoreRect(villageBuildButton, -228, 468, 410, 110);
        CoreRect(villageBuildCloseButton, 228, 468, 410, 110);
        CoreText(villageBuildButton?.GetComponentInChildren<TMP_Text>(), 32);
        CoreText(villageBuildCloseButton?.GetComponentInChildren<TMP_Text>(), 32);
        CorePanel(summonResultPopupRoot, frame);
        CoreHide(summonResultPopupRoot, "Result Parchment", "Result Controls Backplate", "Result Header Glow");
        for (var i = 0; summonResultHeroFrames != null && i < summonResultHeroFrames.Length; i++)
        {
            var image = summonResultHeroFrames[i];
            if (image == null) continue;
            image.sprite = card;
            image.color = Color.white;
            image.type = Image.Type.Simple;
        }
    }

    private void ReadableHeroDetails(Sprite frame)
    {
        if (heroDetailRoot == null) return;
        SetComponentActive(heroSubTabRoot, !heroDetailRoot.gameObject.activeSelf);
        // Detail and equipment are opaque pages inside the content area.
        // Their backgrounds also intercept clicks aimed at the roster underneath.
        CoreRect(heroDetailRoot, 0, 0, 1040, 1450);
        var available = heroesPanel.GetComponent<RectTransform>().rect;
        var scale = Mathf.Min(1f, available.height / 1450f, available.width / 1040f);
        heroDetailRoot.localScale = Vector3.one * Mathf.Max(.1f, scale);
        var pageHeight = Mathf.Max(1450, available.height / Mathf.Max(.1f, scale));
        heroDetailRoot.sizeDelta = new Vector2(1040, pageHeight);
        CorePanel(heroDetailRoot, frame);
        var fill = heroDetailRoot.Find("Core Inset Fill").GetComponent<Image>();
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;
        fill.raycastTarget = true;
        CoreRect(heroDetailRoot.Find("Hero Detail Armory Background"), 0, 0, 1040, pageHeight);
        CoreHide(heroDetailRoot, "Hero Detail Right Edge Scrim", "Hero Detail Tabs Backplate");
        CoreRect(heroDetailCloseButton, 454, 24, 80, 80);
        for (var i = 0; i < heroDetailGearSlotButtons.Length; i++)
        {
            CoreRect(heroDetailGearSlotButtons[i], i < 4 ? -330 : 330, 184 + (i % 4) * 142, 180, 132);
            CoreRect(heroDetailGearSlotIcons[i], 0, 8, 84, 56);
            CoreRect(heroDetailGearSlotTexts[i], 0, 70, 168, 58);
            CoreText(heroDetailGearSlotTexts[i], 20);
        }
        CoreRect(heroDetailStatsText, 0, 808, 900, 110);
        CoreText(heroDetailStatsText, 25);
        CoreRect(heroDetailResourceText, 0, 936, 900, 90);
        CoreText(heroDetailResourceText, 26);
        CoreRect(heroDetailLevelButton, -226, 1050, 420, 94);
        CoreRect(heroDetailStarButton, 226, 1050, 420, 94);
        CoreRect(heroDetailRemoveGearButton, -226, 1160, 420, 94);
        CoreRect(heroDetailEquipGearButton, 226, 1160, 420, 94);
        CoreRect(heroDetailOpenChestButton, 0, 1270, 540, 94);
        foreach (var b in new[] { heroDetailLevelButton, heroDetailStarButton, heroDetailRemoveGearButton, heroDetailEquipGearButton, heroDetailOpenChestButton })
            CoreText(b.GetComponentInChildren<TMP_Text>(), 28);

        if (heroDetailGearListRoot == null) return;
        CoreRect(heroDetailGearListRoot, 0, 0, 1040, pageHeight);
        CorePanel(heroDetailGearListRoot, frame);
        var gearFill = heroDetailGearListRoot.Find("Core Inset Fill").GetComponent<Image>();
        gearFill.rectTransform.offsetMin = Vector2.zero;
        gearFill.rectTransform.offsetMax = Vector2.zero;
        gearFill.raycastTarget = true;
        CoreHide(heroDetailGearListRoot, "Divider");
        CoreRect(heroDetailGearListTitleText, -40, 34, 800, 72);
        CoreText(heroDetailGearListTitleText, 38);
        CoreRect(heroDetailGearListCloseButton, 454, 24, 80, 80);
        for (var i = 0; i < heroDetailGearOptionButtons.Length; i++)
        {
            CoreRect(heroDetailGearOptionButtons[i], 0, 132 + i * 104, 920, 94);
            CoreRect(heroDetailGearOptionButtons[i].transform.Find("Icon Back"), -390, 15, 70, 64);
            CoreRect(heroDetailGearOptionTexts[i], 45, 12, 740, 70);
            CoreText(heroDetailGearOptionTexts[i], 28);
        }
        CoreRect(heroDetailGearConfirmRoot, 0, 790, 920, 600);
        CorePanel(heroDetailGearConfirmRoot, frame);
        CoreHide(heroDetailGearConfirmRoot, "Selected Gear Detail Inner");
        CoreRect(heroDetailGearConfirmIcon, 0, 32, 144, 116);
        CoreRect(heroDetailGearConfirmTitleText, 0, 172, 850, 72);
        CoreText(heroDetailGearConfirmTitleText, 32);
        CoreRect(heroDetailGearConfirmStatsText, 0, 256, 850, 170);
        CoreText(heroDetailGearConfirmStatsText, 28);
        CoreRect(heroDetailGearConfirmEquipButton, 0, 466, 650, 100);
        CoreText(heroDetailGearConfirmEquipButton.GetComponentInChildren<TMP_Text>(), 32);
    }

    private void ReadableTopBar(Sprite button, Sprite node)
    {
        if (topBarRoot == null) return;
        // Keep the original controls and their handlers, replace their decoration.
        SetComponentActive(topbarFrameImage, false);
        var plate = topBarRoot.Find("Core Resource Plate")?.GetComponent<Image>();
        if (plate == null) plate = CreateRuntimeSpriteImage(topBarRoot, "Core Resource Plate", button, Vector2.zero, new Vector2(1080, 158), new Vector2(.5f, 1));
        CoreRect(plate, 0, 6, 1056, 146);
        plate.sprite = Resources.Load<Sprite>("Mythwake/UI/Core/ui_screen_frame");
        plate.type = Image.Type.Sliced;
        plate.pixelsPerUnitMultiplier = 4;
        plate.preserveAspect = false;
        plate.raycastTarget = false;
        plate.transform.SetAsFirstSibling();
        CoreText(topPlayerNameText, 30);
        CoreText(topGemAmountText, 32);
        CoreText(topGoldAmountText, 32);
        CoreRect(topPlayerNameText, -332, 35, 230, 42);
        CoreRect(topPowerText, -330, 83, 216, 36);
        CoreRect(topPowerIconImage, -462, 82, 36, 36);
        CoreRect(topGemAmountText, -12, 48, 172, 48);
        CoreRect(topGoldAmountText, 306, 48, 190, 48);
        CoreRect(topGemPlusButton, 116, 43, 68, 64);
        ApplyRuntimeCoreMapNodeSkin(new[] { topGemPlusButton }, node);
        CoreRect(topManagementMenuButton, 474, 38, 82, 78);
        if (topManagementMenuButton != null)
        {
            var menuImage = topManagementMenuButton.GetComponent<Image>();
            menuImage.sprite = node;
            menuImage.color = Color.white;
            menuImage.enabled = true;
            menuImage.type = Image.Type.Simple;
            var label = EnsureRuntimeChildText(topManagementMenuButton.transform, "Core Menu Label", "≡", 40, new Vector2(0, -10), new Vector2(70, 58));
            label.raycastTarget = false;
        }
        var gem = topBarRoot.Find("Core Gem Icon")?.GetComponent<RawImage>();
        if (gem == null) gem = CreateRuntimeRawImage(topBarRoot, "Core Gem Icon", GetCurrencyIconTexture("mythic_gem"), Vector2.zero, new Vector2(44, 54), new Vector2(.5f, 1));
        CoreRect(gem, -124, 45, 44, 54);
        gem.raycastTarget = false;
        var gold = topBarRoot.Find("Core Gold Icon")?.GetComponent<RawImage>();
        if (gold == null) gold = CreateRuntimeRawImage(topBarRoot, "Core Gold Icon", GetCurrencyIconTexture("gold_coin"), Vector2.zero, new Vector2(48, 48), new Vector2(.5f, 1));
        CoreRect(gold, 192, 48, 48, 48);
        gold.raycastTarget = false;
    }
}
