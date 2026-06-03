# Issue #218 — [FEAT] 타워 삭제 기능

## 1. 시스템 구조

배치된 타워를 선택 → 삭제 확인 팝업 → 확정 시 타워 제거 + 자원 환급.

```
[Tower 클릭]
   ↓  TestRunner.HandleClick() — Physics2D.OverlapPoint
[InventorySystem.SelectTower(tower)]
   ↓  OnTowerSelected 이벤트
[UnitPanelController] → 패널 표시 (기존)
   └─ [신규] DeleteTowerButton 활성화
        ↓ onClick
[TowerDeleteConfirmPopup.Show(tower)]
   ↓ Confirm
[InventorySystem.DeleteSelectedTower()]
   ├─ CubeSystem.Add(CubeType.Lower, 1)         // 환급
   ├─ InventorySystem.Deselect()                // 선택 해제 → 패널 자동 숨김
   └─ Destroy(tower.gameObject)
         ↓ Tower.OnDestroy() (기존, 변경 없음)
         ├─ ItemSystem.UnregisterTower(this)
         └─ MapTileSystem.RemoveTower(TileCoord)
```

### 핵심 결정 (이슈의 미정 항목 처리)

| 항목 | 결정 | 근거 |
|------|------|------|
| 환급 정책 | **하급 큐브 1개 (배치 비용 전액 환급)** | 배치 비용이 `Lower 1개` 고정([TowerPlacer.cs:17](MakeDefence/Assets/Scripts/Gameplay/Tower/TowerPlacer.cs#L17))이고, 기존 판매(`SellConfirmPopup`)도 동일하게 Lower 1개 환급([SellConfirmPopup.cs:115](MakeDefence/Assets/Scripts/UI/SellConfirmPopup.cs#L115))이라 일관성 유지 |
| 삭제 가능 시점 | **언제든 가능 (웨이브 중 포함)** | 기존 스킬/보조옵션 판매에도 시점 제한 없음 — 동일 UX |
| 라운드 중 패널티 | **없음** | 단순화. 추후 별도 이슈로 다룰 수 있음 |
| 장착된 스킬/보조옵션/아이템 | **함께 소멸 (별도 환급 없음)** | 사용자가 판매를 원하면 삭제 전 개별 판매 UI 사용. 타워 단위 삭제는 일괄 제거만 |

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Systems/InventorySystem.cs`
  - `DeleteSelectedTower()` 메서드 신설 — 환급 + `Deselect()` + `Destroy(gameObject)`
- `MakeDefence/Assets/Scenes/SampleScene.unity` *(Unity Editor 수정)*
  - `UnitPanel` 하위에 **Delete Tower** 버튼 추가
  - `TowerDeleteConfirmPopup` 패널(Canvas 하위) 추가 — 기존 `SellConfirmPopup` 프리팹 구조 참고

## 3. 신규 클래스 / 파일

- `MakeDefence/Assets/Scripts/UI/TowerDeleteConfirmPopup.cs`
  - 역할: 삭제 확인 모달. `Show(Tower)` → confirm 시 `InventorySystem.Instance.DeleteSelectedTower()` 호출
  - 패턴: 기존 `SellConfirmPopup` 그대로 본떠 작성 (Awake에서 Instance, panel SetActive(false), 버튼 리스너 등록)
- `MakeDefence/Assets/Scripts/UI/DeleteTowerButton.cs`
  - 역할: UnitPanel 내 삭제 버튼의 onClick 핸들러. `InventorySystem.SelectedTower`가 있으면 팝업 호출
  - (단순하면 별도 스크립트 없이 `TowerDeleteConfirmPopup.Show(InventorySystem.Instance.SelectedTower)`를 Inspector OnClick으로 연결해도 됨 → 코드 추가 0으로 가능)

## 4. 테스트 계획

수동 검증 체크리스트:

1. **기본 흐름**
   - [ ] 타워 배치 → 클릭하여 선택 → UnitPanel에 삭제 버튼 표시
   - [ ] 삭제 버튼 클릭 → 확인 팝업 출력
   - [ ] 취소 → 팝업 닫힘, 타워 유지
   - [ ] 확정 → 타워 GameObject 소멸, 하급 큐브 +1, UnitPanel 자동 숨김
2. **그리드 점유 해제**
   - [ ] 삭제한 셀에 즉시 새 타워 배치 가능 (`MapTileSystem.CanPlaceTower` true)
3. **상태 정리**
   - [ ] 삭제 후 `InventorySystem.SelectedTower == null`
   - [ ] 다른 타워가 영향받지 않음 (인접 타워 정상 동작)
4. **엣지 케이스**
   - [ ] 스킬·보조옵션·아이템 장착된 타워 삭제 → 에러 없음, 큐브는 1개만 환급(전용 옵션은 별도 환급 X)
   - [ ] 웨이브 진행 중 삭제 → 적 추적/공격 정상 종료, 다른 타워 영향 없음
   - [ ] 동일 셀 연속 배치/삭제 반복 → 메모리 누수·이벤트 중복 없음

검증 도구: Unity Editor Play 모드 + Console 로그 확인.

## 5. 위험 요소

- **선택된 타워가 외부에서 Destroy되는 경우** — 기존 코드는 `SelectedTower`를 null로 안 만들 수 있음. `DeleteSelectedTower()`에서 Destroy 직전에 명시적 `Deselect()` 호출로 방지. (관련: `Tower.OnDestroy` 자체는 InventorySystem을 건드리지 않음)
- **TestRunner.HandleClick에 의존하는 선택 입력** — 현재 타워 선택이 디버그 성격 파일(`TestRunner.cs`)에 들어있음. 본 이슈에서는 변경 범위 밖이지만, 추후 정식 InputManager로 옮길 때 함께 정리 필요. (별도 이슈 권장)
- **삭제 후 즉시 같은 프레임에 다른 시스템이 타워 참조** — `Enemy`가 타겟팅하는 타워는 없으나, 향후 그런 시스템 추가 시 null 검사 필요.
- **UI 작업 누락** — `.cs` 변경만으로는 동작하지 않음. SampleScene에 버튼/팝업 패널 추가 필수. 미적용 시 기능 노출 안 됨 (테스트 시 즉시 발견 가능).
- **확인 팝업 미사용 시 오삭제 위험** — 반드시 confirm 단계 필수. 단축키 삭제는 v1에서 제공하지 않음.
