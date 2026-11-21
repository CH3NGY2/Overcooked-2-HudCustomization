using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class UnityRecipeTests
{
    private sealed class UnfoldSample
    {
        private readonly UnityRecipeTests _owner;
        private readonly Transform _space;
        private readonly RecipeWidgetUIController _widget;
        private readonly string _label;
        private readonly List<Graphic> _graphics = new List<Graphic>();
        private bool _captured;
        private bool _unclipped;

        internal UnfoldSample(UnityRecipeTests owner, Transform space, RecipeWidgetUIController widget, string label)
        { _owner = owner; _space = space; _widget = widget; _label = label; }

        internal void Observe(float progress, Rect head, Vector2 offset, float alpha)
        {
            if (alpha < .05f || progress < .78f || progress >= 1f) return;
            if (_graphics.Count == 0)
                foreach (RecipeWidgetTile tile in _widget.GetComponentsInChildren<RecipeWidgetTile>())
                    if (!(tile is TopRecipeWidgetTile)) _graphics.AddRange(tile.GetComponentsInChildren<Graphic>());
            Canvas canvas = _widget.GetComponentInParent<Canvas>().rootCanvas;
            Vector3 displacement = canvas.transform.TransformVector((Vector3)offset);
            float highest = float.MinValue;
            int clipped = 0, active = 0;
            foreach (Graphic graphic in _graphics)
            {
                if (!graphic.isActiveAndEnabled) continue;
                active++;
                graphic.rectTransform.GetWorldCorners(_owner.corners);
                foreach (Vector3 corner in _owner.corners) highest = Mathf.Max(highest, _space.InverseTransformPoint(corner + displacement).y);
                if (graphic.canvasRenderer.hasRectClipping) clipped++;
            }
            float exposed = highest - head.yMax;
            if (exposed > 1f && active != clipped) _unclipped = true;
            File.AppendAllText(Path.Combine(output, _label + "-unfold.tsv"), progress + "\t" + head.yMax + "\t" + highest
                + "\t" + exposed + "\t" + alpha + "\t" + clipped + "\t" + _graphics.Count + "\n");
            if (!_captured && exposed > 1f && alpha > .7f)
            {
                _captured = true;
                ScreenCapture.CaptureScreenshot(Path.Combine(output, _label + "-unfold.png"));
            }
        }

        internal void Verify()
        {
            _owner.Check(_captured, _label + ": captured native ingredient and cookware unfold with geometry above main paper");
            _owner.Check(!_unclipped, _label + ": that part of the animation has a clip on every active branch graphic");
        }
    }

    private void VerifyTopClip(List<RecipeWidgetUIController> widgets, Transform space, string label, bool enabled)
    {
        float maxOverhang = 0f;
        bool valid = true;
        foreach (RecipeWidgetUIController widget in widgets)
        {
            float top = HeadBounds(widget, space).yMax;
            foreach (Graphic graphic in widget.GetComponentsInChildren<Graphic>())
            {
                if (!graphic.isActiveAndEnabled) continue;
                graphic.rectTransform.GetWorldCorners(corners);
                foreach (Vector3 corner in corners) maxOverhang = Mathf.Max(maxOverhang, space.InverseTransformPoint(corner).y - top);
                valid &= graphic.canvasRenderer.hasRectClipping == enabled;
            }
        }
        File.AppendAllText(Path.Combine(output, "top-clip.tsv"), label + "\t" + maxOverhang + "\t" + enabled + "\n");
        Check(!enabled || maxOverhang > 1f, label + ": settled recipe still has native paper geometry sticking up above the displayed head");
        Check(valid, label + ": all active graphics have the expected top clipping state");
    }
}
