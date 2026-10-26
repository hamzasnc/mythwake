using System.Collections.Generic;
using Mythwake.Core.Content;

namespace Mythwake.Core.Meta
{
    public sealed class Amount
    {
        public string Id;
        public long Value;

        public Amount() { }

        public Amount(string id, long value)
        {
            Id = id;
            Value = value;
        }
    }

    /// <summary>Hero upgrade costs and limits.</summary>
    public sealed class ProgressionConfig
    {
        /// <summary>Cost to go from level L to L+1: base + perLevel × L + quadratic × L² / 10.</summary>
        public long LevelExpBase = 60;
        public long LevelExpPerLevel = 0;
        public long LevelExpQuadratic = 180;
        public long LevelGoldBase = 80;
        public long LevelGoldPerLevel = 20;
        public long LevelGoldQuadratic = 60;
        /// <summary>Level cap = LevelCapBase + LevelCapPerStar × (stars − 1).</summary>
        public int LevelCapBase = 30;
        public int LevelCapPerStar = 15;
        /// <summary>Highest star per grade (Common..Transcendent).</summary>
        public int[] MaxStars = { 3, 4, 5, 6, 7 };
        /// <summary>Shards needed to reach star index+2 (2★, 3★, …).</summary>
        public int[] StarShardCost = { 10, 20, 40, 60, 100, 150 };
        /// <summary>Shards granted for a duplicate summon.</summary>
        public int DuplicateShards = 10;
    }

    public sealed class AfkConfig
    {
        /// <summary>Idle rewards stop accumulating after this long (doc: a limit for normal players, a higher one for premium).</summary>
        public long MaxHours = 12;
        /// <summary>Per hour at stage s: base + perStage × s.</summary>
        public List<AfkRate> Rates = new List<AfkRate>();
    }

    public sealed class AfkRate
    {
        public string Id;
        public long Base;
        public long PerStage;
    }

    public sealed class CampaignConfig
    {
        public List<ChapterDefinition> Chapters = new List<ChapterDefinition>();
        /// <summary>Every N-th stage is a boss stage (doc: every 5 or 10 steps).</summary>
        public int BossEvery = 10;
        /// <summary>Enemy level = 1 + (stage − 1) × LevelPerStagePermille / 1000.</summary>
        public int LevelPerStagePermille = 900;
        public int BossStatBonusPermille = 300;
        /// <summary>Enemy stats start at this permille on stage 1 and reach 100 % at EarlyStatStages (gentle first fights).</summary>
        public int EarlyStatStartPermille = 600;
        public int EarlyStatStages = 10;
        /// <summary>Highest enemy grade per stage: stage &lt; FromStage uses MaxGrade. Sorted by FromStage.</summary>
        public List<GradeGate> EnemyGradeGates = new List<GradeGate>();
        /// <summary>Enemy team size by stage: index = minimum stage for that size (1-based). E.g. [1,3,6,12] → 2,3,4,5 enemies.</summary>
        public int[] TeamSizeFromStage = { 1, 3, 6, 12 };
        public int EnemyStarsEveryStages = 40;
        /// <summary>First-clear rewards: base + perStage × s; boss stages add BossRewards.</summary>
        public List<AfkRate> ClearRewards = new List<AfkRate>();
        public List<Amount> BossRewards = new List<Amount>();
        /// <summary>Formation slots unlocked by clearing a stage (doc: start with 2 or 3, unlock more by milestones).</summary>
        public List<SlotUnlock> SlotUnlocks = new List<SlotUnlock>();
    }

    public sealed class GradeGate
    {
        public int BelowStage;
        public Grade MaxGrade;
    }

    public sealed class ChapterDefinition
    {
        public string Id;
        public string Name;
        public int Stages;
    }

    public sealed class SlotUnlock
    {
        public int Slots;
        public int Stage;
    }

    public sealed class BannerDefinition
    {
        public string Id;
        public string Name;
        public Amount Cost;
        public Amount AltCost;
        /// <summary>Permille per grade (Common..Transcendent).</summary>
        public int[] GradeRates = { 500, 380, 105, 15, 0 };
        /// <summary>A Legendary is guaranteed on this pull at the latest (pity).</summary>
        public int LegendaryPity = 50;
        /// <summary>An Epic or better is guaranteed on this pull at the latest.</summary>
        public int EpicPity = 10;
        /// <summary>Empty = every hero of the rolled grade.</summary>
        public List<string> Pool = new List<string>();
    }

    public sealed class StartingState
    {
        public List<Amount> Currencies = new List<Amount>();
        public List<string> Heroes = new List<string>();
        public int FormationSlots = 3;
    }

    /// <summary>All static content the rules need. Loaded once; never mutated at runtime.</summary>
    public sealed class GameContent
    {
        public readonly Dictionary<string, HeroDefinition> Heroes = new Dictionary<string, HeroDefinition>();
        public ProgressionConfig Progression = new ProgressionConfig();
        public AfkConfig Afk = new AfkConfig();
        public CampaignConfig Campaign = new CampaignConfig();
        public List<BannerDefinition> Banners = new List<BannerDefinition>();
        public StartingState Start = new StartingState();
        public VillageConfig Village = new VillageConfig();
        public GearConfig Gear = new GearConfig();
        public QuestConfig Quests = new QuestConfig();
        public HuntConfig Hunts = new HuntConfig();
        public ArenaConfig Arena = new ArenaConfig();

        public HeroDefinition Hero(string id) =>
            Heroes.TryGetValue(id, out var h) ? h : throw new RuleViolation("unknown_hero", id);

        public BannerDefinition Banner(string id)
        {
            foreach (var b in Banners)
                if (b.Id == id)
                    return b;
            throw new RuleViolation("unknown_banner", id);
        }

        public int TotalStages
        {
            get
            {
                var n = 0;
                foreach (var c in Campaign.Chapters)
                    n += c.Stages;
                return n;
            }
        }
    }
}
