# Issue #174 — 타워 서포트 슬롯 동일 옵션 중복 장착 방지

## 1. 시스템 구조

`SupportSlotUI.OnDrop`에서 드롭 전 타워의 다른 슬롯을 순회해 동일 옵션이 있으면 장착 거부.

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/UI/SupportSlotUI.cs`

## 3. 신규 클래스 / 파일

없음

## 4. 구현 상세

```csharp
// OnDrop 내 같은 슬롯 no-op 체크 이후
for (int i = 0; i < tower.UnlockedSupportSlots; i++)
{
    if (i == slotIndex) continue;   // 교체 대상 슬롯 제외
    if (i == sourceSlotIdx) continue; // 소스 슬롯(스왑 시 비워질 자리) 제외
    if (tower.SupportOptions[i] == newOption) return;
}
```

## 5. 테스트 계획

- [ ] 동일 옵션 2개 구매 후 타워 슬롯0, 슬롯1에 각각 장착 시도 → 두 번째 장착 거부 확인
- [ ] 서로 다른 타워에 같은 옵션 각각 장착 → 정상 동작 확인
- [ ] 슬롯 간 스왑 시 소스 슬롯 제외 처리 → 같은 옵션끼리 스왑 시 정상 처리 확인

## 6. 위험 요소

없음
