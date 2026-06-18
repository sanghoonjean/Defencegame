# Issue #254 — [FEAT] 마우스로 카메라 시점 조작 (우클릭 드래그 팬 / 휠 줌)

> 2D 직교(orthographic) Main Camera 에 단일 `CameraControlSystem` 컴포넌트를 부착해 우클릭 드래그 팬과 마우스 휠 줌을 지원한다. 좌클릭은 기존 `InputManager` 그대로, UI 위에서는 비활성. 맵 바깥 이탈 방지와 줌 범위 클램프 포함.

## 1. 시스템 구조

```
[Main Camera GameObject]  ← 기존
   └─ CameraControlSystem  ← 신규 컴포넌트
        ├─ [SerializeField] float zoomMin = 3f         // orthographicSize 최소
        ├─ [SerializeField] float zoomMax = 12f        // orthographicSize 최대
        ├─ [SerializeField] float zoomStep = 1f        // 휠 1틱당 변화량
        ├─ [SerializeField] bool  zoomToCursor = true  // 휠 줌 시 커서 지점 고정
        ├─ [SerializeField] Vector2 panMinWorld        // 카메라 중심 클램프 (좌하)
        ├─ [SerializeField] Vector2 panMaxWorld        // 카메라 중심 클램프 (우상)
        ├─ [SerializeField] bool  useMapBoundsIfAvailable = true
        │     // true 면 Awake 에서 MapTileSystem 의 두 Tilemap 셀 바운드를
        │     // 합쳐 panMin/panMax 자동 계산. 실패/없으면 SerializeField 값 사용.
        │
        ├─ private Camera _cam
        ├─ private bool   _dragging
        ├─ private Vector3 _dragOriginWorld   // RMB down 시점 마우스 월드좌표
        │
        ├─ Awake()
        │   └─ _cam = GetComponent<Camera>()
        │       (orthographic 이라고 가정. 아니면 경고 후 비활성)
        │
        ├─ Start()
        │   └─ useMapBoundsIfAvailable && MapTileSystem.Instance != null
        │       → 셀 바운드 → 월드 바운드 → panMin/panMax 계산
        │
        └─ Update()
            ├─ HandleZoom()
            │   ├─ if (UIBlocked) return
            │   ├─ scroll = Input.mouseScrollDelta.y
            │   ├─ if (scroll == 0) return
            │   ├─ Vector3 beforeWorld = (zoomToCursor) ? ScreenToWorld(mouse) : center
            │   ├─ _cam.orthographicSize = Clamp(size - scroll * zoomStep, min, max)
            │   ├─ if (zoomToCursor) {
            │   │     Vector3 afterWorld = ScreenToWorld(mouse)
            │   │     transform.position += (beforeWorld - afterWorld)  // 커서 지점 고정
            │   │ }
            │   └─ ClampPosition()
            │
            └─ HandlePan()
                ├─ if (Input.GetMouseButtonDown(1))
                │   ├─ if (UIBlocked) return
                │   ├─ _dragging = true
                │   └─ _dragOriginWorld = ScreenToWorld(mouse)
                ├─ if (Input.GetMouseButtonUp(1)) _dragging = false
                ├─ if (!_dragging) return
                ├─ Vector3 currentWorld = ScreenToWorld(mouse)
                ├─ Vector3 delta = _dragOriginWorld - currentWorld
                │     // origin 은 다시 갱신 X — 그래야 "드래그 시작 시점의 월드점이
                │     // 항상 현재 커서 아래" 라는 직관적인 팬이 됨
                ├─ transform.position += delta
                └─ ClampPosition()

[InputManager]  ← 수정 없음
   — 좌클릭(0번)만 사용. 우클릭(1번) / 휠 충돌 없음.
```

### UI 가드 / ScreenToWorld 헬퍼

```
UIBlocked := EventSystem.current != null
             && EventSystem.current.IsPointerOverGameObject()

ScreenToWorld(screenPos) := _cam.ScreenToWorldPoint(
    new Vector3(screenPos.x, screenPos.y, -transform.position.z)
)
// z 는 카메라 거리 보정. orthographic 이라 사실상 무관하지만 안전하게.
```

### 핵심 결정

| 항목 | 결정 | 근거 |
|------|------|------|
| 부착 위치 | **Main Camera GameObject 에 직접 부착** | 다른 시스템은 싱글톤 패턴이지만 카메라 제어는 Camera transform 에 강결합. 별도 Rig 도입은 향후 Cinemachine 전환 시 고려. |
| 싱글톤 여부 | **싱글톤 X** | 외부에서 호출할 API 없음. 순수 입력 → 자신의 transform 반영. |
| 팬 알고리즘 | **드래그 시작 시점 월드점을 커서 아래에 고정** | `delta = origin - current` 누적 없이 매 프레임 재계산. 직관적이고 오차 누적 없음. 줌이 동시에 일어나도 자연스러움. |
| 줌 대상 | **`Camera.orthographicSize`** | 2D 직교 카메라(Physics2D + ScreenToWorldPoint 사용 확인). FOV 가 아님. |
| 줌 커서 고정 | **기본 ON** (`zoomToCursor = true`) | RTS/타워디펜스 표준 UX. 마우스 가리키는 지점이 화면에 머무름. |
| 경계 클램프 | **카메라 중심 좌표 기준 + 가능하면 맵 바운드 자동** | `MapTileSystem` 의 두 Tilemap 셀 바운드를 합쳐 월드 바운드로 변환. 실패 시 SerializeField 값. 카메라 뷰 크기(orthoSize) 보정은 본 이슈에서 생략 — 중심이 맵 안이면 충분. |
| UI 위 동작 | **RMB down/휠 시 `IsPointerOverGameObject` 가드** | `InputManager` 와 동일 규칙. 드래그 중 UI 진입은 허용 (기존 origin 으로 계속 팬). |
| `Time.timeScale` 영향 | **무관** | `GetMouseButton(Down/Up)` / `mouseScrollDelta` / `transform.position` 모두 timeScale 비의존. 1x/2x/3x 어디서나 동일 조작감. |
| 가운데 클릭 / 중간 버튼 | **본 이슈 외** | 우클릭 + 휠로 충분. Unity 표준 RTS 도 우클릭 팬이 일반적. |
| 키보드 이동 | **본 이슈 외** | 이슈 비범위 명시. |
| 기존 우클릭 사용처 | **없음** | `Input.GetMouseButton(1)` grep 결과 0건. `InputManager` 는 0번만 사용. 충돌 없음. |

## 2. 수정 파일

- `MakeDefence/Assets/Scenes/SampleScene.unity` (UnityMCP 경유)
  - Main Camera GameObject 에 `CameraControlSystem` 컴포넌트 추가
  - 필요 시 `panMinWorld` / `panMaxWorld` SerializeField 값 세팅 (또는 `useMapBoundsIfAvailable` 에 위임)

## 3. 신규 클래스 / 파일

- `MakeDefence/Assets/Scripts/Systems/CameraControlSystem.cs`
  - MonoBehaviour, Camera 동반 컴포넌트
  - 우클릭 드래그 팬 + 마우스 휠 줌 + 클램프 + UI 가드
  - 외부 노출 API 없음 (Inspector 노출 필드만)

## 4. 테스트 계획

수동 검증 (Unity Editor Play 모드):

1. **팬 (우클릭 드래그)**
   - [ ] Play 시작 → 우클릭 누른 채 마우스 이동 → 카메라가 마우스 반대 방향으로 따라옴 (드래그 시작점이 커서 아래 유지)
   - [ ] 우클릭 떼면 즉시 멈춤
   - [ ] 좌클릭 드래그는 팬 안 함 (타워 배치/선택 그대로)
2. **줌 (마우스 휠)**
   - [ ] 휠 위 → 확대 (orthographicSize 감소), 휠 아래 → 축소
   - [ ] `zoomToCursor = true` 상태에서 커서 위치의 월드점이 화면에서 거의 안 움직임
   - [ ] `zoomMin` / `zoomMax` 도달 시 더 이상 변하지 않음
3. **UI 가드**
   - [ ] 마우스가 인벤/상점/HUD 버튼 위일 때 우클릭 드래그 시작 안 됨
   - [ ] 마우스가 UI 위일 때 휠 돌려도 줌 안 됨
   - [ ] 드래그 중 마우스가 UI 위로 지나가도 드래그 끊기지 않음 (origin 유지)
4. **경계 클램프**
   - [ ] 맵 좌하단 너머로 끝까지 드래그 → 한계 지점에서 멈춤
   - [ ] 우상단 동일
   - [ ] `useMapBoundsIfAvailable = true` 일 때 SerializeField 값 안 채워도 맵 안에서만 움직임
5. **배속과 호환**
   - [ ] 2x/3x 상태에서도 팬/줌 조작감 동일 (가속 X, 끊김 X)
   - [ ] 게임오버 화면에서도 카메라 조작 가능
6. **회귀 X**
   - [ ] InputManager 좌클릭 (#224) 정상 — 타워 배치/선택
   - [ ] `F` 배속 토글 (#249) 정상
   - [ ] TestRunner 단축키 (Space/A/C/R/D) 정상

## 5. 위험 요소

- **씬 변경 포함** — `SampleScene.unity` Main Camera 에 컴포넌트 추가. AGENTS.md §7 정책에 따라 UnityMCP `manage_components` 로 작업.
- **맵 바운드 추정의 한계** — `MapTileSystem` 이 두 Tilemap 의 `cellBounds` 를 직접 노출하지 않음. 추가 API (`GetMapWorldBounds()`) 가 필요할 수 있음. → 1차 구현은 SerializeField 폴백을 항상 동작하도록, 자동 바운드는 `MapTileSystem` 에 작은 헬퍼 추가하는 식으로 분리 가능.
- **카메라 클램프 vs 뷰포트 크기** — 클램프 기준이 "카메라 중심" 이라 줌 아웃 상태에서는 화면 가장자리가 맵 밖을 비출 수 있음. 본 이슈에선 허용 (UX 자연스러움). 엄격한 "뷰포트 내부가 맵 안" 요구는 별도 이슈.
- **드래그 중 줌 동작** — 줌이 카메라 위치를 보정(`zoomToCursor`)하면서 드래그 origin 의 월드좌표가 어긋날 수 있음. → origin 자체가 "월드좌표" 이고 매 프레임 `ScreenToWorld(현재 커서)` 와 차분만 계산하므로 줌 후에도 일관됨 (시뮬레이션에서 검증 필요).
- **UI 가드 타이밍** — `RMB down` 프레임에만 UI 체크. 드래그 중 UI 위 진입 허용은 의도된 동작 (드래그가 끊기면 더 불편).
- **Cinemachine 미사용** — 현재 프로젝트에 Cinemachine 미부착 (`Camera` + `AudioListener` 만). 단순 `transform` 조작으로 충분. 추후 Cinemachine 도입 시 `CinemachineConfiner2D` 등으로 대체.
- **좌클릭 컨텍스트 메뉴 우려** — 우클릭은 Unity 게임 런타임에서 OS 컨텍스트 메뉴를 띄우지 않음 (브라우저 빌드만 해당). WebGL 빌드 시 `cursor:none` 등 별도 처리 필요 — 본 이슈 외.
