namespace Mythwake.Core.Content
{
    /// <summary>The five Fragment Aspects named in the design doc. Each one beats the next: Ravak → Gorath → Somnara → Veloria → Envyra → Ravak.</summary>
    public enum Aspect { Ravak, Gorath, Somnara, Veloria, Envyra }

    public enum Faction { OrderOfTheVeil, TribeOfTheCrimsonFang, EmpireOfTheShatteredSky, GuildsOfTheHunt, CircleOfAether, TheHollowed }

    public enum Race { Human, Veyr, Drakari, Thalassari, Stoneborn, Hollowed, Aetherial }

    public enum Role { Dps, Tank, Support, Debuffer, HeroLeader }

    public enum Row { Front, Back }

    public enum Grade { Common, Rare, Epic, Legendary, Transcendent }

    public enum SkillSlot { Normal, Special, Special2, Passive, Ultimate }

    public enum TriggerKind
    {
        /// <summary>Normal Skill: repeats on the hero's attack interval.</summary>
        AttackInterval,
        /// <summary>Casts whenever its cooldown is ready.</summary>
        Cooldown,
        /// <summary>Rolls a chance on every Normal Skill and replaces it when it procs.</summary>
        ChanceOnAttack,
        /// <summary>Replaces every N-th Normal Skill.</summary>
        EveryNthAttack,
        /// <summary>Casts (per cooldown) when an ally drops below a HP threshold.</summary>
        AllyBelowHp,
        /// <summary>Casts (per cooldown) when the caster drops below a HP threshold.</summary>
        SelfBelowHp,
        /// <summary>Casts (per cooldown) when an enemy drops below a HP threshold.</summary>
        EnemyBelowHp,
        /// <summary>Casts once at battle start, then on its cooldown if it has one.</summary>
        BattleStart,
        /// <summary>Ultimate: needs a full energy bar.</summary>
        Energy,
        /// <summary>Passive: never cast, modifies the hero permanently.</summary>
        Passive
    }

    public enum TargetSelector
    {
        EnemyFront,
        EnemyBack,
        EnemyLowestHp,
        EnemyHighestAttack,
        EnemyRandom,
        EnemyFrontRow,
        EnemyBackRow,
        EnemyCircle,
        EnemyAll,
        Self,
        AllyLowestHp,
        AllyFrontRow,
        AllyBackRow,
        AllyAll,
        AllyHighestEnergy
    }

    public enum EffectKind
    {
        Damage,
        Heal,
        Shield,
        ApplyStatus,
        GainEnergy,
        DrainEnergy,
        /// <summary>Removes up to Multiplier debuffs (0 = all).</summary>
        Cleanse,
        StealBuff,
        /// <summary>Heals the caster for Multiplier permille of the damage this skill deals.</summary>
        Lifesteal,
        /// <summary>Pushes the target's next attack and running cooldowns back by Multiplier ms.</summary>
        DelayAction,
        /// <summary>Cancels the target's current non-ultimate action.</summary>
        Interrupt
    }

    /// <summary>What an effect's Multiplier is a permille of.</summary>
    public enum Scaling { Attack, CasterMaxHp, TargetMaxHp }

    public enum StatusKind
    {
        // crowd control
        Stun,
        Sleep,
        /// <summary>On the caster: all enemy single-target attacks must hit this unit.</summary>
        Taunt,
        /// <summary>On an enemy: its single-target attacks must hit the status source.</summary>
        Provoke,
        Charm,
        Confuse,
        // damage over time
        Burn,
        Poison,
        // stat changes (Value = permille)
        AttackUp,
        AttackDown,
        DefenseUp,
        DefenseDown,
        DamageTakenUp,
        DamageTakenDown,
        AttackSpeedUp,
        Slow,
        AccuracyDown,
        CritRateUp,
        // sustain
        Regen,
        Immune,
        /// <summary>Dodges the next Value incoming hits.</summary>
        Evade
    }

    /// <summary>One component of a passive. A hero's passive is a list of these.</summary>
    public enum PassiveKind
    {
        None,
        /// <summary>+Value permille damage (optionally only below BelowHp, optionally growing over time).</summary>
        DamageBonus,
        /// <summary>+Value permille attack speed.</summary>
        AttackSpeed,
        /// <summary>+Value permille crit rate.</summary>
        CritRate,
        /// <summary>-Value permille damage taken.</summary>
        DamageReduction,
        /// <summary>+Value permille healing done.</summary>
        HealingBonus,
        /// <summary>Heals Value permille of max HP every second.</summary>
        Regen,
        /// <summary>Heals for Value permille of damage dealt.</summary>
        Lifesteal,
        /// <summary>Chance permille to dodge an incoming hit.</summary>
        Dodge,
        /// <summary>+Value permille attack speed per stack on hit, up to Cap stacks; StackRegen permille max HP per stack per second.</summary>
        StackingAttackSpeed,
        /// <summary>Applies Effect to the hit target (Chance, RequireCrit, EveryN on Normal Skills).</summary>
        OnHit,
        /// <summary>Applies Effect to the attacker when this unit is hit.</summary>
        OnHitTaken,
        /// <summary>Applies Effect when this unit kills an enemy (target = the victim).</summary>
        OnKill,
        /// <summary>Applies Effect to Target every IntervalMs.</summary>
        Periodic,
        /// <summary>Applies Effect to Target once at battle start.</summary>
        BattleStart,
        /// <summary>Hero Leader: team-wide stat bonus.</summary>
        LeaderAura
    }
}
