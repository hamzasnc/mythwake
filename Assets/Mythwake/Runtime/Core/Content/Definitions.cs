using System.Collections.Generic;

namespace Mythwake.Core.Content
{
    /// <summary>Combat stats. Percent-like values are permille (1000 = 100 %).</summary>
    public sealed class Stats
    {
        public long Hp;
        public long Attack;
        public long Defense;
        /// <summary>Normal Skill interval in milliseconds at 0 attack speed bonus.</summary>
        public int AttackIntervalMs = 1600;
        public int Accuracy = 1000;
        public int Evasion = 1000;
        public int CritRate = 100;
        public int CritResistance;
        public int CritDamage = 500;
        public int CritDefense;
        public int HealPower;
        public int EnergyRegen;

        public Stats Clone() => (Stats)MemberwiseClone();
    }

    public sealed class StatusDefinition
    {
        public StatusKind Kind;
        public int DurationMs;
        /// <summary>Permille: stat change; damage per second as permille of the source's attack (Burn/Poison);
        /// heal per second as permille of max HP (Regen); number of dodged hits (Evade).</summary>
        public int Value;

        public StatusDefinition Clone() => (StatusDefinition)MemberwiseClone();
    }

    public sealed class EffectDefinition
    {
        public EffectKind Kind;
        /// <summary>Permille of <see cref="Scaling"/> (Damage/Heal/Shield), flat energy (Gain/DrainEnergy), ms (DelayAction), count (Cleanse).</summary>
        public int Multiplier;
        public Scaling Scaling;
        public StatusDefinition Status;
        /// <summary>Optional: the effect hits a different target group than the skill (e.g. damage enemies, shield allies).</summary>
        public TargetSelector? Target;
        /// <summary>Permille chance for the effect to apply (rolled once per target).</summary>
        public int Chance = 1000;
        /// <summary>Permille bonus multiplier against targets below ExecuteBelowHp (0 = none).</summary>
        public int ExecuteBonus;
        public int ExecuteBelowHp;
        /// <summary>Permille bonus damage against targets that carry BonusVsStatus.</summary>
        public StatusKind? BonusVsStatus;
        public int BonusVsStatusPermille;
        /// <summary>Bonus damage that scales with the caster's missing HP, reaching this permille at 0 HP.</summary>
        public int MissingHpBonus;
        public int ShieldDurationMs = 6000;

        public EffectDefinition Clone()
        {
            var e = (EffectDefinition)MemberwiseClone();
            e.Status = Status?.Clone();
            return e;
        }
    }

    public sealed class TriggerDefinition
    {
        public TriggerKind Kind;
        public int CooldownMs;
        public int ChancePermille;
        public int EveryN;
        public int HpThresholdPermille;
        /// <summary>First cast is delayed by this much (Cooldown skills), so specials do not all fire at t=0.</summary>
        public int InitialDelayMs;
        /// <summary>The skill can only fire once per battle.</summary>
        public bool OncePerBattle;
    }

    public sealed class PassiveDefinition
    {
        public PassiveKind Kind;
        public int Value;
        public int Chance = 1000;
        /// <summary>Cap for the growth part (0 = uncapped) or max stacks.</summary>
        public int Cap;
        /// <summary>Value grows by this much every GrowthIntervalMs of battle time.</summary>
        public int Growth;
        public int GrowthIntervalMs = 10_000;
        /// <summary>Only active while the affected unit is below this HP permille (0 = always).</summary>
        public int BelowHp;
        /// <summary>The component affects every ally (aura), not only its owner.</summary>
        public bool Team;
        public bool RequireCrit;
        /// <summary>OnHit: only every N-th Normal Skill.</summary>
        public int EveryN;
        public int IntervalMs;
        public int StackRegen;
        public EffectDefinition Effect;
        public TargetSelector? Target;
        /// <summary>Leader aura: which stats are boosted, permille.</summary>
        public int AttackBonus;
        public int HpBonus;
        public int HealBonus;
        /// <summary>DamageVsGroup: the enemy group that takes the extra damage (Hollowed also matches the Hollowed race and faction).</summary>
        public EnemyGroup VsGroup;

        public PassiveDefinition Clone()
        {
            var p = (PassiveDefinition)MemberwiseClone();
            p.Effect = Effect?.Clone();
            return p;
        }
    }

    public sealed class SkillDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        public SkillSlot Slot;
        public TriggerDefinition Trigger = new TriggerDefinition();
        public TargetSelector Target;
        /// <summary>Single-target selectors pick this many units (e.g. "the two weakest enemies").</summary>
        public int TargetCount = 1;
        public List<EffectDefinition> Effects = new List<EffectDefinition>();
        /// <summary>Damage is split over this many hits (last hit carries 40 % when Hits &gt; 1).</summary>
        public int Hits = 1;
        /// <summary>Times (ms from cast start) at which the hits land; length should equal Hits. Comes from the Spine "hit" events.</summary>
        public int[] HitTimesMs = { 250 };
        /// <summary>Total action length (ms); the caster is busy for this long.</summary>
        public int DurationMs = 900;
        /// <summary>Spine animation played for this skill.</summary>
        public string Animation = "attack";
        /// <summary>Passive slot only: the components of the passive.</summary>
        public List<PassiveDefinition> Passives = new List<PassiveDefinition>();
    }

    public sealed class HeroDefinition
    {
        public string Id;
        public string Name;
        public string Title;
        public Faction Faction;
        public Race Race;
        public Aspect Aspect;
        public Role Role;
        public Row Row;
        public Grade Grade;
        /// <summary>Base body size used by the presentation: "small", "normal", "large" or "monster".</summary>
        public string Body = "normal";
        public Stats BaseStats = new Stats();
        /// <summary>Per-level growth of HP/Attack/Defense, permille of base.</summary>
        public int GrowthPermille = 80;
        public SkillDefinition Normal;
        public SkillDefinition Special;
        public SkillDefinition Special2;
        public SkillDefinition Passive;
        public SkillDefinition Ultimate;
        /// <summary>Spine skeleton id (folder under Content/Heroes). Not called "skeleton": spine-unity sniffs that key in .json files.</summary>
        public string SpineId;
        /// <summary>Monsters only: enemy group (doc: Enemy System). None for playable heroes.</summary>
        public EnemyGroup Group;
        /// <summary>Monsters only: a named boss (doc: Boss System).</summary>
        public bool Boss;
        /// <summary>Monsters only: a Guardian Raid boss (doc: Guardian Raid System), fought only by guilds.</summary>
        public bool RaidBoss;
        /// <summary>Visual size on the battlefield, permille (bosses are drawn larger).</summary>
        public int SizePermille = 1000;
        /// <summary>Design-only notes (where the data approximates a kit mechanic the rules do not model yet). Never shown in game.</summary>
        public List<string> DesignNotes = new List<string>();

        public IEnumerable<SkillDefinition> Skills()
        {
            if (Normal != null) yield return Normal;
            if (Special != null) yield return Special;
            if (Special2 != null) yield return Special2;
            if (Passive != null) yield return Passive;
            if (Ultimate != null) yield return Ultimate;
        }

        public IEnumerable<PassiveDefinition> PassiveComponents()
        {
            if (Passive?.Passives == null)
                yield break;
            foreach (var p in Passive.Passives)
                yield return p;
        }
    }
}
