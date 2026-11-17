using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OC2HudCustomization
{
    internal sealed class ContentsState : IHudEditorTarget
    {
        internal readonly IngredientContentsUIContainer UI;
        private readonly ContentsAccess _access;
        private readonly HudLog _log;
        private readonly Dictionary<Image, AlphaLayer> _alphas = new Dictionary<Image, AlphaLayer>();
        private readonly List<Image> _dead = new List<Image>();
        private GameObject _generated;
        private ContentsGeometry _geometry;
        private Vector2 _lastNativeSize, _lastCanvasSize;
        private Vector3 _lastParentScale;
        private bool _dirty = true;
        private bool _registered = true;
        private readonly HudGraphicBounds _bounds;
        private Graphic[] _graphics;

        internal ContentsState(IngredientContentsUIContainer ui, ContentsAccess access, HudLog log)
        { UI = ui; _access = access; _log = log; _bounds = new HudGraphicBounds((RectTransform)ui.transform); }

        int IHudEditorTarget.Category { get { return 9; } }
        bool IHudEditorTarget.TryBounds(out Rect bounds) { return TryBounds(out bounds); }
        internal bool TryBounds(out Rect bounds) { bounds = new Rect(); return _registered && _bounds.TryBounds(_graphics, out bounds); }
        internal void Release() { _registered = false; Restore(); }

        internal void BeforeNativeRefresh()
        {
            // UISubElementContainer resets this region itself. Restore our geometry first so
            // that reset is never mistaken for another mod taking ownership.
            if (_geometry != null) _geometry.Restore();
            _dirty = true;
        }

        internal void MarkDirty() { _dirty = true; }

        internal void Apply(HudOptions options)
        {
            if (!UI) return;
            if (!options.Enabled) { Restore(); return; }
            if (!UI.gameObject.activeInHierarchy) return;
            GameObject generated = (GameObject)_access.Container.GetValue(UI);
            if (!generated) return;
            if (_generated != generated)
            {
                Restore(); _alphas.Clear(); _generated = generated; _geometry = null;
                RectTransform rect = generated.transform as RectTransform;
                AnchorGridLayoutGroup grid = generated.GetComponent<AnchorGridLayoutGroup>();
                if (rect && grid && !generated.GetComponent<RectTransformExtension>())
                    _geometry = new ContentsGeometry(rect, grid, _log);
                else _log.WarnOnce("contents-layout-structure", "Unexpected contents region/driver; keep native geometry on affected containers.");
                _dirty = true;
            }
            Vector2 nativeSize = _geometry == null ? Vector2.zero : _geometry.NativeSize;
            Vector2 canvasSize = _geometry == null ? Vector2.zero : _geometry.CanvasSize;
            Vector3 parentScale = _geometry == null ? Vector3.one : _geometry.ParentScale;
            // Native prefabs initially have zero scale before RectTransformExtension
            // runs. Screen-space offsets must follow that scale and canvas size.
            if (!_dirty && nativeSize == _lastNativeSize && canvasSize == _lastCanvasSize && parentScale == _lastParentScale) return;
            List<Image> icons = (List<Image>)_access.Icons.GetValue(UI);
            int count = (int)_access.Count.GetValue(UI);
            if (count < 0 || count > icons.Count) throw new InvalidOperationException("IngredientContentsUIContainer.m_iconsInUse is outside m_icons.");
            if (_graphics == null || _graphics.Length != count) _graphics = new Graphic[count];
            for (int i = 0; i < count; i++) _graphics[i] = icons[i];
            Sprite empty = (Sprite)_access.Empty.GetValue(UI);
            bool pendingAlpha = false;
            if (empty)
            {
                for (int i = 0; i < icons.Count; i++)
                {
                    Image icon = icons[i];
                    if (!icon) continue;
                    AlphaLayer alpha;
                    if (!_alphas.TryGetValue(icon, out alpha))
                    {
                        if (i >= count) continue;
                        alpha = new AlphaLayer(icon.gameObject, _log); _alphas.Add(icon, alpha);
                    }
                    alpha.Apply(i < count ? (icon.sprite == empty ? options.EmptySlotOpacity : options.IngredientOpacity) : 1f);
                    pendingAlpha |= alpha.IsRetiring;
                }
            }
            else
            {
                foreach (AlphaLayer alpha in _alphas.Values) alpha.Restore();
                _log.WarnOnce("missing-empty-sprite", "Contents empty-slot sprite is missing; preserve opacity rather than guess the icon category.");
            }
            if (_geometry != null) _geometry.Apply(count, options);
            _lastNativeSize = nativeSize; _lastCanvasSize = canvasSize; _lastParentScale = parentScale; _dirty = pendingAlpha;
            _dead.Clear();
            foreach (Image image in _alphas.Keys) if (!image) _dead.Add(image);
            for (int i = 0; i < _dead.Count; i++) _alphas.Remove(_dead[i]);
        }

        internal void Restore()
        {
            if (_geometry != null) _geometry.Restore();
            foreach (AlphaLayer alpha in _alphas.Values) alpha.Restore();
            _dirty = true;
        }
    }
}
