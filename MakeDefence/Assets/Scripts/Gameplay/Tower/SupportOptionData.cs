using UnityEngine;

public enum SupportOptionType
{
    // 공격 강화
    OverloadModule, AccelChip, AoeAmplifier, MultiProjectile, ThresholdCircuit, CritAmplifier,
    // 상태이상
    EmpAmplifier, CoolantDevice, CorrosiveRound, IncendiaryRound,
    // 특수
    ChainCircuit, PiercingRound, EnergyDrain
}

[CreateAssetMenu(fileName = "SupportOptionData", menuName = "MakeDefence/Support Option Data")]
public class SupportOptionData : ScriptableObject
{
    public SupportOptionType optionType;
    [TextArea] public string description;
    public float value;
}
