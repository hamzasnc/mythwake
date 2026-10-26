using Mythwake.Core.Combat;
using Spine.Unity;
using UnityEngine;

namespace Mythwake.Presentation
{
    /// <summary>What draws and animates a unit: a Spine rig, or a single image while a monster has no rig yet.</summary>
    internal interface IUnitBody
    {
        /// <summary>World height of the unit (for bars and floating numbers).</summary>
        float Height { get; }
        bool IsBusy { get; }
        bool DeathFinished { get; }
        void Play(string animation, bool loop);
        void SetTimeScale(float scale);
        void SetColor(Color color);
        void SetSortingOrder(int order);
    }

    /// <summary>One unit on the battlefield: body, HP/energy bars, hit flash, death fade. No rules here.</summary>
    public sealed class UnitView : MonoBehaviour
    {
        private const string Idle = "idle";

        private IUnitBody _body;
        private SpriteRenderer _hpFill;
        private SpriteRenderer _hpLag;
        private SpriteRenderer _shieldFill;
        private SpriteRenderer _energyFill;
        private SpriteRenderer _shadow;
        private Transform _bars;
        private float _flash;
        private float _fade = 1f;
        private bool _dying;
        private float _hpShown = 1f;
        private float _hpTarget = 1f;

        public int Index { get; private set; }
        public Side Side { get; private set; }
        public int Slot { get; private set; }
        public string HeroId { get; private set; }
        public long MaxHp { get; private set; }
        public long Hp { get; private set; }
        public long Shield { get; private set; }
        public int Energy { get; private set; }
        public bool Dead { get; private set; }
        public float Height => _body.Height;

        public Vector3 HeadPosition => transform.position + Vector3.up * Height * 0.95f;
        public Vector3 CenterPosition => transform.position + Vector3.up * Height * 0.5f;

        public static UnitView Create(Transform parent, SkeletonDataAsset data, UnitSetup setup, int index, Side side, Vector3 position, float scale)
        {
            var skeleton = SkeletonAnimation.NewSkeletonAnimationGameObject(data, true);
            var view = Place(skeleton.gameObject, parent, setup, side, position, scale);
            view.Init(new SpineBody(skeleton, side, scale), setup, index, side);
            return view;
        }

        /// <summary>A unit drawn from one image (monsters before their Spine rig exists).</summary>
        public static UnitView CreateSprite(Transform parent, Sprite sprite, UnitSetup setup, int index, Side side, Vector3 position, float scale, float heightUnits)
        {
            var go = new GameObject();
            var view = Place(go, parent, setup, side, position, scale);
            view.Init(SpriteBody.Create(go.transform, sprite, side, heightUnits), setup, index, side);
            return view;
        }

        private static UnitView Place(GameObject go, Transform parent, UnitSetup setup, Side side, Vector3 position, float scale)
        {
            go.name = $"{side}_{setup.Slot}_{setup.Hero.Id}";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var size = setup.Hero.SizePermille / 1000f * (setup.Variant == "boss" && !setup.Hero.Boss ? 1.15f : 1f);
            go.transform.localScale = Vector3.one * scale * size;
            return go.AddComponent<UnitView>();
        }

        private void Init(IUnitBody body, UnitSetup setup, int index, Side side)
        {
            _body = body;
            Index = index;
            Side = side;
            Slot = setup.Slot;
            HeroId = setup.Hero.Id;
            MaxHp = Hp = System.Math.Max(1, setup.Stats.Hp);
            AddShadow();
            Play(Idle, true);
            BuildBars();
        }

        private void AddShadow()
        {
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(transform, false);
            var inv = 1f / transform.localScale.x;
            shadow.transform.localScale = new Vector3(1.25f, 0.32f, 1f) * inv * Mathf.Max(1f, transform.localScale.x / 0.8f);
            _shadow = shadow.AddComponent<SpriteRenderer>();
            _shadow.sprite = ShadowSprite.Get();
            _shadow.color = new Color(0f, 0f, 0f, 0.45f);
            _shadow.sortingOrder = -5000;
        }

        public void Play(string animation, bool loop = false)
        {
            if (Dead && animation != "death")
                return;
            _body.Play(animation, loop);
        }

        /// <summary>Short hit reaction, but never over an own action (attacks keep playing when hit).</summary>
        public void PlayHit()
        {
            _flash = 1f;
            if (!_body.IsBusy)
                _body.Play("hit", false);
        }

        public void SetTimeScale(float scale) => _body.SetTimeScale(scale);

        public void SetHp(long hp)
        {
            Hp = System.Math.Max(0, System.Math.Min(MaxHp, hp));
            _hpTarget = (float)Hp / MaxHp;
        }

        public void SetShield(long shield) => Shield = System.Math.Max(0, shield);

        public void SetEnergy(int energy) => Energy = energy;

        public void Die()
        {
            if (Dead)
                return;
            Dead = true;
            _dying = true;
            SetHp(0);
            _body.Play("death", false);
        }

        private void BuildBars()
        {
            _bars = new GameObject("Bars").transform;
            _bars.SetParent(transform, false);
            var inv = 1f / transform.localScale.x;
            _bars.localScale = Vector3.one * inv;
            _bars.localPosition = Vector3.up * (Height + 0.12f) * inv;
            const float width = BarWidth;
            var back = Bar("Back", new Color(0f, 0f, 0f, 0.65f), width + 0.04f, 0.13f, 0f, 0);
            back.transform.localPosition += Vector3.left * 0.02f;
            _hpLag = Bar("Lag", new Color(1f, 0.85f, 0.6f, 0.9f), width, 0.09f, 0f, 1);
            _hpFill = Bar("Hp", Side == Side.Allies ? new Color(0.35f, 0.9f, 0.45f) : new Color(0.95f, 0.3f, 0.3f), width, 0.09f, 0f, 2);
            _shieldFill = Bar("Shield", new Color(0.85f, 0.9f, 1f, 0.85f), width, 0.04f, 0.025f, 3);
            _energyFill = Bar("Energy", new Color(0.35f, 0.7f, 1f), width, 0.035f, -0.075f, 2);
        }

        private SpriteRenderer Bar(string name, Color color, float width, float height, float y, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_bars, false);
            go.transform.localPosition = new Vector3(-width / 2f, y, 0f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = WhiteSprite.Get();
            r.color = color;
            r.sortingOrder = 5000 + order;
            go.transform.localScale = new Vector3(width, height, 1f);
            return r;
        }

        private void LateUpdate()
        {
            var dt = Time.deltaTime;
            _hpShown = Mathf.MoveTowards(_hpShown, _hpTarget, dt * 0.8f);
            SetFill(_hpFill, _hpTarget);
            SetFill(_hpLag, Mathf.Max(_hpShown, _hpTarget));
            SetFill(_shieldFill, Mathf.Clamp01((float)Shield / MaxHp));
            SetFill(_energyFill, Energy / 1000f);
            if (_energyFill != null)
                _energyFill.color = Energy >= 1000 ? Color.Lerp(new Color(1f, 0.85f, 0.3f), Color.white, Mathf.PingPong(Time.time * 3f, 1f)) : new Color(0.35f, 0.7f, 1f);

            if (_flash > 0f)
            {
                _flash = Mathf.Max(0f, _flash - dt * 6f);
                var c = Color.Lerp(Color.white, new Color(1f, 0.45f, 0.45f), _flash);
                c.a = _fade;
                _body.SetColor(c);
            }
            if (_dying && _body.DeathFinished)
            {
                _fade = Mathf.Max(0f, _fade - dt * 1.5f);
                _body.SetColor(new Color(1f, 1f, 1f, _fade));
                _bars.gameObject.SetActive(false);
                if (_shadow != null)
                    _shadow.color = new Color(0f, 0f, 0f, 0.45f * _fade);
                if (_fade <= 0f)
                    _dying = false;
            }
        }

        private static void SetFill(SpriteRenderer bar, float fill)
        {
            if (bar == null)
                return;
            var s = bar.transform.localScale;
            bar.transform.localScale = new Vector3(Mathf.Max(0.0001f, fill) * BarWidth, s.y, 1f);
        }

        private const float BarWidth = 0.9f;

        public void SetSortingOrder(int order) => _body.SetSortingOrder(order);
    }

    /// <summary>Spine rig body: plays the rig's animations, falls back to "attack" for missing skill animations.</summary>
    internal sealed class SpineBody : IUnitBody
    {
        private const string Idle = "idle";
        private readonly SkeletonAnimation _skeleton;

        public float Height { get; }

        public SpineBody(SkeletonAnimation skeleton, Side side, float scale)
        {
            _skeleton = skeleton;
            _skeleton.Initialize(false);
            _skeleton.Skeleton.ScaleX = side == Side.Enemies ? -1f : 1f;
            Height = Mathf.Max(1f, _skeleton.Skeleton.Data.Height) * _skeleton.skeletonDataAsset.scale * skeleton.transform.localScale.y;
            // random phase so a team does not breathe in sync
            _skeleton.AnimationState.SetAnimation(0, Idle, true).TrackTime = Random.Range(0f, 2f);
        }

        public bool IsBusy
        {
            get
            {
                var current = _skeleton.AnimationState.GetCurrent(0);
                return current != null && current.Animation.Name != Idle && current.Animation.Name != "hit";
            }
        }

        public bool DeathFinished
        {
            get
            {
                var current = _skeleton.AnimationState.GetCurrent(0);
                return current == null || current.IsComplete;
            }
        }

        private bool Has(string name) => _skeleton.Skeleton.Data.FindAnimation(name) != null;

        public void Play(string animation, bool loop)
        {
            if (!Has(animation))
                animation = animation == "skill" || animation == "ultimate" ? "attack" : Idle;
            if (!Has(animation))
                return;
            _skeleton.AnimationState.SetAnimation(0, animation, loop);
            if (!loop && animation != "death")
                _skeleton.AnimationState.AddAnimation(0, Idle, true, 0f);
        }

        public void SetTimeScale(float scale) => _skeleton.timeScale = scale;

        public void SetColor(Color color) => _skeleton.Skeleton.SetColor(color);

        public void SetSortingOrder(int order) => _skeleton.GetComponent<MeshRenderer>().sortingOrder = order;
    }

    /// <summary>
    /// Image body with procedural motion: breathing idle, lunge for attacks, a bigger lunge and pulse for skills and
    /// ultimates, shake on hit, tilt-and-fade on death. Used until a monster's Spine rig exists.
    /// </summary>
    internal sealed class SpriteBody : MonoBehaviour, IUnitBody
    {
        private SpriteRenderer _renderer;
        private Transform _pivot;
        private float _direction;
        private string _action;
        private float _time;
        private float _length;
        private float _timeScale = 1f;
        private float _phase;

        public float Height { get; private set; }
        public bool IsBusy => _action != null && _action != "hit";
        public bool DeathFinished => _action == "death" && _time >= _length;

        public static SpriteBody Create(Transform root, Sprite sprite, Side side, float heightUnits)
        {
            var pivot = new GameObject("Body").transform;
            pivot.SetParent(root, false);
            var art = new GameObject("Art");
            art.transform.SetParent(pivot, false);
            var renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            // scale the image to the wanted height, feet on the ground
            var spriteHeight = sprite.bounds.size.y;
            var s = heightUnits / Mathf.Max(0.01f, spriteHeight);
            art.transform.localScale = new Vector3(side == Side.Enemies ? -s : s, s, 1f);
            art.transform.localPosition = new Vector3(0f, -sprite.bounds.min.y * s, 0f);
            var body = pivot.gameObject.AddComponent<SpriteBody>();
            body._renderer = renderer;
            body._pivot = pivot;
            body._direction = side == Side.Enemies ? -1f : 1f;
            body.Height = heightUnits * root.localScale.y;
            body._phase = Random.Range(0f, 6f);
            return body;
        }

        public void Play(string animation, bool loop)
        {
            // idle is the resting motion; a dying unit only plays its death
            if (animation == "idle" || (_action == "death" && animation != "death"))
                return;
            _action = animation;
            _time = 0f;
            _length = animation switch
            {
                "attack" => 0.55f,
                "skill" => 0.9f,
                "ultimate" => 1.3f,
                "hit" => 0.25f,
                "death" => 0.6f,
                _ => 0.5f
            };
        }

        public void SetTimeScale(float scale) => _timeScale = scale;

        public void SetColor(Color color) => _renderer.color = color;

        public void SetSortingOrder(int order) => _renderer.sortingOrder = order;

        private void Update()
        {
            var dt = Time.deltaTime * _timeScale;
            _phase += dt;
            var breathe = 1f + Mathf.Sin(_phase * 2.2f) * 0.025f;
            var offset = Vector3.zero;
            var scale = new Vector3(1f, breathe, 1f);
            var angle = 0f;

            if (_action != null)
            {
                _time += dt;
                var t = Mathf.Clamp01(_time / _length);
                var arc = Mathf.Sin(t * Mathf.PI);
                switch (_action)
                {
                    case "attack":
                        offset.x = arc * 0.45f * _direction;
                        angle = -arc * 6f * _direction;
                        break;
                    case "skill":
                        offset.x = arc * 0.7f * _direction;
                        offset.y = arc * 0.15f;
                        scale *= 1f + arc * 0.12f;
                        break;
                    case "ultimate":
                        offset.y = arc * 0.35f;
                        scale *= 1f + arc * 0.22f;
                        angle = Mathf.Sin(t * Mathf.PI * 6f) * 4f * (1f - t);
                        break;
                    case "hit":
                        offset.x = -Mathf.Sin(t * Mathf.PI * 4f) * 0.08f * (1f - t) * _direction;
                        break;
                    case "death":
                        angle = t * 25f * _direction;
                        offset.y = -t * 0.1f;
                        scale.y *= 1f - t * 0.15f;
                        break;
                }
                if (_action != "death" && _time >= _length)
                    _action = null;
            }
            _pivot.localPosition = offset;
            _pivot.localScale = scale;
            _pivot.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    internal static class ShadowSprite
    {
        private static Sprite _sprite;

        /// <summary>Soft round blob, generated once.</summary>
        public static Sprite Get()
        {
            if (_sprite != null)
                return _sprite;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var dx = (x + 0.5f) / size * 2f - 1f;
                    var dy = (y + 0.5f) / size * 2f - 1f;
                    var a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            _sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _sprite;
        }
    }

    internal static class WhiteSprite
    {
        private static Sprite _sprite;

        /// <summary>1×1 white sprite with its pivot on the left edge, so scaling X fills a bar from the left.</summary>
        public static Sprite Get()
        {
            if (_sprite == null)
                _sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0f, 0.5f), 4f);
            return _sprite;
        }
    }
}
