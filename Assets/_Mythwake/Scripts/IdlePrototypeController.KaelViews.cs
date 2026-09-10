using UnityEngine;

public partial class IdlePrototypeController
{
    private KaelAnimationView kaelFightView;
    private KaelAnimationView kaelFormationView;
    private KaelAnimationView kaelHomeView;
    private KaelCombatVfx kaelFightVfx;
    private KaelUltimateBackdrop kaelUltimateBackdrop;
    // Set only by the isolated acceptance harness to inspect actual animation without presentation effects.
    private bool kaelAnimationReviewOnly;
    private float kaelCombatFacing = 1;
    private long kaelFocusLayerSequence = -1;

    private void EnsureKaelCombatView()
    {
        if (kaelFightView != null) return;
        kaelUltimateBackdrop = KaelUltimateBackdrop.Create(fightRoot);
        kaelFightView = KaelAnimationView.Create(fightRoot, "Kael Unity 2D Combat", 132);
        kaelFightVfx = KaelCombatVfx.Create(fightRoot);
        kaelFightView.Marker += kaelFightVfx.AnimationMarker;
        if (fightFloatingTexts != null)
            foreach (var text in fightFloatingTexts) if (text != null) text.transform.SetAsLastSibling();
        KeepKaelFocusControlsVisible();
    }

    private void KeepKaelFocusControlsVisible()
    {
        if (fightVsText != null) fightVsText.transform.SetAsLastSibling();
        if (fightTimerText != null) fightTimerText.transform.SetAsLastSibling();
        if (fightBossHpFill != null) fightBossHpFill.transform.parent.SetAsLastSibling();
        if (fightEndButton != null) fightEndButton.transform.SetAsLastSibling();
        if (fightAutoSkillButton != null) fightAutoSkillButton.transform.SetAsLastSibling();
        if (fightSpeedButton != null) fightSpeedButton.transform.SetAsLastSibling();
    }

    partial void PresentKaelCombatFrame(int heroIndex, Vector2 position, Vector2 targetPosition, string state,
        long actionSequence, float actionAgeSeconds, float deltaSeconds, bool alive)
    {
        if (heroIndex != KaelHeroIndex) return;
        EnsureKaelCombatView();
        kaelFightVfx.gameObject.SetActive(true);
        kaelFightVfx.enabled = !kaelAnimationReviewOnly;
        if (fightHeroImages != null && fightHeroImages[heroIndex] != null) fightHeroImages[heroIndex].gameObject.SetActive(false);
        var facing = Mathf.Abs(targetPosition.x - position.x) > 2 ? Mathf.Sign(targetPosition.x - position.x) : 1;
        kaelCombatFacing = facing;
        kaelFightView.Present(position, alive ? state : "death", actionSequence, actionAgeSeconds, deltaSeconds, facing);
        var legacySkill = state == "skill_legacy";
        var skillDuration = legacySkill ? 1f : 1.32f;
        var focused = !kaelAnimationReviewOnly && alive && (state == "skill" || legacySkill) &&
            actionSequence == KaelUltimateFocusSequence && actionAgeSeconds < skillDuration;
        var emphasis = 0f;
        if (focused)
        {
            if (kaelFocusLayerSequence != actionSequence)
            {
                kaelFocusLayerSequence = actionSequence;
                // Previous numbers stay with the frozen, darkened arena. The real
                // resolved hit promotes its fresh damage text through the usual path.
                if (fightFloatingTexts != null)
                    foreach (var text in fightFloatingTexts)
                        if (text != null && text.transform.GetSiblingIndex() > kaelUltimateBackdrop.transform.GetSiblingIndex())
                            text.transform.SetSiblingIndex(kaelUltimateBackdrop.transform.GetSiblingIndex());
                KeepKaelFocusControlsVisible();
            }
            var progress = Mathf.Clamp01(KaelUltimateFocusAge / KaelUltimateFocusDuration);
            emphasis = IsKaelUltimateFocusActive ? Mathf.SmoothStep(0, 1, Mathf.Clamp01(progress / .22f))
                : 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(legacySkill ? .70f : 1.02f, skillDuration, actionAgeSeconds));
            // Focus the actual actor, without teleporting it away from its target.
            var focusFoot = position + Vector2.down * 132;
            kaelFightView.SetCinematicProjection(focusFoot, emphasis);
            var backdropProgress = IsKaelUltimateFocusActive ? progress * .72f
                : Mathf.Lerp(.72f, 1, Mathf.InverseLerp(legacySkill ? .40f : .92f, skillDuration, actionAgeSeconds));
            kaelUltimateBackdrop.Present(backdropProgress, focusFoot, facing, GetLocalizedHeroAbilityName(KaelHeroIndex));
        }
        else kaelUltimateBackdrop.Hide();
        kaelFightVfx.SetFrame(kaelFightView, alive ? (legacySkill ? "skill" : state) : "death", actionSequence, actionAgeSeconds, combatClockSeconds, facing, emphasis);
    }
    partial void PresentKaelImpact(int heroIndex, Vector2 targetPosition, bool skill, long sequence, int contactIndex, bool finalContact)
    {
        if (heroIndex != KaelHeroIndex) return;
        EnsureKaelCombatView();
        kaelFightView.ResolvedImpact(sequence, contactIndex);
        var variant = combatLegacyAnimations[heroIndex] ? (skill ? "skill_legacy" : "attack") : combatActionKinds[heroIndex];
        kaelFightVfx.ResolvedImpact(targetPosition, skill, sequence, contactIndex, finalContact, combatClockSeconds, kaelCombatFacing, variant);
    }
    partial void ResetKaelCombatViews()
    {
        kaelFocusLayerSequence = -1;
        if (kaelFightView != null) kaelFightView.ResetView();
        if (kaelFightVfx != null) { kaelFightVfx.ResetEffects(); kaelFightVfx.gameObject.SetActive(false); }
        if (kaelUltimateBackdrop != null) kaelUltimateBackdrop.Hide();
    }

    private void LateUpdate()
    {
        var delta = combatApplicationPaused ? 0 : Time.deltaTime;
        if (formationHeroImages != null)
        {
            var formationVisible = false;
            for (var slot = 0; slot < formationHeroImages.Length; slot++)
            {
                var source = formationHeroImages[slot];
                if (source == null) continue;
                var show = source.transform.parent.gameObject.activeInHierarchy && formationSlotHeroIndices != null && slot < formationSlotHeroIndices.Length && formationSlotHeroIndices[slot] == KaelHeroIndex;
                if (show)
                {
                    source.enabled = false;
                    formationVisible = true;
                    if (kaelFormationView == null) kaelFormationView = KaelAnimationView.Create(source.transform.parent, "Kael Unity 2D Formation", 108);
                    kaelFormationView.Present(source.rectTransform.anchoredPosition, "idle", 0, 0, delta);
                }
                else
                {
                    source.enabled = true;
                }
            }
            if (!formationVisible && kaelFormationView != null) kaelFormationView.gameObject.SetActive(false);
        }
        if (homeIdleHeroImages != null)
        {
            var homeVisible = false;
            var positions = GetHomeIdleHeroPositions();
            for (var slot = 0; slot < homeIdleHeroImages.Length; slot++)
            {
                var source = homeIdleHeroImages[slot];
                if (source == null) continue;
                var show = !accountStartVisible && source.transform.parent.gameObject.activeInHierarchy && GetHomeIdleHeroIndex(slot) == KaelHeroIndex;
                if (show)
                {
                    source.enabled = false;
                    homeVisible = true;
                    if (kaelHomeView == null) kaelHomeView = KaelAnimationView.Create(source.transform.parent, "Kael Unity 2D Home", 118);
                    var cycle = Mathf.FloorToInt(homeIdleCombatTimer / 1.8f);
                    var age = homeIdleCombatTimer - cycle * 1.8f;
                    var basic = cycle % 3 == 0 ? "attack_cross" : cycle % 3 == 1 ? "attack_spin" : "attack_jump";
                    kaelHomeView.Present(positions[slot], age < .84f ? basic : "idle", cycle, age, delta);
                }
                else
                {
                    source.enabled = true;
                }
            }
            if (!homeVisible && kaelHomeView != null) kaelHomeView.gameObject.SetActive(false);
        }
    }
}
