# Issue #126 — 큐브(재화) 수량 UI 표시

## 1. 시스템 구조

```
CubeSystem.OnCubeChanged(CubeType, int)
  → CubeUIDisplay.OnCubeChanged()
      → 해당 타입의 Text.text = count.ToString()
```

## 2. 수정 파일

없음

## 3. 신규 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/UI/CubeUIDisplay.cs` | CubeType별 Text 컴포넌트를 Inspector에서 연결, OnCubeChanged 구독하여 수량 갱신 |

## 4. 구현 세부

### CubeUIDisplay.cs

```csharp
[SerializeField] private Text lowerText;
[SerializeField] private Text upperText;
[SerializeField] private Text topTierText;
[SerializeField] private Text deleteText;
[SerializeField] private Text cloneText;

OnEnable → CubeSystem.OnCubeChanged += OnCubeChanged, 초기값 갱신
OnDisable → CubeSystem.OnCubeChanged -= OnCubeChanged

void OnCubeChanged(CubeType type, int count) → 해당 Text 갱신
void RefreshAll() → GetCount로 전체 초기화
```

## 5. 테스트 계획

- [ ] C키 큐브 지급 시 UI 수치 즉시 갱신 확인
- [ ] 큐브 소모(슬롯 해금 등) 시 수치 감소 확인

## 6. 위험 요소

- CubeSystem.Instance가 null인 경우 RefreshAll 스킵
- Text 컴포넌트 미연결 시 해당 타입 갱신 스킵 (null 체크)
- .unity / .prefab 수정 없음 — Inspector 연결은 사용자 직접
