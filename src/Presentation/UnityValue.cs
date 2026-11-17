using UnityEngine;

namespace OC2HudCustomization
{
    internal static class UnityValue
    {
        internal static bool SameVector(Vector2 a, Vector2 b)
        {
            // RectTransform stores position/size and computes offsets; the opposite offset
            // setter may introduce float noise without transferring ownership.
            return (a - b).sqrMagnitude <= 0.000001f;
        }
    }
}
