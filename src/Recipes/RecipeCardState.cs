using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace OC2HudCustomization
{
    internal sealed class RecipeCardState
    {
        internal readonly RecipeWidgetUIController Widget;
        private RectTransformExtension _extension;
        private readonly HudLog _log;
        private readonly List<RecipeWidgetTile> _tiles;
        private readonly FieldInfo _container, _headPaperField, _topPaper;
        private readonly List<Graphic> _visuals = new List<Graphic>();
        private readonly List<Graphic> _descendants = new List<Graphic>();
        private readonly List<Graphic> _headVisuals = new List<Graphic>();
        private readonly List<AlphaLayer> _branches = new List<AlphaLayer>();
        private readonly List<OwnedValue<bool>> _headers = new List<OwnedValue<bool>>();
        private readonly RecipeMotion _motion;
        private RecipeTopClip _topClip;
        private Image _headPaper;
        private UI_Move _nativeMove;
        private Animator _animator;
        private RecipeAnimationPreview _preview;
        private int _entranceLayer = -1;
        private bool _nativeEntranceSeen;
        private float _horizontalExtent;
        private readonly Vector3[] _corners = new Vector3[4];
        private Vector2 _baseScale;
        private OwnedValue<Vector2> _scale;
        private readonly AlphaLayer _alpha;
        private Vector2 _nativeOffset, _lastOffset;
        private bool _moved, _scaled, _trimmed;
        private bool _boundsDirty = true;
        private int _cachedTileCount;

        internal RecipeCardState(RecipeWidgetUIController widget, RecipeAccess access, HudLog log, bool entering)
        {
            Widget = widget;
            _log = log;
            _tiles = (List<RecipeWidgetTile>)access.Tiles.GetValue(widget);
            _container = access.Container;
            _headPaperField = access.HeadPaper;
            _topPaper = access.TopPaper;
            _motion = new RecipeMotion(entering);
            _alpha = new AlphaLayer(widget.gameObject, log);
            CaptureNativeOffset();
        }

        internal void CaptureNativeOffset()
        {
            // Native LayoutWidgets may create this driver after AddElement registration.
            if (!_extension && Widget)
            {
                _extension = Widget.GetComponent<RectTransformExtension>();
                if (_extension)
                    _scale = new OwnedValue<Vector2>(delegate { return _extension.scale; },
                        delegate(Vector2 value) { _extension.scale = value; },
                        delegate { _log.WarnOnce("recipe-scale-conflict", "Recipe scale changed by another owner; stop changing the affected card."); });
            }
            if (_extension) { _nativeOffset = _extension.PixelOffset; _moved = false; }
        }

        internal void Prepare(HudOptions options)
        {
            if (_boundsDirty || _cachedTileCount != _tiles.Count) CacheBoundsObjects();
            bool customLayout = options.UsesCustomRecipeLayout;
            _trimmed = customLayout;
            for (int i = 0; i < _headers.Count; i++)
                if (_trimmed) _headers[i].Apply(false); else _headers[i].Restore();
            if (!customLayout) { _motion.Reset(); RestoreBranches(); RestoreTopClip(); }
            _alpha.Apply(options.RecipeOpacity);
            if (!_extension || _scale == null) return;
            if (Mathf.Approximately(options.RecipeScale, 1f)) { _scale.Restore(); _scaled = false; }
            else
            {
                if (!_scaled) _baseScale = _extension.scale;
                _scaled = _scale.Apply(_baseScale * options.RecipeScale);
            }
            // Public setter recalculates anchors now instead of waiting for LateUpdate order.
            if (customLayout || _moved)
                _extension.PixelOffset = _nativeOffset;
        }

        internal void InvalidateBounds() { _boundsDirty = true; }

        private void CacheBoundsObjects()
        {
            Preview(false);
            RestoreTopClip();
            for (int i = 0; i < _headers.Count; i++) _headers[i].Restore();
            _headers.Clear();
            _visuals.Clear();
            _headVisuals.Clear();
            _headPaper = null;
            RestoreBranches();
            _branches.Clear();
            GameObject widgetContainer = Widget ? (GameObject)_container.GetValue(Widget) : null;
            _nativeMove = widgetContainer ? widgetContainer.GetComponent<UI_Move>() : null;
            _animator = widgetContainer ? widgetContainer.GetComponent<Animator>() : null;
            _entranceLayer = _animator ? FindEntranceLayer(_animator) : -1;
            _nativeEntranceSeen = false;
            _horizontalExtent = 0f;
            for (int i = 0; i < _tiles.Count; i++)
            {
                RecipeWidgetTile tile = _tiles[i];
                if (!tile) continue;
                // Only the head's upward extension is unnecessary away from the screen edge.
                // Branch stitches, progress bar, pictures and cooking modifiers stay native.
                if (tile is TopRecipeWidgetTile)
                {
                    _headPaper = (Image)_headPaperField.GetValue(tile);
                    Image paper = (Image)_topPaper.GetValue(tile);
                    if (paper)
                    {
                        GameObject header = paper.gameObject;
                        OwnedValue<bool> visible = new OwnedValue<bool>(delegate { return header && header.activeSelf; },
                            delegate(bool value) { if (header) header.SetActive(value); },
                            delegate { _log.WarnOnce("recipe-header-conflict", "Recipe header visibility changed by another owner; preserve that visibility."); });
                        _headers.Add(visible);
                        if (_trimmed) visible.Apply(false);
                    }
                }
                // Measure the rendered graphics, including stitches outside tile rectangles.
                // Invisible layout roots stay fixed while native paper unfolds past them.
                // Use only the current generated container, excluding children pending Destroy.
                GameObject generated = (GameObject)_container.GetValue(tile);
                if (!generated) continue;
                generated.GetComponentsInChildren<Graphic>(true, _descendants);
                _visuals.AddRange(_descendants);
                if (tile is TopRecipeWidgetTile) _headVisuals.AddRange(_descendants);
                else if (_motion.Entering)
                {
                    AlphaLayer branch = new AlphaLayer(generated, _log);
                    _branches.Add(branch);
                    branch.Apply(_motion.BranchOpacity);
                }
            }
            _descendants.Clear();
            _cachedTileCount = _tiles.Count;
            _boundsDirty = false;
        }

        internal bool Measure(Transform space, out Rect bounds, out Rect head)
        {
            if (_boundsDirty || _cachedTileCount != _tiles.Count) CacheBoundsObjects();
            bounds = new Rect();
            head = new Rect();
            bool found = false, foundHead = false;
            bool hasPaper = _headPaper && _headPaper.isActiveAndEnabled;
            if (hasPaper)
            {
                IncludeGraphic(_headPaper, space, ref head, ref foundHead);
            }
            else
            {
                for (int i = 0; i < _headVisuals.Count; i++)
                {
                    Graphic graphic = _headVisuals[i];
                    if (graphic && graphic.isActiveAndEnabled) IncludeGraphic(graphic, space, ref head, ref foundHead);
                }
            }
            for (int i = 0; i < _visuals.Count; i++)
            {
                Graphic graphic = _visuals[i];
                if (!graphic || !graphic.isActiveAndEnabled) continue;
                IncludeGraphic(graphic, space, ref bounds, ref found);
            }
            if (!foundHead) head = bounds;
            // Animated branches and their stitches may extend above the line we render.
            if (_trimmed && hasPaper) bounds.yMax = head.yMax;
            return found && bounds.width > 0.01f && bounds.height > 0.01f;
        }

        private void IncludeGraphic(Graphic graphic, Transform space, ref Rect bounds, ref bool found)
        {
            graphic.rectTransform.GetWorldCorners(_corners);
            for (int i = 0; i < _corners.Length; i++)
            {
                Vector3 point = space.InverseTransformPoint(_corners[i]);
                if (!found) { bounds = new Rect(point.x, point.y, 0f, 0f); found = true; }
                else bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, point.x), Mathf.Min(bounds.yMin, point.y),
                    Mathf.Max(bounds.xMax, point.x), Mathf.Max(bounds.yMax, point.y));
            }
        }

        internal void Move(Vector2 target, Vector2 entry, Rect head, float deltaTime)
        {
            if (!_extension) return;
            Vector2 shaderOffset = _nativeMove ? _nativeMove.Offset : Vector2.zero;
            float horizontal = shaderOffset.magnitude;
            _horizontalExtent = Mathf.Max(_horizontalExtent, horizontal);
            float remaining = _horizontalExtent > .01f ? horizontal / _horizontalExtent : 0f;
            float progress = _animator && _entranceLayer >= 0 ? ReadNativeProgress() : -1f;
            Vector2 position = _motion.Step(target, entry, remaining, progress, deltaTime);
            _lastOffset = _nativeOffset + position - new Vector2(head.xMin, head.yMax);
            _extension.PixelOffset = _lastOffset;
            if (_headPaper && _headPaper.isActiveAndEnabled)
            {
                if (_topClip == null) _topClip = new RecipeTopClip();
                _topClip.Apply(_headPaper, _visuals, shaderOffset.y, _corners);
            }
            else RestoreTopClip();
            for (int i = 0; i < _branches.Count; i++)
                if (_motion.Entering) _branches[i].Apply(_motion.BranchOpacity); else _branches[i].Restore();
            _moved = true;
        }

        private void RestoreBranches()
        { for (int i = 0; i < _branches.Count; i++) _branches[i].Restore(); }

        private void RestoreTopClip()
        { if (_topClip != null) _topClip.Restore(); }

        private static int FindEntranceLayer(Animator animator) { return animator.GetLayerIndex("Override"); }
        internal void Preview(bool active)
        {
            if (!active)
            { if (_preview != null) { _preview.Apply(false); _preview = null; } return; }
            if (!_animator) return;
            if (_preview == null) _preview = new RecipeAnimationPreview(_animator, _log);
            _preview.Apply(true);
        }

        private float ReadNativeProgress()
        {
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(_entranceLayer);
            if (state.IsName("Override.RecipeTransitionIn"))
            { _nativeEntranceSeen = true; return state.normalizedTime; }
            return _nativeEntranceSeen ? 1f : -1f;
        }

        internal void Restore()
        {
            Preview(false);
            RestoreTopClip();
            if (!Widget) return;
            for (int i = 0; i < _headers.Count; i++) _headers[i].Restore();
            _trimmed = false;
            _motion.Reset();
            RestoreBranches();
            _alpha.Restore();
            if (!_extension) return;
            if (_scale != null) _scale.Restore();
            _scaled = false;
            if (_moved && _extension.PixelOffset == _lastOffset) _extension.PixelOffset = _nativeOffset;
            _moved = false;
        }
    }
}
