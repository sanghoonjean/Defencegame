# Issue #224 — [REFACTOR] TestRunner.HandleClick → 정식 InputManager 분리

> 타워 선택 좌클릭이 디버그용 `TestRunner.cs`에 묻혀 있어 향후 빌드에서 누락될 위험을 제거한다. 좌클릭 진입점을 단일 `InputManager` 컴포넌트로 통합하고, `TowerPlacer.Update` 도 InputManager 가 호출하는 public API 로 리팩토링한다.

## 1. 시스템 구조

좌클릭 처리를 한 곳으로 모은다 — UI 가드 → Tower hit → 빈 칸 배치 위임 순으로 분기.

```
[InputManager.Update]  ← 본 작업에서 신설
   ↓ Input.GetMouseButtonDown(0)
   ↓ EventSystem.IsPointerOverGameObject() → return  (UI 가드)
   ↓ worldPos = Camera.main.ScreenToWorldPoint(mouse)
   ↓ Physics2D.OverlapPoint(worldPos)
   │
   ├─ hit && hit.GetComponent<Tower>() != null
   │   └─ InventorySystem.SelectTower(tower)
   │
   └─ hit == null OR Tower 없음
       └─ coord = floor(worldPos)
       └─ TowerPlacer.Instance.TryPlace(coord)
            ├─ 성공 → 새 타워 배치 + InventorySystem.Deselect (기존 선택 해제)
            └─ 실패 → InventorySystem.Deselect()  (빈 칸 미배치 시 선택 해제)

[TowerPlacer]  ← 리팩토링
   ├─ Update() 제거
   ├─ public bool TryPlace(Vector2Int coord) 신설
   │   ├─ MapTileSystem.CanPlaceTower(coord) 통과
   │   ├─ CubeSystem.TryConsume(CubeType.Lower, 1)
   │   ├─ Instantiate + Place + MapTileSystem.PlaceTower
   │   └─ return true (성공) / false (실패)
   └─ public static TowerPlacer Instance (Awake 에서 세팅)
       — InputManager 가 의존하기 위한 singleton 패턴
       (다른 시스템들과 동일 컨벤션)

[TestRunner.HandleClick]  ← 제거
   ├─ 기존 OverlapPoint + SelectTower / Deselect 코드 삭제
   └─ Update() 의 HandleClick() 호출 제거
       (Space/A/C/R 키 디버그 코드는 그대로 유지)
```

### 핵심 정책 결정

| 항목 | 결정 | 근거 |
|------|------|------|
| 좌클릭 진입점 통합 | **단일 `InputManager`** | 이슈 핵심 요구. 두 진입점(TestRunner + TowerPlacer) 충돌 위험 제거 |
| 키보드 입력 통합 | **본 이슈에서 제외** | TestRunner 키(Space/A/C/R) 는 디버그/치트 — 빌드 전 삭제 의도와 부합. 팝업 키(D/Enter/Esc)는 팝업 자체 책임. 이슈 본문도 "(선택)" 표기 |
| `TowerPlacer` 구조 | **싱글톤 + `TryPlace(coord)` public API** | 기존 시스템들(`InventorySystem`/`ShopSystem`/`ItemSystem`)과 동일 `Instance` 패턴. InputManager → TowerPlacer 의존 명시화 |
| 빈 칸 클릭 시 동작 | **`TryPlace` 결과에 따라 동작**: 성공 → 새 타워 자동 선택 X (현 정책 유지), 실패 → `Deselect` | 현 `TestRunner` 동작(빈 칸 클릭 = Deselect) 유지. 배치 성공 후 자동 선택은 정책 변경이라 본 이슈 외 |
| `TestRunner.HandleClick` 제거 | **완전 삭제** (재호출 X) | 책임 일원화. OnGUI 라벨/키 디버그는 그대로 유지 |
| `TowerPlacer.Update` 제거 | **완전 삭제** | 좌클릭은 `InputManager` 가 유일 진입점 |
| 카메라 참조 | **`Camera.main` 유지** | 기존 패턴(`TestRunner`/`TowerPlacer` 모두 동일). 다중 카메라 도입 시 별도 이슈 |
| EventSystem 가드 위치 | **InputManager 안에 1번만** | `TestRunner`/`TowerPlacer` 모두에서 중복 검사하던 것 정리 |

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/TestRunner.cs`
  - `HandleClick()` 메서드 및 `Update()` 의 호출 제거
  - `using UnityEngine.EventSystems;` 가 다른 코드에서 안 쓰이면 제거 (확인 후)
- `MakeDefence/Assets/Scripts/Gameplay/Tower/TowerPlacer.cs`
  - `Update()` 제거
  - `Awake()` 신설 — `Instance = this`
  - `public static TowerPlacer Instance` 노출
  - `public bool TryPlace(Vector2Int coord)` 신설 — 기존 분기 로직 이동

## 3. 신규 클래스 / 파일

- `MakeDefence/Assets/Scripts/Systems/InputManager.cs` 신규
  - `MonoBehaviour`, `Update()` 에서 좌클릭 처리
  - 싱글톤 패턴(`Instance`) — 추후 키 입력 통합 시 다른 시스템에서 참조 가능
  - 씬에 GameObject 1개 추가 필요 (예: `InputManager` 빈 오브젝트)

## 4. 테스트 계획

수동 검증 (Unity Editor Play 모드):

1. **좌클릭 분기 정확성**
   - [ ] 빈 셀(맵 안) 클릭 → 타워 배치 + Lower -1
   - [ ] 타워 있는 셀 클릭 → 해당 타워 선택 (`InventorySystem.SelectedTower` 변경, UI 갱신)
   - [ ] 다른 타워가 선택된 상태에서 빈 셀 클릭 (배치 가능 좌표) → 배치 성공, 새 타워 선택 안 됨 (기존 선택만 해제)
   - [ ] 맵 밖 클릭 (`CanPlaceTower` false) → `TryPlace` 실패 → Deselect
2. **UI 가드**
   - [ ] 인벤/상점/팝업 위 클릭 → 어떤 동작도 발생 X (선택/배치/해제 모두)
3. **재화 부족 시**
   - [ ] Lower 큐브 0개 상태에서 빈 칸 클릭 → 배치 실패 → Deselect (Lower 소비 없음)
4. **TestRunner 영향 없음**
   - [ ] Space → 웨이브 시작 정상
   - [ ] A → 자동 웨이브 정상
   - [ ] C → 큐브 +10 정상
   - [ ] R → 리셋 정상
   - [ ] OnGUI 디버그 라벨 정상 (선택 타워 좌표 표시)
5. **#218 / #221 / #222 회귀 X**
   - [ ] D → 삭제 팝업
   - [ ] 매도/언락 모달 위 클릭 → InputManager 무시
   - [ ] 타워 삭제 시 환급 메시지 정상
6. **씬 변경**
   - [ ] `SampleScene.unity` 에 `InputManager` GameObject 추가 필요 — 본 PR 에 씬 변경 포함

## 5. 위험 요소

- **씬 변경 포함** — `SampleScene.unity` 에 `InputManager` GameObject 1개 추가 필요. 스크립트 only 변경이 아님. 머지 충돌 가능성(다른 PR 도 씬 수정 시) 고려.
- **TowerPlacer 싱글톤 도입** — 씬에 2개 이상이면 마지막 `Awake` 가 `Instance` 차지. 기존 단일 인스턴스 가정과 동일 (다른 시스템들도 같은 패턴).
- **실행 순서** — `InputManager.Update` 와 다른 `Update`(`Tower`, `Enemy`, `WaveSystem`) 간 순서는 무관(같은 프레임 클릭 내에서 select/place 만 발생). 단, `TowerDeleteConfirmPopup.Update` 와 마우스 클릭 분기는 독립적이라 충돌 없음.
- **클릭과 키 동시 입력** — D 키 + 좌클릭이 같은 프레임에 들어와도 두 핸들러가 각자 분리 처리. 회귀 없음.
- **자동 테스트 한계** — 게임 내 클릭은 InputManager 폴링 기반이라 외부 시뮬레이션 어려움. 컴파일 + Play 모드 진입 + 콘솔 에러 0 까지 자동, 실제 클릭 흐름은 사용자 수동.
- **TowerPlacer 의 prefab serializeField 보존** — 인스펙터에 설정된 `towerPrefab` 값 유지. `Update` 만 제거하면 됨.
- **`using` 정리** — `TestRunner.cs` 의 `using UnityEngine.EventSystems;` 가 OnGUI/키 코드에서 안 쓰이면 제거. 다른 파일에서는 그대로 필요.
