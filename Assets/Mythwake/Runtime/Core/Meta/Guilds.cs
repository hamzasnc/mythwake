using System.Collections.Generic;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Math;

namespace Mythwake.Core.Meta
{
    /// <summary>One way to donate to the guild once per day (docs/systems/GUILDS.md).</summary>
    public sealed class GuildDonation
    {
        public string Id;
        public string Name;
        public List<Amount> Cost = new List<Amount>();
        public long GuildExp;
        public List<Amount> Gives = new List<Amount>();
    }

    public sealed class GuildRaidConfig
    {
        public int AttemptsPerDay = 2;
        public int BattleMs = 60_000;
        /// <summary>The doc's Guardian Raid bosses, fought in this order; after the last the order starts again.</summary>
        public List<string> Bosses = new List<string>();
        public long BossHpBase = 3_000_000;
        /// <summary>Boss HP grows by this permille per raid level.</summary>
        public int HpGrowthPermille = 1350;
        public int BossLevelBase = 40;
        public int BossLevelPerRaid = 6;
        /// <summary>Per attempt: base + one unit per DamagePerReward damage.</summary>
        public List<Amount> AttemptRewardsBase = new List<Amount>();
        public long DamagePerReward = 50_000;
        public List<Amount> AttemptRewardsPerDamage = new List<Amount>();
        /// <summary>For every member when the guild defeats a boss: base + perStage × raid level.</summary>
        public List<AfkRate> KillRewards = new List<AfkRate>();
    }

    public sealed class GuildConfig
    {
        public List<Amount> CreateCost = new List<Amount>();
        public int NameMinLength = 3;
        public int NameMaxLength = 20;
        public int MembersBase = 20;
        public int MembersPerLevel = 1;
        /// <summary>Guild EXP needed for level 2, 3, … (index 0 = level 1 → 2).</summary>
        public List<long> LevelExp = new List<long>();
        public List<GuildDonation> Donations = new List<GuildDonation>();
        public GuildRaidConfig Raid = new GuildRaidConfig();
    }

    public static class Guilds
    {
        public static GuildDonation Donation(GameContent content, string id)
        {
            foreach (var d in content.Guilds.Donations)
                if (d.Id == id)
                    return d;
            throw new RuleViolation("unknown_donation", id);
        }

        public static int MaxMembers(GameContent content, int level) =>
            content.Guilds.MembersBase + content.Guilds.MembersPerLevel * System.Math.Max(0, level - 1);

        public static string RaidBoss(GameContent content, int raidLevel)
        {
            var bosses = content.Guilds.Raid.Bosses;
            return bosses.Count == 0 ? null : bosses[(System.Math.Max(1, raidLevel) - 1) % bosses.Count];
        }

        public static long RaidBossHp(GameContent content, int raidLevel)
        {
            var r = content.Guilds.Raid;
            long hp = r.BossHpBase;
            for (var l = 1; l < raidLevel; l++)
                hp = hp * r.HpGrowthPermille / Permille.One;
            return hp;
        }

        /// <summary>A raid attempt: the player's team against the Guardian with the HP it has left; ends after BattleMs.</summary>
        public static BattleSetup RaidSetup(GameContent content, TeamSnapshot team, int raidLevel, long bossHpLeft, ulong seed)
        {
            var r = content.Guilds.Raid;
            var boss = content.Hero(RaidBoss(content, raidLevel));
            var level = r.BossLevelBase + r.BossLevelPerRaid * (raidLevel - 1);
            var unit = Campaign.Arrange(new List<HeroDefinition> { boss }, level, 1 + (raidLevel - 1) / 3)[0];
            unit.Stats.Hp = System.Math.Max(1, bossHpLeft);
            unit.Variant = "boss";
            var setup = new BattleSetup { Seed = seed, Rules = new BattleRules { MaxDurationMs = r.BattleMs } };
            setup.Allies.AddRange(team.ToSetups(content));
            setup.Enemies.Add(unit);
            return setup;
        }

        public static List<Amount> AttemptRewards(GameContent content, long damage)
        {
            var r = content.Guilds.Raid;
            var list = new List<Amount>();
            var units = r.DamagePerReward > 0 ? damage / r.DamagePerReward : 0;
            foreach (var a in r.AttemptRewardsBase)
                list.Add(new Amount(a.Id, a.Value));
            foreach (var a in r.AttemptRewardsPerDamage)
                if (a.Value * units > 0)
                    list.Add(new Amount(a.Id, a.Value * units));
            return list;
        }

        public static List<Amount> KillRewards(GameContent content, int raidLevel)
        {
            var list = new List<Amount>();
            foreach (var a in content.Guilds.Raid.KillRewards)
                list.Add(new Amount(a.Id, a.At(raidLevel)));
            return list;
        }
    }

    // ------------------------------------------------------------------ server-only commands (the server owns guilds)

    /// <summary>Marks the player as a member of a guild (null = no guild). Opens the Guild Shop.</summary>
    [ServerOnly]
    public sealed class SetGuild : ICommand<bool>
    {
        public string GuildId;

        public bool Apply(CommandContext ctx)
        {
            ctx.State.GuildId = string.IsNullOrEmpty(GuildId) ? null : GuildId;
            return true;
        }
    }

    /// <summary>Pays the guild founding cost.</summary>
    [ServerOnly]
    public sealed class PayGuildCreation : ICommand<bool>
    {
        public bool Apply(CommandContext ctx)
        {
            ctx.RequireFunds(ctx.Content.Guilds.CreateCost);
            ctx.Pay(ctx.Content.Guilds.CreateCost);
            return true;
        }
    }

    /// <summary>The player's daily donation: pays its cost and gets its Guild Coins (the server adds the guild EXP).</summary>
    [ServerOnly]
    public sealed class DonateToGuild : ICommand<long>
    {
        public string DonationId;

        public long Apply(CommandContext ctx)
        {
            ctx.Require(!string.IsNullOrEmpty(ctx.State.GuildId), "no_guild");
            var d = Guilds.Donation(ctx.Content, DonationId);
            ctx.RequireFunds(d.Cost);
            ctx.Pay(d.Cost);
            ctx.Grant(d.Gives);
            return d.GuildExp;
        }
    }
}
