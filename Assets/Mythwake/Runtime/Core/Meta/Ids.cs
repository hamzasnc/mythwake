namespace Mythwake.Core.Meta
{
    /// <summary>Currency and item ids. Names follow docs/GAME_DESIGN.md.</summary>
    public static class Ids
    {
        public const string Gold = "gold";
        public const string HeroExp = "heroExp";
        public const string Wood = "wood";
        public const string Stone = "stone";
        public const string Gems = "gems";
        public const string SummonScroll = "summonScroll";
        public const string FragmentDust = "fragmentDust";
        public const string GuardianSigil = "guardianSigil";
        public const string GearMaterials = "gearMaterials";
        public const string HuntTokens = "huntTokens";
        public const string ArenaCoins = "arenaCoins";
        public const string RelicMaterials = "relicMaterials";
        /// <summary>Doc: a key that opens a Shattered Realm for one more entry.</summary>
        public const string RealmKey = "realmKey";
        /// <summary>Doc: "Upgrade stones" (Trial of Fragments reward), used for skill upgrades.</summary>
        public const string UpgradeStones = "upgradeStones";
        /// <summary>Doc: "Guild Coins" (Guardian Raid reward), spent in the Guild Shop.</summary>
        public const string GuildCoins = "guildCoins";

        /// <summary>Hero shards are per hero: "shards.&lt;heroId&gt;".</summary>
        public static string Shards(string heroId) => "shards." + heroId;
    }
}
