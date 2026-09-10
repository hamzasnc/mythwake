using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PlayerPrefs = MythwakePreferences;

/// <summary>
/// Opt-in, real SampleScene Play Mode acceptance. Run with graphics enabled, without -quit:
/// -executeMethod KaelGameplayAcceptance.Run -mythwakeGameplayAcceptance
/// -mythwakeTestProfile UNIQUE_ID [-mythwakeCaptureDir ABSOLUTE_DIRECTORY]
/// ScreenCapture records the live Game View; no pose sampling or synthetic battle events.
/// </summary>
[InitializeOnLoad]
public static class KaelGameplayAcceptance
{
    private const string ActiveKey = "KaelGameplayAcceptance.Active";
    private const string OutputKey = "KaelGameplayAcceptance.Output";
    private const string UsedProfileKey = "Mythwake.QA.GameplayAcceptanceUsed";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    private static IdlePrototypeController controller;
    private static Stack<IEnumerator> steps;
    private static Report report;
    private static string output, phase, pendingError;
    private static double started, nextCapture;
    private static int frameIndex, lastCapturedFrame, exitCode;
    private static bool finishing, captureEnabled;
    private static int captureFps = 10;

    [Serializable] private sealed class BattleRecord
    {
        public string label;
        public int durationMs, kaelNormalHits, kaelMisses, kaelSkills, skillInputs;
        public int kaelNormalActions, kaelSkillContacts;
        public int kaelSkillActionsStarted, kaelSkillActionsCompleted, kaelFinalSkillEffects;
        public bool animationReviewOnly, rearViewSeen;
        public float playbackSpeed;
        public string[] observedAnimations;
        public int kaelImpactEffects, kaelSkillEffects;
        public bool kaelChargeEffectSeen;
        public int focusWindows, focusFrozenFrames, focusClockStarts, focusImpactHolds;
        public float maxFocusScale;
        public bool focusPauseChecked, focusBackdropSeen, focusPoseMoved, focusAgeAdvanced;
        public int teamHp, enemyHp, dealt, taken;
        public bool won;
        public int kaelDeathMs = -1;
        public bool kaelDeathPoseReached;
        public int[] heroMaxHp, heroAttack;
        public string[] participants;
        public MythwakeCombatEventDto[] events;
    }
    [Serializable] private sealed class Report
    {
        public string profile, scene = "Assets/Scenes/SampleScene.unity", status, error, unityVersion;
        public string capture = "Live Play Mode ScreenCapture.CaptureScreenshot";
        public int requestedCaptureFps;
        public bool backgroundOnly = true;
        public string fixture = "Existing progression fields only: other heroes level 1, Kael level 12; all weapons level 1, zero ascensions. Armor is selected from legal progression levels 150/300/600/1200 for front-line survival; exact chosen levels are recorded below. Stage/floor selected with the unchanged combat session. No injected mana, HP, hit, cooldown or animation state.";
        public string repeatedSkills = "Skills are asserted across real repeated battles. The unchanged 30 second limit, 13 successful attacks per charge, travel and recovery do not guarantee two Kael skills in a single battle.";
        public string deathFixture = "Final real defeat: all heroes level1, armor1, weapons1, zero ascensions, campaign60, Kael in front slot. Lethal damage comes only from ordinary enemy attacks; the rig must hold its >=0.9s death pose.";
        public int campaignStage, dungeonFloor, width, height, screenshots;
        public int[] levels, armor, formation;
        public List<string> assertions = new List<string>();
        public List<BattleRecord> battles = new List<BattleRecord>();
    }

    static KaelGameplayAcceptance()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        try
        {
            Require(PlayerPrefs.IsTestProfileActive, "An explicit -mythwakeTestProfile is required; production profiles are never accepted.");
            Require(Environment.GetCommandLineArgs().Contains("-mythwakeGameplayAcceptance"), "Explicit -mythwakeGameplayAcceptance opt-in is required.");
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Start from Edit Mode.");
            Require(!PlayerPrefs.HasKey(UsedProfileKey) && !PlayerPrefs.HasKey("Mythwake.Prototype.SaveJson"), "Use a fresh unique test profile, not an existing save.");
            output = Path.GetFullPath(Argument("-mythwakeCaptureDir") ?? ("artifacts/kael/gameplay-" + PlayerPrefs.TestProfileId));
            Require(!Directory.Exists(output) || !Directory.EnumerateFileSystemEntries(output).Any(), "Capture directory must be empty.");
            Directory.CreateDirectory(output);
            PlayerPrefs.SetInt(UsedProfileKey, 1);
            PlayerPrefs.SetInt("Mythwake.Backend.GameplayEnabled", 0);
            PlayerPrefs.Save();
            SessionState.SetString(OutputKey, output);
            SessionState.SetBool(ActiveKey, true);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            ConfigureGameView();
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Debug.LogError("KAEL_GAMEPLAY_ACCEPTANCE_REFUSED: " + ex);
            SessionState.SetBool(ActiveKey, false);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            // This opt-in, isolated Editor driver must not interrupt desktop work.
            // Runtime/release settings are unchanged; explicit pause tests still
            // exercise the controller's real pause path below.
            Application.runInBackground = true;
            output = SessionState.GetString(OutputKey, "");
            report = new Report { profile = PlayerPrefs.TestProfileId, unityVersion = Application.unityVersion, status = "running" };
            captureFps = int.TryParse(Argument("-mythwakeCaptureFps"), out var requestedFps) ? Mathf.Clamp(requestedFps, 1, 30) : 10;
            report.requestedCaptureFps = captureFps;
            report.capture += " at up to " + captureFps + " frames per real second";
            started = EditorApplication.timeSinceStartup;
            nextCapture = started;
            frameIndex = 0; lastCapturedFrame = -1; finishing = false; pendingError = null;
            captureEnabled = false;
            phase = "account-start";
            steps = new Stack<IEnumerator>(); steps.Push(Exercise());
            File.WriteAllText(Path.Combine(output, "frames.csv"), "index,realSeconds,unityFrame,combatSeconds,phase,width,height,kaelState,kaelPoseAge,focusActive\n");
            Application.logMessageReceived += OnLog;
        }
        else if (change == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(ActiveKey, false);
            if (Application.isBatchMode) EditorApplication.Exit(exitCode);
        }
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (!finishing && (type == LogType.Exception || type == LogType.Error)) pendingError = message + "\n" + stack;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || steps == null || finishing) return;
        try
        {
            Require(pendingError == null, "Unity reported an error during real gameplay: " + pendingError);
            Require(EditorApplication.timeSinceStartup - started < 480, "Gameplay acceptance exceeded its 480 second wall-clock limit.");
            if (controller != null)
            {
                Require(!Get<bool>("backendGameplayEnabled") && !Get<bool>("backendRequestInProgress"), "Test must remain local; backend activity detected.");
                Require(!Get<MythwakeBackendClient>("backendClient").HasSession, "No backend session is permitted in this fixture.");
            }
            CaptureFrame();
            while (steps.Count > 0)
            {
                var current = steps.Peek();
                if (!current.MoveNext()) { steps.Pop(); continue; }
                if (current.Current is IEnumerator nested) { steps.Push(nested); continue; }
                return;
            }
            Finish(null);
        }
        catch (Exception ex) { Finish(ex.ToString()); }
    }

    private static IEnumerator Exercise()
    {
        yield return Wait(1);
        controller = UnityEngine.Object.FindAnyObjectByType<IdlePrototypeController>();
        Require(controller != null, "Normal SampleScene controller was not created.");
        BlockWorkstationInput();
        Require(PlayerPrefs.IsTestProfileActive, "Test profile isolation was lost on domain reload.");
        Require(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null, "Live Game View capture requires graphics; do not pass -nographics.");
        Require(Get<bool>("accountStartVisible"), "Normal account start screen was not shown.");
        var visibleCanvas = Get<GameObject>("homePanel").GetComponentInParent<Canvas>();
        Require(visibleCanvas != null && (visibleCanvas.renderMode == RenderMode.ScreenSpaceOverlay ||
            (visibleCanvas.worldCamera != null && visibleCanvas.worldCamera.targetTexture == null)),
            "Normal game canvas is redirected to an offscreen validation camera; these would not be genuine Game View captures.");
        if (Environment.GetCommandLineArgs().Contains("-mythwakeQuickMotion"))
        {
            captureEnabled = true;
            Click("accountStartContinueButton");
            controller.ShowBattle();
            Set("heroLevels", Enumerable.Repeat(1, 8).ToArray());
            Set("heroArmorLevels", Enumerable.Repeat(1, 8).ToArray());
            Set("heroWeaponLevels", Enumerable.Repeat(1, 8).ToArray());
            Set("formationSlotHeroIndices", new[] { 7, 1, 2, 3, 4, 5, 6 });
            Set("autoContinueFightsEnabled", false); Set("autoAttackEnabled", false);
            Set("fightDoubleSpeedEnabled", false);
            Set("enemyLevel", 60); Set("selectedCampaignStage", 60);
            Set("enemyMaxHp", (int)Call("GetStageMaxHp", 60)); Set("enemyHp", Get<int>("enemyMaxHp"));
            Call("SaveProgress"); Call("RefreshUi");
            phase = "quick-motion-formation"; yield return Wait(1);
            yield return Fight("quick-motion-death", false, false, true);
            captureEnabled = false; yield return Wait(3);
            report.screenshots = Directory.GetFiles(output, "frame-*.png").Length;
            yield break;
        }
        yield return KaelAnimationValidation.ValidateCanvasRuntime();
        Check("Two independent Canvas views passed runtime clock, facing, reset and lifecycle checks before recording.");
        captureEnabled = true;
        phase = "account-start"; yield return Wait(1);
        SeedProgression();
        yield return Wait(1);
        Click("accountStartContinueButton");
        Require(!Get<bool>("accountStartVisible"), "Local saved-account Continue did not enter the game.");
        Check("Normal account start -> local Continue; no guest-auth/network request.");
        phase = "home-idle"; yield return Wait(2);
        controller.ShowHeroes(); phase = "collection"; yield return Wait(1);
        Call("SelectHero", 7); phase = "kael-details"; yield return Wait(1);
        Require(Get<int>("selectedHeroIndex") == 7, "Kael details did not select roster index 7.");
        Check("Kael collection/detail opens through the normal hero selection method.");
        controller.ShowBattle(); phase = "formation"; yield return Wait(1);
        Require(((int[])Call("GetActiveFormationHeroIndices")).Length == 7, "Formation must contain seven of eight heroes.");
        yield return Fight("campaign-manual", false, true);
        Click("fightContinueButton");
        controller.ShowBattle(); phase = "repeat-formation"; yield return Wait(1);
        yield return Fight("campaign-repeat-auto", true, false);
        Click("fightContinueButton");

        controller.ShowBattle(); phase = "animation-only-formation"; yield return Wait(1);
        Set("kaelAnimationReviewOnly", true);
        yield return Fight("campaign-animation-only", true, false);
        Set("kaelAnimationReviewOnly", false);
        Click("fightContinueButton");
        Check("Additional real battle recorded without VFX, floating combat text, cut-in or cinematic enlargement; all three basic choreographies and rear-view sprite swaps were observed.");

        // Use the actual slot/bench selection controls and persistence path.
        controller.ShowBattle();
        Call("SelectFormationSlot", 0); Call("SelectFormationBenchHero", 0);
        Require(!((int[])Call("GetActiveFormationHeroIndices")).Contains(7), "Kael was not benched.");
        phase = "kael-benched"; yield return Wait(1);
        Call("SelectFormationSlot", 6); Call("SelectFormationBenchHero", 7);
        var beforeReload = (int[])Call("GetActiveFormationHeroIndices");
        Require(beforeReload[6] == 7 && beforeReload.Distinct().Count() == 7, "Bench swap lost slot order or uniqueness.");
        Call("SaveProgress");
        phase = "formation-saved"; yield return Wait(1);
        var reload = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
        while (!reload.isDone) yield return null;
        controller = null;
        yield return Wait(1);
        controller = UnityEngine.Object.FindAnyObjectByType<IdlePrototypeController>();
        BlockWorkstationInput();
        Require(controller != null && ((int[])Call("GetActiveFormationHeroIndices")).SequenceEqual(beforeReload), "Formation/profile reload lost the seventh slot or Kael identity.");
        Require(Get<int[]>("heroLevels")[7] == 12, "Independent Kael progression did not survive reload.");
        Click("accountStartContinueButton");
        Check("Bench Kael, deploy him in slot 7, save, reload normal SampleScene, continue same isolated profile; identity and progression preserved.");

        controller.ShowDungeons(); phase = "dungeon-menu"; yield return Wait(1);
        Call("SelectDungeonForPreview", "gold_dungeon");
        Click("dungeonDetailRunButton"); phase = "dungeon-formation"; yield return Wait(1);
        yield return Fight("dungeon-auto-slot7", true, false);
        Click("fightContinueButton");
        Require(report.battles.Sum(x => x.kaelSkills) >= 2, "Repeated real Kael skills were not observed.");
        Check("Multiple actual Kael skill impacts across repeated real campaign/dungeon sessions.");
        controller.ShowBattle();
        var deathFormation = (int[])Call("GetActiveFormationHeroIndices");
        var kaelSlot = Array.IndexOf(deathFormation, 7);
        if (kaelSlot != 0) { Call("SelectFormationSlot", kaelSlot); Call("SelectFormationSlot", 0); }
        Set("heroLevels", Enumerable.Repeat(1, 8).ToArray());
        Set("heroArmorLevels", Enumerable.Repeat(1, 8).ToArray());
        Set("heroWeaponLevels", Enumerable.Repeat(1, 8).ToArray());
        Set("enemyLevel", 60); Set("selectedCampaignStage", 60);
        Set("enemyMaxHp", (int)Call("GetStageMaxHp", 60)); Set("enemyHp", Get<int>("enemyMaxHp"));
        Call("SaveProgress"); Call("RefreshUi");
        phase = "real-death-formation"; yield return Wait(1);
        yield return Fight("campaign-real-death", false, false, true);
        Click("fightContinueButton");
        phase = "complete";
        // Stop issuing asynchronous captures before leaving Play Mode changes Game View dimensions.
        yield return Wait(.3); captureEnabled = false; yield return Wait(3);
        var files = Directory.GetFiles(output, "frame-*.png");
        Require(files.Length >= 60, "Too few completed live Game View screenshots: " + files.Length);
        report.screenshots = files.Length;
        Check("Live Game View PNG sequence recorded throughout account, home, collection, formation, real combat and results.");
    }

    private static void SeedProgression()
    {
        Require(PlayerPrefs.IsTestProfileActive, "Refusing to seed an ordinary profile.");
        var levels = Enumerable.Repeat(1, 8).ToArray(); levels[7] = 12;
        Set("heroLevels", levels); Set("heroAscensions", new int[8]);
        Set("heroWeaponLevels", Enumerable.Repeat(1, 8).ToArray());
        Set("heroArmorLevels", Enumerable.Repeat(150, 8).ToArray());
        Set("formationSlotHeroIndices", new[] { 7, 1, 2, 3, 4, 5, 6 });
        Set("autoContinueFightsEnabled", false); Set("autoAttackEnabled", false);
        Set("fightDoubleSpeedEnabled", false);
        Call("NormalizeLoadedState");
        foreach (var armor in new[] { 150, 300, 600, 1200 })
        {
            Set("heroArmorLevels", Enumerable.Repeat(armor, 8).ToArray());
            report.campaignStage = SelectLongFixture(false);
            report.dungeonFloor = SelectLongFixture(true);
            if (report.campaignStage > 0 && report.dungeonFloor > 0) break;
        }
        Require(report.campaignStage > 0 && report.dungeonFloor > 0, "Could not select valid long-lived normal progression fixtures with Kael alive.");
        Set("enemyLevel", report.campaignStage); Set("selectedCampaignStage", report.campaignStage);
        Set("enemyMaxHp", (int)Call("GetStageMaxHp", report.campaignStage));
        Set("enemyHp", Get<int>("enemyMaxHp"));
        Set("goldDungeonFloor", report.dungeonFloor);
        Call("SaveProgress"); Call("RefreshUi"); Call("RefreshAccountStartUi");
        report.levels = (int[])levels.Clone(); report.armor = (int[])Get<int[]>("heroArmorLevels").Clone();
        report.formation = (int[])Call("GetActiveFormationHeroIndices");
        UnityEngine.Random.InitState(7331);
        Check("Only the isolated test save received progression fixtures; gameplay formulas, animation clocks, mana and damage rules remain unchanged.");
        WriteReport();
    }

    private static int SelectLongFixture(bool dungeon)
    {
        var definition = typeof(IdlePrototypeController).GetField("GoldDungeonDefinition", Flags).GetValue(null);
        var best = -1;
        // This is fixture selection only. Acceptance evidence comes exclusively from the live sessions below.
        for (var level = 1; level <= 130; level++)
        {
            var hp = (int)(dungeon ? Call("GetDungeonEnemyHp", definition, level) : Call("GetStageMaxHp", level));
            var damage = (int)(dungeon ? Call("GetDungeonEnemyDamage", definition, level) : Call("GetCampaignEnemyDamage", level));
            var probe = (MythwakeCombatSession)Call("CreateLocalCombatSession", hp, damage, 7331);
            probe.AutoSkills = true;
            while (!probe.Finished) probe.Step(.1f);
            if (probe.Heroes.First(h => h.id == "hero_kael").hp <= 0 || !probe.Events.Any(e => e.actorId == "hero_kael" && e.eventType == "ultimate")) continue;
            if (probe.TimeMs >= 24000 && probe.TimeMs <= 28500) return level;
            if (probe.TimeMs >= 24000 && best < 0) best = level;
        }
        return best;
    }

    private static IEnumerator Fight(string label, bool auto, bool checkClock, bool expectDeath = false)
    {
        phase = label + "-approach";
        Click("formationConfirmButton");
        var deadline = EditorApplication.timeSinceStartup + 10;
        while (Get<MythwakeCombatSession>("activeLocalCombat") == null)
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Normal formation button did not start local combat.");
            yield return null;
        }
        var session = Get<MythwakeCombatSession>("activeLocalCombat");
        var expected = (string[])Call("GetActiveFormationHeroIds");
        Require(session.Heroes.Select(h => h.id).SequenceEqual(expected), "Combat participants do not match the selected formation order.");
        Require(session.Heroes.Count == 7 && session.Heroes.Any(h => h.id == "hero_kael"), "Selected roster contract failed.");
        if (Get<bool>("fightAutoSkillsEnabled") != auto) Click("fightAutoSkillButton");
        Require(Get<bool>("fightAutoSkillsEnabled") == auto, "AUTO toggle did not reflect requested normal UI state.");
        // Record an entire repeated battle, including its ultimate, at normal UI 2x.
        var requestedSpeed = label == "campaign-repeat-auto" ? 2f : 1f;
        if ((float)Call("GetFightTimeScale") != requestedSpeed) Click("fightSpeedButton");
        Require((float)Call("GetFightTimeScale") == requestedSpeed, "Normal speed control did not select the requested full-battle speed.");
        var record = new BattleRecord { label = label, participants = expected, animationReviewOnly = Get<bool>("kaelAnimationReviewOnly"),
            playbackSpeed = requestedSpeed,
            heroMaxHp = session.Heroes.Select(h => h.maxHp).ToArray(), heroAttack = session.Heroes.Select(h => h.attack).ToArray() };
        var observedAnimations = new HashSet<string>();
        var clockChecked = false;
        var initialPositions = Get<Array>("combatHeroStates");
        var initialKaelPosition = Position(initialPositions.GetValue(7));
        var moved = false;
        FocusSnapshot focusSnapshot = null;
        while (!session.Finished)
        {
            ObserveKaelEffects(record);
            var animatedView = Get<KaelAnimationView>("kaelFightView");
            if (animatedView != null && animatedView.Rig != null)
            {
                observedAnimations.Add(animatedView.Rig.CurrentState);
                record.rearViewSeen |= animatedView.Rig.GetComponentsInChildren<SpriteRenderer>()
                    .Any(r => r.enabled && r.gameObject.activeInHierarchy && r.color.a > .001f && r.sprite != null &&
                        r.sprite.name.IndexOf("_Rear", StringComparison.OrdinalIgnoreCase) >= 0);
                if (record.animationReviewOnly)
                {
                    Require(Mathf.Abs(animatedView.transform.localScale.x - 1f) < .001f, "Animation-only review enlarged the character.");
                    var hiddenEffects = Get<KaelCombatVfx>("kaelFightVfx");
                    Require(hiddenEffects == null || !hiddenEffects.enabled, "Animation-only review displayed combat effects.");
                    var hiddenBackdrop = Get<KaelUltimateBackdrop>("kaelUltimateBackdrop");
                    Require(hiddenBackdrop == null || (!hiddenBackdrop.PortraitVisible && hiddenBackdrop.Opacity <= .001f),
                        "Animation-only review displayed the cut-in or darkened backdrop.");
                    AssertAnimationReviewTextHidden();
                }
            }
            var focusClock = Get<KaelUltimateFocusClock>("kaelUltimateFocusClock");
            if (focusClock != null)
            {
                ObserveKaelFocusCounters(record);
                if (focusClock.IsActive)
                {
                    if (focusSnapshot == null || focusSnapshot.sequence != focusClock.Sequence)
                    {
                        focusSnapshot = new FocusSnapshot(session, focusClock);
                        record.focusWindows++;
                    }
                    else focusSnapshot.Observe(session, focusClock, record);
                    var focusedView = Get<KaelAnimationView>("kaelFightView");
                    var backdrop = Get<KaelUltimateBackdrop>("kaelUltimateBackdrop");
                    record.maxFocusScale = Mathf.Max(record.maxFocusScale, focusedView.transform.localScale.x);
                    record.focusBackdropSeen |= backdrop != null && backdrop.PortraitVisible && backdrop.Opacity > .5f && backdrop.LastVertexCount > 0;
                    if (checkClock && !record.focusPauseChecked && focusClock.Age > .18f && focusClock.Age < .50f)
                    {
                        phase = label + "-ultimate-paused";
                        Call("OnApplicationPause", true);
                        var pausedAge = focusClock.Age;
                        var pausedCombat = Get<float>("combatClockSeconds");
                        var pausedProgress = backdrop.Progress;
                        var pausedPose = CaptureKaelPose();
                        var pausedProjection = focusedView.transform.localToWorldMatrix;
                        yield return Wait(.15);
                        Require(focusClock.IsActive && focusClock.Age == pausedAge && Get<float>("combatClockSeconds") == pausedCombat,
                            "Application pause advanced the active ultimate focus or combat clock.");
                        Require(backdrop.Progress == pausedProgress && MatricesEqual(pausedPose, CaptureKaelPose()) &&
                            MatrixEqual(pausedProjection, focusedView.transform.localToWorldMatrix),
                            "Application pause advanced Kael's focused pose/projection or cinematic backdrop.");
                        focusSnapshot.AssertFrozen(session);
                        record.focusPauseChecked = true;
                        Call("OnApplicationPause", false);
                        Check("Application pause during the accepted manual ultimate freezes its cinematic age, battle time/events/HP, Kael bones/projection and backdrop; resume continues the same action.");
                    }
                }
            }
            moved |= Vector2.Distance(initialKaelPosition, Position(Get<Array>("combatHeroStates").GetValue(7))) > 10f;
            phase = label + "-combat";
            if (expectDeath) ObserveKaelDeathPose(session, record);
            if (checkClock && !clockChecked && session.TimeMs >= 2000)
            {
                phase = label + "-paused";
                Call("OnApplicationPause", true);
                var before = session.TimeMs; var count = session.Events.Count;
                yield return Wait(.5);
                Require(session.TimeMs == before && session.Events.Count == count, "Pause advanced combat time or applied an action.");
                Call("OnApplicationPause", false);
                Click("fightSpeedButton");
                Require((float)Call("GetFightTimeScale") == 2f, "x2 button did not select the shared combat clock.");
                phase = label + "-x2";
                yield return Wait(1);
                Require(session.TimeMs > before, "Resume/x2 did not advance the actual session.");
                Click("fightSpeedButton");
                Require((float)Call("GetFightTimeScale") == 1f, "x1 clock restore failed.");
                clockChecked = true;
                Check("Application pause freezes event count and simulation time; normal speed button selects x2 then restores x1.");
            }
            if (!auto && !expectDeath)
            {
                var kael = session.Heroes.First(h => h.id == "hero_kael");
                if (kael.mana >= 26 && session.TimeMs >= kael.cooldownUntilMs && session.TimeMs >= kael.busyUntilMs)
                {
                    var buttons = Get<Button[]>("fightSkillButtons");
                    Require(buttons[7] != null && buttons[7].interactable, "Full-mana Kael manual skill card is unavailable.");
                    buttons[7].onClick.Invoke(); record.skillInputs++;
                    // Same-frame repeat input must never create a second accepted skill.
                    buttons[7].onClick.Invoke();
                    phase = label + "-manual-skill";
                    yield return Wait(.15);
                }
            }
            yield return null;
        }
        Require(moved, "Kael never approached his target through normal movement.");
        ObserveKaelEffects(record);
        ObserveKaelFocusCounters(record);
        // The resolving frame can finish the session before this driver observes it.
        // Keep sampling the real end-pose coroutine, before cleanup resets its VFX/clock,
        // to cover a final-hit stop and a death of the last surviving participant.
        deadline = EditorApplication.timeSinceStartup + 5;
        phase = label + "-end-pose";
        while (Get<MythwakeCombatSession>("activeLocalCombat") == session)
        {
            Require(EditorApplication.timeSinceStartup < deadline, "The finished session did not release its end-pose presentation.");
            ObserveKaelEffects(record);
            ObserveKaelFocusCounters(record);
            if (record.animationReviewOnly) AssertAnimationReviewTextHidden();
            if (expectDeath) ObserveKaelDeathPose(session, record);
            yield return null;
        }
        record.events = session.Events.ToArray();
        record.durationMs = session.TimeMs; record.teamHp = session.TeamHp; record.enemyHp = session.EnemyHp;
        record.dealt = session.DamageDealt; record.taken = session.DamageTaken;
        record.won = session.TeamHp > 0 && session.EnemyHp <= 0;
        record.kaelNormalHits = record.events.Count(e => e.actorId == "hero_kael" && (e.eventType == "auto_attack" || e.eventType == "critical_attack"));
        record.kaelNormalActions = record.events.Where(e => e.actorId == "hero_kael" && (e.eventType == "auto_attack" || e.eventType == "critical_attack")).Select(e => e.actionId).Distinct().Count();
        record.kaelMisses = record.events.Count(e => e.actorId == "hero_kael" && e.eventType == "miss");
        record.kaelSkillContacts = record.events.Count(e => e.actorId == "hero_kael" && e.eventType == "ultimate");
        record.kaelSkills = record.events.Where(e => e.actorId == "hero_kael" && e.eventType == "ultimate").Select(e => e.actionId).Distinct().Count();
        record.kaelSkillActionsStarted = record.events.Count(e => e.actorId == "hero_kael" && e.eventType == "action_start" && !string.IsNullOrEmpty(e.skillId));
        record.kaelSkillActionsCompleted = record.events.Count(e => e.actorId == "hero_kael" && e.eventType == "ultimate" && MythwakeCombatReplay.IsFinalContact(e));
        record.observedAnimations = observedAnimations.OrderBy(x => x).ToArray();
        Require(record.focusWindows >= record.kaelSkills && record.focusWindows <= record.kaelSkillActionsStarted &&
            record.focusClockStarts == record.kaelSkillActionsStarted,
            label + " cinematic starts do not match accepted skill actions, including actions cancelled before contact.");
        if (record.kaelSkills > 0)
        {
            Require(record.focusFrozenFrames >= 2 && record.focusAgeAdvanced && record.focusPoseMoved,
                label + " lacks observed frozen arena frames with independently advancing Kael charge poses.");
            Require(record.animationReviewOnly || (record.maxFocusScale >= 1.3f && record.focusBackdropSeen),
                label + " did not render the enlarged Kael and visible portrait/arena cut-in.");
            Require(record.focusImpactHolds == record.kaelSkillActionsCompleted,
                label + " did not apply exactly one impact hold per completed skill, including an early killing contact.");
            if (checkClock) Require(record.focusPauseChecked, "Manual ultimate pause/resume was not exercised.");
        }
        Require(record.kaelImpactEffects == record.kaelNormalHits + record.kaelSkillContacts && record.kaelSkillEffects == record.kaelSkillContacts,
            label + " rendered impact counts differ from authoritative successful hits/skills.");
        Require(record.kaelFinalSkillEffects == record.kaelSkillActionsCompleted, label + " final skill effects differ from effective authoritative completions.");
        if (record.kaelSkills > 0 && !record.animationReviewOnly) Require(record.kaelChargeEffectSeen, label + " did not present the real skill charge.");
        report.battles.Add(record); WriteReport();
        if (expectDeath)
        {
            Require(record.kaelDeathMs >= 0 && record.kaelDeathPoseReached && !record.won,
                "Ordinary enemy damage did not produce a real Kael death and stable end pose.");
            Require(record.events.Any(e => e.eventType == "enemy_attack" && e.targetId == "hero_kael" && e.amount > 0),
                "Death must result from an authoritative enemy hit.");
        }
        else
        {
            Require(record.kaelNormalActions >= 13 && record.kaelSkills >= 1, label + " did not produce enough real normal actions and a real Kael skill.");
            Require(new[] { "attack_cross", "attack_spin", "attack_jump" }.All(observedAnimations.Contains), label + " did not render all three basic choreographies.");
            Require(record.rearViewSeen, label + " never rendered an authored rear-view sprite during the turning attack.");
        }
        Require(record.events.All(e => e.actorId == "enemy" || expected.Contains(e.actorId)), "A benched hero produced a combat event.");
        var resolvedSkills = record.events.Where(e => e.actorId == "hero_kael" && e.eventType == "ultimate").ToArray();
        Require(resolvedSkills.Select(e => e.contactId).Distinct().Count() == resolvedSkills.Length, "A Kael contact applied more than once.");
        if (!auto) Require(record.kaelSkillActionsStarted <= record.skillInputs && record.kaelSkills <= record.kaelSkillActionsStarted,
            "Repeated manual input created an extra accepted action or a skill contact without an accepted action.");
        deadline = EditorApplication.timeSinceStartup + 5;
        while (Get<bool>("campaignFightInProgress"))
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Completed actual session did not reach the result UI.");
            yield return null;
        }
        Require(Get<RectTransform>("fightResultRoot").gameObject.activeInHierarchy, "Normal fight result panel is hidden.");
        Require(Get<MythwakeCombatSession>("activeLocalCombat") == null, "Completed fight retained an active simulation.");
        var result = Get<object>("completedLocalCombatResult");
        Require((int)result.GetType().GetField("damageDealt", Flags).GetValue(result) == record.dealt &&
            (int)result.GetType().GetField("teamHpRemaining", Flags).GetValue(result) == record.teamHp &&
            (int)result.GetType().GetField("enemyHpRemaining", Flags).GetValue(result) == record.enemyHp,
            "Displayed/rewarded combat result differs from the actual stepped session.");
        phase = label + "-result";
        Check(label + ": seven selected heroes, real movement, " + record.kaelNormalActions + " normal actions, " + record.kaelSkillActionsStarted +
            " accepted / " + record.kaelSkills + " resolved / " + record.kaelSkillActionsCompleted + " completed skill actions, matching contact delivery, " +
            record.focusWindows + " focus window(s), " + record.focusFrozenFrames + " frozen arena frames, " +
            (record.animationReviewOnly ? "VFX/cut-in/enlargement hidden" : "visible charge and portrait") + ", result and session cleanup.");
        yield return Wait(2);
    }

    private static void ObserveKaelEffects(BattleRecord record)
    {
        var effects = Get<KaelCombatVfx>("kaelFightVfx");
        if (effects == null) return;
        record.kaelImpactEffects = Mathf.Max(record.kaelImpactEffects, effects.ImpactCount);
        record.kaelSkillEffects = Mathf.Max(record.kaelSkillEffects, effects.SkillImpactCount);
        record.kaelFinalSkillEffects = Mathf.Max(record.kaelFinalSkillEffects, effects.FinalSkillImpactCount);
        record.kaelChargeEffectSeen |= effects.isActiveAndEnabled && effects.HasCharge;
    }

    private static void ObserveKaelFocusCounters(BattleRecord record)
    {
        var clock = Get<KaelUltimateFocusClock>("kaelUltimateFocusClock");
        if (clock == null) return;
        record.focusClockStarts = Mathf.Max(record.focusClockStarts, clock.StartCount);
        record.focusImpactHolds = Mathf.Max(record.focusImpactHolds, clock.ImpactHoldCount);
    }

    private static void AssertAnimationReviewTextHidden()
    {
        var labels = Get<TMPro.TMP_Text[]>("fightFloatingTexts");
        Require(labels == null || labels.All(label => label == null || !label.gameObject.activeInHierarchy),
            "Animation-only review displayed damage numbers, MISS or other floating combat text.");
    }

    private static void ObserveKaelDeathPose(MythwakeCombatSession session, BattleRecord record)
    {
        if (session.Heroes.First(h => h.id == "hero_kael").hp > 0) return;
        phase = record.label + "-death";
        if (record.kaelDeathMs < 0) record.kaelDeathMs = session.TimeMs;
        var view = Get<KaelAnimationView>("kaelFightView");
        if (view != null && view.Rig != null && view.Rig.CurrentState == "death" && view.Rig.CurrentTime >= .89f &&
            Mathf.Abs(Mathf.DeltaAngle(view.Rig.transform.Find("Root").localEulerAngles.z, 82f)) < .5f)
            record.kaelDeathPoseReached = true;
    }

    private sealed class FocusSnapshot
    {
        public readonly long sequence;
        readonly int time, eventCount, dealt, taken, teamHp, enemyHp;
        readonly int[] heroValues;
        readonly Vector2[] heroPositions, enemyPositions;
        readonly List<RawImageSnapshot> imageFrames = new List<RawImageSnapshot>();
        readonly Matrix4x4[] initialKaelPose;
        readonly float initialAge;
        int lastObservedFrame;

        public FocusSnapshot(MythwakeCombatSession session, KaelUltimateFocusClock clock)
        {
            sequence = clock.Sequence; time = session.TimeMs; eventCount = session.Events.Count;
            dealt = session.DamageDealt; taken = session.DamageTaken; teamHp = session.TeamHp; enemyHp = session.EnemyHp;
            heroValues = HeroValues(session);
            heroPositions = Positions("combatHeroStates"); enemyPositions = Positions("combatEnemyStates");
            CaptureImages(Get<RawImage[]>("fightHeroImages"), true);
            CaptureImages(Get<RawImage[]>("fightEnemyImages"), false);
            initialKaelPose = CaptureKaelPose(); initialAge = clock.Age; lastObservedFrame = Time.frameCount;
        }
        void CaptureImages(RawImage[] images, bool heroes)
        {
            for (var i = 0; i < images.Length; i++)
                if ((!heroes || i != 7) && images[i] != null && images[i].isActiveAndEnabled)
                    imageFrames.Add(new RawImageSnapshot(images[i]));
        }
        public void Observe(MythwakeCombatSession session, KaelUltimateFocusClock clock, BattleRecord record)
        {
            AssertFrozen(session);
            if (Time.frameCount == lastObservedFrame) return;
            lastObservedFrame = Time.frameCount;
            record.focusFrozenFrames++;
            record.focusAgeAdvanced |= clock.Age > initialAge + .01f;
            record.focusPoseMoved |= !MatricesEqual(initialKaelPose, CaptureKaelPose());
        }
        public void AssertFrozen(MythwakeCombatSession session)
        {
            Require(session.TimeMs == time && session.Events.Count == eventCount && session.DamageDealt == dealt &&
                session.DamageTaken == taken && session.TeamHp == teamHp && session.EnemyHp == enemyHp && HeroValues(session).SequenceEqual(heroValues),
                "Ultimate focus changed authoritative time, events, damage, HP, mana or action/cooldown times before resuming the battle.");
            Require(Positions("combatHeroStates").SequenceEqual(heroPositions) && Positions("combatEnemyStates").SequenceEqual(enemyPositions),
                "An arena participant moved during Kael's isolated ultimate charge.");
            foreach (var frame in imageFrames) frame.AssertFrozen();
        }
        static int[] HeroValues(MythwakeCombatSession session) => session.Heroes.SelectMany(h => new[] { h.hp, h.mana, h.cooldownUntilMs, h.busyUntilMs }).ToArray();
        static Vector2[] Positions(string field)
        {
            var states = Get<Array>(field);
            var result = new Vector2[states.Length];
            for (var i = 0; i < result.Length; i++) if (states.GetValue(i) != null) result[i] = Position(states.GetValue(i));
            return result;
        }
    }
    private sealed class RawImageSnapshot
    {
        readonly RawImage image;
        readonly Texture texture;
        readonly Rect uv;
        readonly Matrix4x4 transform;
        readonly Color color;
        public RawImageSnapshot(RawImage image)
        {
            this.image = image; texture = image.texture; uv = image.uvRect;
            transform = image.transform.localToWorldMatrix; color = image.color;
        }
        public void AssertFrozen() => Require(image != null && image.isActiveAndEnabled && image.texture == texture && image.uvRect == uv &&
            image.color == color && MatrixEqual(transform, image.transform.localToWorldMatrix),
            "Another visible fighter changed its rendered frame, tint or transform during Kael's focus.");
    }
    private static Matrix4x4[] CaptureKaelPose()
    {
        var rig = Get<KaelAnimationView>("kaelFightView").Rig;
        return rig.GetComponentsInChildren<Transform>(true).Select(t => Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale)).ToArray();
    }
    private static bool MatricesEqual(Matrix4x4[] a, Matrix4x4[] b) => a.Length == b.Length && !a.Where((value, i) => !MatrixEqual(value, b[i])).Any();
    private static bool MatrixEqual(Matrix4x4 a, Matrix4x4 b)
    {
        for (var i = 0; i < 16; i++) if (Mathf.Abs(a[i] - b[i]) > .0001f) return false;
        return true;
    }

    private static Vector2 Position(object state) => (Vector2)state.GetType().GetField("position", Flags).GetValue(state);
    private static IEnumerator Wait(double seconds)
    {
        var until = EditorApplication.timeSinceStartup + seconds;
        while (EditorApplication.timeSinceStartup < until) yield return null;
    }
    private static void CaptureFrame()
    {
        if (!captureEnabled) return;
        var now = EditorApplication.timeSinceStartup;
        if (now < nextCapture || Time.frameCount == lastCapturedFrame || Screen.width < 1 || Screen.height < 1) return;
        nextCapture = now + 1.0 / captureFps; lastCapturedFrame = Time.frameCount;
        ScreenCapture.CaptureScreenshot(Path.Combine(output, $"frame-{frameIndex:00000}.png"));
        var clock = controller == null ? 0f : Get<float>("combatClockSeconds");
        var kaelView = controller == null ? null : Get<KaelAnimationView>("kaelFightView");
        var kaelClock = controller == null ? null : Get<KaelUltimateFocusClock>("kaelUltimateFocusClock");
        var kaelState = kaelView == null || kaelView.Rig == null ? "" : kaelView.Rig.CurrentState;
        var kaelAge = kaelView == null || kaelView.Rig == null ? 0f : kaelView.Rig.CurrentTime;
        File.AppendAllText(Path.Combine(output, "frames.csv"), FormattableString.Invariant($"{frameIndex},{now-started:F3},{Time.frameCount},{clock:F3},{phase},{Screen.width},{Screen.height},{kaelState},{kaelAge:F4},{(kaelClock != null && kaelClock.IsActive ? 1 : 0)}\n"));
        report.width = Screen.width; report.height = Screen.height; frameIndex++;
    }
    private static void Finish(string error)
    {
        finishing = true; steps = null; Application.logMessageReceived -= OnLog;
        exitCode = error == null ? 0 : 1;
        report.status = error == null ? "passed" : "failed"; report.error = error;
        report.screenshots = Directory.GetFiles(output, "frame-*.png").Length;
        WriteReport();
        if (error == null) Debug.Log("KAEL_GAMEPLAY_ACCEPTANCE_OK: " + output);
        else Debug.LogError("KAEL_GAMEPLAY_ACCEPTANCE_FAILED: " + error);
        EditorApplication.ExitPlaymode();
    }
    private static void WriteReport() => File.WriteAllText(Path.Combine(output, "acceptance.json"), JsonUtility.ToJson(report, true));
    private static void Check(string value) { report.assertions.Add(value); Debug.Log("KAEL_GAMEPLAY_CHECK: " + value); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void BlockWorkstationInput()
    {
        // Only the opt-in editor driver controls this test. Buttons still execute their normal onClick listeners.
        foreach (var module in UnityEngine.Object.FindObjectsByType<BaseInputModule>()) module.enabled = false;
    }
    private static T Get<T>(string name) => (T)typeof(IdlePrototypeController).GetField(name, Flags).GetValue(controller);
    private static void Set(string name, object value) => typeof(IdlePrototypeController).GetField(name, Flags).SetValue(controller, value);
    private static object Call(string name, params object[] args)
    {
        var methods = typeof(IdlePrototypeController).GetMethods(Flags).Where(m => m.Name == name && m.GetParameters().Length == args.Length).ToArray();
        Require(methods.Length == 1, "Ambiguous or missing normal entry method: " + name);
        return methods[0].Invoke(methods[0].IsStatic ? null : controller, args);
    }
    private static void Click(string field)
    {
        var button = Get<Button>(field);
        Require(button != null && button.gameObject.activeInHierarchy && button.interactable,
            "Normal UI button unavailable: " + field + "; screen=" + Get<object>("activeScreen") +
            "; flow=" + Get<object>("battleFlowMode") + "; enabled=" + (button != null && button.interactable) +
            "; active=" + (button != null && button.gameObject.activeInHierarchy));
        Debug.Log("KAEL_GAMEPLAY_UI: " + field + " at " + phase);
        button.onClick.Invoke();
    }
    private static string Argument(string option)
    {
        var args = Environment.GetCommandLineArgs(); var index = Array.IndexOf(args, option);
        return index < 0 ? null : index + 1 < args.Length ? args[index + 1] : throw new ArgumentException(option + " needs a value.");
    }
    private static void ConfigureGameView()
    {
        var editor = typeof(EditorWindow).Assembly;
        var gameView = editor.GetType("UnityEditor.GameView", true);
        var view = EditorWindow.GetWindow(gameView, false, null, false);
        // Selecting a real Game View size changes only the editor window, never scene/camera rendering.
        var sizesType = editor.GetType("UnityEditor.GameViewSizes", true);
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes = singleton.GetProperty("instance", Flags).GetValue(null);
        var getGroup = sizesType.GetMethod("GetGroup", Flags);
        var groupType = sizesType.GetProperty("currentGroupType", Flags)?.GetValue(sizes)
            ?? Enum.ToObject(getGroup.GetParameters()[0].ParameterType, 0);
        var group = getGroup.Invoke(sizes, new[] { groupType });
        var sizeType = editor.GetType("UnityEditor.GameViewSize", true);
        var enumType = editor.GetType("UnityEditor.GameViewSizeType", true);
        var size = Activator.CreateInstance(sizeType, Flags, null, new[] { Enum.ToObject(enumType, 1), (object)540, 960, "Kael acceptance 540x960" }, null);
        group.GetType().GetMethod("AddCustomSize", Flags).Invoke(group, new[] { size });
        var total = (int)group.GetType().GetMethod("GetTotalCount", Flags).Invoke(group, null);
        gameView.GetProperty("selectedSizeIndex", Flags).SetValue(view, total - 1);
        // Keep the selected Game View render size, without showing/focusing a window.
    }
}
