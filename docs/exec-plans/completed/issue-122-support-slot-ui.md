# Issue #122 — 보조 옵션 슬롯 해금 및 장착 UI 구현

## 1. 시스템 구조

```
[해금 흐름]
SupportSlotUI(잠금 슬롯) 클릭
  → SupportUnlockPopup.Show(slotIndex, cost)
      → 취소: 팝업 닫기
      → 확인: InventorySystem.UnlockSupportSlot() → OnTowerSelected 발생 → 슬롯 갱신

[장착 흐름]
SupportOptionDragHandler(드래그 소스)
  → SupportSlotUI(IDropHandler) OnDrop
      → InventorySystem.SetSupportOption(slotIndex, option) → OnTowerSelected 발생 → 슬롯 갱신
```

## 2. 수정 파일

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/UI/SupportSlotUI.cs` | Button 클릭(잠금 슬롯) → 팝업 호출, IDropHandler 추가 |

## 3. 신규 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/UI/SupportUnlockPopup.cs` | 해금 확인 팝업 — 비용 표시, 취소/확인 버튼, 큐브 부족 시 확인 버튼 비활성 |

## 4. 구현 세부

### SupportUnlockPopup.cs
- `static Instance` — 씬에 하나만 존재
- `Show(int slotIndex, int cost)` — 팝업 활성화, 비용 텍스트 갱신, 확인 버튼 interactable 체크
- 확인 클릭 → `InventorySystem.Instance.UnlockSupportSlot()` → Hide()
- 취소 클릭 → Hide()
- Inspector 연결: costText, confirmButton, cancelButton, panel

### SupportSlotUI.cs 수정
- `IPointerClickHandler` 추가: 잠금 슬롯 클릭 시 `SupportUnlockPopup.Instance.Show(slotIndex, cost)` 호출
- `IDropHandler` 추가: 해금된 슬롯에 드랍 시 `InventorySystem.Instance.SetSupportOption(slotIndex, option)` 호출

## 5. 테스트 계획

- [ ] 잠금 슬롯 클릭 → 팝업 표시, 비용 확인
- [ ] 취소 클릭 → 팝업 닫힘, 재화 유지 확인
- [ ] 확인 클릭 → 상위 큐브 소모, 슬롯 해금 확인
- [ ] 큐브 부족 시 확인 버튼 비활성화 확인
- [ ] 해금 슬롯에 보조 옵션 드랍 → 장착 확인
- [ ] 타워 선택 변경 시 슬롯 상태 갱신 확인

## 6. 위험 요소

- SupportUnlockPopup은 씬에 1개만 존재해야 함 (Inspector 연결 필요)
- 해금 비용은 Tower.SupportSlotCost가 private이므로 Tower에 비용 조회 메서드 추가 필요
- .unity / .prefab 수정 없음 — Inspector 연결은 사용자 직접
