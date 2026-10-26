using System.Collections.Generic;
using Mythwake.Core.Content;

namespace Mythwake.Core.Combat
{
    public enum Side { Allies = 0, Enemies = 1 }

    public enum BattleOutcome { Victory, Defeat }

    /// <summary>One combatant entering a battle. Stats are final (level, stars, gear and synergies already applied).</summary>
    public sealed class UnitSetup
    {
        public HeroDefinition Hero;
        public int Level = 1;
        /// <summary>0,1 = front row; 2,3,4 = back row.</summary>
        public int Slot;
        public Stats Stats;
        /// <summary>Visual only (e.g. corrupted enemy tint), ignored by the rules.</summary>
        public string Variant;
    }

    /// <summary>Battle-wide modifiers (dungeon / Shattered Realm rules).</summary>
    public sealed class BattleRules
    {
        public int HealingModifier;      // permille, e.g. -500 = healing halved
        public int EnemyDamageModifier;  // permille
        public int EnergyGainModifier;   // permille
        /// <summary>Enemies heal this permille of max HP every second.</summary>
        public int EnemyRegenPermille;
        public int MaxDurationMs = 90_000;
    }

    /// <summary>A player tap on an ally portrait: cast that unit's ultimate as soon as possible after TimeMs.</summary>
    public readonly struct UltimateRequest
    {
        public readonly int TimeMs;
        public readonly int Slot;

        public UltimateRequest(int timeMs, int slot)
        {
            TimeMs = timeMs;
            Slot = slot;
        }
    }

    public sealed class BattleSetup
    {
        public List<UnitSetup> Allies = new List<UnitSetup>();
        public List<UnitSetup> Enemies = new List<UnitSetup>();
        public ulong Seed;
        public BattleRules Rules = new BattleRules();
        /// <summary>When true, allied ultimates fire automatically (the in-game "Auto" toggle) from the start.</summary>
        public bool AutoUltimates = true;
        /// <summary>Battle times (ms) at which the player flipped the Auto toggle during the fight.</summary>
        public List<int> AutoToggleTimesMs = new List<int>();
        public List<UltimateRequest> UltimateRequests = new List<UltimateRequest>();
    }

    public enum BattleEventType
    {
        BattleStart,
        ActionStart,
        Hit,
        Miss,
        Dodge,
        Heal,
        ShieldGained,
        ShieldAbsorbed,
        StatusApplied,
        StatusExpired,
        StatusTick,
        EnergyChanged,
        Death,
        BattleEnd
    }

    public static class HitFlags
    {
        public const int Crit = 1;
        public const int Advantage = 2;
        public const int Disadvantage = 4;
        public const int LastHit = 8;
        public const int Execute = 16;
    }

    /// <summary>One entry of the battle log. The presentation replays these; it never computes rules itself.</summary>
    public struct BattleEvent
    {
        public int TimeMs;
        public BattleEventType Type;
        public int Source;
        public int Target;
        public SkillSlot Skill;
        public long Value;
        public int Flags;
        public StatusKind Status;
        public int HitIndex;
        /// <summary>ActionStart: animation length in ms.</summary>
        public int DurationMs;
        public string Animation;

        public override string ToString() => $"{TimeMs,6}ms {Type,-14} {Source,2}->{Target,2} {Skill,-9} {Value,8} f{Flags} {Status}";
    }

    public sealed class UnitSummary
    {
        public int Index;
        public Side Side;
        public int Slot;
        public string HeroId;
        public long MaxHp;
        public long FinalHp;
        public long DamageDealt;
        public long DamageTaken;
        public long Healing;
    }

    public sealed class BattleResult
    {
        public BattleOutcome Outcome;
        public int DurationMs;
        public List<BattleEvent> Events = new List<BattleEvent>();
        public List<UnitSummary> Units = new List<UnitSummary>();
        public int AlliesAlive;
    }
}
