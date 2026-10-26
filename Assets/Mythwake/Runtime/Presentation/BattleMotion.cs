using System.Collections.Generic;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using UnityEngine;

namespace Mythwake.Presentation
{
    /// <summary>
    /// Movement and projectiles on top of the replayed battle log (presentation only: the simulation and the server
    /// never see positions). Melee units run to the target of their next attack so they arrive just before its first
    /// hit and stay engaged there, like AFK Arena; ranged units fire one projectile per hit that lands exactly on the
    /// hit's time; huge "reach" units hit from where they stand.
    /// </summary>
    internal sealed class BattleMotion
    {
        private const float RunSpeed = 7f;
        private const float MaxRunSpeed = 22f;
        private const int ArriveEarlyMs = 70;
        private const float ProjectileSpeed = 12f;

        private struct Engagement
        {
            public int Target;
            public int FirstHitMs;
            public int StartMs;
        }

        private readonly Transform _root;
        private readonly List<UnitView> _units;
        private readonly Dictionary<int, List<Engagement>> _plans = new Dictionary<int, List<Engagement>>();
        private readonly Dictionary<int, int> _next = new Dictionary<int, int>();
        private readonly List<Projectile> _shots = new List<Projectile>();
        private IReadOnlyList<BattleEvent> _events;

        public BattleMotion(Transform root, List<UnitView> units)
        {
            _root = root;
            _units = units;
        }

        /// <summary>Reads the whole log once (again after a re-simulation; earlier events are identical).</summary>
        public void Plan(IReadOnlyList<BattleEvent> events, float nowMs)
        {
            _events = events;
            _plans.Clear();
            _next.Clear();
            for (var i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e.Type != BattleEventType.ActionStart)
                    continue;
                var source = Unit(e.Source);
                if (source == null || source.Range != AttackRange.Melee || !Hostile(source, Unit(e.Target)))
                    continue;
                var firstHit = FirstHit(i, e.TimeMs + e.DurationMs);
                if (firstHit < 0)
                    continue;
                if (!_plans.TryGetValue(e.Source, out var list))
                    _plans[e.Source] = list = new List<Engagement>();
                list.Add(new Engagement { Target = e.Target, FirstHitMs = firstHit, StartMs = e.TimeMs });
            }
            foreach (var kv in _plans)
            {
                var k = 0;
                while (k < kv.Value.Count && kv.Value[k].FirstHitMs < nowMs)
                    k++;
                _next[kv.Key] = k;
            }
        }

        /// <summary>Time of the first hit (or miss) of the action that starts at event <paramref name="start"/>.</summary>
        private int FirstHit(int start, int endMs)
        {
            var source = _events[start].Source;
            for (var j = start + 1; j < _events.Count && _events[j].TimeMs <= endMs; j++)
            {
                var e = _events[j];
                if (e.Source != source)
                    continue;
                if (e.Type == BattleEventType.ActionStart)
                    break;
                if (e.Type == BattleEventType.Hit || e.Type == BattleEventType.Miss || e.Type == BattleEventType.Dodge)
                    return e.TimeMs;
            }
            return -1;
        }

        private UnitView Unit(int index) => index >= 0 && index < _units.Count ? _units[index] : null;

        private static bool Hostile(UnitView a, UnitView b) => a != null && b != null && a.Side != b.Side;

        /// <summary>Advances runs and projectiles to battle time <paramref name="nowMs"/> (<paramref name="dtMs"/> since the last call).</summary>
        public void Tick(float nowMs, float dtMs)
        {
            // which melee units go for which target right now: they spread around it instead of stacking
            _attackers.Clear();
            foreach (var kv in _plans)
            {
                var unit = Unit(kv.Key);
                var k = _next[kv.Key];
                if (unit == null || unit.Dead || k >= kv.Value.Count)
                    continue;
                if (!_attackers.TryGetValue(kv.Value[k].Target, out var group))
                    _attackers[kv.Value[k].Target] = group = new List<int>();
                group.Add(kv.Key);
            }
            foreach (var kv in _plans)
            {
                var unit = Unit(kv.Key);
                if (unit == null || unit.Dead)
                    continue;
                var list = kv.Value;
                var k = _next[kv.Key];
                while (k < list.Count && list[k].FirstHitMs < nowMs)
                    k++;
                _next[kv.Key] = k;
                if (k >= list.Count)
                    continue;
                var plan = list[k];
                var target = Unit(plan.Target);
                if (target == null)
                    continue;
                var place = _attackers.TryGetValue(plan.Target, out var around) ? around.IndexOf(kv.Key) : 0;
                var goal = ContactPoint(unit, target, Mathf.Max(0, place));
                var distance = Vector3.Distance(unit.Ground, goal);
                if (distance < 0.05f)
                    continue;
                // leave just in time: never before the action that hits, and arrive a little before the hit
                var leftMs = plan.FirstHitMs - ArriveEarlyMs - nowMs;
                var travelMs = distance / RunSpeed * 1000f;
                if (nowMs < plan.StartMs && leftMs > travelMs)
                    continue;
                var speed = leftMs > 1f ? Mathf.Clamp(distance / (leftMs / 1000f), RunSpeed, MaxRunSpeed) : MaxRunSpeed;
                unit.MoveTo(goal, speed);
            }
            foreach (var u in _units)
                u?.Step(dtMs / 1000f);
            for (var i = _shots.Count - 1; i >= 0; i--)
                if (!_shots[i].Tick(nowMs))
                    _shots.RemoveAt(i);
        }

        /// <summary>
        /// Where a melee unit stands to hit <paramref name="target"/>: in front of the target's own goal (so two melee
        /// units that charge each other meet), a little higher or lower by formation slot so attackers do not stack.
        /// </summary>
        private Vector3 ContactPoint(UnitView attacker, UnitView target, int place)
        {
            var towardAttacker = attacker.Side == Side.Allies ? -1f : 1f;
            var reach = (attacker.HalfWidth + target.HalfWidth) * 0.85f + 0.2f;
            var at = target.Goal;
            Vector3 Spot(int i)
            {
                var spot = Spots[i % Spots.Length];
                return new Vector3(at.x + towardAttacker * (reach + spot.x), Mathf.Clamp(at.y + spot.y, -1.9f, 1.5f), 0f);
            }
            // the first free place from this attacker's turn on: never on top of a unit of its own side
            for (var i = 0; i < Spots.Length; i++)
            {
                var candidate = Spot(place + i);
                if (Free(attacker, candidate))
                    return candidate;
            }
            return Spot(place);
        }

        private bool Free(UnitView self, Vector3 spot)
        {
            foreach (var u in _units)
                if (u != null && u != self && !u.Dead && u.Side == self.Side
                    && Mathf.Abs(u.Goal.x - spot.x) < 0.5f && Mathf.Abs(u.Goal.y - spot.y) < 0.4f)
                    return false;
            return true;
        }

        /// <summary>Places around one target for the 1st, 2nd, … melee attacker: in front, above, below, then a second ring.</summary>
        private static readonly Vector2[] Spots =
        {
            new Vector2(0f, 0f), new Vector2(0.15f, 0.62f), new Vector2(0.15f, -0.62f),
            new Vector2(0.7f, 0.3f), new Vector2(0.7f, -0.3f)
        };

        private readonly Dictionary<int, List<int>> _attackers = new Dictionary<int, List<int>>();

        /// <summary>A ranged action started at event <paramref name="index"/>: one projectile per hit of it.</summary>
        public void Fire(int index, float nowMs)
        {
            var start = _events[index];
            var source = Unit(start.Source);
            if (source == null || source.Range != AttackRange.Ranged || !Hostile(source, Unit(start.Target)))
                return;
            var arrow = IsArrow(source, start.Skill);
            var seen = new HashSet<long>();
            var endMs = start.TimeMs + start.DurationMs;
            for (var j = index + 1; j < _events.Count && _events[j].TimeMs <= endMs; j++)
            {
                var e = _events[j];
                if (e.Source != start.Source)
                    continue;
                if (e.Type == BattleEventType.ActionStart)
                    break;
                if (e.Type != BattleEventType.Hit && e.Type != BattleEventType.Miss && e.Type != BattleEventType.Dodge)
                    continue;
                if (!seen.Add(((long)e.HitIndex << 16) | (uint)e.Target))
                    continue;
                var target = Unit(e.Target);
                if (target == null)
                    continue;
                var distance = Vector3.Distance(source.CenterPosition, target.CenterPosition);
                var travelMs = Mathf.Clamp(distance / ProjectileSpeed * 1000f, 120f, 450f);
                var launchMs = Mathf.Max(start.TimeMs + 60f, e.TimeMs - travelMs);
                if (launchMs >= e.TimeMs)
                    launchMs = Mathf.Max(nowMs, e.TimeMs - 80f);
                _shots.Add(Projectile.Create(_root, source, target, launchMs, e.TimeMs, AspectColor(source.Aspect), arrow));
            }
        }

        /// <summary>Bows and crossbows shoot arrows; everything else (magic, bombs, stones) flies as a glowing orb.</summary>
        private static bool IsArrow(UnitView unit, SkillSlot slot)
        {
            var name = SkillName(unit.Hero, slot);
            return name != null && (name.Contains("pfeil") || name.Contains("bolzen") || name.Contains("schuss") && !name.Contains("kanone"));
        }

        private static string SkillName(HeroDefinition hero, SkillSlot slot)
        {
            foreach (var s in hero.Skills())
                if (s.Slot == slot)
                    return s.Name?.ToLowerInvariant();
            return null;
        }

        public static Color AspectColor(Aspect aspect) => aspect switch
        {
            Aspect.Ravak => new Color(1f, 0.55f, 0.25f),
            Aspect.Gorath => new Color(0.5f, 0.95f, 0.4f),
            Aspect.Somnara => new Color(0.55f, 0.65f, 1f),
            Aspect.Veloria => new Color(1f, 0.55f, 0.9f),
            _ => new Color(0.3f, 0.95f, 0.85f)
        };

        public void Clear()
        {
            foreach (var s in _shots)
                s.Destroy();
            _shots.Clear();
            _plans.Clear();
            _next.Clear();
        }
    }

    /// <summary>One projectile, positioned by battle time so it lands exactly on its hit (also at x2 speed and in hit-stop).</summary>
    internal sealed class Projectile
    {
        private GameObject _go;
        private UnitView _from;
        private UnitView _to;
        private Vector3 _start;
        private float _launchMs;
        private float _hitMs;
        private bool _arrow;
        private bool _impact;
        private float _impactMs;
        private SpriteRenderer _core;
        private SpriteRenderer _glow;
        private TrailRenderer _trail;
        private Color _color;

        public static Projectile Create(Transform root, UnitView from, UnitView to, float launchMs, float hitMs, Color color, bool arrow)
        {
            var p = new Projectile { _from = from, _to = to, _launchMs = launchMs, _hitMs = hitMs, _arrow = arrow, _color = color };
            p._go = new GameObject(arrow ? "Arrow" : "Orb");
            p._go.transform.SetParent(root, false);
            p._go.SetActive(false);
            p._glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
            p._glow.transform.SetParent(p._go.transform, false);
            p._glow.sprite = ShadowSprite.Get();
            p._glow.color = new Color(color.r, color.g, color.b, 0.75f);
            p._glow.sortingOrder = 4500;
            p._core = new GameObject("Core").AddComponent<SpriteRenderer>();
            p._core.transform.SetParent(p._go.transform, false);
            p._core.sprite = arrow ? WhiteSprite.Get() : ShadowSprite.Get();
            p._core.color = arrow ? new Color(0.95f, 0.9f, 0.8f) : Color.white;
            p._core.sortingOrder = 4501;
            if (arrow)
            {
                // a shaft: the white sprite's pivot is on its left edge, so it trails behind the tip
                p._core.transform.localScale = new Vector3(-0.55f, 0.05f, 1f);
                p._glow.transform.localScale = new Vector3(0.28f, 0.18f, 1f);
            }
            else
            {
                p._core.transform.localScale = Vector3.one * 0.22f;
                p._glow.transform.localScale = Vector3.one * 0.55f;
            }
            p._trail = p._go.AddComponent<TrailRenderer>();
            p._trail.sharedMaterial = p._core.sharedMaterial;
            p._trail.time = arrow ? 0.08f : 0.16f;
            p._trail.startWidth = arrow ? 0.05f : 0.2f;
            p._trail.endWidth = 0f;
            p._trail.startColor = new Color(color.r, color.g, color.b, 0.85f);
            p._trail.endColor = new Color(color.r, color.g, color.b, 0f);
            p._trail.sortingOrder = 4499;
            p._trail.emitting = false;
            return p;
        }

        /// <summary>False once the projectile has landed and faded.</summary>
        public bool Tick(float nowMs)
        {
            if (_go == null)
                return false;
            if (nowMs < _launchMs)
                return true;
            if (_impact)
            {
                var k = (nowMs - _impactMs) / 160f;
                if (k >= 1f)
                {
                    Destroy();
                    return false;
                }
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.3f, k);
                _glow.color = new Color(_color.r, _color.g, _color.b, 0.8f * (1f - k));
                return true;
            }
            if (!_go.activeSelf)
            {
                _start = _from.CenterPosition;
                _go.transform.position = _start;
                _go.SetActive(true);
                _trail.Clear();
                _trail.emitting = true;
            }
            var end = _to.CenterPosition;
            var t = Mathf.Clamp01((nowMs - _launchMs) / Mathf.Max(1f, _hitMs - _launchMs));
            var pos = Vector3.Lerp(_start, end, t);
            if (!_arrow)
                pos += Vector3.up * Mathf.Sin(t * Mathf.PI) * Vector3.Distance(_start, end) * 0.12f;
            var dir = pos - _go.transform.position;
            _go.transform.position = pos;
            if (_arrow && dir.sqrMagnitude > 1e-6f)
                _go.transform.rotation = Quaternion.FromToRotation(Vector3.right, dir.normalized);
            if (t >= 1f)
            {
                // a short flash where it lands
                _impact = true;
                _impactMs = nowMs;
                _trail.emitting = false;
                _core.enabled = false;
                _go.transform.rotation = Quaternion.identity;
            }
            return true;
        }

        public void Destroy()
        {
            if (_go != null)
                Object.Destroy(_go);
            _go = null;
        }
    }
}
