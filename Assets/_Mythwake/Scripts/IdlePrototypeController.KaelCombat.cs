using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class IdlePrototypeController
{
    private MythwakeCombatSession activeLocalCombat;
    private bool authoritativeFightActive, authoritativeServerReplay, combatApplicationPaused;
    private bool[] combatRosterActive;
    private bool[] combatLegacyAnimations;
    private int[] combatCooldownUntilMs;
    private int[] combatParticipantOrder;
    private FightVisualUnitState[] combatHeroStates, combatEnemyStates;
    private float[] combatHeroHp, combatEnemyHp, combatHitUntil, combatDeathStarted;
    private float[] combatActionDurations;
    private string[] combatActionIds, combatActionKinds;
    private long[] combatActionSequences;
    private readonly HashSet<string> consumedCombatRecords = new HashSet<string>();
    private int combatFloatingIndex, combatEventCursor, combatEnemyCount;
    private float combatClockSeconds, combatDisplayEnemyMaxHp, combatDisplayTeamMaxHp;
    private float combatFloatingUntil;
    private bool combatSingleBoss;
    private CombatResult completedLocalCombatResult;
    private ShardRiftRunResult completedLocalRiftResult;
    private readonly KaelUltimateFocusClock kaelUltimateFocusClock = new KaelUltimateFocusClock();
    private const float KaelUltimateFocusDuration = KaelUltimateFocusClock.Duration;
    private bool IsKaelUltimateFocusActive => kaelUltimateFocusClock.IsActive;
    private float KaelUltimateFocusAge => kaelUltimateFocusClock.Age;
    private float KaelUltimateFocusFrameDelta => kaelUltimateFocusClock.FrameDelta;
    private long KaelUltimateFocusSequence => kaelUltimateFocusClock.Sequence;
    private bool IsKaelUltimateImpactHeld => kaelUltimateFocusClock.IsImpactHoldActive;

    partial void PresentKaelCombatFrame(int heroIndex, Vector2 position, Vector2 targetPosition, string state,
        long actionSequence, float actionAgeSeconds, float deltaSeconds, bool alive);
    partial void PresentKaelImpact(int heroIndex, Vector2 targetPosition, bool skill, long sequence, int contactIndex, bool finalContact);
    partial void ResetKaelCombatViews();

    private MythwakeCombatSession CreateLocalCombatSession(int enemyHp, int enemyDamage, int seed)
    {
        var heroes = new List<MythwakeCombatSession.Hero>();
        foreach (var i in GetActiveFormationHeroIndices())
        {
            if (i < 0 || i >= HeroCount) continue;
            var kael = GetHeroTextureName(i) == "hero_kael";
            heroes.Add(new MythwakeCombatSession.Hero {
                id = GetHeroTextureName(i), rosterIndex = i, maxHp = GetHeroCombatMaxHealth(i),
                attack = GetHeroEffectiveAttack(i), accuracy = GetHeroAccuracyPercent(i),
                criticalChance = GetHeroCritChancePercent(i), incomingDamage = GetMitigatedEnemyDamageAgainstHero(enemyDamage, i),
                maxMana = kael ? 26 : GetHeroMaxMana(i), manaGain = GetHeroAutoAttackManaGain(i),
                legacyAttackIntervalJitter = !kael,
                attackIntervalMs = kael ? 1120 : Mathf.RoundToInt(GetFightVisualAttackInterval(true, i) * 1000),
                attackImpactMs = kael ? 240 : 200, attackDurationMs = kael ? MythwakeCombatSession.KaelAttackDurationMs : 600,
                skillImpactMs = 400, skillDurationMs = kael ? MythwakeCombatSession.KaelSkillDurationMs : 1000,
                skillMultiplier = kael ? 4f : GetHeroUltimateDamageMultiplier(i), cooldownMs = 4500
            });
        }
        if (heroes.Count > 0)
        {
            heroes[0].maxHp += GetVillageTeamHealthBonus();
            heroes[0].attack += GetVillageTeamAttackBonus();
        }
        return new MythwakeCombatSession(heroes, enemyHp, DefaultCombatDurationSeconds * 1000, seed) { ExecuteEnemy = ShouldExecuteEnemy };
    }

    private static CombatResult ToCombatResult(MythwakeCombatSession session)
    {
        return new CombatResult { won = session.EnemyHp <= 0 && session.TeamHp > 0, executed = session.Executed,
            elapsedSeconds = Mathf.Max(1, Mathf.CeilToInt(session.TimeMs / 1000f)),
            teamHpRemaining = session.TeamHp, enemyHpRemaining = session.EnemyHp,
            damageDealt = session.DamageDealt, damageTaken = session.DamageTaken,
            healingDone = session.HealingDone, criticalHits = session.CriticalHits, missedHits = session.MissedHits };
    }

    private ShardRiftRunResult ToShardRiftResult(MythwakeCombatSession session)
    {
        var result = ToCombatResult(session);
        result.won = session.EnemiesDefeated > 0 && session.TeamHp > 0;
        var shards = 0;
        for (var i = 1; i <= session.EnemiesDefeated; i++) shards += GetShardRiftAwakeningShardReward(i);
        return new ShardRiftRunResult { combat = result, enemiesDefeated = session.EnemiesDefeated,
            awakeningShards = shards, heroShardChests = GetShardRiftChestReward(session.EnemiesDefeated), elapsedSeconds = result.elapsedSeconds };
    }

    private void BeginAuthoritativePresentation(int stage, string label, bool singleBoss, string bossTexture,
        int teamMaxHp, int enemyMaxHp, bool serverReplay, string[] participantIds = null)
    {
        CancelAuthoritativeCombat();
        authoritativeFightActive = true;
        authoritativeServerReplay = serverReplay;
        fightCancelRequested = false;
        combatSingleBoss = singleBoss;
        combatClockSeconds = 0f;
        combatFloatingIndex = combatEventCursor = 0;
        combatFloatingUntil = 0f;
        combatDisplayTeamMaxHp = Mathf.Max(1, teamMaxHp);
        combatDisplayEnemyMaxHp = Mathf.Max(1, enemyMaxHp);
        combatEnemyCount = singleBoss ? 1 : FormationCapacity;
        consumedCombatRecords.Clear();
        combatRosterActive = new bool[HeroCount];
        combatLegacyAnimations = new bool[HeroCount];
        combatCooldownUntilMs = new int[HeroCount];
        combatHeroHp = new float[HeroCount];
        combatEnemyHp = CreateCombatHealthPercents(combatEnemyCount);
        combatHitUntil = new float[HeroCount];
        combatDeathStarted = new float[HeroCount];
        combatActionIds = new string[HeroCount];
        combatActionKinds = new string[HeroCount];
        combatActionDurations = new float[HeroCount];
        combatActionSequences = new long[HeroCount];
        combatHeroStates = new FightVisualUnitState[HeroCount];
        combatEnemyStates = new FightVisualUnitState[combatEnemyCount];
        var positions = GetFightHeroPositions();
        var enemyPositions = singleBoss ? GetFightBossEnemyPositions() : GetFightEnemyPositions();
        for (var i = 0; i < combatEnemyCount; i++)
            combatEnemyStates[i] = new FightVisualUnitState { position = enemyPositions[Mathf.Min(i, enemyPositions.Length - 1)], attackStartedAt = -99f, targetIndex = -1 };
        var ids = participantIds ?? GetActiveFormationHeroIds();
        combatParticipantOrder = Array.ConvertAll(ids, FindCombatHeroIndex);
        for (var slot = 0; slot < ids.Length && slot < FormationCapacity; slot++)
        {
            var i = FindCombatHeroIndex(ids[slot]);
            if (i < 0 || combatRosterActive[i]) continue;
            combatRosterActive[i] = true;
            combatHeroHp[i] = 1f;
            combatHeroStates[i] = new FightVisualUnitState { position = positions[slot], attackStartedAt = -99f,
                targetIndex = slot % combatEnemyCount, lockedMeleeTargetIndex = -1 };
            combatActionKinds[i] = "idle";
            combatDeathStarted[i] = -1f;
        }
        SetBattleFlowMode(BattleFlowMode.Fight);
        ApplyBattleFlowVisibility();
        HideFightFloatingTexts();
        RefreshFightArenaBackground(singleBoss);
        PrepareFightAnimationTextures(stage, singleBoss, bossTexture);
        ConfigureFightEnemyPresentation(singleBoss);
        InitializeFightSkillState();
        RefreshFightSkillHealthUi(combatHeroHp);
        if (fightVsText != null) fightVsText.text = label;
        if (fightResultRoot != null) fightResultRoot.gameObject.SetActive(false);
    }

    private int FindCombatHeroIndex(string heroId)
    {
        for (var i = 0; i < HeroCount; i++) if (GetHeroTextureName(i) == heroId) return i;
        return -1;
    }

    private bool IsAuthoritativeHeroActive(int index)
    {
        return combatRosterActive != null && index >= 0 && index < combatRosterActive.Length && combatRosterActive[index];
    }

    private float GetAuthoritativeDelta()
    {
        return combatApplicationPaused ? 0f : Mathf.Min(Time.unscaledDeltaTime, 0.1f) * GetFightTimeScale();
    }

    private IEnumerator PlayLocalAuthoritativeCombat(int stage, string label, int enemyHp, int enemyDamage,
        bool singleBoss = false, string bossTexture = null, bool endlessRift = false)
    {
        var session = CreateLocalCombatSession(enemyHp, enemyDamage, UnityEngine.Random.Range(1, int.MaxValue));
        BeginAuthoritativePresentation(stage, label, singleBoss, bossTexture, session.TeamMaxHp, enemyHp, false);
        activeLocalCombat = session;
        if (endlessRift)
        {
            session.NextWaveHealth = GetShardRiftEnemyHp;
            session.NextWaveIncomingDamage = (wave, hero) => GetMitigatedEnemyDamageAgainstHero(GetShardRiftEnemyDamage(wave), hero.rosterIndex);
        }
        session.CanAct = i => IsFightVisualInRange(combatHeroStates[i], combatEnemyStates[combatHeroStates[i].targetIndex].position, true, i);
        try
        {
            while (!fightCancelRequested && !session.Finished)
            {
                var delta = kaelUltimateFocusClock.Consume(GetAuthoritativeDelta());
                MoveAuthoritativeHeroes(delta);
                session.AutoSkills = fightAutoSkillsEnabled;
                session.Step(delta);
                combatClockSeconds = session.TimeMs / 1000f;
                while (combatEventCursor < session.Events.Count)
                {
                    var index = combatEventCursor++;
                    ConsumeAuthoritativeRecord(session.Events[index], index, false);
                }
                foreach (var h in session.Heroes)
                {
                    combatHeroHp[h.rosterIndex] = h.hp / (float)Mathf.Max(1, h.maxHp);
                    fightHeroManaValues[h.rosterIndex] = h.mana;
                    combatCooldownUntilMs[h.rosterIndex] = h.cooldownUntilMs;
                    if (h.mana == 0) fightHeroUltimateQueued[h.rosterIndex] = false;
                }
                combatDisplayEnemyMaxHp = session.EnemyMaxHp;
                SetAggregateEnemyHp(session.EnemyHp);
                PresentAuthoritativeCombat(delta, session.DamageDealt, session.DamageTaken);
                yield return null;
            }
            completedLocalCombatResult = ToCombatResult(session);
            completedLocalRiftResult = ToShardRiftResult(session);
            if (!fightCancelRequested) yield return HoldAuthoritativeEndPose(session.DamageDealt, session.DamageTaken);
        }
        finally { EndAuthoritativePresentation(); }
    }

    private IEnumerator PlayServerAuthoritativeCombat(MythwakeCombatResultDto result, string label, bool singleBoss, string bossTexture)
    {
        var ids = result.heroes == null ? GetActiveFormationHeroIds() : Array.ConvertAll(result.heroes, h => h.heroId);
        BeginAuthoritativePresentation(result.targetLevel, label, singleBoss, bossTexture, result.teamMaxHp, result.enemyMaxHp, true, ids);
        if (result.heroes != null)
            foreach (var hero in result.heroes)
            {
                var index = FindCombatHeroIndex(hero.heroId);
                if (index >= 0) fightHeroMaxManaValues[index] = Mathf.Max(1, hero.maxMana);
            }
        var replay = new MythwakeCombatReplay(result);
        var duration = Mathf.Max(1f, Mathf.Max(result.elapsedSeconds, replay.LastEventTimeMs / 1000f));
        try
        {
            // The backend records begin at contact. Approach uses presentation time before replay starts.
            var approach = 0f;
            while (!fightCancelRequested && approach < 2f && HasApproachingCombatHero())
            {
                var delta = GetAuthoritativeDelta(); approach += delta;
                MoveAuthoritativeHeroes(delta);
                PresentAuthoritativeCombat(delta, 0, 0);
                yield return null;
            }
            while (!fightCancelRequested && (combatClockSeconds < duration || !replay.Finished))
            {
                var delta = kaelUltimateFocusClock.Consume(GetAuthoritativeDelta());
                combatClockSeconds = Mathf.Min(duration, combatClockSeconds + delta);
                MoveAuthoritativeHeroes(delta);
                replay.AdvanceTo(Mathf.RoundToInt(combatClockSeconds * 1000f), (record, index) => ConsumeAuthoritativeRecord(record, index, true));
                PresentAuthoritativeCombat(delta, replay.DamageDealt, replay.DamageTaken);
                yield return null;
            }
            if (!fightCancelRequested)
            {
                SetAggregateServerHp(result.teamHpRemaining);
                SetAggregateEnemyHp(result.enemyHpRemaining);
                PresentAuthoritativeCombat(0f, result.damageDealt, result.damageTaken);
                yield return HoldAuthoritativeEndPose(result.damageDealt, result.damageTaken);
            }
        }
        finally { EndAuthoritativePresentation(); }
    }

    private bool HasApproachingCombatHero()
    {
        for (var i = 0; i < HeroCount; i++)
            if (IsAuthoritativeHeroActive(i) && !IsHeroRangedCombatant(i) &&
                !IsFightVisualInRange(combatHeroStates[i], combatEnemyStates[combatHeroStates[i].targetIndex].position, true, i)) return true;
        return false;
    }

    private void MoveAuthoritativeHeroes(float delta)
    {
        for (var i = 0; i < HeroCount; i++)
        {
            if (!IsAuthoritativeHeroActive(i) || combatHeroHp[i] <= 0f) continue;
            var state = combatHeroStates[i];
            var age = combatClockSeconds - state.attackStartedAt;
            if (age >= 0 && age < combatActionDurations[i]) { state.isMoving = false; combatHeroStates[i] = state; continue; }
            if (IsHeroRangedCombatant(i)) { state.isMoving = false; combatHeroStates[i] = state; continue; }
            var target = combatEnemyStates[state.targetIndex].position;
            state.lockedMeleePosition = GetMeleeContactPosition(state.position, target, true, i, state.targetIndex, false);
            state.lockedMeleeTargetIndex = state.targetIndex;
            state.hasLockedMeleePosition = true;
            state.isMoving = Vector2.Distance(state.position, state.lockedMeleePosition) > 5f;
            state.position = Vector2.MoveTowards(state.position, state.lockedMeleePosition, GetHeroVisualMoveSpeed(i) * delta);
            combatHeroStates[i] = state;
        }
    }

    private bool ConsumeAuthoritativeRecord(MythwakeCombatEventDto record, int index, bool server)
    {
        var key = MythwakeCombatReplay.RecordKey(record, index);
        if (!consumedCombatRecords.Add(key)) return false;
        var actor = FindCombatHeroIndex(record.actorId);
        var damage = record.eventType == "auto_attack" || record.eventType == "critical_attack" || record.eventType == "ultimate";
        var action = record.eventType == "action_start" || damage || record.eventType == "miss";
        if (actor >= 0 && IsAuthoritativeHeroActive(actor))
        {
            fightHeroManaValues[actor] = Mathf.Clamp(record.manaAfter, 0, fightHeroMaxManaValues[actor]);
            combatCooldownUntilMs[actor] = Mathf.Max(combatCooldownUntilMs[actor], record.cooldownUntilMs);
            if (action && combatHeroHp[actor] > 0f)
            {
                var actionId = string.IsNullOrEmpty(record.actionId) ? key : record.actionId;
                if (combatActionIds[actor] != actionId)
                {
                    combatActionIds[actor] = actionId;
                    combatActionSequences[actor]++;
                    combatLegacyAnimations[actor] = string.IsNullOrEmpty(record.animationVariant);
                    combatActionKinds[actor] = record.eventType == "ultimate" || !string.IsNullOrEmpty(record.skillId) ? "skill" :
                        string.IsNullOrEmpty(record.animationVariant) ? "attack" : record.animationVariant;
                    combatActionDurations[actor] = record.actionDurationMs > 0 ? record.actionDurationMs / 1000f :
                        combatActionKinds[actor] == "skill" ? 1f : .6f;
                    var state = combatHeroStates[actor];
                    state.attackStartedAt = (record.actionStartMs > 0 ? record.actionStartMs : record.timeMs) / 1000f;
                    state.attackSequence++;
                    state.isMoving = false;
                    combatHeroStates[actor] = state;
                    if (combatActionKinds[actor] == "skill") { fightHeroUltimateStartedAt[actor] = state.attackStartedAt; fightHeroUltimateQueued[actor] = false; }
                    if (record.eventType == "action_start" && combatActionKinds[actor] == "skill" && GetHeroTextureName(actor) == "hero_kael")
                        kaelUltimateFocusClock.Begin(combatActionSequences[actor], Mathf.Max(0f, combatClockSeconds - state.attackStartedAt));
                }
                var target = combatEnemyStates[combatHeroStates[actor].targetIndex].position + new Vector2(0, -72);
                if (damage)
                {
                    ShowAuthoritativeNumber(record.amount, target, record.eventType == "critical_attack" ? new Color(1f, .5f, .2f) : new Color(1f, .85f, .3f));
                    if (GetHeroTextureName(actor) == "hero_kael")
                    {
                        var finalContact = MythwakeCombatReplay.IsFinalContact(record);
                        if (record.eventType == "ultimate" && finalContact) kaelUltimateFocusClock.HoldResolvedImpact(combatActionSequences[actor]);
                        PresentKaelImpact(actor, target, record.eventType == "ultimate", combatActionSequences[actor],
                            Mathf.Max(0, record.contactIndex), finalContact);
                    }
                }
                else if (record.eventType == "miss") ShowAuthoritativeText("MISS", target, Color.white);
            }
        }
        if (record.eventType == "enemy_attack")
        {
            var target = FindCombatHeroIndex(record.targetId);
            if (target >= 0 && IsAuthoritativeHeroActive(target))
            {
                combatHitUntil[target] = combatClockSeconds + .18f;
                ShowAuthoritativeNumber(record.amount, combatHeroStates[target].position + new Vector2(0, -72), new Color(1f, .3f, .25f));
                combatEnemyStates[0].attackStartedAt = record.timeMs / 1000f;
                combatEnemyStates[0].targetIndex = target;
            }
        }
        if (record.eventType == "enemy_spawn") combatDisplayEnemyMaxHp = Mathf.Max(1, record.amount);
        if (record.eventType == "execute")
            ShowAuthoritativeText("Execute -" + FormatCompactNumber(record.amount), combatEnemyStates[0].position + new Vector2(0, -72), new Color(1f, .6f, .2f));
        if (server) SetAggregateServerHp(record.teamHpRemaining);
        SetAggregateEnemyHp(record.enemyHpRemaining);
        return true;
    }

    private void SetAggregateServerHp(int hp)
    {
        // The service owns one shared team HP pool; it does not report individual deaths.
        var fraction = Mathf.Clamp01(hp / combatDisplayTeamMaxHp);
        for (var i = 0; i < HeroCount; i++) combatHeroHp[i] = IsAuthoritativeHeroActive(i) ? fraction : 0f;
    }

    private void SetAggregateEnemyHp(int hp)
    {
        var fraction = Mathf.Clamp01(hp / combatDisplayEnemyMaxHp);
        for (var i = 0; i < combatEnemyHp.Length; i++) combatEnemyHp[i] = fraction;
    }

    private void ShowAuthoritativeNumber(int amount, Vector2 target, Color color)
    {
        if (amount > 0) ShowAuthoritativeText("-" + FormatCompactNumber(amount), target, color);
    }

    private void ShowAuthoritativeText(string text, Vector2 target, Color color)
    {
        // Animation review hides presentation only; authoritative events and HP still resolve normally.
        if (kaelAnimationReviewOnly) return;
        ShowFightFloatingText(combatFloatingIndex++, text, target, color);
        combatFloatingUntil = combatClockSeconds + .55f;
    }

    private void PresentAuthoritativeCombat(float delta, int dealt, int taken)
    {
        if (kaelUltimateFocusClock.Sequence >= 0)
        {
            var kael = FindCombatHeroIndex("hero_kael");
            if (!IsAuthoritativeHeroActive(kael) || combatHeroHp[kael] <= 0f) kaelUltimateFocusClock.Cancel();
        }
        var legacySkill = -1;
        for (var i = 0; i < HeroCount; i++)
            if (IsAuthoritativeHeroActive(i) && combatHeroHp[i] > 0 && GetHeroTextureName(i) != "hero_kael" &&
                combatClockSeconds - fightHeroUltimateStartedAt[i] >= 0f && combatClockSeconds - fightHeroUltimateStartedAt[i] < .9f) { legacySkill = i; break; }
        var freezeSurroundings = (IsKaelUltimateFocusActive && KaelUltimateFocusAge > 0f) ||
            ((KaelUltimateFocusFrameDelta > 0f || kaelUltimateFocusClock.ImpactHoldFrameDelta > 0f) && delta <= 0f);
        if (!freezeSurroundings)
            AnimateFightUnitsWithState(combatHeroStates, combatEnemyStates, combatHeroHp, combatEnemyHp, combatEnemyCount,
                combatClockSeconds, combatClockSeconds, combatSingleBoss, legacySkill,
                legacySkill >= 0 ? .9f - (combatClockSeconds - fightHeroUltimateStartedAt[legacySkill]) : 0f);
        for (var i = 0; i < HeroCount; i++)
        {
            if (!IsAuthoritativeHeroActive(i) || GetHeroTextureName(i) != "hero_kael") continue;
            var alive = combatHeroHp[i] > 0f;
            var state = combatHeroStates[i];
            var age = combatClockSeconds - state.attackStartedAt;
            var kind = combatActionKinds[i];
            if (!alive)
            {
                if (combatDeathStarted[i] < 0f) { combatDeathStarted[i] = combatClockSeconds; combatActionSequences[i]++; }
                kind = "death"; age = combatClockSeconds - combatDeathStarted[i];
            }
            else if (age < 0 || age >= combatActionDurations[i])
            {
                if (combatHitUntil[i] > combatClockSeconds) { kind = "hit"; age = .18f - (combatHitUntil[i] - combatClockSeconds); }
                else { kind = state.isMoving ? "run" : "idle"; age = combatClockSeconds; }
            }
            if (kind == "skill") age = kaelUltimateFocusClock.MapActionAge(combatActionSequences[i], age);
            var visualKind = kind == "skill" && combatLegacyAnimations[i] ? "skill_legacy" : kind;
            PresentKaelCombatFrame(i, state.position, combatEnemyStates[state.targetIndex].position + new Vector2(0, -72),
                visualKind, combatActionSequences[i], Mathf.Max(0, age), delta + KaelUltimateFocusFrameDelta, alive);
        }
        SetFillValues(fightHeroHpFills, combatHeroHp, HeroCount);
        SetFillValues(fightEnemyHpFills, combatEnemyHp, combatEnemyCount);
        SetHpPercentTexts(fightEnemyHpPercentTexts, combatEnemyHp, combatEnemyCount);
        RefreshFightBossHpUi(combatSingleBoss, combatEnemyHp);
        RefreshFightSkillHealthUi(combatHeroHp);
        RefreshFightSkillUi(combatClockSeconds);
        if (combatClockSeconds > combatFloatingUntil) HideFightFloatingTexts();
        if (fightTimerText != null) fightTimerText.text = FormatFightTimer(Mathf.Max(0, DefaultCombatDurationSeconds - Mathf.FloorToInt(combatClockSeconds)));
        if (fightStatusText != null) fightStatusText.text = (authoritativeServerReplay ? "Server Replay · " : "") + $"Dealt {FormatCompactNumber(dealt)}   Took {FormatCompactNumber(taken)}";
    }

    private IEnumerator HoldAuthoritativeEndPose(int dealt, int taken)
    {
        kaelUltimateFocusClock.BeginEndPose();
        var elapsed = 0f;
        while (!fightCancelRequested && elapsed < 1f)
        {
            var delta = kaelUltimateFocusClock.Consume(GetAuthoritativeDelta()); elapsed += delta; combatClockSeconds += delta;
            PresentAuthoritativeCombat(delta, dealt, taken);
            yield return null;
        }
    }

    private void EndAuthoritativePresentation()
    {
        activeLocalCombat?.Cancel();
        activeLocalCombat = null;
        kaelUltimateFocusClock.Reset();
        ResetKaelCombatViews();
        SetProjectilesVisible(fightHeroProjectileImages, false);
        SetProjectilesVisible(fightEnemyProjectileImages, false);
        SetRawImagesVisible(fightHeroFxImages, false);
        HideFightFloatingTexts();
        authoritativeFightActive = false;
        authoritativeServerReplay = false;
        combatParticipantOrder = null;
    }

    private void CancelAuthoritativeCombat()
    {
        activeLocalCombat?.Cancel();
        EndAuthoritativePresentation();
        consumedCombatRecords.Clear();
    }
}
