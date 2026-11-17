using System;
using UnityEngine;
using UnityEngine.UI;

namespace OC2HudCustomization
{
    internal sealed class HudTarget
    {
        internal readonly Component Owner;
        internal readonly HudTargetKind Kind;
        internal readonly RectTransform Rect;
        internal readonly bool Pickable;
        private readonly HoverIconUIController _hover;
        private readonly UI_Move _move;
        private Canvas _canvas;
        private GraphicRaycaster _raycaster;
        private Transform _parent, _root;
        private bool _gameplayCanvas;
        private readonly AlphaLayer _alpha;
        private readonly HudLog _log;
        private HudPreview _preview;
        private readonly OwnedValue<Vector2> _anchor;
        private readonly OwnedValue<Vector2> _position;
        private readonly OwnedValue<Vector3> _scale;
        private readonly Vector3[] _corners = new Vector3[4];
        private readonly Vector3[] _nativeCorners = new Vector3[4];
        private Graphic[] _graphics;
        private Vector2 _baseAnchor, _basePosition;
        private Vector3 _baseScale;
        private bool _geometryApplied;

        internal HudTarget(Component owner, Component display, HudTargetKind kind, bool pickable, HudLog log)
        {
            Owner = owner; Kind = kind; Pickable = pickable; Rect = display.transform as RectTransform; _log = log;
            if (!Rect) throw new InvalidOperationException("HUD display is not a RectTransform: " + display);
            _hover = display as HoverIconUIController;
            _move = Rect.GetComponent<UI_Move>();
            ResolveCanvas();
            _alpha = new AlphaLayer(Rect.gameObject, log);
            Action conflict = delegate { log.WarnOnce("editor-geometry-" + kind, "HUD editor yielded a presentation property changed by another owner: " + kind); };
            _position = new OwnedValue<Vector2>(delegate { return Rect.anchoredPosition; }, delegate(Vector2 v) { Rect.anchoredPosition = v; }, conflict, UnityValue.SameVector);
            _scale = new OwnedValue<Vector3>(delegate { return Rect.localScale; }, delegate(Vector3 v) { Rect.localScale = v; }, conflict,
                delegate(Vector3 a, Vector3 b) { return (a - b).sqrMagnitude < .000001f; });
            if (_hover) _anchor = new OwnedValue<Vector2>(_hover.GetOffset, _hover.SetAnchorOffset, conflict, UnityValue.SameVector);
            _graphics = Rect.GetComponentsInChildren<Graphic>(true);
        }
        internal bool Alive { get { return Owner && Rect; } }
        private void ResolveCanvas()
        {
            // Native Instantiate invokes Awake before parenting onto ScalingHUDCanvas.
            // Refresh only across a parenting change, not through a scene scan each frame.
            _parent = Rect.parent; _root = Rect.root; _canvas = Rect.GetComponentInParent<Canvas>();
            _raycaster = Rect.GetComponentInParent<GraphicRaycaster>();
            _gameplayCanvas = false;
            for (Transform ancestor = Rect.parent; ancestor; ancestor = ancestor.parent)
                if (ancestor.name == "ScalingHUDCanvas") { _gameplayCanvas = true; break; }
        }
        internal void RefreshGraphics() { if (Rect) _graphics = Rect.GetComponentsInChildren<Graphic>(true); }
        internal void Preview(bool visible)
        {
            if (!visible || !Alive || !Pickable) { if (_preview != null) { _preview.Dispose(); _preview = null; } return; }
            if (_preview == null) _preview = new HudPreview(Rect, _log);
            _preview.Tick();
        }
        internal void BeforeLayout()
        {
            if (!Rect || !_geometryApplied) return;
            _position.Restore(); _scale.Restore(); _geometryApplied = false;
        }
        internal void Apply(HudGeometry value, bool enabled)
        {
            if (!Alive) return;
            if (Rect.parent != _parent || Rect.root != _root) ResolveCanvas();
            BeforeLayout();
            if (!enabled || ((Kind == HudTargetKind.Score || Kind == HudTargetKind.Timer) && !_gameplayCanvas)) { Restore(); return; }
            _basePosition = Rect.anchoredPosition; _baseScale = Rect.localScale;
            Vector3 scale = new Vector3(_baseScale.x * value.Scale * value.Width,
                _baseScale.y * value.Scale * value.Height, _baseScale.z);
            // Keep the visual center fixed while scaling. Native pivots are often in a corner;
            // center compensation makes the shared drag math pin the opposite resize edge.
            bool resizing = (scale - _baseScale).sqrMagnitude > .000001f;
            Vector2 nativeCenter = Vector2.zero;
            bool centered = resizing && GraphicCenter(out nativeCenter);
            if (resizing) _scale.Apply(scale);
            Vector2 scaledCenter;
            Vector2 correction = centered && GraphicCenter(out scaledCenter) ? nativeCenter - scaledCenter : Vector2.zero;
            if (_hover)
            {
                _anchor.Restore(); _baseAnchor = _hover.GetOffset();
                Vector2 delta = new Vector2(value.X + correction.x / Screen.width, value.Y + correction.y / Screen.height);
                if (delta != Vector2.zero) _anchor.Apply(_baseAnchor + delta);
            }
            else if (value.X != 0f || value.Y != 0f || correction != Vector2.zero)
            {
                // Convert normalized screen motion through the actual canvas camera and parent.
                Vector2 origin, shifted; Camera camera = Camera;
                RectTransform parent = Rect.parent as RectTransform;
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, Rect.position);
                if (parent && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, camera, out origin)
                    && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,
                        screen + new Vector2(value.X * Screen.width, value.Y * Screen.height) + correction, camera, out shifted))
                    _position.Apply(_basePosition + shifted - origin);
            }
            _geometryApplied = true;
            _alpha.Apply(value.Opacity);
        }
        private Camera Camera
        {
            get { return !_canvas || _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _raycaster ? _raycaster.eventCamera : _canvas.worldCamera; }
        }
        private bool GraphicCenter(out Vector2 center)
        {
            bool found = false; Vector2 min = Vector2.zero, max = Vector2.zero; Camera camera = Camera;
            foreach (Graphic graphic in _graphics)
            {
                // Include temporarily hidden warning/tick graphics for a stable scale pivot.
                if (!graphic) continue;
                graphic.rectTransform.GetWorldCorners(_nativeCorners);
                foreach (Vector3 corner in _nativeCorners)
                {
                    Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corner);
                    if (!found) { min = max = point; found = true; }
                    else { min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
                }
            }
            center = (min + max) * .5f; return found;
        }
        internal bool TryBounds(out Rect bounds)
        {
            bounds = new Rect();
            if (!Alive || !Rect.gameObject.activeInHierarchy) return false;
            if (Rect.parent != _parent || Rect.root != _root) ResolveCanvas();
            if ((Kind == HudTargetKind.Score || Kind == HudTargetKind.Timer) && !_gameplayCanvas) return false;
            bool found = false; Camera camera = Camera;
            Vector3 shaderOffset = Vector3.zero;
            if (_move && _canvas) shaderOffset = _canvas.transform.TransformVector((Vector3)_move.Offset);
            for (int i = 0; i < _graphics.Length; i++)
            {
                Graphic graphic = _graphics[i];
                if (!graphic || !graphic.isActiveAndEnabled) continue;
                graphic.rectTransform.GetWorldCorners(_corners);
                for (int j = 0; j < 4; j++)
                {
                    Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, _corners[j] + shaderOffset);
                    point.y = Screen.height - point.y; // Editor coordinates have their origin at the top left.
                    if (!found) { bounds = new Rect(point.x, point.y, 0f, 0f); found = true; }
                    else bounds = UnityEngine.Rect.MinMaxRect(Mathf.Min(bounds.xMin, point.x), Mathf.Min(bounds.yMin, point.y), Mathf.Max(bounds.xMax, point.x), Mathf.Max(bounds.yMax, point.y));
                }
            }
            return found && bounds.width > 1f && bounds.height > 1f
                && bounds.xMax >= 0f && bounds.yMax >= 0f && bounds.xMin <= Screen.width && bounds.yMin <= Screen.height;
        }
        internal void Restore()
        {
            Preview(false);
            if (!Rect) return;
            BeforeLayout();
            if (_anchor != null) _anchor.Restore();
            _alpha.Restore();
        }
    }
}
