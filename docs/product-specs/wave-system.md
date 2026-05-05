# 기능 스펙: WaveSystem

## 개요
플레이어가 수동 또는 자동으로 웨이브를 생성하고,
난이도에 따라 적을 스폰하며 클리어 시 큐브 보상을 지급한다.

---

## 기능 요구사항

### 1. 웨이브 생성
- 플레이어가 "웨이브 시작" 버튼을 누르면 웨이브가 시작된다
- 자동 생성 토글(on/off)이 있으며, 활성화 시 이전 웨이브 종료 후 자동으로 다음 웨이브가 시작된다
- 웨이브 진행 중에는 새 웨이브를 시작할 수 없다

### 2. 난이도 단계
- 난이도는 1~16단계이며 플레이어가 직접 선택한다
- 현재 단계를 클리어해야 다음 단계가 해금된다
- 단계별 적 스탯 증가:
  - HP/방어력: `기준값 * (1 + stage * 0.05)`
  - 이동속도: `기준값 * (1 + stage * 0.02)`

### 3. 웨이브당 적 수
| 단계 | 적 수 |
|------|-------|
| 1~4  | 15마리 |
| 5~8  | 20마리 |
| 9~12 | 25마리 |
| 13~16| 30마리 |

### 4. 적 등급 비율
```
Magic  비율 = min(stage * 2%, 35%)
Rare   비율 = max(0, (stage - 4) * 3%)
Unique      = stage >= 10 ? 1마리 고정 : 0
Normal      = 나머지
```

### 5. 클리어 조건
- 웨이브의 모든 적을 처치하면 클리어
- 웨이브 중 플레이어 HP가 0이 되면 실패 → 보상 없음

### 6. 보상
- 클리어 시 큐브 드롭 (5종, 가중치 기반 랜덤)
- 실패 시 보상 없음

### 7. 플레이어 체력
- 기본 HP: 100
- 웨이브 시작 시 HP 초기화
- 적이 기지를 통과할 때 등급별 데미지만큼 감소

---

## 인터페이스

```csharp
public class WaveSystem
{
    public void StartWave();
    public void StopWave();
    public void SetAutoWave(bool enabled);
    public void SetDifficulty(int stage);    // 1~16
    public bool IsWaveActive { get; }
    public int CurrentStage { get; }

    public static event Action<int> OnWaveStarted;   // stage
    public static event Action<bool> OnWaveEnded;    // isCleared
}
```

---

## 검증 조건
- [ ] 1단계에서 normal 적 15마리 스폰
- [ ] 10단계에서 Unique 1마리 고정 포함
- [ ] 클리어 시 큐브 드롭 발생
- [ ] 실패 시 큐브 드롭 없음
- [ ] 자동 토글 on 시 웨이브 자동 재시작
- [ ] 단계 클리어 시 다음 단계 해금
