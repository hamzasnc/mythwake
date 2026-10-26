using System.Collections;
using System.Linq;
using System.IO;
using Mythwake.Core.Meta;
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

            // jump ahead on the throwaway save to reach the Broken Forge, craft and equip a piece
            ui.State.Campaign.HighestCleared = 10;
            foreach (var id in new[] { Ids.Gold, Ids.Wood, Ids.Stone, Ids.GearMaterials })
                ui.State.Add(id, 100_000);
            ui.Authority.Execute(new UpgradeBuilding { BuildingId = "brokenForge" });
            ui.Router.Reset(new VillageScreen());
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

            ui.Router.Reset(new HeroesScreen());
            yield return Shot("heroes", 0.6f);
            ui.Router.Push(new HeroDetailScreen(hero));
            yield return Shot("hero_detail", 0.6f);
            var detail = ui.Router.Current;
            detail.Root.Q<ScrollView>().scrollOffset = new Vector2(0, 1400);
            yield return Shot("hero_gear", 0.6f);

            ui.Router.Reset(new SummonScreen());
            yield return Shot("summon", 0.6f);
            var summon = (SummonScreen)ui.Router.Current;
            summon.PullForAutoPilot(10);
            yield return Shot("summon_result", 2.0f);

            Debug.Log("[AutoPilot] done");
            Application.Quit();
        }
    }
}
