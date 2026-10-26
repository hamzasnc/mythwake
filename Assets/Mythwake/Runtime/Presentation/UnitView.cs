using Mythwake.Core.Combat;
using Spine.Unity;
using UnityEngine;

namespace Mythwake.Presentation
{
    /// <summary>One hero on the battlefield: Spine skeleton, HP/energy bars, hit flash, death fade. No rules here.</summary>
    public sealed class UnitView : MonoBehaviour
    {
        private const string Idle = "idle";

        private SkeletonAnimation _skeleton;
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
        public float Height { get; private set; }
        public SkeletonAnimation Skeleton => _skeleton;

        public Vector3 HeadPosition => transform.position + Vector3.up * Height * 0.95f;
        public Vector3 CenterPosition => transform.position + Vector3.up * Height * 0.5f;

        public static UnitView Create(Transform parent, SkeletonDataAsset data, UnitSetup setup, int index, Side side, Vector3 position, float scale)
        {
            var skeleton = SkeletonAnimation.NewSkeletonAnimationGameObject(data, true);
            var go = skeleton.gameObject;
            go.name = $"{side}_{setup.Slot}_{setup.Hero.Id}";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * scale;
            var view = go.AddComponent<UnitView>();
            view.Init(skeleton, setup, index, side, scale);
            return view;
        }

        private void Init(SkeletonAnimation skeleton, UnitSetup setup, int index, Side side, float scale)
        {
            _skeleton = skeleton;
            Index = index;
            Side = side;
            Slot = setup.Slot;
            HeroId = setup.Hero.Id;
            MaxHp = Hp = System.Math.Max(1, setup.Stats.Hp);
            _skeleton.Initialize(false);
            _skeleton.Skeleton.ScaleX = side == Side.Enemies ? -1f : 1f;
            Height = Mathf.Max(1f, _skeleton.Skeleton.Data.Height) * _skeleton.skeletonDataAsset.scale * scale;
            if (setup.Variant == "boss")
                transform.localScale *= 1.15f;
            AddShadow();
            Play(Idle, true);
            // random phase so a team does not breathe in sync
            var entry = _skeleton.AnimationState.GetCurrent(0);
            if (entry != null)
                entry.TrackTime = Random.Range(0f, entry.AnimationEnd);
            BuildBars();
        }

        private void AddShadow()
        {
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(transform, false);
            var inv = 1f / transform.localScale.x;
            shadow.transform.localScale = new Vector3(1.25f, 0.32f, 1f) * inv;
            _shadow = shadow.AddComponent<SpriteRenderer>();
            _shadow.sprite = ShadowSprite.Get();
            _shadow.color = new Color(0f, 0f, 0f, 0.45f);
            _shadow.sortingOrder = -5000;
        }

        public bool HasAnimation(string name) => _skeleton.Skeleton.Data.FindAnimation(name) != null;

        public void Play(string animation, bool loop = false)
        {
            if (Dead && animation != "death")
                return;
            if (!HasAnimation(animation))
                animation = animation == "skill" || animation == "ultimate" ? "attack" : Idle;
            if (!HasAnimation(animation))
                return;
            _skeleton.AnimationState.SetAnimation(0, animation, loop);
            if (!loop && animation != "death")
                _skeleton.AnimationState.AddAnimation(0, Idle, true, 0f);
        }

        /// <summary>Short hit reaction, but never over an own action (attacks keep playing when hit).</summary>
        public void PlayHit()
        {
            _flash = 1f;
            var current = _skeleton.AnimationState.GetCurrent(0);
            if (current == null || current.Animation.Name == Idle || current.Animation.Name == "hit")
                Play("hit");
        }

        public void SetTimeScale(float scale) => _skeleton.timeScale = scale;

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
            Play("death");
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
                _skeleton.Skeleton.SetColor(c);
            }
            if (_dying)
            {
                var current = _skeleton.AnimationState.GetCurrent(0);
                if (current == null || current.IsComplete)
                {
                    _fade = Mathf.Max(0f, _fade - dt * 1.5f);
                    _skeleton.Skeleton.A = _fade;
                    _bars.gameObject.SetActive(false);
                    if (_shadow != null)
                        _shadow.color = new Color(0f, 0f, 0f, 0.45f * _fade);
                    if (_fade <= 0f)
                        _dying = false;
                }
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

        public void SetSortingOrder(int order)
        {
            var r = _skeleton.GetComponent<MeshRenderer>();
            r.sortingOrder = order;
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
