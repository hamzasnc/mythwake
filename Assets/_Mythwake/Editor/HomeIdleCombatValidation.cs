using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HomeIdleCombatValidation
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Mythwake/Validate Home Idle Combat")]
    public static void RunHomeIdleCombatValidation()
    {
        try
        {
            ValidateHomeIdleCombatUi();
            Debug.Log("Home Idle Combat validated: campaign map, current-stage marker, boss node badges, path progress colors, region texture/UV sync, popup exclusivity, reward progress, patrol info tick details, server guard, clickable stage preview, foreground patrol fight, active loot tick, and no automatic stage clear are present.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateHomeIdleCombatUi()
    {
        EditorSceneManager.OpenScene(ScenePath);

        var controller = FindSceneComponent<IdlePrototypeController>();
        if (controller == null)
        {
            throw new InvalidOperationException("Missing IdlePrototypeController in SampleScene.");
        }

        InvokePrivate(controller, "EnsureRuntimeScreenLayout");
        InvokePrivate(controller, "RegisterNavigation");
        SetPrivateField(controller, "backendGameplayEnabled", false);
        controller.ShowHome();
        Canvas.ForceUpdateCanvases();

        var mapRoot = RequireObject("Home Campaign Map Root", true);
        var scrollRect = mapRoot.GetComponent<ScrollRect>();
        if (scrollRect == null || scrollRect.content == null || !scrollRect.vertical || scrollRect.horizontal)
        {
            throw new InvalidOperationException("Home Campaign Map Root should be a vertical-only ScrollRect with content.");
        }

        var mapContent = RequireObject("Campaign Map Content", true);
        var mapImage = RequireRawImageWithTexture("Campaign World Map Image");
        if (!mapImage.texture.name.Contains("area_map_scorched_plains"))
        {
            throw new InvalidOperationException($"Campaign map should use area_map_scorched_plains, got '{mapImage.texture.name}'.");
        }

        ValidateHomePopupExclusivity(controller);

        var preview = RequireObject("Campaign Stage Preview", true);
        var previewText = RequireText(preview, "Campaign Stage Preview Text");
        RequireCopy(previewText.text, "Abschnitt");
        RequireCopy(previewText.text, "Idle sammelt nur kleine Beute");
        AssertTextFits(previewText, "Campaign Stage Preview Text");

        var node = RequireButton("Campaign Stage Node 3");
        AssertInsideParent(mapContent, node.gameObject);
        node.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        RequireCopy(previewText.text, "Abschnitt");
        var detailPopup = RequireObject("Campaign Stage Detail Popup", true);
        var detailTitle = RequireText(detailPopup, "Title");
        var detailBody = RequireText(detailPopup, "Stage Detail Body");
        var detailEnemyHeader = RequireText(detailPopup, "Stage Detail Enemy Header");
        var detailRewardHeader = RequireText(detailPopup, "Stage Detail Reward Header");
        var detailRewardText = RequireText(detailPopup, "Stage Detail Reward Text 1");
        RequireCopy(detailTitle.text, "Abschnitt");
        RequireCopy(detailBody.text, "Damage");
        RequireCopy(detailBody.text, "Normal");
        RequireCopy(detailEnemyHeader.text, "Feindliche Formation");
        RequireCopy(detailRewardHeader.text, "Belohnungen bei Abschluss");
        RequireCopy(detailRewardText.text, "Essence");
        AssertTextFits(detailBody, "Stage Detail Body");
        AssertTextFits(detailRewardText, "Stage Detail Reward Text 1");
        RequireRawImageWithTexture("Stage Detail Map Preview");
        for (var i = 1; i <= 5; i++)
        {
            RequireRawImageWithTexture($"Stage Detail Enemy {i}");
        }

        RequireRawImageWithTexture("Stage Detail Reward Icon 1");
        var detailCloseButton = RequireButton("Stage Detail Close Button");
        detailCloseButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (detailPopup.activeInHierarchy)
        {
            throw new InvalidOperationException("Campaign Stage Detail Popup should close from its close button.");
        }

        ValidateLockedStageDetailBattleGuard(controller);
        ValidateCurrentStageDetailBattleFlow(controller);
        ValidateCurrentStageNodeMarker(controller);
        ValidateCampaignPathProgress(controller);
        ValidateCampaignBossNodeBadges(controller);
        ValidateCampaignMilestoneNodeBadges(controller);
        ValidateCampaignStagePreviewTags(controller);
        ValidateCampaignStageDetailTags(controller);

        var idleRoot = RequireObject("Home Idle Combat Root", true);
        AssertInsideParent(RequireObject("Home Generated Art Root", true), idleRoot);
        AssertDoesNotOverlap(mapRoot, idleRoot);
        AssertSameWidth(mapRoot, idleRoot);
        AssertConnectedBelow(mapRoot, idleRoot);
        var battleButton = RequireButton("Home Battle Button").gameObject;
        AssertExtendsBehind(mapRoot, battleButton);
        AssertExtendsBelow(idleRoot, battleButton);
        var idleMap = RequireRawImageWithTexture("Home Idle Mini Map Background");
        if (!idleMap.texture.name.Contains("area_map_"))
        {
            throw new InvalidOperationException($"Home Idle Mini Map Background should use an area map texture, got '{idleMap.texture.name}'.");
        }

        AssertInsideParent(idleRoot, idleMap.gameObject);
        ValidateHomeProgressMapTextureSwitch(controller, mapImage, idleMap);
        var idleText = RequireText(idleRoot, "Home Idle Combat Text");
        var rewardText = RequireText(idleRoot, "Home Idle Reward Text");
        var lootPopupText = RequireText(idleRoot, "Home Idle Loot Pop Text");
        RequireCopy(idleText.text, "Patrol");
        RequireCopy(rewardText.text, "Naechste");
        AssertTextFits(idleText, "Home Idle Combat Text");
        AssertTextFits(rewardText, "Home Idle Reward Text");
        ValidateHomeIdleRewardProgressAndServerMode(controller, GetPrivateField<Image>(controller, "homeIdleRewardFill"), rewardText, lootPopupText);

        var infoButton = RequireButton("Home Idle Info Button");
        infoButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        var infoPopup = RequireObject("Home Idle Info Popup", true);
        var infoBody = RequireText(infoPopup, "Home Idle Info Body");
        RequireCopy(infoBody.text, "Patrol");
        RequireCopy(infoBody.text, "Letzte:");
        RequireCopy(infoBody.text, "Naechste:");
        RequireCopy(infoBody.text, "Gold");
        RequireCopy(infoBody.text, "Essence");
        RequireCopy(infoBody.text, "Tickrate");
        RequireCopy(infoBody.text, "schliesst keine Abschnitte");
        AssertTextFits(infoBody, "Home Idle Info Body");
        var infoCloseButton = RequireButton("Home Idle Info Close Button");
        infoCloseButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (infoPopup.activeInHierarchy)
        {
            throw new InvalidOperationException("Home Idle Info Popup should close from its close button.");
        }

        for (var i = 1; i <= 3; i++)
        {
            RequireRawImageWithTexture($"Home Idle Hero {i}");
            RequireRawImageWithTexture($"Home Idle Enemy {i}");
        }

        SetPrivateField(controller, "homeIdleRewardTimer", 0f);
        var stageBefore = GetPrivateField<int>(controller, "enemyLevel");
        var goldBefore = GetPrivateField<int>(controller, "gold");
        var mythEssenceBefore = GetPrivateField<int>(controller, "mythEssence");
        var enemyHpBefore = GetPrivateField<int>(controller, "enemyHp");
        var enemyMaxHpBefore = GetPrivateField<int>(controller, "enemyMaxHp");
        var dailyStageClearCountBefore = GetPrivateField<int>(controller, "dailyStageClearCount");
        try
        {
            InvokePrivate(controller, "TickHomeIdleCombat", 10.1f);
            Canvas.ForceUpdateCanvases();

            var stageAfter = GetPrivateField<int>(controller, "enemyLevel");
            var goldAfter = GetPrivateField<int>(controller, "gold");
            var mythEssenceAfterIdle = GetPrivateField<int>(controller, "mythEssence");
            if (stageAfter != stageBefore)
            {
                throw new InvalidOperationException($"Home idle combat must not auto-clear stages. Before={stageBefore}, after={stageAfter}.");
            }

            if (goldAfter <= goldBefore)
            {
                throw new InvalidOperationException($"Home idle combat should grant a small local gold reward. Before={goldBefore}, after={goldAfter}.");
            }

            if (!lootPopupText.gameObject.activeSelf)
            {
                throw new InvalidOperationException("Home idle loot popup should become visible after a local reward tick.");
            }

            RequireCopy(lootPopupText.text, "Gold");
            RequireCopy(lootPopupText.text, "Essence");
            AssertTextFits(lootPopupText, "Home Idle Loot Pop Text");
            RequireCopy(rewardText.text, "Letzte");
            RequireCopy(rewardText.text, "Gold");
            RequireCopy(rewardText.text, "Essence");
            RequireCopy(rewardText.text, "Naechste");
            RequireLineBreak(rewardText.text, "Home Idle Reward Text after local tick");
            AssertTextFits(rewardText, "Home Idle Reward Text after local tick");

            InvokePrivate(controller, "ShowHomeIdleInfoPopup");
            Canvas.ForceUpdateCanvases();
            var infoPopupAfterTick = RequireObject("Home Idle Info Popup", true);
            var infoBodyAfterTick = RequireText(infoPopupAfterTick, "Home Idle Info Body");
            RequireCopy(infoBodyAfterTick.text, "Letzte:");
            RequireCopy(infoBodyAfterTick.text, "Naechste:");
            RequireCopy(infoBodyAfterTick.text, "Gold");
            RequireCopy(infoBodyAfterTick.text, "Essence");
            RequireCopy(infoBodyAfterTick.text, "Tickrate");
            AssertTextFits(infoBodyAfterTick, "Home Idle Info Body after local tick");
            RequireButton("Home Idle Info Close Button").onClick.Invoke();
            Canvas.ForceUpdateCanvases();

            var stageDefinition = InvokePrivate(controller, "GetStageDefinition", stageBefore);
            var wonResult = CreateWonCombatResult(controller);
            var actionResult = (MythwakeActionResultDto)InvokePrivate(controller, "ApplyCampaignFightResult", stageBefore, stageDefinition, wonResult);
            Canvas.ForceUpdateCanvases();

            var stageAfterClear = GetPrivateField<int>(controller, "enemyLevel");
            var mythEssenceAfterClear = GetPrivateField<int>(controller, "mythEssence");
            if (stageAfterClear <= stageBefore)
            {
                throw new InvalidOperationException($"Campaign clear should advance the stage. Before={stageBefore}, after={stageAfterClear}.");
            }

            if (mythEssenceAfterClear <= mythEssenceAfterIdle)
            {
                throw new InvalidOperationException($"Campaign clear should grant the displayed Myth Essence reward. Before={mythEssenceAfterIdle}, after={mythEssenceAfterClear}.");
            }

            if (!actionResult.success || actionResult.reward.mythEssence <= 0 || string.IsNullOrWhiteSpace(actionResult.reward.rewardId))
            {
                throw new InvalidOperationException($"Campaign clear should return a success reward payload, got success={actionResult.success}, rewardId='{actionResult.reward.rewardId}', essence={actionResult.reward.mythEssence}.");
            }

            RequireCopy(actionResult.message, "Reward");
        }
        finally
        {
            SetPrivateField(controller, "enemyLevel", stageBefore);
            SetPrivateField(controller, "gold", goldBefore);
            SetPrivateField(controller, "mythEssence", mythEssenceBefore);
            SetPrivateField(controller, "enemyHp", enemyHpBefore);
            SetPrivateField(controller, "enemyMaxHp", enemyMaxHpBefore);
            SetPrivateField(controller, "dailyStageClearCount", dailyStageClearCountBefore);
            SetPrivateField(controller, "homeIdleRewardTimer", 0f);
            SetPrivateField(controller, "homeIdleLootPopupTimer", 0f);
            SetPrivateField(controller, "homeIdleLastRewardGold", 0);
            SetPrivateField(controller, "homeIdleLastRewardEssence", 0);
            InvokePrivate(controller, "RefreshHomeIdleCombatUi");
            InvokePrivate(controller, "SaveProgress");
        }
    }

    private static T FindSceneComponent<T>() where T : Component
    {
        foreach (var component in Resources.FindObjectsOfTypeAll<T>())
        {
            if (component.gameObject.scene.IsValid())
            {
                return component;
            }
        }

        return null;
    }

    private static void ValidateLockedStageDetailBattleGuard(IdlePrototypeController controller)
    {
        var currentStage = GetPrivateField<int>(controller, "enemyLevel");
        var startStage = (int)InvokePrivate(controller, "GetCampaignMapStartStage");
        var currentNodeIndex = Mathf.Clamp(currentStage - startStage, 0, 9);
        if (currentNodeIndex >= 9)
        {
            return;
        }

        var lockedNode = RequireButton($"Campaign Stage Node {currentNodeIndex + 2}");
        lockedNode.onClick.Invoke();
        Canvas.ForceUpdateCanvases();

        var detailPopup = RequireObject("Campaign Stage Detail Popup", true);
        var detailBody = RequireText(detailPopup, "Stage Detail Body");
        RequireCopy(detailBody.text, "Gesperrt");
        AssertTextFits(detailBody, "Locked Stage Detail Body");

        var battleButton = RequireButton("Stage Detail Battle Button");
        if (battleButton.interactable)
        {
            throw new InvalidOperationException("Locked stage detail Battle button should not be interactable.");
        }

        battleButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (!detailPopup.activeInHierarchy)
        {
            throw new InvalidOperationException("Locked stage detail popup should stay open when its guarded Battle listener is invoked directly.");
        }

        var formationRoot = RequireObject("Campaign Formation Root", false);
        if (formationRoot.activeInHierarchy)
        {
            throw new InvalidOperationException("Locked stage detail Battle guard should not enter Formation.");
        }

        RequireButton("Stage Detail Close Button").onClick.Invoke();
        Canvas.ForceUpdateCanvases();
    }

    private static void ValidateHomeProgressMapTextureSwitch(IdlePrototypeController controller, RawImage mapImage, RawImage idleMap)
    {
        var enemyLevelBefore = GetPrivateField<int>(controller, "enemyLevel");
        var selectedStageBefore = GetPrivateField<int>(controller, "selectedCampaignStage");
        var centerBefore = GetPrivateField<bool>(controller, "homeCampaignMapNeedsCenter");
        var initialIdleUv = idleMap.uvRect;

        try
        {
            const int regionSwapStage = 25;
            var expectedTextureName = (string)InvokePrivate(controller, "GetHomeProgressMapTextureNameForStage", regionSwapStage);
            if (string.IsNullOrWhiteSpace(expectedTextureName) || expectedTextureName == "area_map_scorched_plains")
            {
                throw new InvalidOperationException($"Stage {regionSwapStage} should resolve to a later Home progress map texture, got '{expectedTextureName}'.");
            }

            SetPrivateField(controller, "enemyLevel", regionSwapStage);
            SetPrivateField(controller, "selectedCampaignStage", regionSwapStage);
            SetPrivateField(controller, "homeCampaignMapNeedsCenter", true);
            InvokePrivate(controller, "RefreshCampaignMapUi");
            Canvas.ForceUpdateCanvases();

            AssertTextureNameContains(mapImage, expectedTextureName, "Home campaign map region switch");
            AssertTextureNameContains(idleMap, expectedTextureName, "Home idle mini-map region switch");
            AssertUvApproximately(idleMap.uvRect, GetExpectedHomeIdleCombatMapUvRect(regionSwapStage), "Home idle mini-map region UV switch");
            if (Mathf.Abs(idleMap.uvRect.y - initialIdleUv.y) <= 0.01f)
            {
                throw new InvalidOperationException($"Home idle mini-map UV should move with stage progress. Before={initialIdleUv}, after={idleMap.uvRect}.");
            }

            var startStage = (int)InvokePrivate(controller, "GetCampaignMapStartStage");
            var currentNodeIndex = Mathf.Clamp(regionSwapStage - startStage, 0, 9);
            RequireButton($"Campaign Stage Node {currentNodeIndex + 1}").onClick.Invoke();
            Canvas.ForceUpdateCanvases();

            var detailMap = RequireRawImageWithTexture("Stage Detail Map Preview");
            AssertTextureNameContains(detailMap, expectedTextureName, "Stage detail map preview region switch");
            AssertUvApproximately(detailMap.uvRect, GetExpectedCampaignStageDetailMapUvRect(regionSwapStage), "Stage detail map preview region UV switch");
            RequireButton("Stage Detail Close Button").onClick.Invoke();
            Canvas.ForceUpdateCanvases();
        }
        finally
        {
            SetPrivateField(controller, "enemyLevel", enemyLevelBefore);
            SetPrivateField(controller, "selectedCampaignStage", selectedStageBefore);
            SetPrivateField(controller, "homeCampaignMapNeedsCenter", centerBefore);
            InvokePrivate(controller, "SetCampaignStageDetailPopupVisible", false);
            InvokePrivate(controller, "RefreshCampaignMapUi");
            Canvas.ForceUpdateCanvases();
        }
    }

    private static void ValidateCurrentStageNodeMarker(IdlePrototypeController controller)
    {
        var currentStage = GetPrivateField<int>(controller, "enemyLevel");
        var startStage = (int)InvokePrivate(controller, "GetCampaignMapStartStage");
        var currentNodeIndex = Mathf.Clamp(currentStage - startStage, 0, 9);
        var currentNode = RequireButton($"Campaign Stage Node {currentNodeIndex + 1}");
        var currentHalo = RequireChildObject(currentNode.gameObject, "Stage Current Halo");
        var haloImage = currentHalo.GetComponent<Image>();
        if (!currentHalo.activeInHierarchy || haloImage == null || haloImage.color.a < 0.2f)
        {
            throw new InvalidOperationException("Current campaign stage node should show a visible current-stage halo.");
        }

        var haloRect = currentHalo.GetComponent<RectTransform>();
        var nodeRect = currentNode.GetComponent<RectTransform>();
        if (haloRect == null || nodeRect == null || haloRect.rect.width <= nodeRect.rect.width || haloRect.rect.height <= nodeRect.rect.height)
        {
            throw new InvalidOperationException("Current campaign stage halo should be larger than the stage node frame.");
        }

        if (currentNodeIndex < 9)
        {
            var lockedNode = RequireButton($"Campaign Stage Node {currentNodeIndex + 2}");
            var lockedHalo = RequireChildObject(lockedNode.gameObject, "Stage Current Halo");
            if (lockedHalo.activeInHierarchy)
            {
                throw new InvalidOperationException("Locked campaign stage node should not show the current-stage halo.");
            }
        }
    }

    private static void ValidateCampaignPathProgress(IdlePrototypeController controller)
    {
        var enemyLevelBefore = GetPrivateField<int>(controller, "enemyLevel");
        var selectedStageBefore = GetPrivateField<int>(controller, "selectedCampaignStage");
        var centerBefore = GetPrivateField<bool>(controller, "homeCampaignMapNeedsCenter");

        try
        {
            const int currentStage = 5;
            SetPrivateField(controller, "enemyLevel", currentStage);
            SetPrivateField(controller, "selectedCampaignStage", currentStage);
            SetPrivateField(controller, "homeCampaignMapNeedsCenter", true);
            InvokePrivate(controller, "RefreshCampaignMapUi");
            Canvas.ForceUpdateCanvases();

            var pathSegments = GetPrivateField<Image[]>(controller, "campaignPathSegmentImages");
            if (pathSegments == null || pathSegments.Length < 5)
            {
                throw new InvalidOperationException("Campaign map should keep path segment images for progress coloring.");
            }

            var reachedSegment = pathSegments[3];
            var lockedSegment = pathSegments[4];
            if (reachedSegment == null || lockedSegment == null)
            {
                throw new InvalidOperationException("Campaign path segment images should not contain null entries.");
            }

            if (reachedSegment.raycastTarget || lockedSegment.raycastTarget)
            {
                throw new InvalidOperationException("Campaign path segment images should not intercept map node input.");
            }

            if (reachedSegment.color.a <= lockedSegment.color.a || reachedSegment.color.r <= lockedSegment.color.r)
            {
                throw new InvalidOperationException($"Reached campaign path segment should read brighter than locked segments. Reached={reachedSegment.color}, locked={lockedSegment.color}.");
            }

            RequireButton("Campaign Stage Node 6").onClick.Invoke();
            Canvas.ForceUpdateCanvases();
            if (pathSegments[4].color.a >= reachedSegment.color.a)
            {
                throw new InvalidOperationException("Selecting a locked future node should not make the next path segment look reached.");
            }
        }
        finally
        {
            SetPrivateField(controller, "enemyLevel", enemyLevelBefore);
            SetPrivateField(controller, "selectedCampaignStage", selectedStageBefore);
            SetPrivateField(controller, "homeCampaignMapNeedsCenter", centerBefore);
            InvokePrivate(controller, "SetCampaignStageDetailPopupVisible", false);
            InvokePrivate(controller, "RefreshCampaignMapUi");
            Canvas.ForceUpdateCanvases();
        }
    }

    private static void ValidateCampaignBossNodeBadges(IdlePrototypeController controller)
    {
        var enemyLevelBefore = GetPrivateField<int>(controller, "enemyLevel");
        var selectedStageBefore = GetPrivateField<int>(controller, "selectedCampaignStage");
        var centerBefore = GetPrivateField<bool>(controller, "homeCampaignMapNeedsCenter");

        try
        {
            const int currentStage = 11;
            SetPrivateField(controller, "enemyLevel", currentStage);
            SetPrivateField(controller, "selectedCampaignStage", currentStage);
            SetPrivateField(controller, "homeCampaignMapNeedsCenter", true);
            InvokePrivate(controller, "RefreshCampaignMapUi");
            Canvas.ForceUpdateCanvases();

            var bossNode = RequireButton("Campaign Stage Node 4");
            var bossBadge = RequireChildObject(bossNode.gameObject, "Stage Boss Badge");
            var bossBadgeImage = bossBadge.GetComponent<Image>();
            var bossBadgeText = RequireText(bossBadge, "Label");
            if (!bossBadge.activeInHierarchy || bossBadgeImage == null || bossBadgeImage.color.a < 0.5f)
            {
                throw new InvalidOperationException("Boss campaign stage node should show a visible boss badge.");
            }

            RequireCopy(bossBadgeText.text, "BOSS");
            AssertTextFits(bossBadgeText, "Campaign boss badge label");
            if (bossBadgeImage.raycastTarget || bossBadgeText.raycastTarget)
            {
                throw new InvalidOperationException("Boss campaign stage badge should not intercept node input.");
            }

            var normalNode = RequireButton("Campaign Stage Node 3");
            var normalBadge = RequireChildObject(normalNode.gameObject, "Stage Boss Badge");
            if (normalBadge.activeInHierarchy)
            {
                throw new InvalidOperationException("Non-boss campaign stage node should not show the boss badge.");
            }
        }
        finally
        {
            SetPrivateField(controller, "enemyLevel", enemyLevelBefore);
            SetPrivateField(controller, "selectedCampaignStage", selectedStageBefore);
            SetPrivateField(controller, "homeCampaignMapNeedsCenter", centerBefore);
            InvokePrivate(controller, "RefreshCampaignMapUi");
            Canvas.ForceUpdateCanvases();
        }
    }

    private static void ValidateCampaignMilestoneNodeBadges(IdlePrototypeController controller)
    {
        var enemyLevelBefore = GetPrivateField<int>(controller, "enemyLevel");
        var selectedStageBefore = GetPrivateField<int>(controller, "selectedCampaignStage");
        var centerBefore = GetPrivateField<bool>(controller, "homeCampaignMapNeedsCenter");

        try
        {
            const int currentStage = 6;
            SetPrivateField(controller, "enemyLevel", currentStage);
            SetPrivateField(controller, "selectedCampaignStage", currentStage);
            SetPrivateField(controller, "homeCampaignMapNeedsCenter", true);
            InvokePrivate(controller, "RefreshCampaignMapUi");
            Canvas.ForceUpdateCanvases();

            var milestoneNode = RequireButton("Campaign Stage Node 5");
            var milestoneBadge = RequireChildObject(milestoneNode.gameObject, "Stage Milestone Badge");
            var milestoneBadgeImage = milestoneBadge.GetComponent<Image>();
            var milestoneBadgeText = RequireText(milestoneBadge, "Label");
            if (!milestoneBadge.activeInHierarchy || milestoneBadgeImage == null || milestoneBadgeImage.color.a < 0.5f)
            {
                throw new InvalidOperationException("Milestone campaign stage node should show a visible bonus badge.");
            }

            RequireCopy(milestoneBadgeText.text, "BONUS");
            AssertTextFits(milestoneBadgeText, "Campaign milestone badge label");
            if (milestoneBadgeImage.raycastTarget || milestoneBadgeText.raycastTarget)
            {
                throw new InvalidOperationException("Milestone campaign stage badge should not intercept node input.");
            }

            var normalNode = RequireButton("Campaign Stage Node 4");
            var normalBadge = RequireChildObject(normalNode.gameObject, "Stage Milestone Badge");
            if (normalBadge.activeInHierarchy)
            {
                throw new InvalidOperationException("Non-milestone campaign stage node should not show the bonus badge.");
            }

            var bossMilestoneNode = RequireButton("Campaign Stage Node 10");
            var bossMilestoneBadge = RequireChildObject(bossMilestoneNode.gameObject, "Stage Milestone Badge");
            var bossBadge = RequireChildObject(bossMilestoneNode.gameObject, "Stage Boss Badge");
            if (bossMilestoneBadge.activeInHierarchy)
            {
                throw new InvalidOperationException("Boss milestone campaign stage node should keep the boss badge instead of also showing the bonus badge.");
            }

            if (!bossBadge.activeInHierarchy)
            {
                throw new InvalidOperationException("Boss milestone campaign stage node should still show the boss badge.");
            }
        }
        finally
        {
            SetPrivateField(controller, "enemyLevel", enemyLevelBefore);
            SetPrivateField(controller, "selectedCampaignStage", selectedStageBefore);
            SetPrivateField(controller, "homeCampaignMapNeedsCenter", centerBefore);
            InvokePrivate(controller, "RefreshCampaignMapUi");
            Canvas.ForceUpdateCanvases();
        }
    }

    private static void ValidateCampaignStagePreviewTags(IdlePrototypeController controller)
    {
        var enemyLevelBefore = GetPrivateField<int>(controller, "enemyLevel");
        var selectedStageBefore = GetPrivateField<int>(controller, "selectedCampaignStage");
        var centerBefore = GetPrivateField<bool>(controller, "homeCampaignMapNeedsCenter");
        var previewText = RequireText(RequireObject("Campaign Stage Preview", true), "Campaign Stage Preview Text");

        try
        {
            SetPrivateField(controller, "enemyLevel", 6);
            SetPrivateField(controller, "selectedCampaignStage", 4);
            SetPrivateField(controller, "homeCampaignMapNeedsCenter", true);
            InvokePrivate(controller, "RefreshCampaignMapUi");
            Canvas.ForceUpdateCanvases();
            RequireCopy(previewText.text, "Normal");
            RequireCopy(previewText.text, "Bonus alle 5");
            AssertTextFits(previewText, "Normal campaign stage preview text");

            SetPrivateField(controller, "selectedCampaignStage", 5);
            InvokePrivate(controller, "RefreshCampaignMapUi");
            Canvas.ForceUpdateCanvases();
            RequireCopy(previewText.text, "Bonus");
            RequireCopy(previewText.text, "Gems");
            RequireCopy(previewText.text, "Pass XP");
            AssertTextFits(previewText, "Bonus campaign stage preview text");

            SetPrivateField(controller, "enemyLevel", 11);
            SetPrivateField(controller, "selectedCampaignStage", 10);
            InvokePrivate(controller, "RefreshCampaignMapUi");
            Canvas.ForceUpdateCanvases();
            RequireCopy(previewText.text, "Boss");
            RequireCopy(previewText.text, "Boss-Knoten");
            AssertTextFits(previewText, "Boss campaign stage preview text");
        }
        finally
        {
            SetPrivateField(controller, "enemyLevel", enemyLevelBefore);
            SetPrivateField(controller, "selectedCampaignStage", selectedStageBefore);
            SetPrivateField(controller, "homeCampaignMapNeedsCenter", centerBefore);
            InvokePrivate(controller, "RefreshCampaignMapUi");
            Canvas.ForceUpdateCanvases();
        }
    }

    private static void ValidateCampaignStageDetailTags(IdlePrototypeController controller)
    {
        var enemyLevelBefore = GetPrivateField<int>(controller, "enemyLevel");
        var selectedStageBefore = GetPrivateField<int>(controller, "selectedCampaignStage");
        var centerBefore = GetPrivateField<bool>(controller, "homeCampaignMapNeedsCenter");

        try
        {
            SetPrivateField(controller, "enemyLevel", 6);
            SetPrivateField(controller, "selectedCampaignStage", 4);
            SetPrivateField(controller, "homeCampaignMapNeedsCenter", true);
            InvokePrivate(controller, "SetCampaignStageDetailPopupVisible", true);
            InvokePrivate(controller, "RefreshCampaignMapUi");
            Canvas.ForceUpdateCanvases();

            var detailPopup = RequireObject("Campaign Stage Detail Popup", true);
            var detailBody = RequireText(detailPopup, "Stage Detail Body");
            RequireCopy(detailBody.text, "Normal");
            RequireCopy(detailBody.text, "Bonus alle 5");
            AssertTextFits(detailBody, "Normal campaign stage detail body");

            SetPrivateField(controller, "selectedCampaignStage", 5);
            InvokePrivate(controller, "RefreshCampaignMapUi");
            Canvas.ForceUpdateCanvases();
            RequireCopy(detailBody.text, "Bonus");
            RequireCopy(detailBody.text, "Gems");
            RequireCopy(detailBody.text, "Pass XP");
            AssertTextFits(detailBody, "Bonus campaign stage detail body");

            SetPrivateField(controller, "enemyLevel", 11);
            SetPrivateField(controller, "selectedCampaignStage", 10);
            InvokePrivate(controller, "RefreshCampaignMapUi");
            Canvas.ForceUpdateCanvases();
            RequireCopy(detailBody.text, "Boss");
            RequireCopy(detailBody.text, "Boss-Knoten");
            AssertTextFits(detailBody, "Boss campaign stage detail body");
        }
        finally
        {
            SetPrivateField(controller, "enemyLevel", enemyLevelBefore);
            SetPrivateField(controller, "selectedCampaignStage", selectedStageBefore);
            SetPrivateField(controller, "homeCampaignMapNeedsCenter", centerBefore);
            InvokePrivate(controller, "SetCampaignStageDetailPopupVisible", false);
            InvokePrivate(controller, "RefreshCampaignMapUi");
            Canvas.ForceUpdateCanvases();
        }
    }

    private static void ValidateHomeIdleRewardProgressAndServerMode(IdlePrototypeController controller, Image rewardFill, TMP_Text rewardText, TMP_Text lootPopupText)
    {
        if (rewardFill == null)
        {
            throw new InvalidOperationException("Missing Home idle reward fill image.");
        }

        var backendBefore = GetPrivateField<bool>(controller, "backendGameplayEnabled");
        var timerBefore = GetPrivateField<float>(controller, "homeIdleRewardTimer");
        var lootTimerBefore = GetPrivateField<float>(controller, "homeIdleLootPopupTimer");
        var goldBefore = GetPrivateField<int>(controller, "gold");
        var essenceBefore = GetPrivateField<int>(controller, "mythEssence");
        var lastGoldBefore = GetPrivateField<int>(controller, "homeIdleLastRewardGold");
        var lastEssenceBefore = GetPrivateField<int>(controller, "homeIdleLastRewardEssence");
        var lootTextBefore = lootPopupText.text;

        try
        {
            SetPrivateField(controller, "backendGameplayEnabled", false);
            SetPrivateField(controller, "homeIdleRewardTimer", 3f);
            InvokePrivate(controller, "RefreshHomeIdleCombatUi");
            Canvas.ForceUpdateCanvases();

            AssertApproximately(rewardFill.rectTransform.anchorMax.x, 0.3f, 0.01f, "Home idle reward fill progress");
            RequireCopy(rewardText.text, "Naechste");
            RequireCopy(rewardText.text, "7s");
            AssertTextFits(rewardText, "Home Idle Reward Text progress state");

            SetPrivateField(controller, "backendGameplayEnabled", true);
            SetPrivateField(controller, "homeIdleRewardTimer", 9.8f);
            SetPrivateField(controller, "homeIdleLootPopupTimer", 1f);
            SetPrivateField(controller, "homeIdleLastRewardGold", 0);
            SetPrivateField(controller, "homeIdleLastRewardEssence", 0);
            lootPopupText.text = "+1 Gold  +1 Essence";
            lootPopupText.gameObject.SetActive(true);

            InvokePrivate(controller, "TickHomeIdleCombat", 10.5f);
            Canvas.ForceUpdateCanvases();

            if (GetPrivateField<int>(controller, "gold") != goldBefore || GetPrivateField<int>(controller, "mythEssence") != essenceBefore)
            {
                throw new InvalidOperationException("Home idle combat should not grant local rewards while Server Mode is active.");
            }

            AssertApproximately(rewardFill.rectTransform.anchorMax.x, 0f, 0.01f, "Home idle reward fill server state");

            if (lootPopupText.gameObject.activeSelf)
            {
                throw new InvalidOperationException("Home idle loot popup should stay hidden while Server Mode blocks local reward ticks.");
            }

            if (!string.IsNullOrWhiteSpace(lootPopupText.text))
            {
                throw new InvalidOperationException($"Home idle loot popup copy should be cleared in Server Mode, got '{lootPopupText.text}'.");
            }

            RequireCopy(rewardText.text, "Server Mode");
            RequireCopy(rewardText.text, "serverseitig");
            AssertTextFits(rewardText, "Home Idle Reward Text server state");

            InvokePrivate(controller, "ShowHomeIdleInfoPopup");
            Canvas.ForceUpdateCanvases();
            var infoPopup = RequireObject("Home Idle Info Popup", true);
            var infoBody = RequireText(infoPopup, "Home Idle Info Body");
            RequireCopy(infoBody.text, "Server Mode");
            RequireCopy(infoBody.text, "serverseitig");
            AssertTextFits(infoBody, "Home Idle Info Body server state");
            RequireButton("Home Idle Info Close Button").onClick.Invoke();
            Canvas.ForceUpdateCanvases();
        }
        finally
        {
            SetPrivateField(controller, "backendGameplayEnabled", backendBefore);
            SetPrivateField(controller, "homeIdleRewardTimer", timerBefore);
            SetPrivateField(controller, "homeIdleLootPopupTimer", lootTimerBefore);
            SetPrivateField(controller, "gold", goldBefore);
            SetPrivateField(controller, "mythEssence", essenceBefore);
            SetPrivateField(controller, "homeIdleLastRewardGold", lastGoldBefore);
            SetPrivateField(controller, "homeIdleLastRewardEssence", lastEssenceBefore);
            lootPopupText.text = lootTextBefore;
            InvokePrivate(controller, "RefreshHomeIdleCombatUi");
            Canvas.ForceUpdateCanvases();
        }
    }

    private static void ValidateHomePopupExclusivity(IdlePrototypeController controller)
    {
        InvokePrivate(controller, "ShowFastRewardsPopup");
        Canvas.ForceUpdateCanvases();
        RequireObject("Fast Rewards Popup", true);

        InvokePrivate(controller, "ShowHomeIdleInfoPopup");
        Canvas.ForceUpdateCanvases();
        RequireObject("Home Idle Info Popup", true);
        RequireInactive("Fast Rewards Popup", "Home Idle Info should close Fast Rewards.");

        InvokePrivate(controller, "ShowFastRewardsPopup");
        Canvas.ForceUpdateCanvases();
        RequireObject("Fast Rewards Popup", true);

        var currentStage = GetPrivateField<int>(controller, "enemyLevel");
        var startStage = (int)InvokePrivate(controller, "GetCampaignMapStartStage");
        var currentNodeIndex = Mathf.Clamp(currentStage - startStage, 0, 9);
        RequireButton($"Campaign Stage Node {currentNodeIndex + 1}").onClick.Invoke();
        Canvas.ForceUpdateCanvases();

        RequireObject("Campaign Stage Detail Popup", true);
        RequireInactive("Fast Rewards Popup", "Campaign Stage Detail should close Fast Rewards.");
        RequireInactive("Home Idle Info Popup", "Campaign Stage Detail should close Home Idle Info.");

        RequireButton("Stage Detail Close Button").onClick.Invoke();
        Canvas.ForceUpdateCanvases();
    }

    private static void ValidateCurrentStageDetailBattleFlow(IdlePrototypeController controller)
    {
        var currentStage = GetPrivateField<int>(controller, "enemyLevel");
        var startStage = (int)InvokePrivate(controller, "GetCampaignMapStartStage");
        var currentNodeIndex = Mathf.Clamp(currentStage - startStage, 0, 9);
        var currentNode = RequireButton($"Campaign Stage Node {currentNodeIndex + 1}");
        currentNode.onClick.Invoke();
        Canvas.ForceUpdateCanvases();

        var detailPopup = RequireObject("Campaign Stage Detail Popup", true);
        var battleButton = RequireButton("Stage Detail Battle Button");
        if (!battleButton.interactable)
        {
            throw new InvalidOperationException("Current stage detail Battle button should be interactable.");
        }

        battleButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (detailPopup.activeInHierarchy)
        {
            throw new InvalidOperationException("Stage detail popup should close when starting the current stage formation.");
        }

        var formationRoot = RequireObject("Campaign Formation Root", true);
        var formationHeader = RequireText(formationRoot, "Formation Header");
        RequireCopy(formationHeader.text, "Formation");
        AssertTextFits(formationHeader, "Formation Header");

        controller.ShowHome();
        Canvas.ForceUpdateCanvases();
    }

    private static object CreateWonCombatResult(object controller)
    {
        var resultType = controller.GetType().GetNestedType("CombatResult", BindingFlags.NonPublic);
        if (resultType == null)
        {
            throw new InvalidOperationException("Missing private CombatResult type.");
        }

        var result = Activator.CreateInstance(resultType);
        SetStructField(result, "won", true);
        SetStructField(result, "executed", true);
        SetStructField(result, "elapsedSeconds", 8);
        SetStructField(result, "teamHpRemaining", 1);
        SetStructField(result, "enemyHpRemaining", 0);
        SetStructField(result, "damageDealt", 100);
        SetStructField(result, "damageTaken", 1);
        return result;
    }

    private static void SetStructField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing struct field {fieldName}.");
        }

        field.SetValue(target, value);
    }

    private static GameObject RequireObject(string name, bool activeInHierarchy)
    {
        var gameObject = FindSceneObject(name);
        if (gameObject == null)
        {
            throw new InvalidOperationException($"Missing scene object: {name}");
        }

        if (activeInHierarchy && !gameObject.activeInHierarchy)
        {
            throw new InvalidOperationException($"{name} should be active.");
        }

        return gameObject;
    }

    private static void RequireInactive(string name, string context)
    {
        var gameObject = RequireObject(name, false);
        if (gameObject.activeInHierarchy)
        {
            throw new InvalidOperationException($"{context} {name} is still active.");
        }
    }

    private static Button RequireButton(string name)
    {
        var gameObject = RequireObject(name, true);
        var button = gameObject.GetComponent<Button>();
        if (button == null)
        {
            throw new InvalidOperationException($"{name} is missing a Button.");
        }

        return button;
    }

    private static TMP_Text RequireText(GameObject parent, string name)
    {
        var transform = parent.transform.Find(name);
        if (transform == null)
        {
            throw new InvalidOperationException($"{parent.name} is missing text child {name}.");
        }

        var text = transform.GetComponent<TMP_Text>();
        if (text == null)
        {
            throw new InvalidOperationException($"{name} is missing TMP_Text.");
        }

        return text;
    }

    private static GameObject RequireChildObject(GameObject parent, string name)
    {
        var transform = parent.transform.Find(name);
        if (transform == null)
        {
            throw new InvalidOperationException($"{parent.name} is missing child object {name}.");
        }

        return transform.gameObject;
    }

    private static RawImage RequireRawImageWithTexture(string name)
    {
        var gameObject = RequireObject(name, true);
        var image = gameObject.GetComponent<RawImage>();
        if (image == null)
        {
            throw new InvalidOperationException($"{name} is missing a RawImage.");
        }

        if (image.texture == null)
        {
            throw new InvalidOperationException($"{name} should have loaded texture art.");
        }

        return image;
    }

    private static GameObject FindSceneObject(string name)
    {
        foreach (var gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (gameObject.name == name && gameObject.scene.IsValid())
            {
                return gameObject;
            }
        }

        return null;
    }

    private static void AssertInsideParent(GameObject parent, GameObject child)
    {
        var parentRect = parent.GetComponent<RectTransform>();
        var childRect = child.GetComponent<RectTransform>();
        if (parentRect == null || childRect == null)
        {
            throw new InvalidOperationException($"{child.name} or {parent.name} is missing a RectTransform.");
        }

        var parentWidth = parentRect.rect.width;
        var parentHeight = parentRect.rect.height;
        var childWidth = childRect.rect.width;
        var childHeight = childRect.rect.height;
        var left = childRect.anchoredPosition.x - childWidth * childRect.pivot.x;
        var right = left + childWidth;
        var top = childRect.anchoredPosition.y + childHeight * (1f - childRect.pivot.y);
        var bottom = top - childHeight;

        if (left < -parentWidth * 0.5f || right > parentWidth * 0.5f || top > 0f || bottom < -parentHeight)
        {
            throw new InvalidOperationException($"{child.name} is outside {parent.name}: left={left}, right={right}, top={top}, bottom={bottom}.");
        }
    }

    private static void AssertDoesNotOverlap(GameObject first, GameObject second)
    {
        var firstRect = first.GetComponent<RectTransform>();
        var secondRect = second.GetComponent<RectTransform>();
        if (firstRect == null || secondRect == null)
        {
            throw new InvalidOperationException($"{first.name} or {second.name} is missing a RectTransform.");
        }

        var firstBounds = GetAnchoredBounds(firstRect);
        var secondBounds = GetAnchoredBounds(secondRect);
        var overlaps = firstBounds.left < secondBounds.right &&
            firstBounds.right > secondBounds.left &&
            firstBounds.bottom < secondBounds.top &&
            firstBounds.top > secondBounds.bottom;

        if (overlaps)
        {
            throw new InvalidOperationException($"{first.name} should not overlap {second.name}.");
        }
    }

    private static void AssertExtendsBehind(GameObject background, GameObject foreground)
    {
        var backgroundRect = background.GetComponent<RectTransform>();
        var foregroundRect = foreground.GetComponent<RectTransform>();
        if (backgroundRect == null || foregroundRect == null)
        {
            throw new InvalidOperationException($"{background.name} or {foreground.name} is missing a RectTransform.");
        }

        var backgroundBounds = GetAnchoredBounds(backgroundRect);
        var foregroundBounds = GetAnchoredBounds(foregroundRect);
        if (backgroundBounds.top >= foregroundBounds.top && backgroundBounds.bottom <= foregroundBounds.bottom)
        {
            return;
        }

        throw new InvalidOperationException($"{background.name} should extend behind {foreground.name}: background top={backgroundBounds.top}, bottom={backgroundBounds.bottom}; foreground top={foregroundBounds.top}, bottom={foregroundBounds.bottom}.");
    }

    private static void AssertConnectedBelow(GameObject upper, GameObject lower)
    {
        var upperRect = upper.GetComponent<RectTransform>();
        var lowerRect = lower.GetComponent<RectTransform>();
        if (upperRect == null || lowerRect == null)
        {
            throw new InvalidOperationException($"{upper.name} or {lower.name} is missing a RectTransform.");
        }

        var upperBounds = GetAnchoredBounds(upperRect);
        var lowerBounds = GetAnchoredBounds(lowerRect);
        if (Mathf.Abs(lowerBounds.top - upperBounds.bottom) <= 2f)
        {
            return;
        }

        throw new InvalidOperationException($"{lower.name} should connect to {upper.name}: lower top={lowerBounds.top}, upper bottom={upperBounds.bottom}.");
    }

    private static void AssertSameWidth(GameObject first, GameObject second)
    {
        var firstRect = first.GetComponent<RectTransform>();
        var secondRect = second.GetComponent<RectTransform>();
        if (firstRect == null || secondRect == null)
        {
            throw new InvalidOperationException($"{first.name} or {second.name} is missing a RectTransform.");
        }

        if (Mathf.Abs(firstRect.rect.width - secondRect.rect.width) <= 2f)
        {
            return;
        }

        throw new InvalidOperationException($"{second.name} should match {first.name} width: first={firstRect.rect.width}, second={secondRect.rect.width}.");
    }

    private static void AssertExtendsBelow(GameObject background, GameObject foreground)
    {
        var backgroundRect = background.GetComponent<RectTransform>();
        var foregroundRect = foreground.GetComponent<RectTransform>();
        if (backgroundRect == null || foregroundRect == null)
        {
            throw new InvalidOperationException($"{background.name} or {foreground.name} is missing a RectTransform.");
        }

        var backgroundBounds = GetAnchoredBounds(backgroundRect);
        var foregroundBounds = GetAnchoredBounds(foregroundRect);
        if (backgroundBounds.bottom < foregroundBounds.bottom)
        {
            return;
        }

        throw new InvalidOperationException($"{background.name} should extend below {foreground.name}: background bottom={backgroundBounds.bottom}, foreground bottom={foregroundBounds.bottom}.");
    }

    private static (float left, float right, float top, float bottom) GetAnchoredBounds(RectTransform rectTransform)
    {
        var rect = rectTransform.rect;
        var left = rectTransform.anchoredPosition.x - rect.width * rectTransform.pivot.x;
        var right = left + rect.width;
        var top = rectTransform.anchoredPosition.y + rect.height * (1f - rectTransform.pivot.y);
        var bottom = top - rect.height;
        return (left, right, top, bottom);
    }

    private static void AssertTextFits(TMP_Text text, string label)
    {
        var rect = text.rectTransform.rect;
        var preferred = text.GetPreferredValues(text.text, rect.width, 0f);
        if (preferred.y > rect.height + 4f)
        {
            throw new InvalidOperationException($"{label} does not fit: preferred height={preferred.y}, rect height={rect.height}, text='{text.text}'.");
        }
    }

    private static void AssertTextureNameContains(RawImage image, string expectedTextureName, string context)
    {
        if (image == null || image.texture == null || !image.texture.name.Contains(expectedTextureName))
        {
            throw new InvalidOperationException($"{context} should use {expectedTextureName}, got '{image?.texture?.name}'.");
        }
    }

    private static Rect GetExpectedCampaignStageDetailMapUvRect(int stageNumber)
    {
        var stageProgress = GetExpectedHomeProgressMapStageProgress(stageNumber);
        return new Rect(0.08f, Mathf.Lerp(0.58f, 0.2f, stageProgress), 0.84f, 0.26f);
    }

    private static Rect GetExpectedHomeIdleCombatMapUvRect(int stageNumber)
    {
        var stageProgress = GetExpectedHomeProgressMapStageProgress(stageNumber);
        return new Rect(0.04f, Mathf.Lerp(0.38f, 0.12f, stageProgress), 0.92f, 0.18f);
    }

    private static float GetExpectedHomeProgressMapStageProgress(int stageNumber)
    {
        stageNumber = Mathf.Max(1, stageNumber);
        return ((stageNumber - 1) % 10) / 9f;
    }

    private static void AssertUvApproximately(Rect actual, Rect expected, string context)
    {
        AssertApproximately(actual.x, expected.x, 0.001f, $"{context} x");
        AssertApproximately(actual.y, expected.y, 0.001f, $"{context} y");
        AssertApproximately(actual.width, expected.width, 0.001f, $"{context} width");
        AssertApproximately(actual.height, expected.height, 0.001f, $"{context} height");
    }

    private static void AssertApproximately(float actual, float expected, float tolerance, string context)
    {
        if (Mathf.Abs(actual - expected) <= tolerance)
        {
            return;
        }

        throw new InvalidOperationException($"{context} expected {expected}, got {actual}.");
    }

    private static void RequireCopy(string text, string expected)
    {
        if (string.IsNullOrWhiteSpace(text) || text.IndexOf(expected, StringComparison.Ordinal) < 0)
        {
            throw new InvalidOperationException($"Expected copy '{expected}' in '{text}'.");
        }
    }

    private static void RequireLineBreak(string text, string context)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('\n') < 0)
        {
            throw new InvalidOperationException($"{context} should split the last and next reward summaries onto separate lines, got '{text}'.");
        }
    }

    private static object InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException($"Missing private method {methodName}.");
        }

        return method.Invoke(target, args);
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field {fieldName}.");
        }

        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field {fieldName}.");
        }

        return (T)field.GetValue(target);
    }
}
