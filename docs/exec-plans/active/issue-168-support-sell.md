# Issue #168 — 보조 옵션(서포트) 인벤토리에서 상점으로 드래그해 판매

## 1. 시스템 구조

스킬 판매(Issue #161)와 동일한 흐름을 서포트에 적용.

- `OwnedSupportSlotUI`에는 이미 `SupportOptionDragHandler`가 붙어 있음
- `SupportSlotUI`(장착 슬롯)에 `SupportOptionDragHandler` 추가 → 드래그 가능하게
- `ShopDropHandler.OnDrop`에서 `SupportOptionDragHandler` 감지 → 판매 처리
- `SellConfirmPopup`에 `ShowSupportSell(SupportOptionData)` 추가

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/UI/SellConfirmPopup.cs`
- `MakeDefence/Assets/Scripts/UI/ShopDropHandler.cs`
- `MakeDefence/Assets/Scripts/UI/SupportSlotUI.cs`
- `MakeDefence/Assets/Scripts/Systems/ShopSystem.cs`

## 3. 신규 클래스 / 파일

없음

## 4. 구현 상세

### ShopSystem — RemoveOwnedSupportOption bool 반환
```csharp
public bool RemoveOwnedSupportOption(SupportOptionData option)
{
    if (!_ownedSupports.Remove(option)) return false;
    OnInventoryChanged?.Invoke();
    return true;
}
```

### SellConfirmPopup — 서포트 판매 지원
- `_pendingSupport` 필드 추가
- `ShowSupportSell(SupportOptionData)` 추가
- `OnConfirm`: support 분기 — 인벤토리 우선, 없으면 장착 슬롯 탐색
- `Hide`: `_pendingSupport = null`

### ShopDropHandler — SupportOptionDragHandler 처리
- `SellSupportOption(SupportOptionData)` 추가
- fallback: `RemoveOwnedSupportOption` 성공 시 큐브 지급, 실패 시 장착 슬롯 탐색

### SupportSlotUI — 장착 슬롯 드래그 지원
- `Awake`에서 `SupportOptionDragHandler` 추가/캐싱
- `SetState`에서 `_dragHandler.Option` 동기화

## 5. 테스트 계획

- [ ] 보유 서포트 슬롯 → 상점 드래그 → 팝업 표시 확인
- [ ] 확인 클릭 → 서포트 제거 + 하급 큐브 1개 획득 확인
- [ ] 취소 클릭 → 서포트 유지 확인
- [ ] 장착된 서포트 슬롯 → 상점 드래그 → 판매 확인

## 6. 위험 요소

- `SupportSlotUI`에 `SupportOptionDragHandler` 추가 시 기존 `IDropHandler` 동작과 충돌 없음
  (드래그 핸들러는 나가는 드래그, 드롭 핸들러는 받는 드롭이므로 독립적)
