using System.Collections.Generic;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Math;
using Mythwake.Core.Progression;

namespace Mythwake.Core.Meta
{
    public static class Formations
    {
        public static int Count(string[] formation)
        {
            var n = 0;
            if (formation != null)
                foreach (var id in formation)
                    if (!string.IsNullOrEmpty(id))
                        n++;
            return n;
        }

        public static int UnlockedSlots(PlayerState state, GameContent content)
        {
            var slots = content.Start.FormationSlots;
            foreach (var u in content.Campaign.SlotUnlocks)
                if (state.Campaign.HighestCleared >= u.Stage && u.Slots > slots)
                    slots = u.Slots;
            return System.Math.Min(TeamBuilder.MaxSlots, slots);
        }

        /// <summary>Puts the strongest owned fighters into free slots, front-row heroes first. Used for new players and "auto" in the formation screen.</summary>
        public static void AutoFill(PlayerState state, GameContent content)
        {
            var unlocked = UnlockedSlots(state, content);
            var candidates = new List<OwnedHero>();
            foreach (var h in state.Heroes)
                if (content.Hero(h.HeroId).Role != Role.HeroLeader)
                    candidates.Add(h);
            candidates.Sort((a, b) =>
            {
                var pa = TeamBuilder.Power(TeamBuilder.StatsOf(Progress(state, content, a)));
                var pb = TeamBuilder.Power(TeamBuilder.StatsOf(Progress(state, content, b)));
                return pb != pa ? pb.CompareTo(pa) : string.CompareOrdinal(a.HeroId, b.HeroId);
            });
            if (candidates.Count > unlocked)
                candidates.RemoveRange(unlocked, candidates.Count - unlocked);

            var formation = new string[5];
            var front = new List<OwnedHero>();
            var back = new List<OwnedHero>();
            foreach (var h in candidates)
                (content.Hero(h.HeroId).Row == Row.Front ? front : back).Add(h);
            // fill front slots with front-row heroes, then back slots, then spill over
            var queue = new List<OwnedHero>(front);
            queue.AddRange(back);
            var order = new List<int> { 0, 1, 2, 3, 4 };
            if (front.Count == 0)
                order = new List<int> { 2, 3, 4, 0, 1 };
            else if (front.Count == 1)
                order = new List<int> { 0, 2, 3, 4, 1 };
            for (var i = 0; i < queue.Count; i++)
                formation[order[i]] = queue[i].HeroId;
            state.Formation = formation;
        }

        public static List<HeroProgress> ToProgress(PlayerState state, GameContent content, string[] formation)
        {
            var list = new List<HeroProgress>();
            for (var i = 0; i < 5; i++)
            {
                var id = formation != null && i < formation.Length ? formation[i] : null;
                var owned = string.IsNullOrEmpty(id) ? null : state.FindHero(id);
                list.Add(owned == null ? null : Progress(state, content, owned));
            }
            return list;
        }

        /// <summary>An owned hero as the rules see it: level, stars and worn gear.</summary>
        public static HeroProgress Progress(PlayerState state, GameContent content, OwnedHero owned)
        {
            var gear = Gear.StatsFor(state, content, owned.HeroId);
            var hero = content.Hero(owned.HeroId);
            var relic = Relics.SlotOpen(state, content) ? Relics.Equipped(state, owned.HeroId) : null;
            var awakened = Awakening.StatPermille(content, owned);
            var fragments = Fragments.BonusFor(state, content, owned.HeroId, hero.Aspect, Relics.FragmentBoost(content, relic));
            var passives = fragments.Passives;
            if (relic != null)
                passives.AddRange(Relics.Passives(content, relic));
            passives.AddRange(Awakening.Passives(content, owned));
            return new HeroProgress
            {
                Hero = hero, Level = owned.Level, Stars = owned.Stars,
                Flat = new Stats { Hp = gear.Hp, Attack = gear.Attack, Defense = gear.Defense, CritRate = gear.CritRate },
                AttackPermille = fragments.AttackPermille + awakened, HpPermille = fragments.HpPermille + awakened, Passives = passives,
                Priority = owned.Target, SkillPower = Skills.Power(content, owned)
            };
        }
    }

    public static class Campaign
    {
        public static bool IsBoss(GameContent content, int stage) => content.Campaign.BossEvery > 0 && stage % content.Campaign.BossEvery == 0;

        /// <summary>Chapter and 1-based stage within it, e.g. stage 23 with 20-stage chapters → (chapter 2, 3).</summary>
        public static (ChapterDefinition chapter, int index, int number) Locate(GameContent content, int stage)
        {
            var left = stage;
            var number = 1;
            foreach (var c in content.Campaign.Chapters)
            {
                if (left <= c.Stages)
                    return (c, left, number);
                left -= c.Stages;
                number++;
            }
            return (null, left, number);
        }

        public static int EnemyLevel(GameContent content, int stage) =>
            1 + (stage - 1) * content.Campaign.LevelPerStagePermille / Permille.One;

        public static int EnemyCount(GameContent content, int stage)
        {
            var sizes = content.Campaign.TeamSizeFromStage;
            var count = 1;
            for (var i = 0; i < sizes.Length; i++)
                if (stage >= sizes[i])
                    count = i + 2;
            return System.Math.Min(5, count);
        }

        /// <summary>The enemy team of a stage. Pure function of the stage number, so every player meets the same team.</summary>
        public static List<UnitSetup> Enemies(GameContent content, int stage)
        {
            var fighters = new List<HeroDefinition>();
            foreach (var h in content.Heroes.Values)
                if (h.Role != Role.HeroLeader)
                    fighters.Add(h);
            fighters.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            // early stages draw from lower grades, so the first fights do not throw Legendaries at a new player
            var maxGrade = Grade.Transcendent;
            foreach (var gate in content.Campaign.EnemyGradeGates)
                if (stage < gate.BelowStage)
                {
                    maxGrade = gate.MaxGrade;
                    break;
                }
            fighters.RemoveAll(h => h.Grade > maxGrade);

            var rng = new Pcg32((ulong)stage * 7919UL, 3);
            var count = EnemyCount(content, stage);
            var (chapter, index, _) = Locate(content, stage);
            var monsters = new List<HeroDefinition>();
            if (chapter != null)
                foreach (var id in chapter.Monsters)
                    if (content.Monsters.TryGetValue(id, out var m))
                        monsters.Add(m);

            var picked = new List<HeroDefinition>();
            // the last stage of a chapter is its boss, escorted by the chapter's monsters
            if (chapter != null && index == chapter.Stages && chapter.Boss != null && content.Monsters.TryGetValue(chapter.Boss, out var boss))
                picked.Add(boss);
            // a boss counts for two places in the team
            var places = picked.Count > 0 && picked[0].Boss ? System.Math.Max(2, count - 1) : count;
            while (picked.Count < places)
            {
                bool useHero;
                if (monsters.Count == 0)
                    useHero = true;
                else if (picked.Count > 0 && picked[0].Boss)
                    useHero = false; // a boss is escorted by monsters only
                else
                    useHero = rng.Range(Permille.One) < chapter.HeroSharePermille;
                if (useHero)
                {
                    if (fighters.Count == 0)
                        break;
                    var i = rng.Range(fighters.Count);
                    picked.Add(fighters[i]);
                    fighters.RemoveAt(i);
                }
                else
                {
                    // monsters come in packs: the same kind may appear more than once
                    picked.Add(monsters[rng.Range(monsters.Count)]);
                }
            }

            var team = Arrange(picked, EnemyLevel(content, stage), EnemyStars(content, stage));
            var c = content.Campaign;
            if (c.EarlyStatStages > 1 && stage < c.EarlyStatStages)
            {
                var scale = c.EarlyStatStartPermille + (Permille.One - c.EarlyStatStartPermille) * (stage - 1) / (c.EarlyStatStages - 1);
                foreach (var u in team)
                {
                    u.Stats.Hp = System.Math.Max(1, u.Stats.Hp * scale / Permille.One);
                    u.Stats.Attack = System.Math.Max(1, u.Stats.Attack * scale / Permille.One);
                }
            }
            if (IsBoss(content, stage))
                foreach (var u in team)
                {
                    if (u.Hero.Boss)
                    {
                        u.Variant = "boss";
                        continue; // named bosses are strong enough on their own
                    }
                    u.Stats.Hp = u.Stats.Hp * (Permille.One + content.Campaign.BossStatBonusPermille) / Permille.One;
                    u.Stats.Attack = u.Stats.Attack * (Permille.One + content.Campaign.BossStatBonusPermille) / Permille.One;
                    u.Variant = "boss";
                }
            return team;
        }

        public static int EnemyStars(GameContent content, int stage) =>
            1 + (content.Campaign.EnemyStarsEveryStages > 0 ? (stage - 1) / content.Campaign.EnemyStarsEveryStages : 0);

        /// <summary>Puts enemy heroes into a formation (front-row heroes in front) and builds their battle stats.</summary>
        public static List<UnitSetup> Arrange(List<HeroDefinition> heroes, int level, int stars)
        {
            var front = heroes.FindAll(h => h.Row == Row.Front);
            var back = heroes.FindAll(h => h.Row == Row.Back);
            var ordered = new List<HeroProgress>();
            for (var slot = 0; slot < 5; slot++)
                ordered.Add(null);
            Place(ordered, front, new[] { 0, 1, 2, 3, 4 }, level, stars);
            Place(ordered, back, new[] { 2, 3, 4, 0, 1 }, level, stars);
            return TeamBuilder.Build(ordered);
        }

        /// <summary>The player's side of any battle: formation, Hero Leader and village bonuses.</summary>
        public static List<UnitSetup> Allies(PlayerState state, GameContent content, string[] formation, bool withLeader = true)
        {
            var leader = !withLeader || string.IsNullOrEmpty(state.Leader) ? null : state.FindHero(state.Leader);
            var leaderProgress = leader == null ? null : new HeroProgress { Hero = content.Hero(leader.HeroId), Level = leader.Level, Stars = leader.Stars };
            return TeamBuilder.Build(Formations.ToProgress(state, content, formation), leaderProgress, Village.CombatBonus(state, content));
        }

        /// <summary>The Hero Leader as an off-field caster of its leader abilities, or null.</summary>
        public static UnitSetup LeaderUnit(PlayerState state, GameContent content)
        {
            var leader = string.IsNullOrEmpty(state.Leader) ? null : state.FindHero(state.Leader);
            if (leader == null)
                return null;
            var progress = Formations.Progress(state, content, leader);
            return new UnitSetup { Hero = progress.Hero, Level = leader.Level, Slot = -1, Stats = TeamBuilder.StatsOf(progress) };
        }

        private static void Place(List<HeroProgress> slots, List<HeroDefinition> heroes, int[] order, int level, int stars)
        {
            foreach (var h in heroes)
                foreach (var s in order)
                    if (slots[s] == null)
                    {
                        slots[s] = new HeroProgress { Hero = h, Level = level, Stars = stars };
                        break;
                    }
        }

        public static BattleSetup BuildBattle(PlayerState state, GameContent content, PendingBattle pending)
        {
            var setup = new BattleSetup
            {
                Seed = pending.Seed,
                Allies = Allies(state, content, pending.Formation),
                Leader = LeaderUnit(state, content),
                Enemies = Enemies(content, pending.Stage)
            };
            BossFragments.Apply(setup, state, content);
            return setup;
        }

        public static List<Amount> ClearRewards(GameContent content, int stage)
        {
            var list = new List<Amount>();
            foreach (var r in content.Campaign.ClearRewards)
                list.Add(new Amount(r.Id, r.At(stage)));
            if (IsBoss(content, stage))
                foreach (var r in content.Campaign.BossRewards)
                    list.Add(new Amount(r.Id, r.Value));
            return list;
        }
    }

    public static class Afk
    {
        public const long HourMs = 3_600_000;

        public static long MaxMs(GameContent content) => content.Afk.MaxHours * HourMs;

        public static long ElapsedMs(PlayerState state, GameContent content, long nowMs) =>
            System.Math.Max(0, System.Math.Min(MaxMs(content), nowMs - state.Afk.LastClaimMs));

        /// <summary>Rewards waiting in the idle chest. Rates follow the highest cleared stage (doc: scale with the current stage).</summary>
        public static List<Amount> Pending(PlayerState state, GameContent content, long nowMs)
        {
            var elapsed = ElapsedMs(state, content, nowMs);
            var stage = state.Campaign.HighestCleared;
            var list = new List<Amount>();
            foreach (var r in content.Afk.Rates)
            {
                var perHour = r.At(stage);
                var value = perHour * elapsed / HourMs;
                if (value > 0)
                    list.Add(new Amount(r.Id, value));
            }
            return list;
        }
    }

    /// <summary>Skill upgrades (doc: Hero Upgrade System – Skills; docs/systems/SKILLS.md).</summary>
    public static class Skills
    {
        public static readonly SkillSlot[] Upgradable = { SkillSlot.Normal, SkillSlot.Special, SkillSlot.Special2, SkillSlot.Ultimate };

        public static int Level(OwnedHero hero, SkillSlot slot) =>
            hero.SkillLevels != null && (int)slot < hero.SkillLevels.Length ? System.Math.Max(1, hero.SkillLevels[(int)slot]) : 1;

        /// <summary>Highest skill level for the hero's stars.</summary>
        public static int MaxLevel(GameContent content, OwnedHero hero) =>
            System.Math.Min(content.Skills.MaxLevel, content.Skills.MaxLevelBase + hero.Stars);

        public static List<Amount> Cost(GameContent content, int level)
        {
            var list = new List<Amount>();
            foreach (var c in content.Skills.CostBase)
                list.Add(new Amount(c.Id, c.Id == Ids.Gold ? c.Value * level * level : c.Value * level));
            return list;
        }

        public static int[] Power(GameContent content, OwnedHero hero)
        {
            if (hero.SkillLevels == null)
                return null;
            var power = new int[5];
            foreach (var slot in Upgradable)
                power[(int)slot] = content.Skills.PowerPerLevelPermille * (Level(hero, slot) - 1);
            return power;
        }

        public static SkillDefinition Skill(HeroDefinition hero, SkillSlot slot) => slot switch
        {
            SkillSlot.Normal => hero.Normal,
            SkillSlot.Special => hero.Special,
            SkillSlot.Special2 => hero.Special2,
            SkillSlot.Ultimate => hero.Ultimate,
            _ => null
        };
    }

    public static class Progression
    {
        public static int LevelCap(GameContent content, OwnedHero hero) =>
            content.Progression.LevelCapBase + content.Progression.LevelCapPerStar * (hero.Stars - 1);

        /// <summary>Star cap of an owned hero: awakened heroes count as Transcendent.</summary>
        public static int MaxStars(GameContent content, OwnedHero hero) =>
            MaxStars(content, hero.Awakened ? Grade.Transcendent : content.Hero(hero.HeroId).Grade);

        public static int MaxStars(GameContent content, Grade grade)
        {
            var table = content.Progression.MaxStars;
            return table[System.Math.Min((int)grade, table.Length - 1)];
        }

        public static List<Amount> LevelCost(GameContent content, int fromLevel, int levels)
        {
            var p = content.Progression;
            long exp = 0, gold = 0;
            for (var l = fromLevel; l < fromLevel + levels; l++)
            {
                long sq = (long)l * l;
                exp += p.LevelExpBase + p.LevelExpPerLevel * l + p.LevelExpQuadratic * sq / 10;
                gold += p.LevelGoldBase + p.LevelGoldPerLevel * l + p.LevelGoldQuadratic * sq / 10;
            }
            return new List<Amount> { new Amount(Ids.HeroExp, exp), new Amount(Ids.Gold, gold) };
        }

        public static Amount StarCost(GameContent content, int currentStars, string heroId)
        {
            var table = content.Progression.StarShardCost;
            var cost = table[System.Math.Min(currentStars - 1, table.Length - 1)];
            return new Amount(Ids.Shards(heroId), cost);
        }
    }

    public static class Summoning
    {
        public static int GateLevel(PlayerState state, GameContent content)
        {
            foreach (var b in content.Village.Buildings)
                if (b.Effect == BuildingEffect.UnlockSummon)
                    return Village.Level(state, b);
            return 0;
        }

        /// <summary>A hero filter for a focused banner, or null when the focus is missing or unknown.</summary>
        public static System.Func<HeroDefinition, bool> ParseFocus(BannerDefinition banner, string focus)
        {
            if (banner.Focus == BannerFocus.None || string.IsNullOrEmpty(focus))
                return null;
            if (banner.Focus == BannerFocus.Faction && System.Enum.TryParse<Faction>(focus, true, out var faction) && System.Enum.IsDefined(typeof(Faction), faction))
                return h => h.Faction == faction;
            if (banner.Focus == BannerFocus.Aspect && System.Enum.TryParse<Aspect>(focus, true, out var aspect) && System.Enum.IsDefined(typeof(Aspect), aspect))
                return h => h.Aspect == aspect;
            return null;
        }

        public static List<SummonResult> Pull(CommandContext ctx, BannerDefinition banner, int count, System.Func<HeroDefinition, bool> focus = null)
        {
            var state = ctx.State;
            var summon = state.Summon;
            summon.PullsSinceLegendary.TryGetValue(banner.Id, out var sinceLegendary);
            summon.PullsSinceEpic.TryGetValue(banner.Id, out var sinceEpic);
            var rng = ctx.NextRng();
            var results = new List<SummonResult>();

            for (var i = 0; i < count; i++)
            {
                sinceLegendary++;
                sinceEpic++;
                var grade = RollGrade(rng, banner.GradeRates);
                if (banner.LegendaryPity > 0 && sinceLegendary >= banner.LegendaryPity && grade < Grade.Legendary)
                    grade = Grade.Legendary;
                else if (banner.EpicPity > 0 && sinceEpic >= banner.EpicPity && grade < Grade.Epic)
                    grade = Grade.Epic;
                if (grade >= Grade.Legendary)
                    sinceLegendary = 0;
                if (grade >= Grade.Epic)
                    sinceEpic = 0;

                // focused banners: most pulls come from the chosen faction / Aspect (only these draw an extra number)
                var focused = focus != null && rng.Range(Permille.One) < banner.FocusPermille;
                var hero = PickHero(ctx.Content, banner, grade, rng, focused ? focus : null);
                var owned = state.FindHero(hero);
                var r = new SummonResult { HeroId = hero, IsNew = owned == null };
                if (owned == null)
                {
                    state.Heroes.Add(new OwnedHero { HeroId = hero });
                }
                else
                {
                    r.Shards = ctx.Content.Progression.DuplicateShards;
                    state.Add(Ids.Shards(hero), r.Shards);
                }
                results.Add(r);
            }

            summon.PullsSinceLegendary[banner.Id] = sinceLegendary;
            summon.PullsSinceEpic[banner.Id] = sinceEpic;
            summon.TotalPulls += count;
            return results;
        }

        private static Grade RollGrade(Pcg32 rng, int[] rates)
        {
            var total = 0;
            foreach (var r in rates)
                total += r;
            var roll = rng.Range(System.Math.Max(1, total));
            for (var g = 0; g < rates.Length; g++)
            {
                if (roll < rates[g])
                    return (Grade)g;
                roll -= rates[g];
            }
            return Grade.Common;
        }

        private static string PickHero(GameContent content, BannerDefinition banner, Grade grade, Pcg32 rng, System.Func<HeroDefinition, bool> focus)
        {
            if (focus != null)
                for (var g = (int)grade; g >= 0; g--)
                {
                    var focusPool = new List<string>();
                    foreach (var h in content.Heroes.Values)
                        if ((int)h.Grade == g && focus(h) && (banner.Pool.Count == 0 || banner.Pool.Contains(h.Id)))
                            focusPool.Add(h.Id);
                    if (focusPool.Count == 0)
                        continue;
                    focusPool.Sort(string.CompareOrdinal);
                    return focusPool[rng.Range(focusPool.Count)];
                }
            // fall back to lower grades if the pool has no hero of the rolled grade
            for (var g = (int)grade; g >= 0; g--)
            {
                var pool = new List<string>();
                foreach (var h in content.Heroes.Values)
                    if ((int)h.Grade == g && (banner.Pool.Count == 0 || banner.Pool.Contains(h.Id)))
                        pool.Add(h.Id);
                if (pool.Count == 0)
                    continue;
                pool.Sort(string.CompareOrdinal);
                return pool[rng.Range(pool.Count)];
            }
            throw new RuleViolation("empty_banner", banner.Id);
        }
    }
}
