using UnityEngine;

public static class AoeUtils
{
    public static bool IsInAoe(Vector2 point, Vector2 origin, Vector2 forward,
        AoeShape shape, float radius, float width, float halfAngleDeg)
    {
        Vector2 toPoint = point - origin;

        switch (shape)
        {
            case AoeShape.Circle:
                return toPoint.sqrMagnitude <= radius * radius;

            case AoeShape.Rectangle:
            {
                Vector2 fwd   = forward.normalized;
                Vector2 right = new Vector2(-fwd.y, fwd.x);
                float along   = Vector2.Dot(toPoint, fwd);
                float perp    = Mathf.Abs(Vector2.Dot(toPoint, right));
                return along >= 0f && along <= radius && perp <= width * 0.5f;
            }

            case AoeShape.Cone:
                if (toPoint.sqrMagnitude > radius * radius) return false;
                if (toPoint.sqrMagnitude < 0.0001f) return true;
                return Vector2.Angle(forward, toPoint) <= halfAngleDeg;

            default:
                return false;
        }
    }

    public static void ShowAoeHit(Vector2 origin, Vector2 forward, AoeShape shape,
        float radius, float width, float halfAngleDeg, GameObject fxPrefab = null)
    {
        switch (shape)
        {
            case AoeShape.Circle:
                GameUIManager.ShowAoeHit(origin, radius, fxPrefab);
                break;
            case AoeShape.Rectangle:
                GameUIManager.ShowRectAoeHit(origin, forward, width, radius);
                break;
            case AoeShape.Cone:
                GameUIManager.ShowConeAoeHit(origin, forward, halfAngleDeg, radius);
                break;
        }
    }

    public static Vector2 Rotate(Vector2 v, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
