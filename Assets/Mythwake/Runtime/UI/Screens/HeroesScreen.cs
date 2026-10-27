using System.Linq;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using Mythwake.Core.Progression;
using Mythwake.Game;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Owned heroes, strongest first.</summary>
    public sealed class HeroesScreen : UiScreen
    {
        private CurrencyBar _currencies;
        private VisualElement _grid;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            root.AddToClassList("screen--heroes");
            _currencies = new CurrencyBar(Ids.Gold, Ids.HeroExp, Ids.Gems).Into(root);
            var safe = UiKit.Box("safe").Into(root);
            UiKit.Text(Loc.T("heroes.title"), "title").Into(safe);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(safe);
            scroll.style.flexGrow = 1;
            _grid = UiKit.Box("hero-grid").Into(scroll);
            root.Add(MainNav.Build(Ui, Tab.Heroes));
        }

        public override void Refresh()
        {
            _currencies.Refresh(Ui.State);
            _grid.Clear();
            foreach (var owned in Ui.State.Heroes.OrderByDescending(h => UiKit.Power(Ui.State, Ui.Content, h)).ThenBy(h => h.HeroId))
            {
                var id = owned.HeroId;
                var card = UiKit.HeroCard(Ui.Catalog, Ui.Content.Hero(id), owned).Into(_grid);
                card.RegisterCallback<ClickEvent>(_ => Ui.Router.Push(new HeroDetailScreen(id)));
            }
        }
    }

    /// <summary>One hero: art, identity, stats, upgrades and the full kit.</summary>
    public sealed class HeroDetailScreen : UiScreen
    {
        private readonly string _heroId;
        private CurrencyBar _currencies;
        private Label _level;
        private Label _power;
        private VisualElement _stats;
        private Button _levelUp;
        private Button _levelUp5;
        private Label _levelCost;
        private Button _starUp;
        private Label _shards;
        private VisualElement _stars;
        private VisualElement _gear;
        private Label _setBonus;
        private VisualElement _fragments;
        private VisualElement _awakening;
        private VisualElement _targets;
        private VisualElement _skills;
        private VisualElement[] _skillRings;
        private VisualElement _gearPanel;
        private VisualElement _skillsPanel;

        public HeroDetailScreen(string heroId) => _heroId = heroId;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            root.AddToClassList("screen--heroes");
            var hero = Ui.Content.Hero(_heroId);
            _currencies = new CurrencyBar(Ids.Gold, Ids.HeroExp).Into(root);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(root);
            scroll.style.flexGrow = 1;
            var safe = UiKit.Box("safe").Into(scroll);

            // the hero on a stage, AFK Arena style (docs/UI_STYLE.md); details further down
            var stage = UiKit.Box("hero-stage").Into(safe);
            UiKit.IconBtn("back", () => Ui.Router.Pop(), "back-btn").Into(root);
            var names = UiKit.Box("hero-stage__names").Into(stage);
            var gradeRow = UiKit.Box("row", "center").Into(names);
            UiKit.Icon("grade" + hero.Grade, "hero-stage__grade").Into(gradeRow);
            UiKit.Text(Loc.T("grade." + UiKit.Lower(hero.Grade)), "hero-stage__grade-text", "grade--" + UiKit.GradeClass(hero.Grade)).Into(gradeRow);
            UiKit.Text(hero.Name, "hero-stage__name").Into(names);
            UiKit.Text(hero.Title, "hero-stage__title").Into(names);
            var art = UiKit.Box("hero-art").Into(stage);
            var assets = Ui.Catalog.Hero(_heroId);
            var image = assets?.full != null ? assets.full : assets?.portrait;
            if (image != null)
                art.style.backgroundImage = new StyleBackground(image);
            // Aspect, role, faction, race as small emblems under the name
            var emblems = UiKit.Box("row", "center", "hero-stage__emblems").Into(names);
            foreach (var (icon, name) in new[]
                     {
                         ("aspect" + hero.Aspect, Loc.T("aspect." + UiKit.Lower(hero.Aspect))),
                         ("role" + hero.Role, Loc.T("role." + UiKit.Lower(hero.Role))),
                         ("faction" + hero.Faction, Loc.T("faction." + UiKit.Lower(hero.Faction))),
                         ("race" + hero.Race, Loc.T("race." + UiKit.Lower(hero.Race))),
                     })
                UiKit.Icon(icon, "hero-stage__emblem").Into(emblems).tooltip = name;
            // skills as rings left and right of the hero (AFK style); the full texts stay further down
            _skillRings = new[]
            {
                UiKit.Box("hero-stage__skills", "hero-stage__skills--left").Into(stage),
                UiKit.Box("hero-stage__skills", "hero-stage__skills--right").Into(stage)
            };
            // switch to the previous / next owned hero
            UiKit.Box("hero-stage__arrow", "hero-stage__arrow--left").Into(stage).RegisterCallback<ClickEvent>(_ => Switch(-1));
            UiKit.Box("hero-stage__arrow", "hero-stage__arrow--right").Into(stage).RegisterCallback<ClickEvent>(_ => Switch(1));

            // power and stats on a dark band, then costs and the one teal action (AFK style)
            var progress = UiKit.Box("hero-stats").Into(safe);
            var powerRow = UiKit.Box("row", "center").Into(progress);
            UiKit.Icon("power", "hero-stats__power-icon").Into(powerRow);
            _power = UiKit.Text("", "hero-stats__power").Into(powerRow);
            _stars = UiKit.StarRow(0, "hero-stats__stars").Into(powerRow);
            _stats = UiKit.Box("row", "hero-stats__row").Into(progress);
            var actions = UiKit.Box("hero-actions").Into(safe);
            _level = UiKit.Text("", "hero-actions__level").Into(actions);
            _levelCost = UiKit.Text("", "hero-actions__cost").Into(actions);
            var buttons = UiKit.Box("row", "center").Into(actions);
            _levelUp5 = UiKit.Btn(Loc.T("hero.levelUp5"), () => LevelUp(5), "btn--secondary").Into(buttons);
            _levelUp = UiKit.Btn(Loc.T("hero.levelUp"), () => LevelUp(1), "btn--big").Into(buttons);
            _starUp = UiKit.Btn(Loc.T("hero.starUp"), StarUp, "btn--secondary").Into(buttons);
            _shards = UiKit.Text("", "hero-actions__shards").Into(actions);

            var gear = UiKit.Box("panel").Into(safe);
            _gearPanel = gear;
            var gearHead = UiKit.Box("row").Into(gear);
            UiKit.Text(Loc.T("gear.title"), "title").Into(gearHead);
            UiKit.Box("spacer").Into(gearHead);
            _setBonus = UiKit.Text("", "label--good").Into(gearHead);
            _gear = UiKit.Box("gear-row").Into(gear);

            var fragments = UiKit.Box("panel").Into(safe);
            UiKit.Text(Loc.T("fragment.titleRelic"), "title").Into(fragments);
            _fragments = UiKit.Box("gear-row").Into(fragments);

            var target = UiKit.Box("panel").Into(safe);
            UiKit.Text(Loc.T("target.title"), "title").Into(target);
            UiKit.Text(Loc.T("target.hint"), "label--small").Into(target).style.whiteSpace = WhiteSpace.Normal;
            _targets = UiKit.Box("row", "focus-row").Into(target);

            _awakening = UiKit.Box("panel", "awakening").Into(safe);

            var skills = UiKit.Box("panel").Into(safe);
            _skillsPanel = skills;
            // round shortcuts like AFK's hero screen: jump to equipment and skills
            var jumps = UiKit.Box("hero-jumps").Into(root);
            UiKit.SideButton(jumps, "slotWeapon", Loc.T("gear.title"), () => scroll.ScrollTo(_gearPanel));
            UiKit.SideButton(jumps, "skillUltimate", Loc.T("hero.skills"), () => scroll.ScrollTo(_skillsPanel));
            UiKit.Text(Loc.T("hero.skills"), "title").Into(skills);
            _skills = UiKit.Box().Into(skills);
        }

        public override void Refresh()
        {
            var content = Ui.Content;
            var owned = Ui.State.FindHero(_heroId);
            var hero = content.Hero(_heroId);
            _currencies.Refresh(Ui.State);
            if (owned == null)
                return;

            var cap = Progression.LevelCap(content, owned);
            _level.text = $"{Loc.T("heroes.level", owned.Level)} / {cap}";
            UiKit.SetStars(_stars, owned.Stars);
            var stats = TeamBuilder.StatsOf(Formations.Progress(Ui.State, content, owned));
            _power.text = UiKit.Short(TeamBuilder.Power(stats));
            _stats.Clear();
            Stat("statHp", Loc.T("hero.hp"), stats.Hp);
            Stat("statAttack", Loc.T("hero.attack"), stats.Attack);
            Stat("statDefense", Loc.T("hero.defense"), stats.Defense);

            var atCap = owned.Level >= cap;
            var cost = Progression.LevelCost(content, owned.Level, 1);
            _levelCost.text = atCap
                ? Loc.T("hero.maxLevel", owned.Stars)
                : string.Join("   ", cost.Select(c => $"{UiKit.Short(c.Value)} {UiKit.CurrencyName(c.Id)}"));
            var affordable = cost.All(c => Ui.State.Get(c.Id) >= c.Value);
            _levelUp.SetEnabled(!atCap && affordable);
            _levelUp5.SetEnabled(owned.Level + 5 <= cap && Progression.LevelCost(content, owned.Level, 5).All(c => Ui.State.Get(c.Id) >= c.Value));

            RefreshGear();
            FragmentUi.Slots(Ui, _fragments, Root, _heroId, Refresh);
            RefreshAwakening(owned);
            RefreshTargets(owned);
            RefreshSkills(owned);
            _fragments.Add(RelicUi.Slot(Ui, Root, _heroId, Refresh));

            var maxStars = Progression.MaxStars(content, owned);
            if (owned.Stars >= maxStars)
            {
                _shards.text = Loc.T("hero.maxStars");
                _starUp.SetEnabled(false);
            }
            else
            {
                var starCost = Progression.StarCost(content, owned.Stars, _heroId);
                var have = Ui.State.Get(starCost.Id);
                _shards.text = Loc.T("hero.shards", have, starCost.Value);
                _starUp.SetEnabled(have >= starCost.Value);
            }
        }

        /// <summary>Skills with their level; Normal, Special, Special II and Ultimate can be upgraded (docs/systems/SKILLS.md).</summary>
        /// <summary>Opens the previous / next owned hero in place.</summary>
        private void Switch(int step)
        {
            var owned = Ui.State.Heroes.Select(h => h.HeroId).ToList();
            var i = owned.IndexOf(_heroId);
            if (owned.Count < 2 || i < 0)
                return;
            var next = owned[(i + step + owned.Count) % owned.Count];
            Ui.Router.Pop();
            Ui.Router.Push(new HeroDetailScreen(next));
        }

        private static string SkillIcon(SkillSlot slot) => slot switch
        {
            SkillSlot.Normal => "skillNormal",
            SkillSlot.Passive => "skillPassive",
            SkillSlot.Ultimate => "skillUltimate",
            _ => "skillSpecial"
        };

        private void RefreshSkills(OwnedHero owned)
        {
            _skills.Clear();
            var content = Ui.Content;
            var hero = content.Hero(_heroId);
            // the rings beside the hero: the first half left, the rest right
            foreach (var column in _skillRings)
                column.Clear();
            var all = hero.Skills().ToList();
            for (var i = 0; i < all.Count; i++)
            {
                var ring = UiKit.Box("skill-ring").Into(_skillRings[i < (all.Count + 1) / 2 ? 0 : 1]);
                UiKit.Icon(SkillIcon(all[i].Slot), "skill-ring__icon").Into(ring);
                UiKit.Text(Skills.Level(owned, all[i].Slot).ToString(), "skill-ring__level").Into(ring);
                ring.tooltip = all[i].Name;
                ring.RegisterCallback<ClickEvent>(_ => Root.Q<ScrollView>().ScrollTo(_skillsPanel));
            }
            var max = Skills.MaxLevel(content, owned);
            foreach (var skill in hero.Skills())
            {
                var row = UiKit.Box("skill").Into(_skills);
                var head = UiKit.Box("row").Into(row);
                UiKit.Text(Loc.T("slot." + UiKit.Lower(skill.Slot)), "skill__slot").Into(head);
                UiKit.Box("spacer").Into(head);
                var upgradable = System.Array.IndexOf(Skills.Upgradable, skill.Slot) >= 0;
                var level = Skills.Level(owned, skill.Slot);
                if (upgradable)
                    UiKit.Text(Loc.T("skill.level", level, max), "gear__level").Into(head);
                UiKit.Text(skill.Name, "skill__name").Into(row);
                UiKit.Text(skill.Description, "skill__text").Into(row);
                if (!upgradable)
                    continue;
                var line = UiKit.Box("row").Into(row);
                if (level > 1)
                    UiKit.Text(Loc.T("skill.bonus", content.Skills.PowerPerLevelPermille * (level - 1) / 10), "label--good").Into(line);
                UiKit.Box("spacer").Into(line);
                if (level >= max)
                {
                    UiKit.Text(Loc.T("skill.max"), "label--small").Into(line);
                    continue;
                }
                var cost = Skills.Cost(content, level);
                line.Add(GearUi.CostRow(Ui.State, cost));
                var slot = skill.Slot;
                var button = UiKit.Btn(Loc.T("skill.upgrade"), () =>
                {
                    if (Ui.Try(new UpgradeSkill { HeroId = _heroId, Slot = slot }, out int _))
                        Refresh();
                }, "btn--secondary", "btn--small").Into(line);
                button.SetEnabled(cost.All(c => Ui.State.Get(c.Id) >= c.Value));
            }
        }

        private void RefreshTargets(OwnedHero owned)
        {
            _targets.Clear();
            foreach (Core.Combat.TargetPriority p in System.Enum.GetValues(typeof(Core.Combat.TargetPriority)))
            {
                var priority = p;
                var chip = UiKit.Btn(Loc.T("target." + UiKit.Lower(p)), () =>
                {
                    if (Ui.Try(new SetTargetPriority { HeroId = _heroId, Priority = priority }, out bool _))
                        Refresh();
                }, "btn--secondary", "btn--small", "focus-chip").Into(_targets);
                chip.EnableInClassList("focus-chip--active", owned.Target == p);
            }
        }

        /// <summary>Guardian Awakening panel: the doc's form, requirements and cost, or the awakened passive and Trait.</summary>
        private void RefreshAwakening(OwnedHero owned)
        {
            _awakening.Clear();
            var def = Awakening.Of(Ui.Content, _heroId);
            _awakening.style.display = def == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (def == null)
                return;
            var head = UiKit.Box("row").Into(_awakening);
            UiKit.Text(Loc.T("awaken.title"), "title", "screen-title").Into(head);
            UiKit.Box("spacer").Into(head);
            if (owned.Awakened)
                UiKit.Text(Loc.T("grade.transcendent"), "tag", "grade--transcendent").Into(head);
            UiKit.Text(def.Description, "skill__text").Into(_awakening);
            UiKit.Text(def.PassiveText, owned.Awakened ? "label--good" : "label--small").Into(_awakening).style.whiteSpace = WhiteSpace.Normal;
            UiKit.Text(def.TraitText, owned.Awakened ? "label--good" : "label--small").Into(_awakening).style.whiteSpace = WhiteSpace.Normal;
            if (owned.Awakened)
            {
                UiKit.Text(Loc.T("awaken.awakened"), "subtitle").Into(_awakening);
                return;
            }
            var content = Ui.Content;
            void Req(bool ok, string text)
            {
                var line = UiKit.Box("row").Into(_awakening);
                UiKit.Icon(ok ? "check" : "close", "requirement__icon").Into(line);
                UiKit.Text(text, ok ? "label--good" : "label--bad").Into(line);
            }
            Req(Awakening.ShrineLevel(Ui.State, content) > 0, Loc.T("awaken.req.shrine"));
            Req(Ui.State.Campaign.HighestCleared >= content.Awakening.MinStage, Loc.T("awaken.req.stage", content.Awakening.MinStage));
            var stars = Progression.MaxStars(content, content.Hero(_heroId).Grade);
            Req(owned.Stars >= stars, Loc.T("awaken.req.stars", stars));
            _awakening.Add(GearUi.CostRow(Ui.State, content.Awakening.Cost));
            var button = UiKit.Btn(Loc.T("awaken.button"), () =>
            {
                if (Ui.Try(new AwakenHero { HeroId = _heroId }, out bool _))
                    Refresh();
            }, "btn--big").Into(_awakening);
            button.SetEnabled(Awakening.Blocker(Ui.State, content, _heroId) == null);
        }

        private void RefreshGear()
        {
            _gear.Clear();
            foreach (GearSlot slot in System.Enum.GetValues(typeof(GearSlot)))
            {
                var worn = Gear.Equipped(Ui.State, _heroId, slot);
                var tile = worn != null ? GearUi.Tile(Ui, worn) : GearUi.EmptyTile(slot);
                var s = slot;
                tile.RegisterCallback<ClickEvent>(_ => GearUi.OpenSlot(Ui, Root, _heroId, s, Refresh));
                _gear.Add(tile);
            }
            var stats = Gear.StatsFor(Ui.State, Ui.Content, _heroId);
            _setBonus.text = stats.SetBonusActive ? Loc.T("gear.setActive") : "";
        }

        /// <summary>Round emblem with the name underneath (Aspect, role, faction, race on the hero stage).</summary>
        private static void Emblem(VisualElement column, string icon, string name)
        {
            var e = UiKit.Box("emblem").Into(column);
            UiKit.Icon(icon, "emblem__icon").Into(e);
            UiKit.Text(name, "emblem__label").Into(e);
        }

        private void Stat(string icon, string name, long value)
        {
            var row = UiKit.Box("stat-chip").Into(_stats);
            row.tooltip = name;
            UiKit.Icon(icon, "stat-chip__icon").Into(row);
            UiKit.Text(UiKit.Short(value)).Into(row);
        }

        private void LevelUp(int levels)
        {
            if (Ui.Try(new LevelUpHero { HeroId = _heroId, Levels = levels }, out _))
                Refresh();
        }

        private void StarUp()
        {
            if (Ui.Try(new StarUpHero { HeroId = _heroId }, out _))
                Refresh();
        }
    }
}
