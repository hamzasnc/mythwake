using System.Collections;
using System.IO;
using Mythwake.Core.Meta;
using UnityEngine;

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

            ui.Router.Reset(new HeroesScreen());
            yield return Shot("heroes", 0.6f);
            ui.Router.Push(new HeroDetailScreen(ui.State.Heroes[0].HeroId));
            yield return Shot("hero_detail", 0.6f);

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
