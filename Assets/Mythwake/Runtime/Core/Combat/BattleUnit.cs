using System.Collections.Generic;
using Mythwake.Core.Content;

namespace Mythwake.Core.Combat
{
    internal sealed class ActiveStatus
    {
        public StatusKind Kind;
        public int Value;
        public int UntilMs;
        public int Source;
        public int NextTickMs;
    }

    /// <summary>A passive component affecting a unit, with the unit that owns it (auras stop when the owner dies).</summary>
    internal readonly struct Modifier
    {
        public readonly PassiveDefinition Passive;
        public readonly BattleUnit Owner;

        public Modifier(PassiveDefinition passive, BattleUnit owner)
        {
            Passive = passive;
            Owner = owner;
        }
    }

    internal sealed class BattleUnit
    {
        public int Index;
        public Side Side;
        public int Slot;
        public Row Row => Slot < 2 ? Row.Front : Row.Back;
        public HeroDefinition Hero;
        /// <summary>The hero's passive components plus the extra ones of its setup (Fragments).</summary>
        public readonly List<PassiveDefinition> Passives = new List<PassiveDefinition>();
        public Stats Stats;
        public TargetPriority Priority;
        public long MaxHp;
        public long Hp;
        public long Shield;
        public int ShieldUntilMs;
        public int Energy;
        public int NextNormalMs;
        public int BusyUntilMs;
        public SkillSlot BusySlot;
        public int AttackCount;
        public int StackCount;
        public readonly Dictionary<SkillSlot, int> ReadyAtMs = new Dictionary<SkillSlot, int>();
        public readonly List<ActiveStatus> Statuses = new List<ActiveStatus>();
        /// <summary>Passive components that apply to this unit: its own plus team auras of its allies.</summary>
        public readonly List<Modifier> Modifiers = new List<Modifier>();
        public readonly Dictionary<PassiveDefinition, int> NextPeriodicMs = new Dictionary<PassiveDefinition, int>();
        public bool StartSkillsDone;
        public int NextRegenTickMs = 1000;

        public long DamageDealt;
        public long DamageTaken;
        public long Healing;

        public bool Alive => Hp > 0;

        public ActiveStatus Find(StatusKind kind)
        {
            for (var i = 0; i < Statuses.Count; i++)
                if (Statuses[i].Kind == kind)
                    return Statuses[i];
            return null;
        }

        public bool Has(StatusKind kind) => Find(kind) != null;

        public int StatusValue(StatusKind kind)
        {
            var s = Find(kind);
            return s == null ? 0 : s.Value;
        }

        public bool Disabled => Has(StatusKind.Stun) || Has(StatusKind.Sleep);

        /// <summary>True while the modifier's owner lives and its HP condition holds for this unit.</summary>
        public bool IsActive(in Modifier m) =>
            m.Owner.Alive && (m.Passive.BelowHp <= 0 || Hp * 1000 < MaxHp * m.Passive.BelowHp);

        /// <summary>Sum of all active modifiers of a kind at battle time <paramref name="nowMs"/>.</summary>
        public int Sum(PassiveKind kind, int nowMs)
        {
            var total = 0;
            foreach (var m in Modifiers)
            {
                if (m.Passive.Kind != kind || !IsActive(m))
                    continue;
                var p = m.Passive;
                var growth = p.Growth > 0 && p.GrowthIntervalMs > 0 ? p.Growth * (nowMs / p.GrowthIntervalMs) : 0;
                if (p.Cap > 0 && growth > p.Cap)
                    growth = p.Cap;
                total += p.Value + growth;
            }
            return total;
        }

        public PassiveDefinition First(PassiveKind kind)
        {
            foreach (var m in Modifiers)
                if (m.Passive.Kind == kind && IsActive(m))
                    return m.Passive;
            return null;
        }
    }
}
