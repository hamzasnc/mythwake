using System.Collections.Generic;
using Mythwake.Core.Content;
using Mythwake.Core.Math;

namespace Mythwake.Core.Combat
{
    /// <summary>
    /// Deterministic battle simulation. Integer math, 50 ms steps, seeded RNG.
    /// The same <see cref="BattleSetup"/> always produces the same <see cref="BattleResult"/>.
    /// </summary>
    public sealed class BattleSimulator
    {
        public const int StepMs = 50;
        public const int MaxEnergy = 1000;
        public const int EnergyPerCast = 100;
        public const int EnergyPerHitTaken = 40;
        public const int EnergyPerKill = 150;
        public const int AdvantageDamage = 250;
        public const int AdvantageAccuracy = 150;
        public const int DisadvantageDamage = -150;

        private sealed class ScheduledHit
        {
            public int TimeMs;
            public int Order;
            public BattleUnit Caster;
            public SkillDefinition Skill;
            public List<BattleUnit> Targets;
            public int HitIndex;
        }

        private readonly BattleSetup _setup;
        private readonly Pcg32 _rng;
        private readonly List<BattleUnit> _units = new List<BattleUnit>();
        private readonly List<ScheduledHit> _pending = new List<ScheduledHit>();
        private readonly List<int> _requestCursor = new List<int>();
        private readonly BattleResult _result = new BattleResult();
        private int _scheduleOrder;
        private int _now;

        public BattleSimulator(BattleSetup setup)
        {
            _setup = setup;
            _rng = new Pcg32(setup.Seed);
        }

        public static BattleResult Run(BattleSetup setup) => new BattleSimulator(setup).Run();

        public BattleResult Run()
        {
            Spawn(_setup.Allies, Side.Allies);
            Spawn(_setup.Enemies, Side.Enemies);
            DistributePassives();
            Emit(BattleEventType.BattleStart, -1, -1, SkillSlot.Normal, 0);
            ApplyBattleStartPassives();

            var maxMs = _setup.Rules.MaxDurationMs;
            for (_now = 0; _now <= maxMs; _now += StepMs)
            {
                ResolvePendingHits();
                TickStatuses();
                if (CheckEnd())
                    break;
                foreach (var u in _units)
                    if (u.Alive)
                        TryAct(u);
                if (CheckEnd())
                    break;
            }

            var victory = AliveCount(Side.Enemies) == 0 && AliveCount(Side.Allies) > 0;
            _result.Outcome = victory ? BattleOutcome.Victory : BattleOutcome.Defeat;
            _result.DurationMs = System.Math.Min(_now, maxMs);
            _result.AlliesAlive = AliveCount(Side.Allies);
            Emit(BattleEventType.BattleEnd, -1, -1, SkillSlot.Normal, victory ? 1 : 0);
            foreach (var u in _units)
            {
                _result.Units.Add(new UnitSummary
                {
                    Index = u.Index, Side = u.Side, Slot = u.Slot, HeroId = u.Hero.Id, MaxHp = u.MaxHp,
                    FinalHp = u.Hp, DamageDealt = u.DamageDealt, DamageTaken = u.DamageTaken, Healing = u.Healing
                });
            }
            return _result;
        }

        // ------------------------------------------------------------------ setup

        private void Spawn(List<UnitSetup> team, Side side)
        {
            foreach (var s in team)
            {
                var u = new BattleUnit
                {
                    Index = _units.Count, Side = side, Slot = s.Slot, Hero = s.Hero, Stats = s.Stats.Clone()
                };
                u.MaxHp = u.Hp = System.Math.Max(1, u.Stats.Hp);
                u.Passives.AddRange(s.Hero.PassiveComponents());
                if (s.Passives != null)
                    u.Passives.AddRange(s.Passives);
                // stagger first attacks a little so a team does not swing in perfect unison
                u.NextNormalMs = 300 + s.Slot * 90 + (side == Side.Enemies ? 150 : 0);
                foreach (var skill in s.Hero.Skills())
                {
                    var t = skill.Trigger;
                    u.ReadyAtMs[skill.Slot] = t.Kind == TriggerKind.Cooldown
                        ? (t.InitialDelayMs > 0 ? t.InitialDelayMs : t.CooldownMs / 2)
                        : 0;
                }
                _units.Add(u);
                if (side == Side.Allies)
                    _requestCursor.Add(0);
            }
        }

        private static bool IsTriggered(PassiveKind k) =>
            k == PassiveKind.Periodic || k == PassiveKind.BattleStart || k == PassiveKind.LeaderAura;

        private void DistributePassives()
        {
            foreach (var owner in _units)
            {
                foreach (var p in owner.Passives)
                {
                    if (p.Kind == PassiveKind.Periodic)
                        owner.NextPeriodicMs[p] = p.IntervalMs > 0 ? p.IntervalMs : 10_000;
                    if (IsTriggered(p.Kind))
                        continue;
                    if (!p.Team)
                    {
                        owner.Modifiers.Add(new Modifier(p, owner));
                        continue;
                    }
                    foreach (var ally in _units)
                        if (ally.Side == owner.Side)
                            ally.Modifiers.Add(new Modifier(p, owner));
                }
            }
        }

        private void ApplyBattleStartPassives()
        {
            foreach (var owner in _units)
                foreach (var p in owner.Passives)
                    if (p.Kind == PassiveKind.BattleStart && p.Effect != null)
                        foreach (var target in SelectTargets(owner, p.Target ?? TargetSelector.Self, 1))
                            ApplyPassiveEffect(owner, target, p.Effect);
        }

        // ------------------------------------------------------------------ main loop

        private bool CheckEnd() => AliveCount(Side.Allies) == 0 || AliveCount(Side.Enemies) == 0;

        private int AliveCount(Side side)
        {
            var n = 0;
            foreach (var u in _units)
                if (u.Side == side && u.Alive)
                    n++;
            return n;
        }

        private void TryAct(BattleUnit u)
        {
            if (_now < u.BusyUntilMs || u.Disabled)
                return;

            if (!u.StartSkillsDone)
            {
                u.StartSkillsDone = true;
                foreach (var skill in u.Hero.Skills())
                    if (skill.Trigger.Kind == TriggerKind.BattleStart)
                    {
                        Cast(u, skill);
                        return;
                    }
            }

            if (u.Energy >= MaxEnergy && u.Hero.Ultimate != null && UltimateAllowed(u))
            {
                Cast(u, u.Hero.Ultimate);
                return;
            }

            foreach (var skill in Specials(u))
            {
                if (u.ReadyAtMs[skill.Slot] > _now)
                    continue;
                var t = skill.Trigger;
                var ready = t.Kind switch
                {
                    TriggerKind.Cooldown => true,
                    TriggerKind.BattleStart => t.CooldownMs > 0,
                    TriggerKind.SelfBelowHp => Below(u, t.HpThresholdPermille),
                    TriggerKind.AllyBelowHp => AnyBelow(u.Side, t.HpThresholdPermille),
                    TriggerKind.EnemyBelowHp => AnyBelow(Opposite(u.Side), t.HpThresholdPermille),
                    _ => false
                };
                if (ready)
                {
                    Cast(u, skill);
                    return;
                }
            }

            if (_now >= u.NextNormalMs && u.Hero.Normal != null)
            {
                u.AttackCount++;
                var skill = u.Hero.Normal;
                foreach (var proc in Specials(u))
                {
                    if (u.ReadyAtMs[proc.Slot] > _now)
                        continue;
                    var t = proc.Trigger;
                    if ((t.Kind == TriggerKind.ChanceOnAttack && _rng.Chance(t.ChancePermille))
                        || (t.Kind == TriggerKind.EveryNthAttack && t.EveryN > 0 && u.AttackCount % t.EveryN == 0))
                    {
                        skill = proc;
                        break;
                    }
                }
                Cast(u, skill);
                u.NextNormalMs = _now + AttackInterval(u);
            }
        }

        private static IEnumerable<SkillDefinition> Specials(BattleUnit u)
        {
            if (u.Hero.Special != null) yield return u.Hero.Special;
            if (u.Hero.Special2 != null) yield return u.Hero.Special2;
        }

        private static Side Opposite(Side s) => s == Side.Allies ? Side.Enemies : Side.Allies;

        private static bool Below(BattleUnit u, int permille) => u.Hp * Permille.One < u.MaxHp * permille;

        private bool AnyBelow(Side side, int permille)
        {
            foreach (var a in _units)
                if (a.Side == side && a.Alive && Below(a, permille))
                    return true;
            return false;
        }

        private bool UltimateAllowed(BattleUnit u)
        {
            if (u.Side == Side.Enemies || AutoAt(_now))
                return true;
            // allies are spawned first, so an ally's unit index is also its request cursor index
            var requests = _setup.UltimateRequests;
            for (var i = _requestCursor[u.Index]; i < requests.Count; i++)
            {
                var r = requests[i];
                if (r.Slot == u.Slot && r.TimeMs <= _now)
                {
                    _requestCursor[u.Index] = i + 1;
                    return true;
                }
            }
            return false;
        }

        /// <summary>The Auto toggle's state at a battle time: the starting value flipped once per toggle so far.</summary>
        private bool AutoAt(int timeMs)
        {
            var auto = _setup.AutoUltimates;
            foreach (var t in _setup.AutoToggleTimesMs)
                if (t <= timeMs)
                    auto = !auto;
            return auto;
        }

        private int AttackInterval(BattleUnit u)
        {
            var speed = u.StatusValue(StatusKind.AttackSpeedUp) - u.StatusValue(StatusKind.Slow) + u.Sum(PassiveKind.AttackSpeed, _now);
            var stacking = u.First(PassiveKind.StackingAttackSpeed);
            if (stacking != null)
                speed += stacking.Value * u.StackCount;
            speed = Permille.Clamp(speed, -600, 2000);
            return System.Math.Max(400, u.Stats.AttackIntervalMs * Permille.One / (Permille.One + speed));
        }

        // ------------------------------------------------------------------ casting

        private void Cast(BattleUnit caster, SkillDefinition skill)
        {
            var targets = SelectTargets(caster, skill.Target, skill.TargetCount);
            caster.BusyUntilMs = _now + skill.DurationMs;
            caster.BusySlot = skill.Slot;
            var t = skill.Trigger;
            if (t.OncePerBattle)
                caster.ReadyAtMs[skill.Slot] = int.MaxValue;
            else if (t.Kind != TriggerKind.AttackInterval && t.Kind != TriggerKind.Energy)
                caster.ReadyAtMs[skill.Slot] = _now + System.Math.Max(t.CooldownMs, 1000);

            Emit(BattleEventType.ActionStart, caster.Index, targets.Count > 0 ? targets[0].Index : -1, skill.Slot, 0,
                durationMs: skill.DurationMs, animation: skill.Animation);

            if (skill.Slot == SkillSlot.Ultimate)
            {
                SetEnergy(caster, 0);
                foreach (var p in caster.Passives)
                    if (p.Kind == PassiveKind.OnUltimate && p.Effect != null)
                        foreach (var target in SelectTargets(caster, p.Target ?? TargetSelector.AllyAll, 1))
                            ApplyPassiveEffect(caster, target, p.Effect);
            }
            else
                AddEnergy(caster, EnergyPerCast * (Permille.One + caster.Stats.EnergyRegen) / Permille.One);

            var hits = System.Math.Max(1, skill.Hits);
            for (var h = 0; h < hits; h++)
            {
                var at = skill.HitTimesMs != null && h < skill.HitTimesMs.Length ? skill.HitTimesMs[h] : skill.DurationMs / 3;
                _pending.Add(new ScheduledHit
                {
                    TimeMs = _now + at, Order = _scheduleOrder++, Caster = caster, Skill = skill, Targets = targets, HitIndex = h
                });
            }
        }

        /// <summary>Cancels the unit's running non-ultimate action (stun, sleep, interrupt).</summary>
        private void CancelAction(BattleUnit u)
        {
            if (u.BusyUntilMs <= _now || u.BusySlot == SkillSlot.Ultimate)
                return;
            _pending.RemoveAll(h => h.Caster == u);
            u.BusyUntilMs = _now;
        }

        private void ResolvePendingHits()
        {
            if (_pending.Count == 0)
                return;
            _pending.Sort((a, b) => a.TimeMs != b.TimeMs ? a.TimeMs.CompareTo(b.TimeMs) : a.Order.CompareTo(b.Order));
            var due = 0;
            while (due < _pending.Count && _pending[due].TimeMs <= _now)
                due++;
            if (due == 0)
                return;
            var batch = _pending.GetRange(0, due);
            _pending.RemoveRange(0, due);
            foreach (var hit in batch)
                ResolveHit(hit);
        }

        private void ResolveHit(ScheduledHit hit)
        {
            var caster = hit.Caster;
            if (!caster.Alive)
                return;
            var skill = hit.Skill;
            var hits = System.Math.Max(1, skill.Hits);
            var portion = HitPortion(hit.HitIndex, hits);
            var last = hit.HitIndex == hits - 1 && hits > 1;

            foreach (var effect in skill.Effects)
            {
                List<BattleUnit> targets;
                if (effect.Target.HasValue)
                {
                    targets = SelectTargets(caster, effect.Target.Value, skill.TargetCount);
                }
                else
                {
                    if (AllDead(hit.Targets))
                        hit.Targets = SelectTargets(caster, skill.Target, skill.TargetCount);
                    targets = hit.Targets;
                }
                foreach (var target in targets)
                    if (target.Alive)
                        ApplyEffect(caster, target, skill, effect, portion, hit.HitIndex, last);
            }
        }

        private static bool AllDead(List<BattleUnit> list)
        {
            foreach (var u in list)
                if (u.Alive)
                    return false;
            return true;
        }

        /// <summary>Share of a multi-hit skill dealt by one hit (permille). The last hit carries 40 %.</summary>
        public static int HitPortion(int index, int hits)
        {
            if (hits <= 1)
                return Permille.One;
            if (index == hits - 1)
                return 400;
            return 600 / (hits - 1);
        }

        private void ApplyPassiveEffect(BattleUnit owner, BattleUnit target, EffectDefinition e)
        {
            if (target.Alive)
                ApplyEffect(owner, target, null, e, Permille.One, 0, false);
        }

        private void ApplyEffect(BattleUnit caster, BattleUnit target, SkillDefinition skill, EffectDefinition e, int portion, int hitIndex, bool last)
        {
            var slot = skill?.Slot ?? SkillSlot.Passive;
            // non-damage effects apply once per skill (on the first hit) and roll their chance per target
            if (e.Kind != EffectKind.Damage && e.Kind != EffectKind.Heal && hitIndex != 0)
                return;
            if (e.Chance < Permille.One && !_rng.Chance(e.Chance))
                return;

            switch (e.Kind)
            {
                case EffectKind.Damage:
                    DealDamage(caster, target, skill, e, portion, hitIndex, last);
                    break;
                case EffectKind.Heal:
                    Heal(caster, target, Base(caster, target, e) * portion / Permille.One, slot);
                    break;
                case EffectKind.Shield:
                    GrantShield(caster, target, Base(caster, target, e) * (Permille.One + caster.Stats.HealPower) / Permille.One, e.ShieldDurationMs, slot);
                    break;
                case EffectKind.ApplyStatus:
                    TryApplyStatus(caster, target, e.Status, slot);
                    break;
                case EffectKind.GainEnergy:
                    AddEnergy(target, e.Multiplier);
                    break;
                case EffectKind.DrainEnergy:
                    AddEnergy(target, -e.Multiplier);
                    break;
                case EffectKind.Cleanse:
                    Cleanse(target, e.Multiplier);
                    break;
                case EffectKind.StealBuff:
                    StealBuffs(caster, target, System.Math.Max(1, e.Multiplier));
                    break;
                case EffectKind.DelayAction:
                    Delay(target, e.Multiplier);
                    break;
                case EffectKind.Interrupt:
                    CancelAction(target);
                    break;
            }
        }

        private long Base(BattleUnit caster, BattleUnit target, EffectDefinition e)
        {
            var source = e.Scaling switch
            {
                Scaling.CasterMaxHp => caster.MaxHp,
                Scaling.TargetMaxHp => target.MaxHp,
                _ => EffectiveAttack(caster)
            };
            return Permille.Apply(source, e.Multiplier);
        }

        private void Delay(BattleUnit u, int ms)
        {
            u.NextNormalMs = System.Math.Max(u.NextNormalMs, _now) + ms;
            foreach (var skill in Specials(u))
                if (skill.Trigger.Kind == TriggerKind.Cooldown && u.ReadyAtMs[skill.Slot] != int.MaxValue)
                    u.ReadyAtMs[skill.Slot] = System.Math.Max(u.ReadyAtMs[skill.Slot], _now) + ms;
        }

        // ------------------------------------------------------------------ damage

        private long EffectiveAttack(BattleUnit u)
        {
            var bonus = u.StatusValue(StatusKind.AttackUp) - u.StatusValue(StatusKind.AttackDown) + u.Sum(PassiveKind.DamageBonus, _now);
            return System.Math.Max(1, u.Stats.Attack * System.Math.Max(100, Permille.One + bonus) / Permille.One);
        }

        private static int VsGroupBonus(BattleUnit caster, BattleUnit target)
        {
            var bonus = 0;
            foreach (var m in caster.Modifiers)
            {
                var p = m.Passive;
                if (p.Kind != PassiveKind.DamageVsGroup || !caster.IsActive(m))
                    continue;
                var h = target.Hero;
                var match = h.Group == p.VsGroup ||
                            (p.VsGroup == EnemyGroup.Hollowed && (h.Race == Race.Hollowed || h.Faction == Faction.TheHollowed));
                if (match)
                    bonus += p.Value;
            }
            return bonus;
        }

        public static int AspectRelation(Aspect attacker, Aspect defender)
        {
            if (((int)attacker + 1) % 5 == (int)defender)
                return 1;
            if (((int)defender + 1) % 5 == (int)attacker)
                return -1;
            return 0;
        }

        private void DealDamage(BattleUnit caster, BattleUnit target, SkillDefinition skill, EffectDefinition e, int portion, int hitIndex, bool last)
        {
            var slot = skill?.Slot ?? SkillSlot.Passive;
            var flags = last ? HitFlags.LastHit : 0;
            var relation = AspectRelation(caster.Hero.Aspect, target.Hero.Aspect);
            if (relation > 0) flags |= HitFlags.Advantage;
            if (relation < 0) flags |= HitFlags.Disadvantage;

            var evade = target.Find(StatusKind.Evade);
            if (evade != null)
            {
                if (--evade.Value <= 0)
                    RemoveStatus(target, StatusKind.Evade);
                Emit(BattleEventType.Dodge, caster.Index, target.Index, slot, 0, hitIndex: hitIndex);
                return;
            }

            // accuracy vs evasion: 100 % when equal, never below 20 %
            var accuracy = caster.Stats.Accuracy - caster.StatusValue(StatusKind.AccuracyDown) + (relation > 0 ? AdvantageAccuracy : 0);
            var hitChance = Permille.Clamp(accuracy * Permille.One / System.Math.Max(1, target.Stats.Evasion), 200, 1000);
            if (!_rng.Chance(hitChance))
            {
                Emit(BattleEventType.Miss, caster.Index, target.Index, slot, 0, hitIndex: hitIndex);
                return;
            }
            var dodge = target.Sum(PassiveKind.Dodge, _now);
            if (dodge > 0 && _rng.Chance(dodge))
            {
                Emit(BattleEventType.Dodge, caster.Index, target.Index, slot, 0, hitIndex: hitIndex);
                return;
            }

            var amount = Base(caster, target, e) * portion / Permille.One;
            var bonus = 0;
            if (e.ExecuteBonus > 0 && Below(target, e.ExecuteBelowHp))
            {
                bonus += e.ExecuteBonus;
                flags |= HitFlags.Execute;
            }
            if (e.BonusVsStatus.HasValue && target.Has(e.BonusVsStatus.Value))
                bonus += e.BonusVsStatusPermille;
            if (e.MissingHpBonus > 0)
                bonus += (int)(e.MissingHpBonus * (caster.MaxHp - caster.Hp) / caster.MaxHp);
            if (slot == SkillSlot.Ultimate)
                bonus += caster.Sum(PassiveKind.UltimateDamage, _now);
            bonus += VsGroupBonus(caster, target);
            amount = amount * (Permille.One + bonus) / Permille.One;

            var defenseScale = Permille.One + target.StatusValue(StatusKind.DefenseUp) - target.StatusValue(StatusKind.DefenseDown);
            var defense = System.Math.Max(0, target.Stats.Defense * defenseScale / Permille.One);
            amount = amount * 1000 / (1000 + defense);

            var critChance = Permille.Clamp(caster.Stats.CritRate + caster.StatusValue(StatusKind.CritRateUp) + caster.Sum(PassiveKind.CritRate, _now)
                                            - target.Stats.CritResistance, 0, 1000);
            var crit = _rng.Chance(critChance);
            if (crit)
            {
                flags |= HitFlags.Crit;
                amount = amount * System.Math.Max(Permille.One, Permille.One + caster.Stats.CritDamage - target.Stats.CritDefense) / Permille.One;
            }

            var modifier = relation > 0 ? AdvantageDamage : relation < 0 ? DisadvantageDamage : 0;
            modifier += target.StatusValue(StatusKind.DamageTakenUp) - target.StatusValue(StatusKind.DamageTakenDown);
            modifier -= target.Sum(PassiveKind.DamageReduction, _now);
            if (caster.Side == Side.Enemies)
                modifier += _setup.Rules.EnemyDamageModifier;
            amount = amount * System.Math.Max(100, Permille.One + modifier) / Permille.One;
            amount = amount * _rng.Range(950, 1051) / Permille.One;
            amount = System.Math.Max(1, amount);

            var dealt = TakeDamage(target, amount, caster, slot, flags, hitIndex, true);
            caster.DamageDealt += dealt;

            if (dealt > 0)
            {
                var lifesteal = caster.Sum(PassiveKind.Lifesteal, _now);
                if (skill != null)
                    foreach (var other in skill.Effects)
                        if (other.Kind == EffectKind.Lifesteal)
                            lifesteal += other.Multiplier;
                if (lifesteal > 0)
                    Heal(caster, caster, dealt * lifesteal / Permille.One, slot);
            }

            var stacking = caster.First(PassiveKind.StackingAttackSpeed);
            if (stacking != null && caster.StackCount < System.Math.Max(1, stacking.Cap))
                caster.StackCount++;

            if (skill == null || hitIndex != 0)
                return;
            // on-hit passives of the attacker
            foreach (var m in caster.Modifiers)
            {
                var p = m.Passive;
                if (p.Kind != PassiveKind.OnHit || p.Effect == null || !caster.IsActive(m) || !target.Alive)
                    continue;
                if (p.RequireCrit && !crit)
                    continue;
                if (p.EveryN > 0 && (slot != SkillSlot.Normal || caster.AttackCount % p.EveryN != 0))
                    continue;
                if (_rng.Chance(p.Chance))
                    ApplyPassiveEffect(caster, target, p.Effect);
            }
            // on-hit-taken passives of the defender (including team auras such as "whoever attacks an ally is slowed")
            foreach (var m in target.Modifiers)
            {
                var p = m.Passive;
                if (p.Kind != PassiveKind.OnHitTaken || p.Effect == null || !target.IsActive(m) || !caster.Alive)
                    continue;
                if (_rng.Chance(p.Chance))
                    ApplyPassiveEffect(m.Owner, caster, p.Effect);
            }
        }

        /// <summary>Applies damage through shields. Returns HP actually lost.</summary>
        private long TakeDamage(BattleUnit target, long amount, BattleUnit source, SkillSlot slot, int flags, int hitIndex, bool grantsEnergy)
        {
            if (target.Has(StatusKind.Immune))
                amount = 0;
            var absorbed = 0L;
            if (target.Shield > 0 && _now < target.ShieldUntilMs)
            {
                absorbed = System.Math.Min(target.Shield, amount);
                target.Shield -= absorbed;
                amount -= absorbed;
                if (absorbed > 0)
                    Emit(BattleEventType.ShieldAbsorbed, source?.Index ?? -1, target.Index, slot, absorbed, hitIndex: hitIndex);
            }
            var lost = System.Math.Min(target.Hp, amount);
            target.Hp -= lost;
            target.DamageTaken += lost;
            Emit(BattleEventType.Hit, source?.Index ?? -1, target.Index, slot, lost + absorbed, flags, hitIndex: hitIndex);

            if (target.Has(StatusKind.Sleep) && amount > 0)
                RemoveStatus(target, StatusKind.Sleep);
            if (target.Alive)
            {
                if (grantsEnergy)
                    AddEnergy(target, EnergyPerHitTaken);
                return lost;
            }

            if (source != null && source.Alive)
            {
                foreach (var m in source.Modifiers)
                    if (m.Passive.Kind == PassiveKind.OnKill && m.Passive.Effect != null && source.IsActive(m))
                        ApplyEffect(source, target, null, m.Passive.Effect, Permille.One, 0, false);
                AddEnergy(source, EnergyPerKill);
            }
            target.Statuses.Clear();
            target.Shield = 0;
            _pending.RemoveAll(h => h.Caster == target);
            Emit(BattleEventType.Death, source?.Index ?? -1, target.Index, slot, 0);
            return lost;
        }

        private void Heal(BattleUnit source, BattleUnit target, long amount, SkillSlot slot)
        {
            if (!target.Alive || amount <= 0)
                return;
            amount = amount * (Permille.One + source.Stats.HealPower + source.Sum(PassiveKind.HealingBonus, _now)) / Permille.One;
            amount = amount * System.Math.Max(0, Permille.One + _setup.Rules.HealingModifier) / Permille.One;
            var healed = System.Math.Min(target.MaxHp - target.Hp, amount);
            if (healed <= 0)
                return;
            target.Hp += healed;
            source.Healing += healed;
            Emit(BattleEventType.Heal, source.Index, target.Index, slot, healed);
        }

        private void GrantShield(BattleUnit source, BattleUnit target, long amount, int durationMs, SkillSlot slot)
        {
            if (!target.Alive || amount <= 0)
                return;
            var bonus = source.Sum(PassiveKind.ShieldBonus, _now) + (target == source ? 0 : target.Sum(PassiveKind.ShieldBonus, _now));
            amount = amount * System.Math.Max(0, Permille.One + bonus) / Permille.One;
            target.Shield = System.Math.Max(target.Shield, amount);
            target.ShieldUntilMs = _now + durationMs;
            Emit(BattleEventType.ShieldGained, source.Index, target.Index, slot, amount, durationMs: durationMs);
        }

        // ------------------------------------------------------------------ statuses

        public static bool IsDebuff(StatusKind k) =>
            k == StatusKind.Stun || k == StatusKind.Sleep || k == StatusKind.Provoke || k == StatusKind.Charm || k == StatusKind.Confuse ||
            k == StatusKind.Burn || k == StatusKind.Poison || k == StatusKind.AttackDown || k == StatusKind.DefenseDown ||
            k == StatusKind.DamageTakenUp || k == StatusKind.Slow || k == StatusKind.AccuracyDown;

        private void TryApplyStatus(BattleUnit source, BattleUnit target, StatusDefinition def, SkillSlot slot)
        {
            if (def == null || !target.Alive)
                return;
            if (IsDebuff(def.Kind) && target.Has(StatusKind.Immune))
                return;
            var existing = target.Find(def.Kind);
            if (existing != null)
            {
                existing.Value = System.Math.Max(existing.Value, def.Value);
                existing.UntilMs = System.Math.Max(existing.UntilMs, _now + def.DurationMs);
                existing.Source = source.Index;
            }
            else
            {
                target.Statuses.Add(new ActiveStatus
                {
                    Kind = def.Kind, Value = def.Value, UntilMs = _now + def.DurationMs, Source = source.Index, NextTickMs = _now + 1000
                });
            }
            if (def.Kind == StatusKind.Stun || def.Kind == StatusKind.Sleep)
                CancelAction(target);
            Emit(BattleEventType.StatusApplied, source.Index, target.Index, slot, def.Value, status: def.Kind, durationMs: def.DurationMs);
        }

        private void RemoveStatus(BattleUnit u, StatusKind kind)
        {
            var s = u.Find(kind);
            if (s == null)
                return;
            u.Statuses.Remove(s);
            Emit(BattleEventType.StatusExpired, -1, u.Index, SkillSlot.Normal, 0, status: kind);
        }

        private void Cleanse(BattleUnit u, int count)
        {
            var removed = 0;
            for (var i = u.Statuses.Count - 1; i >= 0 && (count <= 0 || removed < count); i--)
            {
                var kind = u.Statuses[i].Kind;
                if (!IsDebuff(kind))
                    continue;
                u.Statuses.RemoveAt(i);
                removed++;
                Emit(BattleEventType.StatusExpired, -1, u.Index, SkillSlot.Normal, 0, status: kind);
            }
        }

        private void StealBuffs(BattleUnit thief, BattleUnit victim, int count)
        {
            for (var n = 0; n < count; n++)
            {
                ActiveStatus buff = null;
                foreach (var s in victim.Statuses)
                    if (!IsDebuff(s.Kind) && s.Kind != StatusKind.Immune)
                    {
                        buff = s;
                        break;
                    }
                if (buff == null)
                    return;
                victim.Statuses.Remove(buff);
                Emit(BattleEventType.StatusExpired, thief.Index, victim.Index, SkillSlot.Normal, 0, status: buff.Kind);
                if (thief.Alive)
                    TryApplyStatus(thief, thief, new StatusDefinition { Kind = buff.Kind, Value = buff.Value, DurationMs = System.Math.Max(1000, buff.UntilMs - _now) }, SkillSlot.Passive);
            }
        }

        private void TickStatuses()
        {
            foreach (var u in _units)
            {
                if (!u.Alive)
                    continue;
                for (var i = u.Statuses.Count - 1; i >= 0 && u.Alive; i--)
                {
                    if (i >= u.Statuses.Count)
                        continue;
                    var s = u.Statuses[i];
                    if ((s.Kind == StatusKind.Burn || s.Kind == StatusKind.Poison || s.Kind == StatusKind.Regen) && _now >= s.NextTickMs)
                    {
                        s.NextTickMs += 1000;
                        var source = s.Source >= 0 ? _units[s.Source] : u;
                        if (s.Kind == StatusKind.Regen)
                        {
                            Heal(source, u, Permille.Apply(u.MaxHp, s.Value), SkillSlot.Passive);
                        }
                        else
                        {
                            var dmg = System.Math.Max(1, Permille.Apply(source.Stats.Attack, s.Value));
                            Emit(BattleEventType.StatusTick, source.Index, u.Index, SkillSlot.Passive, dmg, status: s.Kind);
                            source.DamageDealt += TakeDamage(u, dmg, source, SkillSlot.Passive, 0, 0, false);
                        }
                    }
                    if (u.Alive && i < u.Statuses.Count && u.Statuses[i] == s && _now >= s.UntilMs)
                    {
                        u.Statuses.RemoveAt(i);
                        Emit(BattleEventType.StatusExpired, -1, u.Index, SkillSlot.Normal, 0, status: s.Kind);
                    }
                }
                if (!u.Alive)
                    continue;
                if (u.Shield > 0 && _now >= u.ShieldUntilMs)
                    u.Shield = 0;

                if (_now >= u.NextRegenTickMs)
                {
                    u.NextRegenTickMs += 1000;
                    var regen = u.Sum(PassiveKind.Regen, _now);
                    if (u.Side == Side.Enemies)
                        regen += _setup.Rules.EnemyRegenPermille;
                    var stacking = u.First(PassiveKind.StackingAttackSpeed);
                    if (stacking != null)
                        regen += stacking.StackRegen * u.StackCount;
                    if (regen > 0)
                        Heal(u, u, Permille.Apply(u.MaxHp, regen), SkillSlot.Passive);
                }

                foreach (var p in u.Passives)
                {
                    if (p.Kind != PassiveKind.Periodic || p.Effect == null || _now < u.NextPeriodicMs[p])
                        continue;
                    u.NextPeriodicMs[p] = _now + (p.IntervalMs > 0 ? p.IntervalMs : 10_000);
                    foreach (var target in SelectTargets(u, p.Target ?? TargetSelector.EnemyRandom, 1))
                        ApplyPassiveEffect(u, target, p.Effect);
                }
            }
        }

        // ------------------------------------------------------------------ energy

        private void AddEnergy(BattleUnit u, int amount)
        {
            if (!u.Alive || u.Hero.Ultimate == null)
                return;
            if (amount > 0)
                amount = amount * System.Math.Max(0, Permille.One + _setup.Rules.EnergyGainModifier) / Permille.One;
            SetEnergy(u, Permille.Clamp(u.Energy + amount, 0, MaxEnergy));
        }

        private void SetEnergy(BattleUnit u, int value)
        {
            if (value == u.Energy)
                return;
            u.Energy = value;
            Emit(BattleEventType.EnergyChanged, u.Index, u.Index, SkillSlot.Ultimate, value);
        }

        // ------------------------------------------------------------------ targeting

        private static bool IsSingleEnemy(TargetSelector s) =>
            s == TargetSelector.EnemyFront || s == TargetSelector.EnemyBack || s == TargetSelector.EnemyLowestHp ||
            s == TargetSelector.EnemyHighestAttack || s == TargetSelector.EnemyRandom;

        private List<BattleUnit> SelectTargets(BattleUnit caster, TargetSelector selector, int count)
        {
            var result = new List<BattleUnit>();
            var enemySide = Opposite(caster.Side);
            var allySide = caster.Side;
            var charmed = caster.Has(StatusKind.Charm);
            if (charmed)
                (enemySide, allySide) = (allySide, enemySide);
            count = System.Math.Max(1, count);

            if (IsSingleEnemy(selector))
            {
                if (caster.Has(StatusKind.Confuse))
                {
                    var pool = new List<BattleUnit>();
                    foreach (var u in _units)
                        if (u.Alive && u != caster)
                            pool.Add(u);
                    if (pool.Count > 0)
                        result.Add(pool[_rng.Range(pool.Count)]);
                    return result;
                }
                if (!charmed)
                {
                    var forced = ForcedTarget(caster, enemySide);
                    if (forced != null)
                        result.Add(forced);
                }
            }

            switch (selector)
            {
                case TargetSelector.Self:
                    result.Add(caster);
                    break;
                case TargetSelector.EnemyFront:
                    TakeBest(result, enemySide, count, u => -(u.Row == Row.Front ? 0 : 10) - u.Slot);
                    break;
                case TargetSelector.EnemyBack:
                    TakeBest(result, enemySide, count, u => -(u.Row == Row.Back ? 0 : 10) - u.Slot);
                    break;
                case TargetSelector.EnemyLowestHp:
                    TakeBest(result, enemySide, count, u => -u.Hp * Permille.One / u.MaxHp);
                    break;
                case TargetSelector.EnemyHighestAttack:
                    TakeBest(result, enemySide, count, u => u.Stats.Attack);
                    break;
                case TargetSelector.EnemyRandom:
                {
                    var pool = AliveOf(enemySide);
                    pool.RemoveAll(result.Contains);
                    while (result.Count < count && pool.Count > 0)
                    {
                        var pick = _rng.Range(pool.Count);
                        result.Add(pool[pick]);
                        pool.RemoveAt(pick);
                    }
                    break;
                }
                case TargetSelector.EnemyFrontRow:
                    AddRowOrOther(result, enemySide, Row.Front);
                    break;
                case TargetSelector.EnemyBackRow:
                    AddRowOrOther(result, enemySide, Row.Back);
                    break;
                case TargetSelector.EnemyCircle:
                {
                    var center = ForcedTarget(caster, enemySide) ?? Best(enemySide, u => -(u.Row == Row.Front ? 0 : 10) - u.Slot);
                    if (center != null)
                        foreach (var u in AliveOf(enemySide))
                            if (u.Row == center.Row && System.Math.Abs(u.Slot - center.Slot) <= 1)
                                result.Add(u);
                    break;
                }
                case TargetSelector.EnemyAll:
                    result.AddRange(AliveOf(enemySide));
                    break;
                case TargetSelector.AllyLowestHp:
                    TakeBest(result, allySide, count, u => -u.Hp * Permille.One / u.MaxHp);
                    break;
                case TargetSelector.AllyFrontRow:
                    AddRowOrOther(result, allySide, Row.Front);
                    break;
                case TargetSelector.AllyBackRow:
                    AddRowOrOther(result, allySide, Row.Back);
                    break;
                case TargetSelector.AllyAll:
                    result.AddRange(AliveOf(allySide));
                    break;
                case TargetSelector.AllyHighestEnergy:
                    TakeBest(result, allySide, count, u => u == caster ? -1 : u.Energy);
                    break;
            }
            return result;
        }

        /// <summary>Provoke (on the caster) beats Taunt (on an enemy).</summary>
        private BattleUnit ForcedTarget(BattleUnit caster, Side enemySide)
        {
            var provoke = caster.Find(StatusKind.Provoke);
            if (provoke != null && provoke.Source >= 0)
            {
                var source = _units[provoke.Source];
                if (source.Alive && source.Side == enemySide)
                    return source;
            }
            foreach (var u in _units)
                if (u.Side == enemySide && u.Alive && u.Has(StatusKind.Taunt))
                    return u;
            return null;
        }

        private void TakeBest(List<BattleUnit> result, Side side, int count, System.Func<BattleUnit, long> score)
        {
            while (result.Count < count)
            {
                BattleUnit best = null;
                var bestScore = long.MinValue;
                foreach (var u in _units)
                {
                    if (u.Side != side || !u.Alive || result.Contains(u))
                        continue;
                    var s = score(u);
                    if (best == null || s > bestScore)
                    {
                        best = u;
                        bestScore = s;
                    }
                }
                if (best == null)
                    return;
                result.Add(best);
            }
        }

        private BattleUnit Best(Side side, System.Func<BattleUnit, long> score)
        {
            var list = new List<BattleUnit>(1);
            TakeBest(list, side, 1, score);
            return list.Count > 0 ? list[0] : null;
        }

        private void AddRowOrOther(List<BattleUnit> result, Side side, Row row)
        {
            foreach (var u in _units)
                if (u.Side == side && u.Alive && u.Row == row)
                    result.Add(u);
            if (result.Count > 0)
                return;
            foreach (var u in _units)
                if (u.Side == side && u.Alive)
                    result.Add(u);
        }

        private List<BattleUnit> AliveOf(Side side)
        {
            var list = new List<BattleUnit>();
            foreach (var u in _units)
                if (u.Side == side && u.Alive)
                    list.Add(u);
            return list;
        }

        // ------------------------------------------------------------------ log

        private void Emit(BattleEventType type, int source, int target, SkillSlot slot, long value, int flags = 0,
            StatusKind status = StatusKind.Stun, int hitIndex = 0, int durationMs = 0, string animation = null)
        {
            _result.Events.Add(new BattleEvent
            {
                TimeMs = _now, Type = type, Source = source, Target = target, Skill = slot, Value = value, Flags = flags,
                Status = status, HitIndex = hitIndex, DurationMs = durationMs, Animation = animation
            });
        }
    }
}
