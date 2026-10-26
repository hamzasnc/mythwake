using System.Collections.Generic;
using Mythwake.Core.Content;

namespace Mythwake.Core.Meta
{
    /// <summary>One offer of a shop (docs/systems/SHOPS.md).</summary>
    public sealed class ShopItem
    {
        public string Id;
        public string Name;
        public List<Amount> Price = new List<Amount>();
        public List<Amount> Gives = new List<Amount>();
        /// <summary>A gear piece of this grade (random slot and set).</summary>
        public Grade? GearGrade;
        /// <summary>A Fragment of this slot and grade (random Aspect and effect).</summary>
        public FragmentSlot? FragmentSlot;
        public Grade FragmentGrade = Grade.Rare;
        /// <summary>Hero shards of a hero of this grade that changes every day (AFK Arena–style rotation).</summary>
        public Grade? ShardGrade;
        public int ShardAmount;
        /// <summary>0 = unlimited.</summary>
        public int Limit;
        /// <summary>Limit per week instead of per day.</summary>
        public bool Weekly;
    }

    public sealed class ShopDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        /// <summary>Opens with this building at level ≥ 1 (null = always open).</summary>
        public string Building;
        /// <summary>Opens after this campaign stage.</summary>
        public int UnlockStage;
        /// <summary>Only for guild members (Guild Shop).</summary>
        public bool RequiresGuild;
        public List<ShopItem> Items = new List<ShopItem>();
    }

    public sealed class ShopConfig
    {
        public List<ShopDefinition> Shops = new List<ShopDefinition>();
    }

    public sealed class ShopState
    {
        public long Day = -1;
        public long Week = -1;
        /// <summary>"shop/item" → bought this day / week.</summary>
        public Dictionary<string, int> Daily = new Dictionary<string, int>();
        public Dictionary<string, int> Weekly = new Dictionary<string, int>();
    }

    public sealed class ShopPurchase
    {
        public List<Amount> Gives = new List<Amount>();
        public GearItem Gear;
        public FragmentItem Fragment;
        public string ShardHero;
    }

    public static class Shops
    {
        public static ShopDefinition Get(GameContent content, string id)
        {
            foreach (var s in content.Shops.Shops)
                if (s.Id == id)
                    return s;
            throw new RuleViolation("unknown_shop", id);
        }

        public static ShopItem Item(ShopDefinition shop, string id)
        {
            foreach (var i in shop.Items)
                if (i.Id == id)
                    return i;
            throw new RuleViolation("unknown_item", id);
        }

        public static bool Open(PlayerState state, GameContent content, ShopDefinition shop)
        {
            if (state.Campaign.HighestCleared < shop.UnlockStage)
                return false;
            if (shop.RequiresGuild && string.IsNullOrEmpty(state.GuildId))
                return false;
            if (string.IsNullOrEmpty(shop.Building))
                return true;
            foreach (var b in content.Village.Buildings)
                if (b.Id == shop.Building)
                    return Village.Level(state, b) > 0;
            return false;
        }

        private static string Key(ShopDefinition shop, ShopItem item) => shop.Id + "/" + item.Id;

        public static int Bought(PlayerState state, GameContent content, ShopDefinition shop, ShopItem item, long nowMs)
        {
            var s = state.Shops;
            if (item.Weekly)
                return s.Week == Quests.WeekIndex(content, nowMs) && s.Weekly.TryGetValue(Key(shop, item), out var w) ? w : 0;
            return s.Day == Quests.DayIndex(content, nowMs) && s.Daily.TryGetValue(Key(shop, item), out var d) ? d : 0;
        }

        /// <summary>The hero whose shards a rotating offer sells today (same for every player).</summary>
        public static string ShardHero(GameContent content, ShopItem item, long nowMs)
        {
            if (item.ShardGrade == null)
                return null;
            var pool = new List<string>();
            foreach (var h in content.Heroes.Values)
                if (h.Grade == item.ShardGrade.Value)
                    pool.Add(h.Id);
            if (pool.Count == 0)
                return null;
            pool.Sort(string.CompareOrdinal);
            var day = Quests.DayIndex(content, nowMs);
            var offset = (item.Id?.Length ?? 0) * 7;
            return pool[(int)(((day + offset) % pool.Count + pool.Count) % pool.Count)];
        }

        private static void Roll(PlayerState state, GameContent content, long nowMs)
        {
            var s = state.Shops;
            var day = Quests.DayIndex(content, nowMs);
            if (s.Day != day)
            {
                s.Day = day;
                s.Daily.Clear();
            }
            var week = Quests.WeekIndex(content, nowMs);
            if (s.Week != week)
            {
                s.Week = week;
                s.Weekly.Clear();
            }
        }

        internal static void Count(PlayerState state, GameContent content, ShopDefinition shop, ShopItem item, long nowMs)
        {
            Roll(state, content, nowMs);
            var map = item.Weekly ? state.Shops.Weekly : state.Shops.Daily;
            map.TryGetValue(Key(shop, item), out var n);
            map[Key(shop, item)] = n + 1;
        }
    }

    public sealed class BuyShopItem : ICommand<ShopPurchase>
    {
        public string ShopId;
        public string ItemId;

        public ShopPurchase Apply(CommandContext ctx)
        {
            var state = ctx.State;
            var shop = Shops.Get(ctx.Content, ShopId);
            var item = Shops.Item(shop, ItemId);
            ctx.Require(Shops.Open(state, ctx.Content, shop), "shop_locked");
            ctx.Require(item.Limit <= 0 || Shops.Bought(state, ctx.Content, shop, item, ctx.NowMs) < item.Limit, "sold_out");
            ctx.RequireFunds(item.Price);
            ctx.Pay(item.Price);
            Shops.Count(state, ctx.Content, shop, item, ctx.NowMs);

            var result = new ShopPurchase();
            result.Gives.AddRange(item.Gives);
            ctx.Grant(item.Gives);
            if (item.GearGrade != null)
            {
                var rng = ctx.NextRng();
                result.Gear = Gear.Create(state, (GearSlot)rng.Range(4), item.GearGrade.Value, (Faction)rng.Range(6));
            }
            if (item.FragmentSlot != null)
            {
                var rng = ctx.NextRng();
                var aspect = (Aspect)rng.Range(5);
                result.Fragment = Fragments.Create(state, item.FragmentSlot.Value, aspect, item.FragmentGrade,
                    Fragments.RollEffect(rng, ctx.Content, item.FragmentSlot.Value, aspect));
            }
            var hero = Shops.ShardHero(ctx.Content, item, ctx.NowMs);
            if (hero != null && item.ShardAmount > 0)
            {
                result.ShardHero = hero;
                var shards = new Amount(Ids.Shards(hero), item.ShardAmount);
                state.Add(shards.Id, shards.Value);
                result.Gives.Add(shards);
            }
            return result;
        }
    }
}
