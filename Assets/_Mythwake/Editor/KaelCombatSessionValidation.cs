using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

/// <summary>Pure combat checks: no scene, save files, PlayerPrefs, server or economy writes.</summary>
public static class KaelCombatSessionValidation
{
#if UNITY_EDITOR
    [MenuItem("Mythwake/Tests/Kael Combat Authority")]
#endif
    public static void Run()
    {
        ManualSkillCommitsOnceAtImpact();
        AutoAndFrameCadenceAgree();
        CancelAndDeathDiscardPendingHits();
        PausedStepDoesNotAdvance();
        SelectedHeroIdentityIsPreserved();
        ServerReplayPreservesAuthorityAndOrder();
        ExistingExecuteAndWaveCancellationRemainCorrect();
        BasicVariantsShareOneRollAndBudget();
        MultiContactReplayDeduplicatesContacts();
        PartialActionsCancelWithoutInventingRemainingDamage();
#if UNITY_EDITOR
        Debug.Log("Kael combat authority: 10 focused test groups passed; no player state was written.");
#endif
    }

    private static MythwakeCombatSession.Hero Hero(int rosterIndex = 7)
    {
        return new MythwakeCombatSession.Hero { id = "hero_kael", rosterIndex = rosterIndex,
            maxHp = 10000, attack = 20, accuracy = 100, criticalChance = 0, incomingDamage = 1,
            maxMana = 26, manaGain = 2, attackIntervalMs = 1120, attackImpactMs = 240,
            attackDurationMs = 840, skillImpactMs = 400, skillDurationMs = 1320,
            cooldownMs = 4500, skillMultiplier = 4f };
    }

    private static MythwakeCombatSession Session(MythwakeCombatSession.Hero hero = null, int duration = 60000)
    {
        return new MythwakeCombatSession(new[] { hero ?? Hero() }, 1000000, duration, 314159);
    }

    private static void ManualSkillCommitsOnceAtImpact()
    {
        var s = Session();
        Assert(!s.QueueSkill(7), "Skill must reject insufficient mana.");
        while (s.Heroes[0].mana < 26) s.Step(.01f);
        Assert(s.QueueSkill(7) && s.QueueSkill(7), "Repeated ready clicks may queue the same action.");
        while (!s.Events.Any(e => e.eventType == "action_start" && e.skillId == "hero_kael_ultimate")) s.Step(.01f);
        var start = s.Events.Last(e => e.eventType == "action_start");
        var damageBefore = s.DamageDealt;
        Assert(s.Heroes[0].mana == 0 && start.cooldownUntilMs == start.timeMs + 4500, "Mana and cooldown commit once at accepted start.");
        Assert(!s.QueueSkill(7), "An active skill cannot be queued again without mana.");
        while (s.TimeMs < start.timeMs + 390) s.Step(.01f);
        Assert(s.DamageDealt == damageBefore, "Skill must not deal damage during charge.");
        while (s.TimeMs < start.timeMs + 400) s.Step(.01f);
        var impacts = s.Events.Where(e => e.actionId == start.actionId && e.eventType == "ultimate").ToArray();
        Assert(impacts.Length == 1 && impacts[0].amount == 20 && impacts[0].contactIndex == 0,
            "The rising contact resolves 25 percent of the one 4x skill budget.");
        s.Step(.1f);
        Assert(s.Events.Count(e => e.actionId == start.actionId && e.eventType == "ultimate") == 1, "Later updates must not repeat an impact.");
        while (s.TimeMs < start.timeMs + 920) s.Step(.01f);
        impacts = s.Events.Where(e => e.actionId == start.actionId && e.eventType == "ultimate").ToArray();
        Assert(impacts.Length == 2 && impacts[1].amount == 60 && impacts[1].contactIndex == 1 && impacts.Sum(e => e.amount) == 80,
            "The final contact owns the remaining 75 percent; the two contacts remain one skill.");
        while (s.TimeMs < start.timeMs + 1320) s.Step(.01f);
        Assert(s.Events.Any(e => e.eventType == "action_start" && e.timeMs == start.timeMs + 1320 && string.IsNullOrEmpty(e.skillId)),
            "Kael resumes autos at the end of 1320ms skill recovery without another interval delay.");
        Assert(s.EnemyMaxHp - s.EnemyHp == s.DamageDealt, "Result damage must match authoritative health.");
    }

    private static void AutoAndFrameCadenceAgree()
    {
        var fine = Session(); var coarse = Session(); fine.AutoSkills = coarse.AutoSkills = true;
        while (!fine.Finished) fine.Step(.01f);
        while (!coarse.Finished) coarse.Step(.25f);
        Assert(fine.DamageDealt == coarse.DamageDealt && fine.TeamHp == coarse.TeamHp, "Presentation frame cadence cannot change outcome.");
        Assert(fine.Events.Count == coarse.Events.Count, "Different frame cadences must emit the same number of records.");
        for (var i = 0; i < fine.Events.Count; i++)
            Assert(fine.Events[i].timeMs == coarse.Events[i].timeMs && fine.Events[i].eventType == coarse.Events[i].eventType &&
                fine.Events[i].amount == coarse.Events[i].amount, "Every event must retain its simulation timestamp and amount.");
        Assert(fine.Events.Count(e => e.eventType == "ultimate") >= 2, "Automatic skills must survive repeated use.");
        Assert(fine.Events.Where(e => e.eventType == "auto_attack" || e.eventType == "critical_attack" || e.eventType == "ultimate").Sum(e => e.amount) == fine.DamageDealt,
            "No duplicate or unreported damage is allowed.");
    }

    private static void CancelAndDeathDiscardPendingHits()
    {
        var cancelled = Session(); cancelled.Step(.26f); cancelled.Cancel(); cancelled.Step(5f);
        Assert(cancelled.Events.Count(e => e.eventType == "auto_attack") == 0, "Cancel before impact must invalidate the pending hit.");
        var fragile = Hero(); fragile.id = "hero_astra"; fragile.maxHp = 1; fragile.attackImpactMs = 2000; fragile.attackDurationMs = 2500;
        var dead = Session(fragile); dead.Step(3f);
        Assert(dead.TeamHp == 0 && dead.DamageDealt == 0, "Dead actors cannot resolve a pending attack.");
        var count = dead.Events.Count; dead.Step(10f);
        Assert(dead.Events.Count == count, "A finished session must emit no late effects or damage records.");
    }

    private static void PausedStepDoesNotAdvance()
    {
        var s = Session(); s.Step(.26f); var time = s.TimeMs; var count = s.Events.Count;
        for (var i = 0; i < 120; i++) s.Step(0f);
        Assert(s.TimeMs == time && s.Events.Count == count, "Pause must freeze simulation and pending impacts.");
    }

    private static void SelectedHeroIdentityIsPreserved()
    {
        var s = Session(Hero(7)); s.Step(1f);
        Assert(s.Heroes.Count == 1 && s.TeamMaxHp == 10000, "Only supplied formation members contribute health.");
        Assert(!s.QueueSkill(0), "A benched hero cannot issue commands.");
        Assert(s.Events.Where(e => e.actorId != "enemy").All(e => e.actorId == "hero_kael" && e.actorIndex == 0),
            "Roster index seven remains hero_kael in ordered slot zero.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void ExistingExecuteAndWaveCancellationRemainCorrect()
    {
        var healer = Hero(0); healer.id = "hero_elowen"; healer.maxHp = 100;
        var ally = Hero(7); ally.maxHp = 100;
        var healing = new MythwakeCombatSession(new[] { healer, ally }, 100000, 30000, 12);
        healer.hp = ally.hp = 50; healer.mana = 26;
        Assert(healing.QueueSkill(0), "Ready healer skill should queue.");
        healing.Step(.4f);
        Assert(healer.hp == 68 && ally.hp == 68 && healing.HealingDone == 36,
            "Existing team healing must share its budget across selected heroes rather than pool it into the front target.");
        var legacy = Hero(); legacy.id = "hero_cyra"; legacy.legacyAttackIntervalJitter = true;
        var jittered = Session(legacy); while (!jittered.Finished) jittered.Step(.1f);
        var starts = jittered.Events.Where(e => e.eventType == "action_start").Select(e => e.timeMs).ToArray();
        var intervals = starts.Skip(1).Select((time, i) => time - starts[i]).ToArray();
        Assert(intervals.Distinct().Count() > 1 && intervals.All(ms => ms >= 1050 && ms <= 1220),
            "Legacy normal attack intervals retain the original 0.94-1.08 jitter, rounded to simulation ticks.");
        var hero = Hero(); hero.attack = 90;
        var execute = new MythwakeCombatSession(new[] { hero }, 100, 30000, 12) { ExecuteEnemy = (hp, max) => hp <= max * .12f };
        execute.Step(1f);
        Assert(execute.Executed && execute.DamageDealt == 100 && execute.Events.Single(e => e.eventType == "execute").amount == 10,
            "The existing team ranger execute must remain a distinct authoritative result.");
        var waves = new MythwakeCombatSession(new[] { Hero() }, 30, 30000, 12) { NextWaveHealth = wave => 100 };
        waves.Step(2f);
        Assert(waves.EnemiesDefeated == 1 && waves.Events.Count(e => e.eventType == "enemy_spawn") == 1,
            "A defeated wave must advance once and retain its earned count.");
        var damage = waves.DamageDealt; waves.Cancel(); waves.Step(30f);
        Assert(waves.EnemiesDefeated == 1 && waves.DamageDealt == damage,
            "Ending a rift preserves the actual earned waves without re-simulating rewards.");
    }

    private static void ServerReplayPreservesAuthorityAndOrder()
    {
        var start = new MythwakeCombatEventDto { timeMs = 100, actionStartMs = 100, actionId = "skill1", eventType = "action_start",
            actorId = "hero_kael", skillId = "hero_kael_ultimate", manaAfter = 0, cooldownUntilMs = 4600, teamHpRemaining = 50, enemyHpRemaining = 100 };
        var hit = new MythwakeCombatEventDto { timeMs = 500, actionStartMs = 100, actionId = "skill1", eventType = "ultimate",
            actorId = "hero_kael", amount = 100, teamHpRemaining = 70, enemyHpRemaining = 20 };
        var input = new[] { hit, start, hit };
        var replay = new MythwakeCombatReplay(new MythwakeCombatResultDto { teamMaxHp = 50, enemyMaxHp = 100, events = input });
        var delivered = new System.Collections.Generic.List<MythwakeCombatEventDto>();
        replay.AdvanceTo(100, (record, index) => delivered.Add(record));
        Assert(delivered.Count == 1 && delivered[0].eventType == "action_start" && replay.DamageDealt == 0,
            "Out-of-order input must start the skill without applying its future result.");
        replay.AdvanceTo(500, (record, index) => delivered.Add(record));
        Assert(delivered.Count == 2 && delivered[1].amount == 80 && replay.DamageDealt == 80 && replay.TeamHp == 70 && replay.EnemyHp == 20,
            "Start and impact share an ID; duplicate impacts are suppressed and healing is not displayed as damage.");
        replay.AdvanceTo(100, (record, index) => delivered.Add(record));
        replay.AdvanceTo(1000, (record, index) => delivered.Add(record));
        Assert(delivered.Count == 2 && input[0].amount == 100, "Clock rewinds cannot repeat events or mutate the server result.");
    }

    private static void BasicVariantsShareOneRollAndBudget()
    {
        foreach (var critical in new[] { false, true })
        {
            var hero = Hero(); hero.attack = 21; hero.criticalChance = critical ? 100 : 0;
            var session = Session(hero, 4000); session.Step(4f);
            var starts = session.Events.Where(e => e.eventType == "action_start").Take(3).ToArray();
            var variants = new[] { "attack_cross", "attack_spin", "attack_jump" };
            var offsets = new[] { new[] { 240, 520 }, new[] { 420 }, new[] { 460 } };
            var total = (int)Math.Round(21 * (critical ? 1.5f : 1f));
            for (var i = 0; i < 3; i++)
            {
                var contacts = session.Events.Where(e => e.actionId == starts[i].actionId && e.contactIndex >= 0).ToArray();
                Assert(starts[i].animationVariant == variants[i] && starts[i].actionDurationMs == 840 && contacts.Length == offsets[i].Length,
                    "The three variants follow the authoritative deterministic cycle and common recovery.");
                Assert(contacts.Sum(e => e.amount) == total && contacts.All(e => e.eventType == (critical ? "critical_attack" : "auto_attack")),
                    "Each basic has exactly one full damage/critical budget regardless of contact count.");
                Assert(contacts.Select(e => e.contactId).Distinct().Count() == contacts.Length &&
                    contacts.Select(e => e.timeMs - e.actionStartMs).SequenceEqual(offsets[i]), "Contacts have exact timestamps and unique IDs.");
                Assert(contacts.All(e => e.manaAfter == (i + 1) * 2), "Mana is granted once per successful basic, never once per contact.");
            }
            Assert(starts[1].timeMs - starts[0].timeMs == 1120 && starts[2].timeMs - starts[1].timeMs == 1120,
                "Choreography does not change the nominal basic interval.");
            Assert(session.CriticalHits == (critical ? session.Events.Count(e => e.eventType == "action_start") : 0),
                "Critical count describes actions, not visual contacts.");
        }
        var missedHero = Hero(); missedHero.accuracy = 0;
        var misses = Session(missedHero, 1000); misses.Step(1f);
        Assert(misses.Events.Count(e => e.eventType == "miss") == 2 && misses.MissedHits == 1 && misses.DamageDealt == 0 && missedHero.mana == 0,
            "A missed cross shares one accuracy roll and yields neither damage nor mana at either contact.");
    }

    private static void MultiContactReplayDeduplicatesContacts()
    {
        var source = Session(); source.AutoSkills = true; source.Step(60f);
        var input = source.Events.Concat(source.Events.Where(e => e.contactIndex >= 0)).OrderByDescending(e => e.timeMs).ToArray();
        var replay = new MythwakeCombatReplay(new MythwakeCombatResultDto {
            teamMaxHp = source.TeamMaxHp, enemyMaxHp = source.EnemyMaxHp, events = input });
        var delivered = new System.Collections.Generic.List<MythwakeCombatEventDto>();
        replay.AdvanceTo(60000, (record, index) => delivered.Add(record));
        replay.AdvanceTo(60000, (record, index) => delivered.Add(record));
        Assert(delivered.Count == source.Events.Count && replay.DamageDealt == source.DamageDealt && replay.DamageTaken == source.DamageTaken &&
            replay.EnemyHp == source.EnemyHp && replay.TeamHp == source.TeamHp,
            "Out-of-order duplicate delivery preserves every distinct contact exactly once and all HP sums.");
        var a = new MythwakeCombatEventDto { actionId = "a", eventType = "auto_attack", contactCount = 2, contactIndex = 0 };
        var b = a; b.contactIndex = 1;
        Assert(MythwakeCombatReplay.RecordKey(a, 0) != MythwakeCombatReplay.RecordKey(b, 1),
            "Contact metadata still deduplicates safely if an additive contactId was omitted.");
    }

    private static void PartialActionsCancelWithoutInventingRemainingDamage()
    {
        var cancelled = Session(); cancelled.Step(.5f); var first = cancelled.DamageDealt;
        cancelled.Cancel(); cancelled.Step(10f);
        Assert(first == 10 && cancelled.DamageDealt == first && cancelled.Events.Count(e => e.eventType == "auto_attack") == 1,
            "Cancellation between cross contacts retains only the already resolved share.");
        var hero = Hero(); hero.maxHp = 1;
        var dead = Session(hero); hero.mana = 26; dead.QueueSkill(7); dead.Step(3f);
        Assert(dead.TeamHp == 0 && dead.DamageDealt == 20 && dead.Events.Count(e => e.eventType == "ultimate") == 1,
            "Death after the rising skill contact cancels the dominant contact at 920ms.");
        var winner = new MythwakeCombatSession(new[] { Hero() }, 5, 60000, 8); winner.Step(3f);
        Assert(winner.Finished && winner.DamageDealt == 5 && winner.Events.Count(e => e.eventType == "auto_attack") == 1,
            "An early killing contact ends the action without applying or reporting the remaining budget.");
        var waves = new MythwakeCombatSession(new[] { Hero() }, 5, 30000, 9) { NextWaveHealth = wave => 100 };
        waves.Step(1f);
        Assert(waves.EnemiesDefeated == 1 && waves.EnemyHp == 100 && waves.DamageDealt == 5,
            "A second contact cannot migrate to a replacement wave target.");
    }

    public static void VerifyServerFixture(MythwakeCombatResultDto result)
    {
        Assert(result.events != null && result.events.Length > 180 && result.heroes != null,
            "The actual Go fixture must cover a complete long replay and ordered participants.");
        var kael = Array.FindIndex(result.heroes, h => h.heroId == "hero_kael");
        Assert(kael > 0, "The fixture must exercise Kael in a reordered participant slot.");
        var input = result.events.Concat(result.events.Where(e => e.actorId == "hero_kael"))
            .OrderByDescending(e => e.timeMs).ToArray();
        var replayResult = result; replayResult.events = input;
        var replay = new MythwakeCombatReplay(replayResult);
        var delivered = new System.Collections.Generic.List<MythwakeCombatEventDto>();
        for (var time = 0; time <= result.maxSeconds * 1000; time += 10)
            replay.AdvanceTo(time, (record, index) => delivered.Add(record));
        Assert(replay.Finished && delivered.Count == result.events.Length && replay.DamageDealt == result.damageDealt &&
            replay.DamageTaken == result.damageTaken && replay.TeamHp == result.teamHpRemaining && replay.EnemyHp == result.enemyHpRemaining,
            "Real Go JSON must replay every contact once with exact server-owned totals and snapshots.");
        var kaelRecords = delivered.Where(e => e.actorId == "hero_kael").ToArray();
        Assert(kaelRecords.All(e => e.actorIndex == kael), "Reordered participant IDs must survive wire transfer without roster-index assumptions.");
        var starts = kaelRecords.Where(e => e.eventType == "action_start").ToArray();
        Assert(starts.Count(e => e.animationVariant == "skill") >= 2 &&
            new[] { "attack_cross", "attack_spin", "attack_jump" }.All(v => starts.Any(e => e.animationVariant == v)),
            "The server fixture must contain all basics and repeated two-contact skills.");
        foreach (var start in starts)
        {
            var expectedOffsets = start.animationVariant == "skill" ? new[] { 400, 920 } :
                start.animationVariant == "attack_cross" ? new[] { 240, 520 } :
                start.animationVariant == "attack_spin" ? new[] { 420 } : new[] { 460 };
            foreach (var contact in kaelRecords.Where(e => e.actionId == start.actionId && e.eventType != "action_start"))
                Assert(contact.contactCount == expectedOffsets.Length && contact.contactIndex >= 0 &&
                    contact.contactIndex < expectedOffsets.Length && !string.IsNullOrEmpty(contact.contactId) &&
                    contact.contactTimeMs == expectedOffsets[contact.contactIndex] &&
                    contact.timeMs == start.timeMs + expectedOffsets[contact.contactIndex],
                    "Go contact timing and IDs must match the Unity animation profile exactly.");
        }
    }

#if MYTHWAKE_COMBAT_STANDALONE
    public static void Main() { Run(); Console.WriteLine("PASS: all 10 Kael combat authority test groups"); }
#endif
}
