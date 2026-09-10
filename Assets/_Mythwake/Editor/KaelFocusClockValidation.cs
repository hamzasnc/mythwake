using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

/// <summary>Focus checks use real local sessions and replay records, without scene or save writes.</summary>
public static class KaelFocusClockValidation
{
#if UNITY_EDITOR
    [MenuItem("Mythwake/Tests/Kael Ultimate Focus Clock")]
#endif
    public static void Run()
    {
        FocusConservesFrameTimeAndPause();
        PoseNeverRewindsAndReachesAuthoritativeContact();
        RepeatedAndCancelledFocusCannotLeakAcrossBattles();
        LocalOutcomesSurviveFocusAndSpeedChanges();
        ServerReplayRetainsEveryHitAndSnapshot();
        KillingUltimateRetainsHitHoldAndFocusFade();
#if UNITY_EDITOR
        Debug.Log("Kael ultimate focus: 6 test groups passed (pause, 1x/2x, pose continuity, lifecycle, local event equality, server replay totals and killing-blow end pose). No player state was written.");
#endif
    }

    private static void FocusConservesFrameTimeAndPause()
    {
        var clock = new KaelUltimateFocusClock();
        Require(clock.Begin(1, .07f), "A newly accepted skill must enter focus.");
        Near(clock.Consume(.2f), 0f, "The first frame belongs entirely to focus.");
        var age = clock.Age;
        for (var i = 0; i < 120; i++) Near(clock.Consume(0f), 0f, "Paused frames cannot advance either clock.");
        Near(clock.Age, age, "Application pause freezes the cinematic.");
        Near(clock.Consume(.2f), 0f, "Combat must remain held in the middle of focus.");
        Near(clock.Consume(.2f), 0f, "Combat must remain held until the full charge finishes.");
        Near(clock.Consume(.1f), .08f, "The finishing frame must return its unconsumed remainder to combat.");
        Require(!clock.IsActive, "Focus must end after exactly its bounded duration.");
        Near(clock.Age, KaelUltimateFocusClock.Duration, "The completed focus retains its terminal age for fade-out.");
        Near(clock.Consume(.1f), .1f, "Subsequent frames belong entirely to combat.");
        Require(clock.HoldResolvedImpact(1) && !clock.HoldResolvedImpact(1), "A unique resolved hit may enter its separate impact hold once.");
        Require(!clock.IsActive && clock.IsImpactHoldActive, "Impact hold must not restart the focus cut-in.");
        Near(clock.Consume(.03f), 0f, "The resolved impact pose receives a short full hold.");
        for (var i = 0; i < 90; i++) Near(clock.Consume(0f), 0f, "Application pause freezes impact hold too.");
        Near(clock.Consume(.1f), .05f, "Impact hold returns the finishing frame's exact remainder.");
        Require(!clock.IsImpactHoldActive && clock.ImpactHoldCount == 1, "The 80ms hold cannot repeat its hit.");

        var normal = MeasureFocusWallTime(1f);
        var fast = MeasureFocusWallTime(2f);
        Near(normal, .62f, "1x focus lasts 620ms of wall time.");
        Near(fast, .31f, "2x applies the same shared speed to focus.");
    }

    private static float MeasureFocusWallTime(float speed)
    {
        var clock = new KaelUltimateFocusClock(); clock.Begin(1, 0f);
        var wall = 0f;
        while (clock.IsActive)
        {
            var delta = .01f * speed;
            wall += (delta - clock.Consume(delta)) / speed;
        }
        return wall;
    }

    private static void PoseNeverRewindsAndReachesAuthoritativeContact()
    {
        foreach (var startAge in new[] { 0f, .07f, .19f, .38f })
        {
            var clock = new KaelUltimateFocusClock(); clock.Begin(4, startAge);
            var previous = clock.MapActionAge(4, startAge);
            Near(previous, startAge, "Starting focus must preserve the pose already reached this frame.");
            while (clock.IsActive)
            {
                clock.Consume(.01f);
                var current = clock.MapActionAge(4, startAge);
                Require(current >= previous - .00001f && current < .4f, "The inserted charge cannot rewind or reach contact early.");
                previous = current;
            }
            for (var age = startAge; age < .4f; age += .005f)
            {
                var current = clock.MapActionAge(4, age);
                Require(current >= previous - .00001f && current < .4f, "Resumed combat must approach the same contact continuously.");
                previous = current;
            }
            Near(clock.MapActionAge(4, .4f), .4f, "The blade reaches contact only at the authoritative 400ms hit.");
            Near(clock.MapActionAge(4, .8f), .8f, "Recovery timing remains unchanged.");
            Near(clock.MapActionAge(5, .1f), .1f, "Other actions cannot inherit a previous focus pose.");
        }
    }

    private static void RepeatedAndCancelledFocusCannotLeakAcrossBattles()
    {
        var clock = new KaelUltimateFocusClock();
        Require(clock.Begin(1, 0f) && !clock.Begin(1, 0f), "Duplicate action starts cannot restart focus.");
        clock.Consume(.7f);
        Require(!clock.Begin(1, 0f), "A completed action cannot replay its cinematic.");
        Require(clock.Begin(3, .1f), "A later genuine ultimate must receive its own focus.");
        clock.Cancel();
        Require(!clock.IsActive && clock.Sequence == -1 && clock.Age == 0f, "Death/cancel must invalidate the focused actor and any fade.");
        Near(clock.MapActionAge(3, .15f), .15f, "An invalidated focus cannot alter a reused view.");
        Require(!clock.Begin(3, 0f), "An old start cannot resurrect cancelled focus.");
        clock.Reset();
        Require(clock.StartCount == 0 && clock.Begin(1, 0f), "A new battle starts a clean action sequence.");
        var other = new KaelUltimateFocusClock();
        Require(other.Begin(1, 0f), "Two Kael instances own independent clocks.");
        clock.Consume(.2f);
        Near(other.Age, 0f, "One actor's focus must not advance another instance.");
        other.Reset();
        Require(!other.Begin(1, .4f), "An already resolved legacy hit cannot introduce a late charge.");
    }

    private static MythwakeCombatSession NewSession()
    {
        return new MythwakeCombatSession(new[] {
            new MythwakeCombatSession.Hero { id = "hero_kael", rosterIndex = 7, maxHp = 10000, attack = 20,
                accuracy = 100, criticalChance = 0, incomingDamage = 1, maxMana = 26, manaGain = 2,
                attackIntervalMs = 1120, skillMultiplier = 4f },
            new MythwakeCombatSession.Hero { id = "hero_astra", rosterIndex = 0, maxHp = 10000, attack = 11,
                accuracy = 91, criticalChance = 12, incomingDamage = 1, maxMana = 28, manaGain = 2,
                legacyAttackIntervalJitter = true }
        }, 1000000, 60000, 314159) { AutoSkills = true };
    }

    private static void LocalOutcomesSurviveFocusAndSpeedChanges()
    {
        var baseline = NewSession(); while (!baseline.Finished) baseline.Step(.01f);
        foreach (var speed in new[] { 1f, 2f })
        {
            var focused = NewSession(); var clock = new KaelUltimateFocusClock();
            var cursor = 0; long sequence = 0;
            var testedPause = false; var frames = 0;
            while (!focused.Finished && frames++ < 10000)
            {
                var oldTime = focused.TimeMs; var oldCount = focused.Events.Count;
                var delta = clock.Consume(.0333333f * speed);
                focused.Step(delta);
                if (delta == 0f)
                    Require(focused.TimeMs == oldTime && focused.Events.Count == oldCount,
                        "Focus must freeze allies, enemies and every pending hit in the actual session.");
                while (cursor < focused.Events.Count)
                {
                    var record = focused.Events[cursor++];
                    if (record.actorId == "hero_kael" && record.eventType == "ultimate" && MythwakeCombatReplay.IsFinalContact(record))
                        Require(clock.HoldResolvedImpact(sequence), "The resolved local ultimate should receive one impact hold.");
                    if (record.actorId != "hero_kael" || record.eventType != "action_start") continue;
                    sequence++;
                    if (!string.IsNullOrEmpty(record.skillId))
                        Require(clock.Begin(sequence, (focused.TimeMs - record.actionStartMs) / 1000f), "Every accepted skill should enter focus before contact.");
                }
                if (clock.IsActive && !testedPause)
                {
                    var time = focused.TimeMs; var count = focused.Events.Count; var focusAge = clock.Age;
                    for (var paused = 0; paused < 90; paused++) focused.Step(clock.Consume(0f));
                    Require(focused.TimeMs == time && focused.Events.Count == count && clock.Age == focusAge,
                        "Application pause must freeze an active focus and its real combat session together.");
                    testedPause = true;
                }
            }
            Require(focused.Finished && testedPause && clock.StartCount >= 2, "The fixture must finish and cover repeated ultimates with pause.");
            Require(focused.Events.SequenceEqual(baseline.Events),
                "Adding focus and changing speed must preserve every local event, amount, mana, cooldown and HP snapshot.");
            Require(focused.DamageDealt == baseline.DamageDealt && focused.TeamHp == baseline.TeamHp &&
                focused.EnemyHp == baseline.EnemyHp && focused.TimeMs == baseline.TimeMs,
                "Focus may add presentation time but cannot change the authoritative result.");
        }
    }

    private static void ServerReplayRetainsEveryHitAndSnapshot()
    {
        var session = NewSession(); while (!session.Finished) session.Step(.1f);
        // Shuffle timestamp groups while preserving the service's causal order within a tick.
        var records = session.Events.Concat(session.Events.Where(e => e.eventType == "ultimate"))
            .OrderByDescending(e => e.timeMs).ToArray();
        var replay = new MythwakeCombatReplay(new MythwakeCombatResultDto {
            teamMaxHp = session.TeamMaxHp, enemyMaxHp = session.EnemyMaxHp, events = records });
        var clock = new KaelUltimateFocusClock();
        var delivered = new List<MythwakeCombatEventDto>();
        var time = 0f; long sequence = 0; var frames = 0;
        while (!replay.Finished && frames++ < 10000)
        {
            var oldTime = time; var oldCount = delivered.Count;
            var delta = clock.Consume(.0666666f);
            time += delta;
            replay.AdvanceTo((int)Math.Round(time * 1000f), (record, index) => {
                delivered.Add(record);
                if (record.actorId == "hero_kael" && record.eventType == "ultimate" && MythwakeCombatReplay.IsFinalContact(record))
                    Require(clock.HoldResolvedImpact(sequence), "The resolved replay ultimate should receive one impact hold.");
                if (record.actorId != "hero_kael" || record.eventType != "action_start") return;
                sequence++;
                if (!string.IsNullOrEmpty(record.skillId))
                    Require(clock.Begin(sequence, Math.Max(0f, time - record.actionStartMs / 1000f)), "A server skill should focus before its authoritative hit.");
            });
            if (delta == 0f) Require(time == oldTime && delivered.Count == oldCount, "Server replay events must remain frozen during focus.");
        }
        Require(replay.Finished && clock.StartCount >= 2, "The replay must finish after repeated focus insertions.");
        Require(delivered.Count == session.Events.Count && delivered.Count(e => e.eventType == "ultimate") ==
            session.Events.Count(e => e.eventType == "ultimate"), "Focus and duplicate input must not drop or replay a hit.");
        Require(replay.DamageDealt == session.DamageDealt && replay.DamageTaken == session.DamageTaken &&
            replay.TeamHp == session.TeamHp && replay.EnemyHp == session.EnemyHp,
            "Replay focus must preserve all server-owned totals and final HP.");
    }

    private static void KillingUltimateRetainsHitHoldAndFocusFade()
    {
        foreach (var enemyHealth in new[] { 80, 20 }) KillingUltimateRetainsHitHoldAndFocusFade(enemyHealth);
    }

    private static void KillingUltimateRetainsHitHoldAndFocusFade(int enemyHealth)
    {
        var hero = new MythwakeCombatSession.Hero { id = "hero_kael", rosterIndex = 7,
            maxHp = 1000, attack = 20, accuracy = 100, incomingDamage = 1,
            maxMana = 26, manaGain = 2, skillMultiplier = 4f };
        var session = new MythwakeCombatSession(new[] { hero }, enemyHealth, 60000, 314159);
        hero.mana = hero.maxMana; // In-memory ready-skill fixture; no player state is involved.
        Require(session.QueueSkill(7), "The killing-blow fixture must accept a real ready ultimate.");
        var clock = new KaelUltimateFocusClock(); var cursor = 0;
        while (!session.Finished)
        {
            session.Step(clock.Consume(.01f));
            while (cursor < session.Events.Count)
            {
                var record = session.Events[cursor++];
                if (record.eventType == "action_start")
                    Require(clock.Begin(1, (session.TimeMs - record.actionStartMs) / 1000f), "The real killing ultimate must enter focus.");
                if (record.eventType == "ultimate" && MythwakeCombatReplay.IsFinalContact(record))
                    Require(clock.HoldResolvedImpact(1), "The real killing hit must enter impact hold before the result.");
            }
        }
        Require(session.EnemyHp == 0 && session.DamageDealt == enemyHealth &&
            session.Events.Count(e => e.eventType == "ultimate") == (enemyHealth == 80 ? 2 : 1),
            "The fixture must finish from the first or second contact without inventing a late hit.");
        clock.BeginEndPose();
        Require(clock.Sequence == 1 && clock.IsImpactHoldActive && !clock.IsActive,
            "The end-pose path must retain the resolved skill identity and hit hold without replaying focus.");
        Near(clock.Age, .62f, "The terminal charge age must remain available for the focus fade.");
        var presentationTime = session.TimeMs / 1000f;
        var holdTime = presentationTime;
        presentationTime += clock.Consume(.03f);
        Near(presentationTime, holdTime, "The killing-hit pose and burst must remain frozen during end-pose impact hold.");
        presentationTime += clock.Consume(.1f);
        Near(presentationTime, holdTime + .05f, "The finishing end-pose frame must return the hold remainder to the fade.");
        Near(clock.MapActionAge(1, .45f), .45f, "Recovery and focus fade must continue without pose rewind.");
        var count = session.Events.Count;
        session.Step(10f);
        Require(session.Events.Count == count && session.DamageDealt == enemyHealth,
            "End-pose presentation must not allow a finished session to resolve future actions.");

        clock.Reset(); clock.Begin(1, 0f); clock.Consume(.2f); clock.BeginEndPose();
        Require(clock.Sequence == -1 && !clock.IsActive, "An unfinished charge must be cancelled when another action ends the battle.");
        clock.Reset(); clock.Begin(1, 0f); clock.Consume(.7f); clock.BeginEndPose();
        Require(clock.Sequence == -1 && !clock.IsImpactHoldActive, "A completed charge whose hit never resolved must not retain a result-screen focus.");
    }

    private static void Near(float actual, float expected, string message)
    {
        Require(Math.Abs(actual - expected) < .0001f, message + " Actual=" + actual + " Expected=" + expected);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

#if MYTHWAKE_FOCUS_STANDALONE
    public static void Main() { Run(); Console.WriteLine("PASS: all 6 Kael ultimate focus test groups"); }
#endif
}
