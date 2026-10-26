using System.Collections.Generic;
using Mythwake.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>
    /// A hub picture (town, world map) that covers the screen like a background, keeps its aspect and can be dragged
    /// where it overflows. Children are placed in picture coordinates (<see cref="Spot"/>), so hotspots stay on
    /// the painted locations at every screen size.
    /// </summary>
    public sealed class MapView : VisualElement
    {
        private const float DragThreshold = 18f;

        private sealed class Placed
        {
            public VisualElement Element;
            public Spot Spot;
            public float Aspect;
            public bool Bottom;
        }

        private readonly VisualElement _canvas;
        private readonly float _aspect;
        private readonly List<Placed> _placed = new List<Placed>();
        private Vector2 _offset;
        private Vector2 _size;
        private Vector2 _pressAt;
        private Vector2 _pressOffset;
        private bool _pressed;

        /// <summary>Width / height of the picture.</summary>
        public float Aspect => _aspect;

        /// <summary>True while (and right after) the player drags the map, so taps on hotspots can be ignored.</summary>
        public bool Dragged { get; private set; }

        public MapView(Texture2D image, float fallbackAspect, string emptyClass)
        {
            AddToClassList("map");
            _canvas = UiKit.Box("map__canvas").Into(this);
            if (image != null)
            {
                _canvas.style.backgroundImage = new StyleBackground(image);
                _aspect = image.width / (float)image.height;
            }
            else
            {
                _canvas.AddToClassList(emptyClass);
                _aspect = fallbackAspect;
            }
            RegisterCallback<GeometryChangedEvent>(_ => Fit());
            RegisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
            RegisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
            RegisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
        }

        /// <summary>Places an element on the picture. <paramref name="aspect"/> is width/height of the element;
        /// <paramref name="bottom"/> anchors its bottom centre (buildings) instead of its centre (pins).</summary>
        public T Place<T>(T element, Spot spot, float aspect, bool bottom) where T : VisualElement
        {
            element.style.position = Position.Absolute;
            _canvas.Add(element);
            _placed.Add(new Placed { Element = element, Spot = spot, Aspect = aspect, Bottom = bottom });
            if (_size.x > 0)
                Layout(_placed[_placed.Count - 1]);
            return element;
        }

        public void ClearPlaced()
        {
            foreach (var p in _placed)
                p.Element.RemoveFromHierarchy();
            _placed.Clear();
        }

        private void Fit()
        {
            var w = resolvedStyle.width;
            var h = resolvedStyle.height;
            if (float.IsNaN(w) || float.IsNaN(h) || w <= 0 || h <= 0)
                return;
            _size = w / h > _aspect ? new Vector2(w, w / _aspect) : new Vector2(h * _aspect, h);
            _canvas.style.width = _size.x;
            _canvas.style.height = _size.y;
            // start centred; later drags keep their offset inside the bounds
            if (_offset == Vector2.zero)
                _offset = new Vector2((w - _size.x) / 2f, (h - _size.y) / 2f);
            Move(_offset);
            foreach (var p in _placed)
                Layout(p);
        }

        private void Layout(Placed p)
        {
            var width = p.Spot.size * _size.x;
            var height = width / Mathf.Max(0.01f, p.Aspect);
            p.Element.style.width = width;
            p.Element.style.height = height;
            p.Element.style.left = p.Spot.x * _size.x - width / 2f;
            p.Element.style.top = p.Spot.y * _size.y - (p.Bottom ? height : height / 2f);
        }

        private void Move(Vector2 offset)
        {
            var w = resolvedStyle.width;
            var h = resolvedStyle.height;
            offset.x = Mathf.Clamp(offset.x, Mathf.Min(0f, w - _size.x), 0f);
            offset.y = Mathf.Clamp(offset.y, Mathf.Min(0f, h - _size.y), 0f);
            _offset = offset;
            _canvas.style.left = offset.x;
            _canvas.style.top = offset.y;
        }

        private void OnDown(PointerDownEvent e)
        {
            _pressed = true;
            Dragged = false;
            _pressAt = e.position;
            _pressOffset = _offset;
        }

        private void OnMove(PointerMoveEvent e)
        {
            if (!_pressed)
                return;
            var delta = (Vector2)e.position - _pressAt;
            if (!Dragged && delta.magnitude < DragThreshold)
                return;
            Dragged = true;
            Move(_pressOffset + delta);
        }

        private void OnUp(PointerUpEvent e)
        {
            _pressed = false;
            // clicks fire after pointer up in the same frame; reset afterwards so the next tap works
            if (Dragged)
                schedule.Execute(() => Dragged = false);
        }
    }
}
