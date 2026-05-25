# Issue #162 — Support 슬롯 잠금 상태 시 Lock 아이콘 표시

## 1. 시스템 구조

`SupportSlotUI.SetState(locked, ...)`에서 `lockedLabel`(GameObject) ON/OFF로 잠금 상태를 표시하고 있음.
`lockedLabel`에 Lock 아이콘 Image를 넣으면 이미 동작하지만, Inspector에서 직접 Sprite를 지정할 수 있는 전용 `lockIcon` Image 필드가 없어 명시성이 부족.

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/UI/SupportSlotUI.cs`

## 3. 신규 클래스 / 파일

없음

## 4. 구현 상세

### SupportSlotUI.cs — lockIcon 필드 추가
```csharp
[SerializeField] private Image lockIcon;
```

### SetState — lockIcon 처리 추가
```csharp
if (lockIcon != null)
    lockIcon.gameObject.SetActive(locked);
```

### Unity Inspector 설정 필요
- `lockIcon` 필드에 Lock 아이콘 Image 컴포넌트 연결
- Lock Sprite를 해당 Image에 지정

## 5. 테스트 계획

- [ ] 잠긴 슬롯에 Lock 아이콘 표시 확인
- [ ] 슬롯 해금 후 Lock 아이콘 사라짐 확인
- [ ] 타워 미선택 시 Lock 아이콘 상태 정상 확인

## 6. 위험 요소

- `lockIcon` 미연결 시 null 체크로 무시 — 기존 `lockedLabel` 동작은 그대로 유지
