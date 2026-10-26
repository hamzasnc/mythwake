using System.Collections.Generic;

namespace Mythwake.Core.Meta
{
    public sealed class OwnedHero
    {
        public string HeroId;
        public int Level = 1;
        public int Stars = 1;
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
        public int GearUidCounter;
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
