# Issue #116 — 드래그 중 인벤토리 슬롯 강조 효과

## 1. 시스템 구조

- `InvenSlotDragHandler`에 static 이벤트 추가
- `DropTargetHighlight` 컴포넌트가 이벤트를 구독해 배경 색상 변경
- 드랍 대상 GameObject에 `DropTargetHighlight` 부착 (Inspector)

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/UI/InvenSlotDragHandler.cs`

## 3. 신규 파일

- `MakeDefence/Assets/Scripts/UI/DropTargetHighlight.cs`

## 4. 구현 상세

### InvenSlotDragHandler
```csharp
public static event Action OnSkillDragStarted;
public static event Action OnSkillDragEnded;
// OnBeginDrag → OnSkillDragStarted?.Invoke()
// OnEndDrag   → OnSkillDragEnded?.Invoke()
```

### DropTargetHighlight
- `[SerializeField] Image targetImage` — 미연결 시 GetComponent 자동 탐색
- `[SerializeField] Color highlightColor` — 기본값: 노란색 반투명
- `OnEnable/OnDisable`에서 이벤트 구독/해제
- `ShowHighlight()`: targetImage.color = highlightColor
- `HideHighlight()`: targetImage.color = _originalColor

## 5. Unity Inspector 설정

아래 GameObject에 `DropTargetHighlight` 컴포넌트 추가:
- SkillSlotUI가 있는 GameObject
- InvenDropHandler가 있는 GameObject
- ShopDropHandler가 있는 GameObject

## 6. 테스트 계획

- [ ] 인벤토리 슬롯 드래그 시작 → 드랍 대상 강조 확인
- [ ] 드래그 종료 / 드랍 후 강조 제거 확인

## 7. 위험 요소

- `targetImage` 미연결 시 GetComponentInChildren으로 첫 번째 Image를 사용 — 원하지 않는 Image가 선택될 수 있으므로 Inspector에서 직접 지정 권장
