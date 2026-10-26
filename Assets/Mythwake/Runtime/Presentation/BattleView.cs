using System;
using System.Collections.Generic;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Game;
using UnityEngine;

namespace Mythwake.Presentation
{
    public enum FloatingTextKind { Damage, Crit, Heal, Shield, Miss, Status, Advantage }

    /// <summary>
    /// Plays a battle. The simulator computes the whole fight up front; this view replays its event log in time,
    /// adding hit-stop, camera shake and ultimate cut-ins. Manual ultimates re-simulate from the same seed:
    /// everything before the tap is identical, so playback continues seamlessly.
    /// </summary>
    public sealed class BattleView : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private float unitScale = 0.8f;
        [Tooltip("Vertical half-size of the battle camera on 9:16 screens.")]
        [SerializeField] private float baseOrthoSize = 5.6f;
        [Tooltip("The camera widens on taller phones so this many world units always fit horizontally.")]
        [SerializeField] private float minVisibleWidth = 6.6f;
        [SerializeField] private float hitStopSeconds = 0.07f;
        [SerializeField] private float cutInSeconds = 0.95f;

        // side view: allies on the left facing right, enemies mirrored. Front row (slots 0,1) stands closer to the middle.
        private static readonly Vector2[] SlotPositions =
        {
            new Vector2(0.95f, 0.55f), new Vector2(1.1f, -0.95f),
            new Vector2(2.3f, 1.15f), new Vector2(2.45f, -0.2f), new Vector2(2.35f, -1.55f)
        };

        private readonly List<UnitView> _units = new List<UnitView>();
        private BattleSetup _setup;
        private BattleResult _result;
        private readonly List<UltimateRequest> _requests = new List<UltimateRequest>();
        private readonly List<int> _toggles = new List<int>();
        private bool _autoAtStart = true;
        private int _cursor;
        private float _timeMs;
        private float _freeze;
        private float _shake;
        private Vector3 _cameraHome;
        private bool _finished;
        private float _endDelay;

        public float Speed { get; set; } = 1f;
        public bool AutoUltimates { get; private set; } = true;
        public bool Running => _setup != null && !_finished;
        public IReadOnlyList<UnitView> Units => _units;
        public IReadOnlyList<UltimateRequest> Requests => _requests;
        public IReadOnlyList<int> AutoToggles => _toggles;
        public bool AutoAtStart => _autoAtStart;
        public BattleResult Result => _result;
        public float TimeMs => _timeMs;

        public event Action<UnitView, long, FloatingTextKind> FloatingText;
        public event Action<UnitView, string> UltimateCutIn;
        public event Action<UnitView, StatusKind> StatusApplied;
        /// <summary>The off-field Hero Leader used a leader ability (its skill slot).</summary>
        public event Action<SkillSlot> LeaderAbility;
        /// <summary>The equipped Boss Fragment fired its ability.</summary>
        public event Action BossFragmentAbility;
        public event Action<BattleResult> Finished;
        public event Action Changed;

        private ContentCatalog _catalog;
        private SpriteRenderer _background;
        private BattleMotion _motion;

        /// <summary>World height of the horizon in painted backgrounds (the back row stands just below it).</summary>
        private const float Horizon = 2.3f;
        /// <summary>Painted backgrounds have their horizon this far down from the top edge.</summary>
        private const float HorizonFromTop = 0.42f;

        /// <summary>
        /// Puts a painted background behind the fight (chapter or game mode); null keeps the default battlefield.
        /// It is scaled so it covers the whole camera with its horizon at <see cref="Horizon"/>.
        /// </summary>
        public void SetBackground(Sprite sprite)
        {
            if (_background == null)
            {
                var go = new GameObject("Painted Background");
                _background = go.AddComponent<SpriteRenderer>();
                _background.sortingOrder = -9999;
            }
            _background.sprite = sprite;
            _background.enabled = sprite != null;
            if (sprite == null)
                return;
            FitCamera();
            var size = sprite.bounds.size;
            var camHeight = cam.orthographicSize * 2f;
            var camBottom = _cameraHome.y - cam.orthographicSize;
            var camTop = _cameraHome.y + cam.orthographicSize;
            // a little margin so camera shake never shows the edge
            var scale = 1.04f * Mathf.Max(camHeight * cam.aspect / size.x,
                Mathf.Max((Horizon - camBottom) / ((1f - HorizonFromTop) * size.y), (camTop - Horizon) / (HorizonFromTop * size.y)));
            _background.transform.localScale = new Vector3(scale, scale, 1f);
            var top = Horizon + HorizonFromTop * size.y * scale;
            _background.transform.position = new Vector3(_cameraHome.x, top - size.y * scale / 2f, 5f);
        }

        /// <summary>Shows a team standing idle (home screen background). No simulation runs.</summary>
        public void ShowIdle(ContentCatalog catalog, IEnumerable<UnitSetup> team)
        {
            Clear();
            _catalog = catalog;
            FitCamera();
            var index = 0;
            // centre the team on screen instead of on the left half
            foreach (var u in team)
                Spawn(u, index++, Side.Allies, new Vector3(1.6f, -0.3f, 0f));
        }

        public void Begin(ContentCatalog catalog, BattleSetup setup, bool autoUltimates)
        {
            Clear();
            _catalog = catalog;
            _setup = setup;
            AutoUltimates = _autoAtStart = autoUltimates;
            _requests.Clear();
            _toggles.Clear();
            Simulate();
            FitCamera();

            var index = 0;
            foreach (var u in setup.Allies)
                Spawn(u, index++, Side.Allies, Vector3.zero);
            foreach (var u in setup.Enemies)
                Spawn(u, index++, Side.Enemies, Vector3.zero);
            _motion = new BattleMotion(transform, _units);
            _motion.Plan(_result.Events, 0f);
            Changed?.Invoke();
        }

        public void Clear()
        {
            _motion?.Clear();
            _motion = null;
            foreach (var u in _units)
                if (u != null)
                    Destroy(u.gameObject);
            _units.Clear();
            _setup = null;
            _result = null;
            _cursor = 0;
            _timeMs = 0f;
            _freeze = 0f;
            _finished = false;
            _endDelay = 0f;
            if (cam != null && _cameraHome != Vector3.zero)
                cam.transform.position = _cameraHome;
            _shake = 0f;
        }

        private void FitCamera()
        {
            if (cam == null)
                cam = Camera.main;
            if (_cameraHome == Vector3.zero)
                _cameraHome = cam.transform.position;
            cam.orthographicSize = Mathf.Max(baseOrthoSize, minVisibleWidth / (2f * Mathf.Max(0.1f, cam.aspect)));
        }

        private void Spawn(UnitSetup setup, int index, Side side, Vector3 offset)
        {
            var assets = _catalog.Hero(setup.Hero.SpineId ?? setup.Hero.Id);
            if (assets?.skeleton == null && assets?.sprite == null)
            {
                Debug.LogError($"[Battle] no art for {setup.Hero.Id}");
                _units.Add(null);
                return;
            }
            var p = SlotPositions[Mathf.Clamp(setup.Slot, 0, 4)];
            var position = new Vector3(side == Side.Allies ? -p.x : p.x, p.y, 0f) + offset;
            var view = assets.skeleton != null
                ? UnitView.Create(transform, assets.skeleton, setup, index, side, position, unitScale)
                : UnitView.CreateSprite(transform, assets.sprite, setup, index, side, position, unitScale, SpriteHeight(setup.Hero.Body));
            view.SetSortingOrder(Mathf.RoundToInt(-position.y * 100f));
            _units.Add(view);
        }

        /// <summary>Height in world units of a monster drawn as a sprite, by body type (heroes are about 2.4).</summary>
        private static float SpriteHeight(string body) => body switch
        {
            "quadruped" => 1.6f,
            "flying" => 2.1f,
            "floating" => 2.1f,
            "huge" => 2.7f,
            _ => 2.4f
        };

        private void Simulate()
        {
            _setup.AutoUltimates = _autoAtStart;
            _setup.AutoToggleTimesMs.Clear();
            _setup.AutoToggleTimesMs.AddRange(_toggles);
            _setup.UltimateRequests.Clear();
            _setup.UltimateRequests.AddRange(_requests);
            _result = BattleSimulator.Run(_setup);
        }

        /// <summary>Player tapped an ally's portrait. Returns false when the ultimate is not ready.</summary>
        public bool RequestUltimate(int allySlot)
        {
            if (!Running || AutoUltimates)
                return false;
            var unit = _units.Find(u => u != null && u.Side == Side.Allies && u.Slot == allySlot);
            if (unit == null || unit.Dead || unit.Energy < BattleSimulator.MaxEnergy)
                return false;
            // the next simulation step after what has already been shown
            _requests.Add(new UltimateRequest(NextStep(), allySlot));
            Resimulate();
            return true;
        }

        public void SetAutoUltimates(bool auto)
        {
            if (AutoUltimates == auto)
                return;
            AutoUltimates = auto;
            if (!Running)
                return;
            _toggles.Add(NextStep());
            Resimulate();
        }

        /// <summary>The first simulation step after what has already been shown.</summary>
        private int NextStep() => ((int)_timeMs / BattleSimulator.StepMs + 1) * BattleSimulator.StepMs;

        private void Resimulate()
        {
            var shown = _timeMs;
            Simulate();
            _motion?.Plan(_result.Events, shown);
            // events up to the shown time are identical (determinism); continue after them
            _cursor = 0;
            while (_cursor < _result.Events.Count && _result.Events[_cursor].TimeMs <= (int)shown)
                _cursor++;
        }

        private void Update()
        {
            if (cam != null)
                FitCamera();
            if (_setup == null)
                return;
            var dt = Time.deltaTime;
            UpdateShake(dt);

            if (_finished)
                return;
            if (_freeze > 0f)
            {
                _freeze -= dt;
                SetUnitsTimeScale(_freeze > 0f ? 0.05f : Speed);
                return;
            }
            SetUnitsTimeScale(Speed);
            var step = dt * 1000f * Speed;
            _timeMs += step;
            while (_cursor < _result.Events.Count && _result.Events[_cursor].TimeMs <= _timeMs && _freeze <= 0f)
                Apply(_result.Events[_cursor++]);
            _motion?.Tick(_timeMs, step);

            if (_cursor >= _result.Events.Count)
            {
                _endDelay += dt;
                if (_endDelay > 1.2f)
                {
                    _finished = true;
                    Finished?.Invoke(_result);
                }
            }
        }

        private void SetUnitsTimeScale(float scale)
        {
            foreach (var u in _units)
                if (u != null)
                    u.SetTimeScale(scale);
        }

        private UnitView Unit(int index) => index >= 0 && index < _units.Count ? _units[index] : null;

        private void Apply(BattleEvent e)
        {
            var source = Unit(e.Source);
            var target = Unit(e.Target);
            switch (e.Type)
            {
                case BattleEventType.ActionStart:
                    if (source == null)
                    {
                        if (e.Source == _units.Count)
                            LeaderAbility?.Invoke(e.Skill);
                        else if (e.Source == _units.Count + 1)
                            BossFragmentAbility?.Invoke();
                        break;
                    }
                    source.Play(e.Animation ?? "attack");
                    _motion?.Fire(_cursor - 1, _timeMs);
                    if (e.Skill == SkillSlot.Ultimate)
                    {
                        if (source.Side == Side.Allies)
                        {
                            _freeze = cutInSeconds;
                            UltimateCutIn?.Invoke(source, source.HeroId);
                        }
                        Shake(0.12f);
                    }
                    break;
                case BattleEventType.Hit:
                    if (target == null)
                        break;
                    target.SetHp(target.Hp - System.Math.Max(0, e.Value - AbsorbedOnSameHit(e)));
                    target.PlayHit();
                    var crit = (e.Flags & HitFlags.Crit) != 0;
                    FloatingText?.Invoke(target, e.Value, crit ? FloatingTextKind.Crit : (e.Flags & HitFlags.Advantage) != 0 ? FloatingTextKind.Advantage : FloatingTextKind.Damage);
                    if (crit || (e.Flags & HitFlags.LastHit) != 0)
                        _freeze = Mathf.Max(_freeze, hitStopSeconds);
                    if (e.Skill == SkillSlot.Ultimate)
                    {
                        _freeze = Mathf.Max(_freeze, hitStopSeconds * 1.4f);
                        Shake((e.Flags & HitFlags.LastHit) != 0 ? 0.35f : 0.18f);
                    }
                    else if (crit)
                    {
                        Shake(0.12f);
                    }
                    break;
                case BattleEventType.ShieldAbsorbed:
                    if (target != null)
                        target.SetShield(target.Shield - e.Value);
                    break;
                case BattleEventType.Miss:
                case BattleEventType.Dodge:
                    if (target != null)
                        FloatingText?.Invoke(target, 0, FloatingTextKind.Miss);
                    break;
                case BattleEventType.Heal:
                    if (target == null)
                        break;
                    target.SetHp(target.Hp + e.Value);
                    FloatingText?.Invoke(target, e.Value, FloatingTextKind.Heal);
                    break;
                case BattleEventType.ShieldGained:
                    if (target == null)
                        break;
                    target.SetShield(System.Math.Max(target.Shield, e.Value));
                    FloatingText?.Invoke(target, e.Value, FloatingTextKind.Shield);
                    break;
                case BattleEventType.StatusTick:
                    break;
                case BattleEventType.StatusApplied:
                    if (target != null)
                        StatusApplied?.Invoke(target, e.Status);
                    break;
                case BattleEventType.EnergyChanged:
                    source?.SetEnergy((int)e.Value);
                    break;
                case BattleEventType.Death:
                    target?.Die();
                    break;
            }
            Changed?.Invoke();
        }

        /// <summary>A Hit event's value includes shield absorption, which arrives as its own event just before it.</summary>
        private long AbsorbedOnSameHit(BattleEvent hit)
        {
            var i = _cursor - 2;
            if (i < 0)
                return 0;
            var prev = _result.Events[i];
            return prev.Type == BattleEventType.ShieldAbsorbed && prev.Target == hit.Target && prev.TimeMs == hit.TimeMs ? prev.Value : 0;
        }

        private void Shake(float strength) => _shake = Mathf.Max(_shake, strength);

        private void UpdateShake(float dt)
        {
            if (cam == null)
                return;
            if (_shake <= 0.001f)
            {
                cam.transform.position = _cameraHome;
                return;
            }
            var offset = (Vector3)(UnityEngine.Random.insideUnitCircle * _shake * 0.35f);
            cam.transform.position = _cameraHome + offset;
            _shake = Mathf.MoveTowards(_shake, 0f, dt * 2.2f);
        }
    }
}
