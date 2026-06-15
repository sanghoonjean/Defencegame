# Issue #221 — [FEAT] 타워 삭제 시 장착 아이템 개별 환급

> #218 타워 삭제의 후속. 현재는 배치 비용(Lower 1)만 환급되고 장착된 스킬/보조 옵션/아이템은 함께 소멸한다. 본 작업은 각 장착물을 정책에 맞춰 회수해 사용자의 사전 판매/탈착 부담을 제거한다.

## 1. 시스템 구조

타워 삭제 확정 시 다음을 일괄 처리한다.

```
[TowerDeleteConfirmPopup.Show(tower)]
   ↓ _pendingTower = tower 캡처
   ↓ InventorySystem.BuildDeleteSummary(tower) 호출
   ↓   ├─ skillReturn       : EquippedSkill != null
   ↓   ├─ supportReturn[5]  : 슬롯별 SupportOptions[i] != null 카운트
   ↓   └─ itemSell          : ItemSystem.GetItem(tower, slot) != null 카운트
   ↓ messageText 동적 구성 — "타워 + 스킬 X / 서포트 Y → 인벤 / 아이템 Z + 1 → Lower (Z+1)"
   ↓ Confirm
[InventorySystem.DeleteTower(target)]
   ├─ summary = BuildDeleteSummary(target)
   ├─ 스킬 회수 : ShopSystem.ReturnSkill(EquippedSkill)
   ├─ 서포트 회수: 5슬롯 순회 → ShopSystem.ReturnSupportOption(opt)
   ├─ 아이템 정산: 비어있지 않은 슬롯 수 × Lower 1 → CubeSystem.Add
   ├─ 배치비 환급: CubeSystem.Add(Lower, 1)
   ├─ SelectedTower == target → Deselect()
   └─ Destroy(target.gameObject)
         ↓ Tower.OnDestroy() (변경 없음)
         ├─ ItemSystem.UnregisterTower(this)
         └─ MapTileSystem.RemoveTower(TileCoord)
```

### 핵심 정책 결정 (이슈 미정 항목 처리)

| 항목 | 결정 | 근거 |
|------|------|------|
| 스킬 / 보조 옵션 | **인벤토리 복귀(`ShopSystem.ReturnSkill` / `ReturnSupportOption`)** | 자산 참조형 데이터(`ScriptableObject`)라 보존 가치가 높음. `ShopSystem` 보유 목록은 `List<>` 기반이라 슬롯 한도가 없어 "인벤 가득 참" 케이스가 발생하지 않음 |
| 아이템(`ItemData`) | **자동 판매 — 슬롯당 Lower 1개 환급** | 아이템은 슬롯 잠금 시 무작위 롤된 인스턴스로 인벤 표시 대상이 아님. `SellConfirmPopup`의 "아이템당 Lower 1개" 컨벤션과 일치 ([SellConfirmPopup.cs:124](MakeDefence/Assets/Scripts/UI/SellConfirmPopup.cs#L124)) |
| 아이템 슬롯 잠금 비용 환급 (10/20/30) | **환급하지 않음** | 잠금 비용은 진행 자원이지 자산이 아님. 환급 시 슬롯 잠금→해체 무한 루프로 큐브 농사가 가능해짐 |
| 배치 비용 환급 | **Lower 1개 유지** | #218 정책 그대로 |
| 부분 환급 비율 | **적용 안 함 (전액)** | 기존 판매 로직과 동일. 추후 밸런싱 이슈 발생 시 별도 처리 |
| 확인 팝업 메시지 | **회수 예상 결과 표시** | 이슈 요구사항. "타워 + 스킬 N / 서포트 M → 인벤, 아이템 K → Lower (K+1)" 형식 |

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Systems/InventorySystem.cs`
  - `DeleteTower(Tower target)` 확장 — 스킬/보조옵션 회수 호출, 아이템 슬롯 카운팅 후 큐브 가산, 기존 배치비 환급 유지
  - `BuildDeleteSummary(Tower target)` 신설 — 팝업 표시용 카운트 구조체 반환 (`SkillReturned`, `SupportReturned`, `ItemsSold`, `LowerCubes`)
  - `public readonly struct DeleteRefundSummary` 신설 (InventorySystem 내부)
- `MakeDefence/Assets/Scripts/UI/TowerDeleteConfirmPopup.cs`
  - `Show(Tower tower)` 진입 시 `BuildDeleteSummary` 호출 → `messageText` 동적 구성
  - 메시지 포맷 헬퍼 `BuildMessage(DeleteRefundSummary s)` private 메서드

## 3. 신규 클래스 / 파일

신규 클래스 없음. `DeleteRefundSummary`는 `InventorySystem` 내부 `readonly struct`로 둔다 (스코프 최소화, ItemSystem/ShopSystem 의존 노출 차단).

## 4. 테스트 계획

수동 검증 체크리스트 (Unity Editor Play 모드):

1. **회수 정확성**
   - [ ] 빈 타워 삭제 → Lower +1만 가산, 팝업 메시지 "Lower 1"
   - [ ] 스킬 1개 장착 타워 삭제 → 스킬 인벤 복귀(`ShopSystem.OwnedSkills` 증가), Lower +1
   - [ ] 서포트 3슬롯 장착 타워 삭제 → 서포트 3개 인벤 복귀, Lower +1
   - [ ] 아이템 슬롯 2개 활성(롤됨) 타워 삭제 → Lower +3 (배치 1 + 아이템 2)
   - [ ] 풀세팅(스킬 1 + 서포트 5 + 아이템 3) 타워 삭제 → 스킬·서포트 인벤 복귀, Lower +4
2. **팝업 메시지**
   - [ ] 장착물 0 → "타워를 삭제하시겠습니까?\n하급 큐브 1개를 획득합니다."
   - [ ] 스킬/서포트만 있음 → "... 스킬 1, 서포트 N → 인벤 복귀, 하급 큐브 1개"
   - [ ] 아이템만 있음 → "... 아이템 K → 하급 큐브 (K+1)개"
   - [ ] 모든 카테고리 → "... 스킬 1, 서포트 N → 인벤, 아이템 K → 하급 큐브 (K+1)개"
3. **사이드 이펙트**
   - [ ] 동일 셀 즉시 재배치 가능
   - [ ] `InventorySystem.SelectedTower == null`
   - [ ] `ItemSystem`에 삭제된 타워 키 잔존 없음
   - [ ] `ShopSystem.OwnedDisplayOrder`에 복귀된 스킬/서포트가 끝에 추가됨 (기존 `AddSkillInternal` 동작)
4. **엣지 케이스**
   - [ ] 팝업 오픈 → 다른 타워 선택 → 확정: 캡처된 원본 타워가 삭제됨 (#218 캡처 패턴 유지)
   - [ ] 웨이브 진행 중 삭제 → 적 추적 영향 없음
   - [ ] 빈 슬롯이 섞인 서포트 5슬롯(예: [A, null, B, null, C]) → 3개만 회수

## 5. 위험 요소

- **ShopSystem 인벤 한도 가정** — 현재 `_ownedSkills`/`_ownedSupports`는 `List<>`로 한도 없음. 향후 한도 도입 시 회수 실패 케이스 대응 필요 (현재는 unconditional `Add`라 안전). 슬롯 한도 도입은 별도 이슈에서 처리.
- **아이템 환급 비율의 게임 밸런스** — 슬롯당 Lower 1개는 잠금 비용(10/20/30) 대비 손실이지만, 옵션 강화에 투입한 Upper/TopTier/Delete 큐브는 미환급. 이는 의도된 손실(잠금/강화 코스트는 진행 자원).
- **메시지 길이/줄바꿈** — 동적 메시지가 길어질 수 있음. `TowerDeleteConfirmPopup`의 `Text` 컴포넌트가 줄바꿈/리사이즈 가능한지 씬에서 확인. 필요 시 단축 포맷 채택.
- **`SupportOptions[i]` 인덱스 범위** — `_unlockedSupportSlots` 미만 슬롯만 유효하나 `IReadOnlyList<>`로 전체 5칸 노출됨. `BuildDeleteSummary`/`DeleteTower`에서 `tower.UnlockedSupportSlots`까지만 순회.
- **이벤트 발화 순서** — `ReturnSkill`/`ReturnSupportOption`은 `OnInventoryChanged` 발화. 다수 항목 회수 시 UI 다중 리프레시 발생 — 성능 영향은 미미하나, 추후 `BatchReturn` API로 묶을 여지 있음 (본 이슈에서는 단순 호출 유지).
- **씬 수정 불요** — `TowerDeleteConfirmPopup` 패널/버튼은 #218에서 이미 추가됨. 본 작업은 스크립트 변경만으로 동작.
