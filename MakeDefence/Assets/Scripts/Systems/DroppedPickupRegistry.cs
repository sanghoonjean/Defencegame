using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 드랍 픽업(차원석/큐브 등)의 활성 위치를 공유.
/// 스폰 시 종류와 무관하게 다른 픽업 라벨과 겹치지 않도록 위치 보정에 사용.
/// </summary>
public static class DroppedPickupRegistry
{
    private static readonly HashSet<Transform> _all = new();

    public static void Register(Transform t)
    {
        if (t != null) _all.Add(t);
    }

    public static void Unregister(Transform t)
    {
        _all.Remove(t);
    }

    /// <summary>등록된 모든 픽업과 minSeparation 이상 떨어진 위치를 반환 (반복 횟수 제한).</summary>
    public static Vector2 ResolveSpawnPos(Vector2 desired, float minSeparation, int attempts)
    {
        if (minSeparation <= 0f || attempts <= 0) return desired;
        float sqr = minSeparation * minSeparation;
        for (int i = 0; i < attempts; i++)
        {
            bool ok = true;
            foreach (var t in _all)
            {
                if (t == null) continue;
                Vector2 pos = t.position;
                if ((pos - desired).sqrMagnitude < sqr)
                {
                    Vector2 away = desired - pos;
                    if (away.sqrMagnitude < 0.0001f)
                        away = Random.insideUnitCircle.normalized;
                    else
                        away.Normalize();
                    desired = pos + away * minSeparation;
                    ok = false;
                    break;
                }
            }
            if (ok) break;
        }
        return desired;
    }
}
