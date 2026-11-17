using System;

namespace OC2HudCustomization
{
    internal enum HudTargetKind { Score, Timer, Chopping, Cooking, Mixing, Heat, Washing, Fire, Count }

    // Coordinates are fractions of the screen, positive Y points upwards. Whitelisting only
    // controls editor picking; saved presentation remains applied when picking is disabled.
    internal sealed class HudGeometry
    {
        internal float X, Y, Scale = 1f, Width = 1f, Height = 1f, Opacity = 1f;
        internal bool Allowed = true;
        internal HudGeometry Copy() { return (HudGeometry)MemberwiseClone(); }
        internal bool Same(HudGeometry other)
        { return X == other.X && Y == other.Y && Scale == other.Scale && Width == other.Width && Height == other.Height && Opacity == other.Opacity && Allowed == other.Allowed; }
        internal void Validate()
        {
            X = Limit(X, -1f, 1f, 0f); Y = Limit(Y, -1f, 1f, 0f);
            Scale = Limit(Scale, .2f, 3f, 1f);
            Width = Limit(Width, .25f, 4f, 1f); Height = Limit(Height, .25f, 4f, 1f);
            Opacity = Limit(Opacity, 0f, 1f, 1f);
        }
        private static float Limit(float value, float min, float max, float fallback)
        { return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value)); }
        internal static HudGeometry[] CopyAll(HudGeometry[] source)
        {
            HudGeometry[] result = new HudGeometry[source.Length];
            for (int i = 0; i < result.Length; i++) result[i] = source[i].Copy();
            return result;
        }
    }

    [Flags]
    internal enum HudHandle { Move = 0, Left = 1, Right = 2, Top = 4, Bottom = 8 }

    internal static class HudDragMath
    {
        // Resizing pins the opposite edge. Corner movement projects onto the diagonal to
        // keep the original aspect ratio, independent of the pointer's movement direction.
        internal static HudGeometry Drag(HudGeometry start, HudHandle handle, float dx, float dy,
            float width, float height, float screenWidth, float screenHeight)
        {
            HudGeometry next = start.Copy();
            if (handle == HudHandle.Move)
            { next.X += dx / screenWidth; next.Y -= dy / screenHeight; }
            else
            {
                float sx = (handle & HudHandle.Left) != 0 ? -1f : (handle & HudHandle.Right) != 0 ? 1f : 0f;
                float sy = (handle & HudHandle.Top) != 0 ? -1f : (handle & HudHandle.Bottom) != 0 ? 1f : 0f;
                float fx = width > 0f ? Math.Max(.01f, 1f + sx * dx / width) : 1f;
                float fy = height > 0f ? Math.Max(.01f, 1f + sy * dy / height) : 1f;
                if (sx != 0f && sy != 0f)
                {
                    float factor = (width * width * fx + height * height * fy) / (width * width + height * height);
                    next.Scale *= factor; next.Validate(); fx = fy = next.Scale / start.Scale;
                }
                else
                {
                    if (sx != 0f) next.Width *= fx;
                    if (sy != 0f) next.Height *= fy;
                    next.Validate(); fx = next.Width / start.Width; fy = next.Height / start.Height;
                }
                next.X += sx * width * (fx - 1f) * .5f / screenWidth;
                next.Y -= sy * height * (fy - 1f) * .5f / screenHeight;
            }
            next.Validate(); return next;
        }
    }
}
