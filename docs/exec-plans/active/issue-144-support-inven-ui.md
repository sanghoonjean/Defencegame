# Issue #144 — Support Option 인벤토리 UI 및 타워 장착 기능 추가

## 1. 시스템 구조

### 현재 스킬 흐름 (참고)
```
ShopSystem.OwnedSkills
    → InvenUI (슬롯 목록 표시, InvenSlotDragHandler 부착)
        → SkillSlotUI.OnDrop (드래그 드롭 장착)
            → InventorySystem.EquipSkill()
```

### 구현할 Support Option 흐름
```
ShopSystem.OwnedSupports
    → SupportInvenUI (슬롯 목록 표시, SupportOptionDragHandler 부착)  ← 신규
        → SupportSlotUI.OnDrop (드래그 드롭 장착)  ← 기존 (이미 구현됨)
            → InventorySystem.SetSupportOption()
```

### 관련 기존 컴포넌트
- `SupportOptionDragHandler` — 이미 존재, `SupportSlotUI.OnDrop`에서 사용 중
- `SupportSlotUI.OnDrop` — 이미 구현됨, `SupportOptionDragHandler.Option` 읽어 장착
- `ShopSystem.OwnedSupports` — 구매한 Support Option 보관
- `ShopSystem.OnInventoryChanged` — 구매/반환 시 발생하는 이벤트

## 2. 수정 파일

없음 (기존 파일 수정 불필요)

## 3. 신규 클래스 / 파일

### `SupportInvenUI.cs`
- 역할: 구매한 Support Option 목록을 슬롯으로 표시
- 부착 위치: Shop/Inven 패널 내 Support Option 슬롯들의 부모 오브젝트
- 구조: `InvenUI`와 동일한 패턴

```
SupportInvenUI (MonoBehaviour)
├── Awake()        : 자식 슬롯 순회 → Image + SupportOptionDragHandler 등록
├── OnEnable()     : ShopSystem.OnInventoryChanged 구독, Refresh()
├── OnDisable()    : 구독 해제
└── Refresh()      : OwnedSupports 목록대로 슬롯 아이콘/드래그 데이터 갱신
```

## 4. 테스트 계획

- [ ] Shop에서 Support Option 구매 → SupportInvenUI 슬롯에 아이콘 표시 확인
- [ ] 슬롯에서 타워 Support Slot으로 드래그 앤 드롭 → 장착 확인
- [ ] 장착 후 슬롯에서 아이콘 제거 확인 (`RemoveOwnedSupportOption` 호출)
- [ ] 타워에서 다른 옵션으로 교체 시 기존 옵션 인벤토리로 반환 확인
- [ ] 타워 미선택 시 드롭 → 무반응 확인 (SupportSlotUI 기존 가드)

## 5. 위험 요소

- Unity Prefab/씬 설정 필요: SupportInvenUI 오브젝트 생성 및 슬롯 자식 배치 (스크립트만으로 해결 불가)
- 슬롯 수: 몇 개로 할지 미확정 — Inspector에서 자식 수로 자동 결정
- `SupportOptionDragHandler.Init(iconImage)` 호출 시 부모 Canvas가 없으면 NPE 발생 가능 → Awake에서 null 체크 필요
