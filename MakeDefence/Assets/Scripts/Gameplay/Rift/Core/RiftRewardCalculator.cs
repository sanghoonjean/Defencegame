using UnityEngine;

public static class RiftRewardCalculator
{
    /// <summary>
    /// 기본 보상에 RewardCubeMult 를 곱해 반환. 결과는 비음수 정수로 라운딩.
    /// 음수/0 입력은 0 으로 가드.
    /// </summary>
    public static int CalculateCubeReward(int baseReward, float rewardCubeMult)
    {
        if (baseReward <= 0) return 0;
        if (rewardCubeMult <= 0f) return 0;
        return Mathf.Max(0, Mathf.RoundToInt(baseReward * rewardCubeMult));
    }
}
