using System;

namespace OC2HudCustomization
{
    // No Unity calls: these are the exact rules used by the production adapters and local tests.
    internal static class HudLayoutMath
    {
        internal static bool NeedsWrap(int rowCount, int limit, float usedWidth, float nextWidth, float gap, float safeWidth)
        {
            return limit > 0 && rowCount > 0 &&
                (rowCount >= limit || usedWidth + gap + nextWidth > safeWidth);
        }

        internal static int Columns(int count, int limit) { return count == 0 ? 0 : Math.Min(count, Math.Max(1, limit)); }
        internal static int Rows(int count, int columns) { return count == 0 ? 0 : 1 + (count - 1) / Math.Max(1, columns); }
        internal static float Extent(int cells, float cell, float gap, float padding)
        {
            return cells == 0 ? 0f : cells * cell + (cells - 1) * gap + padding;
        }
    }

    internal struct HudRowLayout
    {
        private readonly int _limit;
        private readonly float _origin, _gap, _rowGap, _safeWidth;
        private float _cursor, _top, _height, _used;
        private int _count;
        internal HudRowLayout(float origin, float top, int limit, float gap, float rowGap, float safeWidth)
        {
            _origin = origin; _cursor = origin; _top = top; _limit = limit;
            _gap = gap; _rowGap = rowGap; _safeWidth = safeWidth;
            _height = 0f; _used = 0f; _count = 0;
        }

        internal void Place(float width, float height, out float left, out float top)
        {
            if (HudLayoutMath.NeedsWrap(_count, _limit, _used, width, _gap, _safeWidth))
            {
                _top -= _height + _rowGap;
                _cursor = _origin; _height = 0f; _count = 0; _used = 0f;
            }
            left = _cursor; top = _top;
            _height = Math.Max(_height, height);
            _used += (_count == 0 ? 0f : _gap) + width;
            _cursor += width + _gap;
            _count++;
        }
    }
}
