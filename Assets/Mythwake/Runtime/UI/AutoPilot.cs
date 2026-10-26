using System.Collections;
using System.Linq;
using System.IO;
using Mythwake.Core.Meta;
using Mythwake.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>
    /// Scripted smoke run for builds: "-autopilot &lt;dir&gt;" walks through the main screens, plays a stage and
    /// writes a screenshot per step, then quits. Used to check builds without a person at the device.
    /// </summary>
    public static class AutoPilot
    {
        public static string OutputDir()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == "-autopilot")
                    return args[i + 1];
            return null;
        }

        public static IEnumerator Run(UiContext ui, string dir)
        {
            Directory.CreateDirectory(dir);
            var step = 0;

            IEnumerator Shot(string name, float wait)
            {
                yield return new WaitForSeconds(wait);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, $"{++step:00}_{name}.png"));
                Debug.Log($"[AutoPilot] {step:00}_{name}");
            }

            yield return Shot("home", 1.5f);
            if (ui.Server != null)
            {
                yield return RunOnline(ui, Shot);
                Application.Quit();
                yield break;
            }

            ui.Router.Push(new FormationScreen());
            yield return Shot("formation", 0.8f);

            ui.Authority.Execute(new SetFormation { Slots = (string[])ui.State.Formation.Clone(), Leader = ui.State.Leader });
            var setup = ui.Authority.Execute(new BeginStageBattle()).Value;
            ui.Router.Push(new BattleScreen(setup, ui.State.Battle.Stage));
            ui.Battle.Speed = 1f;
            yield return Shot("battle_start", 1.2f);
            yield return Shot("battle_mid", 2.5f);
            yield return Shot("battle_late", 3.5f);
            var timeout = 120f;
            while (ui.Battle.Running && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            yield return Shot("result", 0.6f);

            ui.Router.Reset(new HomeScreen());
            yield return Shot("home_quest", 0.6f);
            ui.Router.Push(new QuestScreen());
            yield return Shot("quests_main", 0.6f);
            ui.Authority.Execute(new ClaimMainQuest());
            var tabs = ui.Router.Current.Root.Query<Button>(className: "tab").ToList();
            using (var click = NavigationSubmitEvent.GetPooled())
            {
                click.target = tabs[1];
                tabs[1].SendEvent(click);
            }
            yield return Shot("quests_daily", 0.6f);

            // second stage straight through the rules (no playback), then rebuild the Summoning Gate
            ui.Authority.Execute(new BeginStageBattle());
            ui.Authority.Execute(new FinishStageBattle());
            ui.Router.Reset(new SummonScreen());
            yield return Shot("summon_locked", 0.6f);
            ui.Router.Reset(new VillageScreen());
            yield return Shot("village", 0.8f);
            ui.Authority.Execute(new UpgradeBuilding { BuildingId = "summoningGate" });
            ui.Authority.Execute(new AssignVillageHero { BuildingId = "survivorCamp", Slot = 0, HeroId = ui.State.Heroes[0].HeroId });
            yield return Shot("village_built", 0.8f);
            if (ui.Router.Current is VillageScreen camp)
                camp.OpenBuilding("survivorCamp");
            yield return Shot("village_camp", 0.6f);

            // jump ahead on the throwaway save to reach the Broken Forge, craft and equip a piece
            ui.State.Campaign.HighestCleared = 10;
            foreach (var id in new[] { Ids.Gold, Ids.Wood, Ids.Stone, Ids.GearMaterials })
                ui.State.Add(id, 100_000);
            ui.Authority.Execute(new UpgradeBuilding { BuildingId = "brokenForge" });
            var forgeScreen = new VillageScreen();
            ui.Router.Reset(forgeScreen);
            forgeScreen.OpenBuilding("brokenForge");
            yield return Shot("forge", 0.8f);
            var hero = ui.State.Formation.First(h => !string.IsNullOrEmpty(h));
            foreach (GearSlot slot in System.Enum.GetValues(typeof(GearSlot)))
            {
                var item = ui.Authority.Execute(new CraftGear { Slot = slot }).Value;
                ui.Authority.Execute(new EquipGear { HeroId = hero, Uid = item.Uid });
            }

            ui.State.Campaign.HighestCleared = 12;
            ui.Authority.Execute(new UpgradeBuilding { BuildingId = "huntersHut" });
            ui.Router.Reset(new HuntScreen());
            yield return Shot("hunt_board", 0.8f);
            var hunt = Hunts.Board(ui.State, ui.Content, ui.Authority.NowMs).OrderBy(o => o.Tier).First(o => !o.Weekly);
            var huntSetup = ui.Authority.Execute(new BeginHunt { HuntId = hunt.Id }).Value;
            ui.Router.Push(new BattleScreen(huntSetup, new HuntBattle(hunt.Mission.Name)));
            ui.Battle.Speed = 2f;
            yield return Shot("hunt_battle", 2.0f);
            timeout = 120f;
            while (ui.Battle.Running && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            yield return Shot("hunt_result", 0.6f);
            ui.State.Add(Ids.HuntTokens, 300);
            ui.Router.Reset(new HuntScreen());
            var huntTabs = ui.Router.Current.Root.Query<Button>(className: "tab").ToList();
            using (var click = NavigationSubmitEvent.GetPooled())
            {
                click.target = huntTabs[1];
                huntTabs[1].SendEvent(click);
            }
            yield return Shot("hunt_shop", 0.6f);

            ui.State.Campaign.HighestCleared = 20;
            ui.Authority.Execute(new UpgradeBuilding { BuildingId = "portalChamber" });
            ui.Authority.Execute(new UpgradeBuilding { BuildingId = "portalChamber" });
            ui.Router.Reset(new AdventureScreen());
            yield return Shot("adventure", 0.8f);
            ui.Router.Reset(new DungeonScreen());
            yield return Shot("dungeons", 1.0f);
            var dungeonSetup = ui.Authority.Execute(new BeginDungeon { DungeonId = "material", Level = 1 }).Value;
            ui.Router.Push(new BattleScreen(dungeonSetup, new DungeonBattle("Material Dungeon", 1)));
            ui.Battle.Speed = 2f;
            yield return Shot("dungeon_battle", 2.5f);
            timeout = 120f;
            while (ui.Battle.Running && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            yield return Shot("dungeon_result", 0.6f);

            // Boss Fragments: give the Vault Sentinel's Fragment, equip it, show the screen
            ui.State.BossFragments.Owned["bf_obsidian_vault_sentinel"] = new BossFragmentProgress { Level = 2, Copies = 1 };
            ui.Authority.Execute(new EquipBossFragment { FragmentId = "bf_obsidian_vault_sentinel" });
            ui.Router.Reset(new BossFragmentScreen());
            yield return Shot("boss_fragments", 0.8f);

            // Watchtower defense: rebuild, start a run, fight wave 1
            ui.State.Campaign.HighestCleared = System.Math.Max(ui.State.Campaign.HighestCleared, 35);
            ui.State.Add(Ids.Gold, 200_000);
            ui.State.Add(Ids.Wood, 200_000);
            ui.State.Add(Ids.Stone, 200_000);
            ui.Authority.Execute(new UpgradeBuilding { BuildingId = "watchtower" });
            ui.Authority.Execute(new StartDefense());
            ui.Router.Reset(new DefenseScreen());
            yield return Shot("defense", 0.6f);
            var waveSetup = ui.Authority.Execute(new BeginDefenseWave()).Value;
            if (waveSetup != null)
            {
                ui.Router.Push(new BattleScreen(waveSetup, new DefenseBattle(1)));
                ui.Battle.Speed = 2f;
                yield return Shot("defense_battle", 2.0f);
                timeout = 120f;
                while (ui.Battle.Running && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }
                yield return Shot("defense_result", 0.6f);
            }

            // Fragment Shrine: rebuild to level 5, stabilize one Fragment per slot, equip them on the first hero
            ui.State.Add(Ids.FragmentDust, 20_000);
            ui.State.Add(Ids.Gold, 200_000);
            for (var i = 0; i < 5; i++)
                ui.Authority.Execute(new UpgradeBuilding { BuildingId = "fragmentShrine" });
            foreach (FragmentSlot slot in System.Enum.GetValues(typeof(FragmentSlot)))
            {
                var fragment = ui.Authority.Execute(new StabilizeFragment { Slot = slot }).Value;
                if (fragment != null)
                    ui.Authority.Execute(new EquipFragment { HeroId = hero, Uid = fragment.Uid });
            }
            for (var i = 0; i < 4; i++)
                ui.Authority.Execute(new StabilizeFragment { Slot = FragmentSlot.Echo });
            var shrineScreen = new VillageScreen();
            ui.Router.Reset(shrineScreen);
            shrineScreen.OpenBuilding("fragmentShrine");
            yield return Shot("fragment_shrine", 0.8f);
            ui.Router.Push(new FragmentScreen());
            yield return Shot("fragments", 0.6f);

            // Trial of Fragments: Shrine level 4 opens the towers; climb one floor of the first tower with heroes
            ui.State.Add(Ids.Gold, 500_000);
            ui.State.Add(Ids.Wood, 500_000);
            ui.State.Add(Ids.Stone, 500_000);
            ui.Router.Reset(new TrialScreen());
            yield return Shot("trials", 0.8f);
            for (var i = 0; i < 5; i++)
                ui.Authority.Execute(new UpgradeBuilding { BuildingId = "portalChamber" });
            ui.Router.Reset(new RealmScreen());
            yield return Shot("realms", 0.8f);
            ui.Router.Reset(new AdventureScreen());
            yield return Shot("adventure_open", 0.8f);
            var trial = ui.Content.Trials.Trials.First(t => ui.State.Heroes.Any(h => ui.Content.Hero(h.HeroId).Aspect == t.Aspect));
            ui.Router.Push(new FormationScreen(new TrialFormation(trial)));
            yield return Shot("trial_team", 0.6f);
            var trialSetup = ui.Authority.Execute(new BeginTrial { Aspect = trial.Aspect, Slots = Trials.SuggestedTeam(ui.State, ui.Content, trial.Aspect) }).Value;
            if (trialSetup != null)
            {
                ui.Router.Push(new BattleScreen(trialSetup, new TrialBattle(trial.Name, ui.State.Battle.Floor)));
                ui.Battle.Speed = 2f;
                yield return Shot("trial_battle", 2.5f);
                timeout = 120f;
                while (ui.Battle.Running && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }
                yield return Shot("trial_result", 0.6f);
            }
            foreach (var relicId in new[] { "relic_hollowtusk_tusk", "relic_treant_root", "relic_dragon_scale" })
                Relics.Create(ui.State, relicId);
            ui.Authority.Execute(new EquipRelic { HeroId = hero, Uid = ui.State.Relics[ui.State.Relics.Count - 1].Uid });
            ui.Router.Reset(new RelicScreen());
            yield return Shot("relics", 0.6f);

            // Guardian Awakening: Elara at max stars, Guardian Shrine rebuilt, awaken her
            ui.State.Campaign.HighestCleared = System.Math.Max(ui.State.Campaign.HighestCleared, 60);
            foreach (var id in new[] { Ids.GuardianSigil, Ids.FragmentDust, Ids.RelicMaterials, Ids.Gold, Ids.Wood, Ids.Stone })
                ui.State.Add(id, 500_000);
            if (ui.State.FindHero("elara") == null)
                ui.State.Heroes.Add(new OwnedHero { HeroId = "elara", Level = 80, Stars = 6 });
            ui.Authority.Execute(new UpgradeBuilding { BuildingId = "guardianShrine" });
            ui.Router.Reset(new HeroesScreen());
            ui.Router.Push(new HeroDetailScreen("elara"));
            ui.Router.Current.Root.Q<ScrollView>().scrollOffset = new Vector2(0, 2600);
            yield return Shot("awakening_before", 0.6f);
            ui.Authority.Execute(new AwakenHero { HeroId = "elara" });
            ui.Router.Current.Refresh();
            yield return Shot("awakening_after", 0.6f);

            ui.Router.Reset(new HeroesScreen());
            yield return Shot("heroes", 0.6f);
            ui.Router.Push(new HeroDetailScreen(hero));
            yield return Shot("hero_detail", 0.6f);
            var detail = ui.Router.Current;
            detail.Root.Q<ScrollView>().scrollOffset = new Vector2(0, 1400);
            yield return Shot("hero_gear", 0.6f);
            detail.Root.Q<ScrollView>().scrollOffset = new Vector2(0, 2000);
            yield return Shot("hero_fragments", 0.6f);

            ui.Router.Reset(new SummonScreen());
            yield return Shot("summon", 0.6f);
            var summon = (SummonScreen)ui.Router.Current;
            summon.PullForAutoPilot(10);
            yield return Shot("summon_result", 2.0f);
            for (var i = 0; i < 2; i++)
                ui.Authority.Execute(new UpgradeBuilding { BuildingId = "summoningGate" });
            ui.Router.Reset(new SummonScreen());
            ((SummonScreen)ui.Router.Current).SelectForAutoPilot("fragment");
            yield return Shot("summon_fragment", 0.6f);

            Debug.Log("[AutoPilot] done");
            Application.Quit();
        }

        /// <summary>Against a running server: real commands, sync check, arena (when the account has unlocked it).</summary>
        private static IEnumerator RunOnline(UiContext ui, System.Func<string, float, IEnumerator> shot)
        {
            var server = ui.Server;
            var resyncs = 0;
            server.Resynced += _ => resyncs++;
            for (var i = 0; i < 3; i++)
            {
                ui.Authority.Execute(new BeginStageBattle());
                ui.Authority.Execute(new FinishStageBattle());
            }
            ui.Authority.Execute(new ClaimAfkRewards());
            yield return server.Flush();
            var hash = Mythwake.Content.SaveJson.Hash(ui.State);
            Debug.Log($"[AutoPilot] online: stage {ui.State.Campaign.HighestCleared}, pending {server.Pending}, resyncs {resyncs}, hash {hash.Substring(0, 12)}");

            ui.Router.Reset(new HomeScreen());
            yield return shot("online_home", 0.8f);
            ui.Router.Reset(new ArenaScreen());
            yield return shot("arena", 2.0f);

            ApiResponse arena = null;
            yield return server.Api.Send("GET", "/v1/arena", null, r => arena = r);
            if (arena.Ok && arena.Body.Value<bool>("unlocked"))
            {
                yield return server.Api.Send("POST", "/v1/arena/defense", new Newtonsoft.Json.Linq.JObject(), _ => { });
                ui.Router.Reset(new ArenaScreen());
                yield return shot("arena_opponents", 2.0f);
                var fight = ui.Router.Current.Root.Query<Button>().ToList().FirstOrDefault(b => b.text == Loc.T("arena.fight") && b.enabledSelf);
                if (fight != null)
                {
                    using (var click = NavigationSubmitEvent.GetPooled())
                    {
                        click.target = fight;
                        fight.SendEvent(click);
                    }
                    yield return shot("arena_battle", 3.0f);
                    var timeout = 120f;
                    while ((ui.Battle.Running || !(ui.Router.Current is BattleScreen)) && timeout > 0f)
                    {
                        timeout -= Time.deltaTime;
                        yield return null;
                    }
                    yield return shot("arena_result", 1.0f);
                }
            }
            Debug.Log($"[AutoPilot] online done, resyncs {resyncs}");
        }
    }
}
