using System.Collections.Generic;
using Mythwake.Core.Content;

namespace Mythwake.Core.Meta
{
    public sealed class OwnedHero
    {
        public string HeroId;
        public int Level = 1;
        public int Stars = 1;
        /// <summary>Guardian Awakening done (docs/systems/AWAKENING.md): counts as Transcendent.</summary>
        public bool Awakened;
        /// <summary>Who this hero attacks first (doc: target priorities).</summary>
        public Combat.TargetPriority Target;
        /// <summary>Skill level per SkillSlot (null = all level 1).</summary>
        public int[] SkillLevels;
    }

    public sealed class CampaignState
    {
        /// <summary>Highest stage cleared (0 = none). The next stage to fight is HighestCleared + 1.</summary>
        public int HighestCleared;
    }

    public sealed class AfkState
    {
        /// <summary>Unix ms of the last claim; idle rewards accumulate from here.</summary>
        public long LastClaimMs;
    }

    public sealed class SummonState
    {
        /// <summary>Per banner: pulls since the last Legendary and since the last Epic-or-better.</summary>
        public Dictionary<string, int> PullsSinceLegendary = new Dictionary<string, int>();
        public Dictionary<string, int> PullsSinceEpic = new Dictionary<string, int>();
        public int TotalPulls;
    }

    /// <summary>A battle handed out by <see cref="BeginStageBattle"/> and not finished yet.</summary>
    public sealed class PendingBattle
    {
        public int Stage;
        /// <summary>Set when the battle is a hunt from the Hunt Board instead of a campaign stage.</summary>
        public string HuntId;
        /// <summary>Set when the battle is a dungeon level (Floor) instead of a campaign stage.</summary>
        public string DungeonId;
        /// <summary>Set when the battle is a floor (Floor) of the Trial of Fragments tower of this Aspect.</summary>
        public Aspect? TrialAspect;
        /// <summary>Set when the battle is a level (Floor) of a Shattered Realm.</summary>
        public string RealmId;
        /// <summary>Set when the battle is a Boss Fragment refight (Floor = dungeon level).</summary>
        public string BossFragmentId;
        /// <summary>Set when the battle is a Watchtower defense wave (Floor = wave).</summary>
        public bool DefenseWave;
        public int Floor;
        public ulong Seed;
        public string[] Formation;
    }

    /// <summary>Everything that is saved for one player. Plain data; all changes go through commands.</summary>
    public sealed class PlayerState
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public long CreatedMs;
        public Dictionary<string, long> Currencies = new Dictionary<string, long>();
        public List<OwnedHero> Heroes = new List<OwnedHero>();
        /// <summary>Hero id per formation slot (0,1 front; 2..4 back); null = empty.</summary>
        public string[] Formation = new string[5];
        public string Leader;
        public CampaignState Campaign = new CampaignState();
        public AfkState Afk = new AfkState();
        public SummonState Summon = new SummonState();
        public VillageState Village = new VillageState();
        public List<GearItem> Gear = new List<GearItem>();
        /// <summary>Lifetime counters (see <see cref="Stat"/>), measured by quests.</summary>
        public Dictionary<string, long> Stats = new Dictionary<string, long>();
        public QuestState Quests = new QuestState();
        public HuntState Hunts = new HuntState();
        public DungeonState Dungeons = new DungeonState();
        public TrialState Trials = new TrialState();
        public RealmState Realms = new RealmState();
        public BossFragmentState BossFragments = new BossFragmentState();
        public DefenseState Defense = new DefenseState();
        public ShopState Shops = new ShopState();
        public int GearUidCounter;
        public List<FragmentItem> Fragments = new List<FragmentItem>();
        public int FragmentUidCounter;
        public List<RelicItem> Relics = new List<RelicItem>();
        public int RelicUidCounter;
        public PendingBattle Battle;
        /// <summary>Server-side randomness (summons, battle seeds): seed + how many draws were made.</summary>
        public ulong RngSeed;
        public ulong RngCounter;

        public long Get(string currency) => Currencies.TryGetValue(currency, out var v) ? v : 0;

        public void Add(string currency, long amount)
        {
            var v = Get(currency) + amount;
            if (v == 0)
                Currencies.Remove(currency);
            else
                Currencies[currency] = v;
        }

        public OwnedHero FindHero(string heroId)
        {
            foreach (var h in Heroes)
                if (h.HeroId == heroId)
                    return h;
            return null;
        }
    }
}
