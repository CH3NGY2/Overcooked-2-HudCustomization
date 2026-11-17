using UnityEngine;
using UnityEngine.UI;

namespace OC2HudCustomization
{
    internal sealed class ContentsGeometry
    {
        private readonly RectTransform _rect;
        private readonly AnchorGridLayoutGroup _grid;
        private readonly Vector2 _baseCell, _baseSpacing, _baseMin, _baseMax, _anchorMin, _anchorMax;
        private readonly OwnedValue<Vector2> _min, _max, _cell, _spacing;
        private readonly OwnedValue<AnchorGridLayoutGroup.Constraint> _constraint;
        private readonly OwnedValue<AnchorGridLayoutGroup.Axis> _axis;
        private readonly OwnedValue<int> _columns;
        private bool _blocked;

        internal ContentsGeometry(RectTransform rect, AnchorGridLayoutGroup grid, HudLog log)
        {
            _rect = rect; _grid = grid;
            _baseCell = grid.cellSize; _baseSpacing = grid.spacing;
            _baseMin = rect.offsetMin; _baseMax = rect.offsetMax;
            _anchorMin = rect.anchorMin; _anchorMax = rect.anchorMax;
            System.Action conflict = delegate { log.WarnOnce("contents-layout-conflict", "Contents geometry changed by another owner; stop writing the affected properties."); };
            _min = new OwnedValue<Vector2>(delegate { return rect.offsetMin; }, delegate(Vector2 value) { rect.offsetMin = value; }, conflict, UnityValue.SameVector);
            _max = new OwnedValue<Vector2>(delegate { return rect.offsetMax; }, delegate(Vector2 value) { rect.offsetMax = value; }, conflict, UnityValue.SameVector);
            _cell = new OwnedValue<Vector2>(delegate { return grid.cellSize; }, delegate(Vector2 value) { grid.cellSize = value; }, conflict);
            _spacing = new OwnedValue<Vector2>(delegate { return grid.spacing; }, delegate(Vector2 value) { grid.spacing = value; }, conflict);
            _constraint = new OwnedValue<AnchorGridLayoutGroup.Constraint>(delegate { return grid.constraint; }, delegate(AnchorGridLayoutGroup.Constraint value) { grid.constraint = value; }, conflict);
            _axis = new OwnedValue<AnchorGridLayoutGroup.Axis>(delegate { return grid.startAxis; }, delegate(AnchorGridLayoutGroup.Axis value) { grid.startAxis = value; }, conflict);
            _columns = new OwnedValue<int>(delegate { return grid.constraintCount; }, delegate(int value) { grid.constraintCount = value; }, conflict);
        }

        internal Vector2 NativeSize
        {
            get
            {
                RectTransform parent = _rect.parent as RectTransform;
                if (!parent) return Vector2.zero;
                return Vector2.Scale(parent.rect.size, _anchorMax - _anchorMin) + _baseMax - _baseMin;
            }
        }

        internal void Apply(int count, HudOptions options)
        {
            if (_blocked) return;
            if (!_min.CanApply() || !_max.CanApply() || !_cell.CanApply() || !_spacing.CanApply()
                || !_constraint.CanApply() || !_axis.CanApply() || !_columns.CanApply())
            {
                _blocked = true;
                Restore(); // Restore the remaining owned fields; preserve the conflicting one.
                return;
            }
            if (count == 0 || (options.ContentsPerRow == 0 && Mathf.Approximately(options.ContentsScale, 1f)))
            { Restore(); return; }
            Vector2 native = NativeSize;
            if (native.x <= 0.01f || native.y <= 0.01f) return;
            Vector2 target = native * options.ContentsScale;
            if (options.ContentsPerRow > 0)
            {
                int columns = HudLayoutMath.Columns(count, options.ContentsPerRow);
                int rows = HudLayoutMath.Rows(count, columns);
                Vector2 pixels = Vector2.Scale(native, _baseCell) * options.ContentsScale;
                Vector2 gaps = Vector2.Scale(native, _baseSpacing) * options.ContentsScale;
                target = new Vector2(HudLayoutMath.Extent(columns, pixels.x, gaps.x, _grid.padding.horizontal),
                    HudLayoutMath.Extent(rows, pixels.y, gaps.y, _grid.padding.vertical));
                if (target.x <= 0.01f || target.y <= 0.01f) return;
                _cell.Apply(new Vector2(pixels.x / target.x, pixels.y / target.y));
                _spacing.Apply(new Vector2(gaps.x / target.x, gaps.y / target.y));
                _constraint.Apply(AnchorGridLayoutGroup.Constraint.FixedColumnCount);
                _columns.Apply(columns);
                _axis.Apply(AnchorGridLayoutGroup.Axis.Horizontal);
            }
            else
            {
                _cell.Restore(); _spacing.Restore(); _constraint.Restore(); _columns.Restore(); _axis.Restore();
            }
            // Expand the existing generated region around its original center; the native hover
            // controller, parent anchors, pivot, material and child ownership remain intact.
            Vector2 halfChange = (target - native) * 0.5f;
            _min.Apply(_baseMin - halfChange);
            _max.Apply(_baseMax + halfChange);
        }

        internal void Restore()
        {
            if (!_rect || !_grid) return;
            _min.Restore(); _max.Restore(); _cell.Restore(); _spacing.Restore();
            _constraint.Restore(); _columns.Restore(); _axis.Restore();
        }
    }
}
