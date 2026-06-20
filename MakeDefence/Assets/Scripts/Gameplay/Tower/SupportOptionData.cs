using UnityEngine;

public enum SupportOptionType
{
    // 공격 강화
    OverloadModule, AccelChip, AoeAmplifier, MultiProjectile, ThresholdCircuit, CritAmplifier,
    // 상태이상
    EmpAmplifier, CoolantDevice, CorrosiveRound, IncendiaryRound,
    // 특수
    ChainCircuit, PiercingRound, EnergyDrain,
    // Restriction — Physical 증폭 대신 원소·카오스 차단
    BrutalitySupport,
}

[CreateAssetMenu(fileName = "SupportOptionData", menuName = "MakeDefence/Support Option Data")]
public class SupportOptionData : ScriptableObject, IInventoryItem
{
    [Header("Display")]
    public string displayName;
    public Sprite icon;

    string            IInventoryItem.DisplayName => displayName;
    Sprite            IInventoryItem.Icon        => icon;
    InventoryItemKind IInventoryItem.Kind        => InventoryItemKind.Support;

    [Header("Stats — value는 0.0~1.0 비율로 입력 (예: 0.3 = 30%)")]
    public SupportOptionType optionType;
    [TextArea] public string description;
    [Range(0f, 1f)] public float value;
}
