using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OC2HudCustomization
{
    internal interface IHudEditorTarget
    {
        int Category { get; }
        bool TryBounds(out Rect bounds);
    }

    // Both ordinary HUD controls and contents icons use the rendered UI_Move offset,
    // actual canvas camera and top-left editor coordinates when hit testing.
    internal sealed class HudGraphicBounds
    {
        private readonly RectTransform _rect;
        private readonly Vector3[] _corners = new Vector3[4];
        private Transform _parent, _root;
        private Canvas _canvas;
        private GraphicRaycaster _raycaster;
        private UI_Move _move;
        internal HudGraphicBounds(RectTransform rect) { _rect = rect; }
        internal bool TryBounds(IList<Graphic> graphics, out Rect bounds)
        {
            bounds = new Rect();
            if (!_rect || !_rect.gameObject.activeInHierarchy || graphics == null) return false;
            if (!_canvas || _parent != _rect.parent || _root != _rect.root)
            {
                _parent = _rect.parent; _root = _rect.root;
                _canvas = _rect.GetComponentInParent<Canvas>();
                _raycaster = _rect.GetComponentInParent<GraphicRaycaster>();
                _move = _rect.GetComponentInParent<UI_Move>();
            }
            Camera camera = !_canvas || _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null
                : _raycaster ? _raycaster.eventCamera : _canvas.worldCamera;
            Vector3 offset = _move && _canvas ? _canvas.transform.TransformVector((Vector3)_move.Offset) : Vector3.zero;
            bool found = false;
            for (int i = 0; i < graphics.Count; i++)
            {
                Graphic graphic = graphics[i];
                if (!graphic || !graphic.isActiveAndEnabled) continue;
                graphic.rectTransform.GetWorldCorners(_corners);
                for (int j = 0; j < 4; j++)
                {
                    Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, _corners[j] + offset);
                    point.y = Screen.height - point.y;
                    if (!found) { bounds = new Rect(point.x, point.y, 0, 0); found = true; }
                    else bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, point.x), Mathf.Min(bounds.yMin, point.y),
                        Mathf.Max(bounds.xMax, point.x), Mathf.Max(bounds.yMax, point.y));
                }
            }
            return found && bounds.width > 1f && bounds.height > 1f
                && bounds.xMax >= 0f && bounds.yMax >= 0f && bounds.xMin <= Screen.width && bounds.yMin <= Screen.height;
        }
    }
}
