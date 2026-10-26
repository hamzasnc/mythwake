using System.Collections.Generic;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Math;

namespace Mythwake.Core.Meta
{
    /// <summary>A mission type from the design doc's Hunt Board list.</summary>
    public sealed class HuntMission
    {
        public string Id;
        public string Name;
        /// <summary>Enemies are drawn from this faction when set.</summary>
        public Faction? EnemyFaction;
        /// <summary>Weekly elite hunt instead of a daily one.</summary>
        public bool Weekly;
        /// <summary>Rewards at ★: base + perStage × highest cleared stage.</summary>
        public List<AfkRate> Rewards = new List<AfkRate>();
    }

    public sealed class HuntShopItem
    {
        public string Id;
        public List<Amount> Price = new List<Amount>();
        public List<Amount> Gives = new List<Amount>();
        /// <summary>Set for gear: one piece of this grade, random slot and set.</summary>
        public Grade? GearGrade;
        public int Limit;
        /// <summary>Limit per week instead of per day.</summary>
        public bool Weekly;
    }

    public sealed class HuntConfig
    {
        public int DailyBase = 3;
        /// <summary>One more daily hunt every N hut levels.</summary>
        public int DailyEveryLevels = 3;
        public int WeeklyBase = 1;
        public int WeeklyExtraAtLevel = 6;
        public int RewardPerLevelPermille = 100;
        public int TierRewardPermille = 250;
        public int EnemyLevelEveryHutLevels = 3;
        /// <summary>Enemy level offset for ★, ★★, ★★★.</summary>
        public int[] TierLevelOffset = { -3, 0, 3 };
        public int EliteLevelBonus = 5;
        public int EliteStatBonusPermille = 300;
        public int[] TokensPerTier = { 10, 15, 20 };
        public int WeeklyTokens = 60;
        public List<HuntMission> Missions = new List<HuntMission>();
        public List<HuntShopItem> Shop = new List<HuntShopItem>();
    }

    public sealed class HuntState
    {
        public long Day = -1;
        public List<string> DoneDaily = new List<string>();
        public long Week = -1;
        public List<string> DoneWeekly = new List<string>();
        public long ShopDay = -1;
        public Dictionary<string, int> BoughtDaily = new Dictionary<string, int>();
        public long ShopWeek = -1;
        public Dictionary<string, int> BoughtWeekly = new Dictionary<string, int>();
    }

    /// <summary>One hunt on the board. Generated, never stored: the board is a pure function of player seed and day.</summary>
    public sealed class HuntOffer
    {
        public string Id;
        public bool Weekly;
        public HuntMission Mission;
        /// <summary>1..3 stars.</summary>
        public int Tier;
        public int EnemyLevel;
        public int EnemyCount;
        public int EnemyStars;
        public ulong Seed;
        public List<Amount> Rewards = new List<Amount>();
    }

    public sealed class HuntResult
    {
        public BattleOutcome Outcome;
        public List<Amount> Rewards = new List<Amount>();
    }

    /// <summary>Hunter's Hut and Hunt Board (docs/systems/HUNT_BOARD.md).</summary>
    public static class Hunts
    {
        public const string Tokens = "huntTokens";

        public static BuildingDefinition Hut(GameContent content)
        {
            foreach (var b in content.Village.Buildings)
                if (b.Effect == BuildingEffect.Hunting)
                    return b;
            return null;
        }

        public static int HutLevel(PlayerState state, GameContent content)
        {
            var hut = Hut(content);
            return hut == null ? 0 : Village.Level(state, hut);
        }

        public static int DailyCount(GameContent content, int hutLevel) =>
            hutLevel <= 0 ? 0 : content.Hunts.DailyBase + (hutLevel - 1) / System.Math.Max(1, content.Hunts.DailyEveryLevels);

        public static int WeeklyCount(GameContent content, int hutLevel) =>
            hutLevel <= 0 ? 0 : content.Hunts.WeeklyBase + (hutLevel >= content.Hunts.WeeklyExtraAtLevel ? 1 : 0);

        public static List<HuntOffer> Board(PlayerState state, GameContent content, long nowMs)
        {
            var offers = new List<HuntOffer>();
            var level = HutLevel(state, content);
            if (level <= 0)
                return offers;
            var day = Quests.DayIndex(content, nowMs);
            var week = Quests.WeekIndex(content, nowMs);
            AddOffers(offers, state, content, false, day, DailyCount(content, level), level);
            AddOffers(offers, state, content, true, week, WeeklyCount(content, level), level);
            return offers;
        }

        private static void AddOffers(List<HuntOffer> offers, PlayerState state, GameContent content, bool weekly, long period, int count, int hutLevel)
        {
            var pool = new List<HuntMission>();
            foreach (var m in content.Hunts.Missions)
                if (m.Weekly == weekly)
                    pool.Add(m);
            if (pool.Count == 0 || count <= 0)
                return;
            var rng = new Pcg32(state.RngSeed ^ ((ulong)period * 0x9E3779B97F4A7C15UL), weekly ? 13UL : 11UL);
            var bag = new List<HuntMission>();
            for (var i = 0; i < count; i++)
            {
                if (bag.Count == 0)
                    bag.AddRange(pool);
                var pick = rng.Range(bag.Count);
                var mission = bag[pick];
                bag.RemoveAt(pick);
                var tier = weekly ? 3 : 1 + rng.Range(3);
                offers.Add(Offer(state, content, mission, weekly, tier, $"{(weekly ? "w" : "d")}{period}-{i}", rng.NextUInt(), hutLevel));
            }
        }

        private static HuntOffer Offer(PlayerState state, GameContent content, HuntMission mission, bool weekly, int tier, string id, uint seed, int hutLevel)
        {
            var c = content.Hunts;
            var stage = System.Math.Max(1, state.Campaign.HighestCleared);
            var level = Campaign.EnemyLevel(content, stage) + c.TierLevelOffset[System.Math.Min(tier, c.TierLevelOffset.Length) - 1]
                        + (hutLevel - 1) / System.Math.Max(1, c.EnemyLevelEveryHutLevels) + (weekly ? c.EliteLevelBonus : 0);
            var offer = new HuntOffer
            {
                Id = id, Weekly = weekly, Mission = mission, Tier = tier, Seed = seed,
                EnemyLevel = System.Math.Max(1, level),
                EnemyCount = System.Math.Max(2, Campaign.EnemyCount(content, stage)),
                EnemyStars = Campaign.EnemyStars(content, stage)
            };

            var hut = Hut(content);
            var heroBonus = hut == null ? 0 : Village.TotalBonus(state, content, hut) - hut.EffectPerLevel * hutLevel;
            var scale = Permille.One + c.TierRewardPermille * (tier - 1) + c.RewardPerLevelPermille * (hutLevel - 1) + heroBonus;
            foreach (var r in mission.Rewards)
                offer.Rewards.Add(new Amount(r.Id, r.At(stage) * scale / Permille.One));
            var tokens = weekly ? c.WeeklyTokens : c.TokensPerTier[System.Math.Min(tier, c.TokensPerTier.Length) - 1];
            offer.Rewards.Add(new Amount(Tokens, tokens * scale / Permille.One));
            return offer;
        }

        public static HuntOffer Find(PlayerState state, GameContent content, long nowMs, string id)
        {
            foreach (var o in Board(state, content, nowMs))
                if (o.Id == id)
                    return o;
            return null;
        }

        public static bool IsDone(PlayerState state, GameContent content, HuntOffer offer, long nowMs)
        {
            var h = state.Hunts;
            return offer.Weekly
                ? h.Week == Quests.WeekIndex(content, nowMs) && h.DoneWeekly.Contains(offer.Id)
                : h.Day == Quests.DayIndex(content, nowMs) && h.DoneDaily.Contains(offer.Id);
        }

        public static List<UnitSetup> Enemies(PlayerState state, GameContent content, HuntOffer offer)
        {
            var stage = System.Math.Max(1, state.Campaign.HighestCleared);
            var fighters = new List<HeroDefinition>();
            foreach (var h in content.Heroes.Values)
                if (h.Role != Role.HeroLeader && (offer.Mission.EnemyFaction == null || h.Faction == offer.Mission.EnemyFaction))
                    fighters.Add(h);
            if (fighters.Count < offer.EnemyCount)
                foreach (var h in content.Heroes.Values)
                    if (h.Role != Role.HeroLeader && !fighters.Contains(h))
                        fighters.Add(h);
            fighters.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            var maxGrade = Grade.Transcendent;
            foreach (var gate in content.Campaign.EnemyGradeGates)
                if (stage < gate.BelowStage)
                {
                    maxGrade = gate.MaxGrade;
                    break;
                }
            var gated = fighters.FindAll(h => h.Grade <= maxGrade);
            if (gated.Count >= offer.EnemyCount)
                fighters = gated;

            var rng = new Pcg32(offer.Seed, 17);
            var picked = new List<HeroDefinition>();
            while (picked.Count < offer.EnemyCount && fighters.Count > 0)
            {
                var i = rng.Range(fighters.Count);
                picked.Add(fighters[i]);
                fighters.RemoveAt(i);
            }
            var team = Campaign.Arrange(picked, offer.EnemyLevel, offer.EnemyStars);
            if (offer.Weekly)
                foreach (var u in team)
                {
                    u.Stats.Hp = u.Stats.Hp * (Permille.One + content.Hunts.EliteStatBonusPermille) / Permille.One;
                    u.Stats.Attack = u.Stats.Attack * (Permille.One + content.Hunts.EliteStatBonusPermille) / Permille.One;
                    u.Variant = "boss";
                }
            return team;
        }

        public static BattleSetup BuildBattle(PlayerState state, GameContent content, HuntOffer offer, PendingBattle pending) =>
            new BattleSetup { Seed = pending.Seed, Allies = Campaign.Allies(state, content, pending.Formation), Enemies = Enemies(state, content, offer) };

        public static HuntShopItem ShopItem(GameContent content, string id)
        {
            foreach (var i in content.Hunts.Shop)
                if (i.Id == id)
                    return i;
            throw new RuleViolation("unknown_item", id);
        }

        /// <summary>How many of an item were bought in its current period.</summary>
        public static int Bought(PlayerState state, GameContent content, HuntShopItem item, long nowMs)
        {
            var h = state.Hunts;
            if (item.Weekly)
                return h.ShopWeek == Quests.WeekIndex(content, nowMs) && h.BoughtWeekly.TryGetValue(item.Id, out var w) ? w : 0;
            return h.ShopDay == Quests.DayIndex(content, nowMs) && h.BoughtDaily.TryGetValue(item.Id, out var d) ? d : 0;
        }

        internal static void Roll(PlayerState state, GameContent content, long nowMs)
        {
            var h = state.Hunts;
            var day = Quests.DayIndex(content, nowMs);
            var week = Quests.WeekIndex(content, nowMs);
            if (h.Day != day)
            {
                h.Day = day;
                h.DoneDaily.Clear();
            }
            if (h.Week != week)
            {
                h.Week = week;
                h.DoneWeekly.Clear();
            }
            if (h.ShopDay != day)
            {
                h.ShopDay = day;
                h.BoughtDaily.Clear();
            }
            if (h.ShopWeek != week)
            {
                h.ShopWeek = week;
                h.BoughtWeekly.Clear();
            }
        }
    }

    // ------------------------------------------------------------------ commands

    public sealed class BeginHunt : ICommand<BattleSetup>
    {
        public string HuntId;

        public BattleSetup Apply(CommandContext ctx)
        {
            var state = ctx.State;
            ctx.Require(Hunts.HutLevel(state, ctx.Content) > 0, "hunts_locked");
            var offer = Hunts.Find(state, ctx.Content, ctx.NowMs, HuntId);
            ctx.Require(offer != null, "hunt_expired");
            ctx.Require(!Hunts.IsDone(state, ctx.Content, offer, ctx.NowMs), "hunt_done");
            ctx.Require(Formations.Count(state.Formation) > 0, "formation_empty");

            var seed = ((ulong)ctx.NextRng().NextUInt() << 32) | ctx.NextRng().NextUInt();
            state.Battle = new PendingBattle { Stage = state.Campaign.HighestCleared, HuntId = HuntId, Seed = seed, Formation = (string[])state.Formation.Clone() };
            return Hunts.BuildBattle(state, ctx.Content, offer, state.Battle);
        }
    }

    public sealed class FinishHunt : ICommand<HuntResult>
    {
        public bool AutoUltimates = true;
        public List<int> AutoToggleTimesMs = new List<int>();
        public List<UltimateRequest> UltimateRequests = new List<UltimateRequest>();

        public HuntResult Apply(CommandContext ctx)
        {
            var state = ctx.State;
            ctx.Require(state.Battle != null && state.Battle.HuntId != null && state.Battle.DungeonId == null, "no_battle");
            var pending = state.Battle;
            var offer = Hunts.Find(state, ctx.Content, ctx.NowMs, pending.HuntId);
            if (offer == null)
            {
                // the board rolled over during the fight
                state.Battle = null;
                throw new RuleViolation("hunt_expired");
            }
            var setup = Hunts.BuildBattle(state, ctx.Content, offer, pending);
            setup.AutoUltimates = AutoUltimates;
            setup.AutoToggleTimesMs.AddRange(AutoToggleTimesMs);
            setup.UltimateRequests.AddRange(UltimateRequests);
            var battle = BattleSimulator.Run(setup);

            state.Battle = null;
            var result = new HuntResult { Outcome = battle.Outcome };
            if (battle.Outcome != BattleOutcome.Victory)
                return result;
            Hunts.Roll(state, ctx.Content, ctx.NowMs);
            (offer.Weekly ? state.Hunts.DoneWeekly : state.Hunts.DoneDaily).Add(offer.Id);
            result.Rewards.AddRange(offer.Rewards);
            ctx.Grant(result.Rewards);
            Quests.Track(ctx, Stat.BattleWon);
            Quests.Track(ctx, Stat.HuntCompleted);
            return result;
        }
    }

    public sealed class BuyHuntItem : ICommand<GearItem>
    {
        public string ItemId;

        /// <returns>The gear piece for gear items, otherwise null.</returns>
        public GearItem Apply(CommandContext ctx)
        {
            var state = ctx.State;
            ctx.Require(Hunts.HutLevel(state, ctx.Content) > 0, "hunts_locked");
            var item = Hunts.ShopItem(ctx.Content, ItemId);
            ctx.Require(Hunts.Bought(state, ctx.Content, item, ctx.NowMs) < item.Limit, "sold_out");
            ctx.RequireFunds(item.Price);

            Hunts.Roll(state, ctx.Content, ctx.NowMs);
            ctx.Pay(item.Price);
            var bought = item.Weekly ? state.Hunts.BoughtWeekly : state.Hunts.BoughtDaily;
            bought.TryGetValue(item.Id, out var n);
            bought[item.Id] = n + 1;
            ctx.Grant(item.Gives);
            if (item.GearGrade == null)
                return null;
            var rng = ctx.NextRng();
            return Gear.Create(state, (GearSlot)rng.Range(4), item.GearGrade.Value, (Faction)rng.Range(6));
        }
    }
}
