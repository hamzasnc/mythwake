using System.Collections.Generic;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Progression;

namespace Mythwake.Core.Meta
{
    /// <summary>Marks commands only the server may issue (rewards decided by server-side systems such as the Arena).</summary>
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class ServerOnlyAttribute : System.Attribute { }

    /// <summary>One fighter of a stored team: enough to rebuild the battle without the owner's full state.</summary>
    public sealed class UnitSnapshot
    {
        public string HeroId;
        public int Level;
        public int Stars;
        public int Slot;
        public Stats Stats;
        /// <summary>Fragment components at the time the snapshot was taken.</summary>
        public List<PassiveDefinition> Passives = new List<PassiveDefinition>();
    }

    /// <summary>A team frozen at the moment it was set (Arena defense). Battles against it use exactly these stats.</summary>
    public sealed class TeamSnapshot
    {
        public List<UnitSnapshot> Units = new List<UnitSnapshot>();
        public long Power;

        public static TeamSnapshot Of(List<UnitSetup> team)
        {
            var snap = new TeamSnapshot();
            foreach (var u in team)
            {
                snap.Units.Add(new UnitSnapshot
                {
                    HeroId = u.Hero.Id, Level = u.Level, Slot = u.Slot, Stats = u.Stats.Clone(),
                    Passives = u.Passives.ConvertAll(p => p.Clone())
                });
                snap.Power += TeamBuilder.Power(u.Stats);
            }
            return snap;
        }

        public List<UnitSetup> ToSetups(GameContent content)
        {
            var list = new List<UnitSetup>();
            foreach (var u in Units)
                list.Add(new UnitSetup
                {
                    Hero = content.Hero(u.HeroId), Level = u.Level, Slot = u.Slot, Stats = u.Stats.Clone(),
                    Passives = u.Passives?.ConvertAll(p => p.Clone()) ?? new List<PassiveDefinition>()
                });
            return list;
        }
    }

    public sealed class ArenaRank
    {
        public string Name;
        public int MinRating;
    }

    public sealed class ArenaConfig
    {
        public int StartRating = 1000;
        public int EloK = 32;
        /// <summary>Doc ranks: Bronze, Silver, Gold, Platinum, Mythic, Guardian, Shattered King.</summary>
        public List<ArenaRank> Ranks = new List<ArenaRank>();
        public int FreeFightsPerDay = 5;
        public int UnlockStage = 15;
        public List<Amount> WinRewards = new List<Amount>();
        public List<Amount> LossRewards = new List<Amount>();
        public int SeasonDays = 14;
        /// <summary>Season 1 starts here (unix ms, a Monday 00:00 UTC).</summary>
        public long SeasonEpochMs;
        /// <summary>End-of-season rewards per rank name.</summary>
        public Dictionary<string, List<Amount>> SeasonRewards = new Dictionary<string, List<Amount>>();
        public BattleRules Rules = new BattleRules();
    }

    public static class Arena
    {
        public const string Coins = "arenaCoins";

        public static ArenaRank RankOf(GameContent content, int rating)
        {
            ArenaRank rank = null;
            foreach (var r in content.Arena.Ranks)
                if (rating >= r.MinRating && (rank == null || r.MinRating >= rank.MinRating))
                    rank = r;
            return rank;
        }

        /// <summary>Elo change for the attacker; the defender changes by the negative.</summary>
        public static int RatingChange(GameContent content, int attacker, int defender, bool attackerWon)
        {
            // expected score with integer math: 1 / (1 + 10^((d - a) / 400)), table-free approximation via double is fine here:
            // the server alone computes ratings, so cross-platform float differences cannot desync anything
            var expected = 1.0 / (1.0 + System.Math.Pow(10.0, (defender - attacker) / 400.0));
            var change = (int)System.Math.Round(content.Arena.EloK * ((attackerWon ? 1.0 : 0.0) - expected));
            if (attackerWon && change < 1)
                change = 1;
            if (!attackerWon && change > -1)
                change = -1;
            return change;
        }

        /// <summary>The player's arena team as it fights now (formation, leader, village and gear bonuses).</summary>
        public static TeamSnapshot TeamOf(PlayerState state, GameContent content) =>
            TeamSnapshot.Of(Campaign.Allies(state, content, state.Formation));

        public static BattleSetup Setup(GameContent content, TeamSnapshot attackers, TeamSnapshot defenders, ulong seed)
        {
            var setup = new BattleSetup { Seed = seed, Rules = content.Arena.Rules ?? new BattleRules() };
            setup.Allies.AddRange(attackers.ToSetups(content));
            setup.Enemies.AddRange(defenders.ToSetups(content));
            return setup;
        }
    }

    /// <summary>Arena rewards decided by the server after a fight or at season end.</summary>
    [ServerOnly]
    public sealed class GrantRewards : ICommand<bool>
    {
        public string Reason;
        public List<Amount> Rewards = new List<Amount>();

        public bool Apply(CommandContext ctx)
        {
            ctx.Grant(Rewards);
            if (Reason == "arenaWin")
                Quests.Track(ctx, Stat.BattleWon);
            return true;
        }
    }
}
