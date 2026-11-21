using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OC2HudCustomization
{
    // Keep the native Animator, shader and hierarchy. Clip only the pixels above the main paper.
    internal sealed class RecipeTopClip
    {
        private readonly HashSet<CanvasRenderer> _owned = new HashSet<CanvasRenderer>();
        private Rect _lastRect;

        internal void Apply(Image paper, List<Graphic> visuals, float shaderY, Vector3[] corners)
        {
            Canvas canvas = paper.canvas;
            if (!canvas) { Restore(); return; }
            Transform space = canvas.rootCanvas.transform;
            paper.rectTransform.GetWorldCorners(corners);
            float top = space.InverseTransformPoint(corners[0]).y;
            for (int i = 1; i < corners.Length; i++) top = Mathf.Max(top, space.InverseTransformPoint(corners[i]).y);
            // The stock UI/UI Move shader tests the clip rect after applying _Offset.
            // Leave the other edges outside the viewport so wide orders and cookware keep moving normally.
            const float extent = 32768f;
            Rect clip = Rect.MinMaxRect(-extent, -extent, extent, top + shaderY);
            bool changed = clip != _lastRect;
            for (int i = 0; i < visuals.Count; i++)
            {
                Graphic graphic = visuals[i];
                if (!graphic || !graphic.isActiveAndEnabled) continue;
                CanvasRenderer renderer = graphic.canvasRenderer;
                bool clipped = renderer.hasRectClipping;
                if (!_owned.Contains(renderer))
                {
                    if (clipped) continue;
                    MaskableGraphic maskable = graphic as MaskableGraphic;
                    if (maskable && maskable.maskable && MaskUtilities.GetRectMaskForClippable(maskable)) continue;
                    _owned.Add(renderer);
                }
                if (changed || !clipped) renderer.EnableRectClipping(clip);
            }
            _lastRect = clip;
        }

        internal void Restore()
        {
            foreach (CanvasRenderer renderer in _owned)
                if (renderer && renderer.hasRectClipping) renderer.DisableRectClipping();
            _owned.Clear();
            _lastRect = new Rect();
        }
    }
}
