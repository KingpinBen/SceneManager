using UnityEngine;
using System.Collections;

public static partial class Extensions
{
    public static bool Contains(this Rect rect, Bounds bounds)
    {
        //  Bottom left is bounds.min
        //  Top right is bounds.max

        //  Bounds left < rect.right
        //  Bounds right > rect.left
        //  Bounds top > rect.bottom
        //  Bounds bottom < rect.top

        if (bounds.min.x < rect.xMax &&
            bounds.max.x > rect.x &&
            bounds.max.y > rect.y - rect.height &&
            bounds.min.y < rect.y)
            return true;

        return false;
    }

    public static Vector2 ClosestPoint(this Rect rect, Vector2 other)
    {
        return new Vector2(
            Mathf.Clamp(other.x, rect.x, rect.x + rect.width),
            Mathf.Clamp(other.y, rect.y - rect.height, rect.y));
    }
}