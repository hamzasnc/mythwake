using System;
using System.Collections.Generic;

/// <summary>Local combat authority. Presentation consumes its records and never applies damage.</summary>
public sealed class MythwakeCombatSession
{
    public const int KaelAttackDurationMs = 840;
    public const int KaelSkillDurationMs = 1320;

    public sealed class Hero
    {
        public string id;
        public int rosterIndex, maxHp, attack, accuracy, criticalChance, incomingDamage;
        public int maxMana = 28, manaGain = 2, attackIntervalMs = 1120;
        public int attackImpactMs = 200, attackDurationMs = 600;
        public int skillImpactMs = 400, skillDurationMs = 1000, cooldownMs = 4500;
        public float skillMultiplier = 4.8f;
        public bool legacyAttackIntervalJitter;
        public int hp, mana, cooldownUntilMs, nextAttackMs, busyUntilMs;
        internal bool skillQueued;
        internal int basicActionCount;
    }

    private sealed class Pending
    {
        public Hero hero;
        public int startMs, impactMs, durationMs, contactIndex, totalDamage;
        public int[] contacts;
        public string actionId, animationVariant;
        public bool skill, rolled, missed, critical, manaGranted;
    }

    public readonly List<Hero> Heroes;
    public readonly List<MythwakeCombatEventDto> Events = new List<MythwakeCombatEventDto>();
    private readonly List<Pending> pending = new List<Pending>();
    private readonly Random random;
    private int sequence, nextEnemyAttackMs = 900, nextTickMs = 0;
    private double requestedMs;
    public int TimeMs { get; private set; }
    public int EnemyMaxHp { get; private set; }
    public int EnemyHp { get; private set; }
    public int TeamMaxHp { get; private set; }
    public int DamageDealt { get; private set; }
    public int DamageTaken { get; private set; }
    public int HealingDone { get; private set; }
    public int CriticalHits { get; private set; }
    public int MissedHits { get; private set; }
    public int MaxTimeMs { get; private set; }
    public int EnemiesDefeated { get; private set; }
    public bool Cancelled { get; private set; }
    public bool Executed { get; private set; }
    public bool AutoSkills;
    public Func<int, bool> CanAct;
    public Func<int, int, bool> ExecuteEnemy;
    public Func<int, int> NextWaveHealth;
    public Func<int, Hero, int> NextWaveIncomingDamage;
    public bool Finished => Cancelled || TeamHp <= 0 || TimeMs >= MaxTimeMs || (EnemyHp <= 0 && NextWaveHealth == null);
    public int TeamHp { get { var hp = 0; foreach (var h in Heroes) hp += Math.Max(0, h.hp); return hp; } }

    public MythwakeCombatSession(IEnumerable<Hero> heroes, int enemyHp, int maxTimeMs, int seed)
    {
        Heroes = new List<Hero>(heroes);
        EnemyHp = EnemyMaxHp = Math.Max(1, enemyHp);
        MaxTimeMs = Math.Max(1, maxTimeMs);
        random = new Random(seed);
        for (var i = 0; i < Heroes.Count; i++)
        {
            var h = Heroes[i];
            h.hp = Math.Max(1, h.maxHp);
            h.mana = 0;
            h.nextAttackMs = 250 + i * 170;
            h.cooldownUntilMs = h.busyUntilMs = 0;
            h.skillQueued = false;
            h.basicActionCount = 0;
            TeamMaxHp += h.hp;
        }
    }

    public bool QueueSkill(int rosterIndex)
    {
        if (Finished) return false;
        var h = Heroes.Find(hero => hero.rosterIndex == rosterIndex);
        if (h == null || h.hp <= 0 || h.mana < h.maxMana || TimeMs < h.cooldownUntilMs) return false;
        h.skillQueued = true;
        return true;
    }

    public void Cancel() { Cancelled = true; pending.Clear(); foreach (var h in Heroes) h.skillQueued = false; }

    /// <summary>Fixed simulation steps give identical outcomes across presentation frame rates.</summary>
    public void Step(float deltaSeconds)
    {
        if (Finished || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds <= 0f) return;
        requestedMs += deltaSeconds * 1000.0;
        while (!Finished && nextTickMs <= requestedMs + 0.001)
        {
            TimeMs = Math.Min(nextTickMs, MaxTimeMs);
            ResolvePending();
            if (Finished) break;
            foreach (var h in Heroes)
            {
                if (h.hp <= 0 || TimeMs < h.busyUntilMs || (CanAct != null && !CanAct(h.rosterIndex))) continue;
                var skill = (AutoSkills || h.skillQueued) && h.mana >= h.maxMana && TimeMs >= h.cooldownUntilMs;
                if (!skill && TimeMs < h.nextAttackMs) continue;
                BeginAction(h, skill);
            }
            if (TimeMs >= nextEnemyAttackMs && TeamHp > 0)
            {
                var target = Heroes.Find(h => h.hp > 0);
                var amount = Math.Min(target.hp, Math.Max(1, target.incomingDamage));
                target.hp -= amount;
                DamageTaken += amount;
                Events.Add(new MythwakeCombatEventDto { timeMs = TimeMs, actionStartMs = TimeMs,
                    actionId = "local_enemy_" + (++sequence), eventType = "enemy_attack", actorId = "enemy",
                    targetId = target.id, targetIndex = target.rosterIndex, amount = amount,
                    teamHpRemaining = TeamHp, enemyHpRemaining = EnemyHp });
                nextEnemyAttackMs = TimeMs + 1450;
                if (target.hp <= 0) { target.skillQueued = false; pending.RemoveAll(p => p.hero == target); }
            }
            nextTickMs += 10;
        }
        if (Finished) pending.Clear();
    }

    private void BeginAction(Hero h, bool skill)
    {
        var kael = h.id == "hero_kael";
        var variant = kael && !skill ? h.basicActionCount++ % 3 : -1;
        var contacts = kael ? (skill ? new[] { 400, 920 } : variant == 0 ? new[] { 240, 520 } :
            variant == 1 ? new[] { 420 } : new[] { 460 }) : new[] { skill ? h.skillImpactMs : h.attackImpactMs };
        var action = new Pending { hero = h, startMs = TimeMs, contacts = contacts,
            impactMs = TimeMs + contacts[0], durationMs = kael ? (skill ? KaelSkillDurationMs : KaelAttackDurationMs) :
                (skill ? h.skillDurationMs : h.attackDurationMs),
            animationVariant = kael ? (skill ? "skill" : variant == 0 ? "attack_cross" : variant == 1 ? "attack_spin" : "attack_jump") : "",
            actionId = "local_" + (++sequence), skill = skill };
        if (skill) { h.mana = 0; h.cooldownUntilMs = TimeMs + h.cooldownMs; h.skillQueued = false; }
        h.busyUntilMs = TimeMs + action.durationMs;
        var interval = h.attackIntervalMs;
        if (!skill && h.legacyAttackIntervalJitter)
            interval = Math.Max(1, (int)Math.Round(interval * (0.94 + random.NextDouble() * 0.14)));
        h.nextAttackMs = skill && h.id == "hero_kael"
            ? Math.Max(h.nextAttackMs, h.busyUntilMs)
            : TimeMs + interval;
        pending.Add(action);
        Emit(action, "action_start", 0);
    }

    private void ResolvePending()
    {
        for (var i = 0; i < pending.Count;)
        {
            var p = pending[i];
            if (p.impactMs > TimeMs) { i++; continue; }
            var h = p.hero;
            if (h.hp <= 0 || EnemyHp <= 0) { pending.RemoveAt(i); continue; }
            if (!p.rolled)
            {
                p.rolled = true;
                p.missed = !p.skill && random.Next(100) >= h.accuracy;
                p.critical = !p.skill && !p.missed && random.Next(100) < h.criticalChance;
                var multiplier = p.skill ? h.skillMultiplier : p.critical ? 1.5f : 1f;
                p.totalDamage = p.missed ? 0 : Math.Max(1, (int)Math.Round(h.attack * multiplier));
                if (p.missed) MissedHits++;
                if (p.critical) CriticalHits++;
            }
            // The first share rounds down; the final share owns the integer remainder.
            var firstShare = p.totalDamage / (p.skill ? 4 : 2);
            var budget = p.contacts.Length == 1 ? p.totalDamage : p.contactIndex == 0 ? firstShare : p.totalDamage - firstShare;
            var amount = Math.Min(EnemyHp, budget);
            EnemyHp -= amount;
            DamageDealt += amount;
            if (!p.skill && !p.missed && !p.manaGranted)
            { h.mana = Math.Min(h.maxMana, h.mana + h.manaGain); p.manaGranted = true; }
            Emit(p, p.missed ? "miss" : p.skill ? "ultimate" : p.critical ? "critical_attack" : "auto_attack", amount);
            // Legacy healing hooks run once per action. Threshold execution still
            // reacts to the actual HP after a contact and ends that target immediately.
            var finalContact = p.contactIndex == p.contacts.Length - 1;
            if (!p.missed && EnemyHp > 0 && ExecuteEnemy != null && ExecuteEnemy(EnemyHp, EnemyMaxHp))
            {
                var executedDamage = EnemyHp;
                EnemyHp = 0;
                DamageDealt += executedDamage;
                Executed = true;
                Emit(p, "execute", executedDamage);
            }
            // Retain existing healer identity behavior without giving the new warrior a passive.
            if (finalContact && !p.missed && h.id == "hero_elowen" && (p.skill || random.Next(100) < 25))
                Heal(p, p.skill ? 0.18f : 0.035f);
            if (finalContact && p.skill && h.id == "hero_borin") Heal(p, 0.06f);
            if (EnemyHp <= 0)
            {
                EnemiesDefeated++;
                if (NextWaveHealth == null) break;
                EnemyHp = EnemyMaxHp = Math.Max(1, NextWaveHealth(EnemiesDefeated + 1));
                foreach (var hero in Heroes)
                    if (NextWaveIncomingDamage != null) hero.incomingDamage = NextWaveIncomingDamage(EnemiesDefeated + 1, hero);
                Events.Add(new MythwakeCombatEventDto { timeMs = TimeMs, eventType = "enemy_spawn", actorId = "enemy",
                    amount = EnemyMaxHp, enemyHpRemaining = EnemyHp, teamHpRemaining = TeamHp });
                // Pending actions belong to the defeated wave, not its replacement instance.
                pending.Clear();
                break;
            }
            if (finalContact) pending.RemoveAt(i);
            else { p.contactIndex++; p.impactMs = p.startMs + p.contacts[p.contactIndex]; i++; }
        }
    }

    private void Heal(Pending p, float fraction)
    {
        var total = Math.Max(1, (int)(TeamMaxHp * fraction));
        var share = Math.Max(1, (int)Math.Ceiling(total / (double)Math.Max(1, Heroes.Count)));
        var healed = 0;
        foreach (var h in Heroes)
        {
            if (h.hp <= 0) continue;
            var amount = Math.Min(h.maxHp - h.hp, share);
            h.hp += amount; healed += amount;
        }
        HealingDone += healed;
        if (healed > 0) Emit(p, "passive_heal", healed);
    }

    private void Emit(Pending p, string type, int amount)
    {
        Events.Add(new MythwakeCombatEventDto { timeMs = TimeMs, actionStartMs = p.startMs, actionId = p.actionId,
            animationVariant = p.animationVariant, actionDurationMs = p.durationMs,
            contactIndex = type == "action_start" ? -1 : p.contactIndex, contactCount = p.contacts.Length,
            contactTimeMs = p.contacts[p.contactIndex], contactId = type == "action_start" ? "" : p.actionId + ":contact:" + p.contactIndex,
            eventType = type, actorId = p.hero.id, actorIndex = Heroes.IndexOf(p.hero), targetId = "enemy", targetIndex = 0,
            skillId = p.skill ? (p.hero.id == "hero_kael" ? "hero_kael_ultimate" : p.hero.id + "_ultimate") : "",
            amount = amount, manaAfter = p.hero.mana, cooldownUntilMs = p.hero.cooldownUntilMs,
            teamHpRemaining = TeamHp, enemyHpRemaining = EnemyHp });
    }
}
