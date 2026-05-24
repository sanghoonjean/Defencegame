# Issue #152 — SupportOptionData.value 범위 정규화

## 1. 시스템 구조

`SupportOptionData.value`가 비율(0.0~1.0)과 퍼센트(0~100) 중 어느 단위인지
필드 선언부에 명시가 없어 잘못 입력 시 데미지 폭증 위험.

현재 사용:
```csharp
// Tower.AccumulateSupportOption
case SupportOptionType.IncendiaryRound: AddedFireRatio += opt.value; break;
```

value = 0.3 → AddedFireRatio = 0.3 (30%) → 정상  
value = 30  → AddedFireRatio = 30   (3000%) → 데미지 30배 폭증

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Gameplay/Tower/SupportOptionData.cs`
- `MakeDefence/Assets/Scripts/Gameplay/Tower/Tower.cs`

## 3. 신규 클래스 / 파일

없음

## 4. 구현 상세

### SupportOptionData.cs — Range 어트리뷰트 + Header 명시
```csharp
[Header("Stats — value는 0.0~1.0 비율로 입력 (예: 0.3 = 30%)")]
public SupportOptionType optionType;
[TextArea] public string description;
[Range(0f, 1f)] public float value;
```

### Tower.AccumulateSupportOption — Clamp01 방어 처리
잘못 입력된 에셋이 있어도 런타임에서 1.0으로 상한 제한.
```csharp
case SupportOptionType.IncendiaryRound:
    AddedFireRatio += Mathf.Clamp01(opt.value);
    break;
```

## 5. 테스트 계획

- [ ] Inspector에서 value 슬라이더가 0~1 범위로 제한되는지 확인
- [ ] value = 0.3 설정 → AddedFireRatio = 0.3 확인
- [ ] value = 1.0 설정 → AddedFireRatio = 1.0 (100%) 확인 (상한)
- [ ] IncendiaryRound 미장착 시 기존 동작 동일 확인

## 6. 위험 요소

- 기존 `.asset` 파일에 value > 1.0이 저장된 경우 Clamp01로 1.0으로 강제 조정됨
  → 의도치 않은 데미지 감소 가능. 에셋 값 점검 권장.
- `[Range]` 어트리뷰트는 Inspector에서만 제한, 코드로 직접 할당 시 무관
  → 런타임 Clamp01로 이중 방어.
