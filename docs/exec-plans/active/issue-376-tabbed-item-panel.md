# Issue #376 — 인벤토리 UI와 상점 UI를 탭 방식 단일 패널로 통합

## 1. 시스템 구조

### 현재 상태

인벤토리와 상점이 완전히 분리된 두 패널로 존재하고, HUD 버튼도 각각 따로 토글한다.

```
Canvas
├─ Invertorybtn (UIToggleButton → InventoryUI)          # HUD 열기 버튼
├─ SHOPbtn      (UIToggleButton → SHOP_UI)              # HUD 열기 버튼
├─ InventoryUI  (Image, InvenDropHandler, DraggablePanel)   # 기본 비활성
│  ├─ SkillScroll View        # 보유 스킬/서포트 통합 그리드 (InvenUI)
│  ├─ CancelButton            # UICloseButton → InventoryUI
│  └─ Unit_Panel              # 선택 타워 장착 UI (UnitPanelController, SkillSlotUI, CanvasGroup)
└─ SHOP_UI      (Image, ShopDropHandler, DraggablePanel)    # 기본 비활성
   ├─ TXTPanel                # 상점 헤더 텍스트
   ├─ Main_Scroll View        # 메인 스킬 상점 목록
   ├─ Sup_Scroll View         # 서포트 상점 목록
   ├─ Main_skill_ShopButton   # 메인 스킬 구매 버튼
   ├─ Sub_skill_ShopButton    # 서포트 구매 버튼
   └─ CancelButton            # UICloseButton → SHOP_UI
```

- 두 DropHandler(InvenDropHandler/ShopDropHandler)는 패널 루트 Image(raycast target)에 붙어
  "패널 배경에 드랍 = 회수/판매"를 처리한다.
- 스크립트 코드에는 두 패널 GameObject 를 직접 참조하는 곳이 없다 (씬 직렬화 참조만 존재:
  UIToggleButton.targetPanel, UICloseButton.targetPanel).
- 내부 목록 UI(InvenUI, OwnedSkillsListUI 등)는 OnEnable 에서 Refresh 하는 패턴이라
  SetActive 기반 페이지 전환과 호환된다.

### 변경 후 구조

두 패널의 내용물을 페이지로 품는 단일 패널 `ItemHubPanel` 을 만들고, 상단 탭으로 전환한다.

```
Canvas
├─ Invertorybtn (UITabOpenButton → ItemHubPanel, tab 0)
├─ SHOPbtn      (UITabOpenButton → ItemHubPanel, tab 1)
└─ ItemHubPanel (Image 배경, DraggablePanel, UITabView)     # 기본 비활성
   ├─ TabHeader
   │  ├─ InventoryTabButton   # Button + TMP "Inventory" (영어 — 한글 TMP 폰트 없음)
   │  ├─ ShopTabButton        # Button + TMP "Shop"
   │  └─ CancelButton         # UICloseButton → ItemHubPanel (기존 것 재사용)
   ├─ InventoryPage (투명 Image raycast on, InvenDropHandler)   # 기존 InventoryUI 자식 이동
   │  ├─ SkillScroll View
   │  └─ Unit_Panel
   └─ ShopPage      (투명 Image raycast on, ShopDropHandler)    # 기존 SHOP_UI 자식 이동
      ├─ Main_Scroll View
      ├─ Sup_Scroll View
      ├─ Main_skill_ShopButton
      └─ Sub_skill_ShopButton
```

- **탭 전환은 페이지 GameObject SetActive** 로 처리 — 기존 목록 UI 들의 OnEnable Refresh
  패턴이 그대로 동작한다.
- **DropHandler 는 각 페이지 루트로 이동**하고 페이지 루트에 풀사이즈 투명 Image(raycast
  target on)를 둔다 → "인벤 배경 드랍 = 회수", "상점 배경 드랍 = 판매" 의미가 페이지
  단위로 유지된다.
- **DraggablePanel 은 ItemHubPanel 루트 하나만** 유지. 페이지 투명 Image 는 드래그 핸들러가
  없으므로 배경 드래그 이벤트가 루트로 버블업되어 패널 이동이 기존처럼 동작한다.
- SHOP_UI 의 TXTPanel(헤더)은 통합 TabHeader 로 대체되어 제거한다.
- 기존 InventoryUI / SHOP_UI 루트 GameObject 는 자식·컴포넌트 이동 후 삭제한다.

### 데이터 흐름

```
HUD 버튼 클릭 (Invertorybtn / SHOPbtn)
 ↓
UITabOpenButton.OnClick
 ├─ 패널 닫힘        → ItemHubPanel.SetActive(true) + UITabView.SelectTab(tabIndex)
 ├─ 열림 & 같은 탭   → ItemHubPanel.SetActive(false)   # 기존 토글 동작 유지
 └─ 열림 & 다른 탭   → UITabView.SelectTab(tabIndex)
 ↓
UITabView.SelectTab(i)
 ├─ pages[i] SetActive(true), 나머지 false
 └─ 탭 버튼 하이라이트 갱신 (활성 탭 색상 강조)
```

## 2. 수정 파일

코드 수정 없음 — 기존 스크립트는 전부 재사용:

- `UICloseButton.cs` — CancelButton 이 ItemHubPanel 을 닫도록 씬 참조만 변경
- `InvenDropHandler.cs` / `ShopDropHandler.cs` — 컴포넌트가 페이지 루트로 이동 (코드 동일)
- `DraggablePanel.cs` — ItemHubPanel 루트에 부착 (코드 동일)
- `UIToggleButton.cs` — Invertorybtn/SHOPbtn 에서 제거되지만 파일은 유지
  (DimesionStoneInventoryUI 등 다른 곳에서 계속 사용 여부와 무관하게 범용 스크립트)

### 씬: `MakeDefence/Assets/Scenes/SampleScene.unity`

- `ItemHubPanel` + TabHeader + 2개 페이지 생성, 기존 두 패널의 자식 reparent
- Invertorybtn/SHOPbtn: UIToggleButton 제거 → UITabOpenButton 부착 및 wiring
- 기존 InventoryUI / SHOP_UI 루트 삭제
- 씬 편집은 전부 UnityMCP 로 진행 (.unity YAML 직접 편집 금지)

## 3. 신규 클래스 / 파일

### `MakeDefence/Assets/Scripts/UI/UITabView.cs`

탭시트 컨트롤러. 범용으로 설계해 이후 다른 탭 UI 에도 재사용 가능하게 한다.

- `[SerializeField] Button[] tabButtons;` / `[SerializeField] GameObject[] pages;` (인덱스 짝)
- `[SerializeField] int defaultTabIndex;` — OnEnable 시 마지막 선택 탭 유지, 최초엔 default
- `[SerializeField] Color activeTabColor / inactiveTabColor;` — 탭 버튼 Image 색상으로 활성 표시
- `public void SelectTab(int index)` — 해당 페이지만 SetActive(true), 탭 색상 갱신
- Awake 에서 각 탭 버튼에 onClick 리스너 등록 (클로저 인덱스 캡처 주의)
- 배열 길이 불일치/null 은 경고 로그 후 no-op (기존 UIToggleButton 방어 패턴과 동일)

### `MakeDefence/Assets/Scripts/UI/UITabOpenButton.cs`

HUD 열기 버튼용. UIToggleButton 의 탭 지정 버전.

- `[SerializeField] GameObject targetPanel;` / `[SerializeField] UITabView tabView;`
- `[SerializeField] int tabIndex;`
- 클릭 시: 닫힘 → 열고 SelectTab / 열림+같은 탭 → 닫기 / 열림+다른 탭 → SelectTab
- "같은 탭" 판정을 위해 UITabView 에 `public int CurrentTabIndex { get; }` 노출

## 4. 테스트 계획

수동 (Unity Editor, Play Mode):

- [ ] Invertorybtn 클릭 → 패널이 Inventory 탭으로 열림. 재클릭 → 닫힘 (토글 유지)
- [ ] SHOPbtn 클릭 → 패널이 Shop 탭으로 열림. Inventory 탭이 열린 상태에서 SHOPbtn → 탭만 전환
- [ ] 패널 내 탭 버튼 클릭으로 Inventory ↔ Shop 전환, 활성 탭 하이라이트 표시
- [ ] CancelButton 으로 패널 닫힘
- [ ] 패널 배경/헤더 드래그로 패널 이동 (DraggablePanel, 캔버스 밖 클램프 포함)
- [ ] Shop 페이지: 메인 스킬/서포트 구매 → 목록 갱신
- [ ] Inventory 페이지: 보유 스킬 드래그 → 타워 장착, 장착 슬롯 드래그 → 인벤 배경 드랍 = 회수
- [ ] Shop 페이지 배경에 인벤/장착 아이템 드랍 → 판매 확인 팝업 (SellConfirmPopup)
- [ ] 탭 전환 후에도 위 드랍 동작이 각 페이지 의미대로 동작 (인벤=회수, 상점=판매)
- [ ] Unit_Panel: 타워 선택/해제 시 표시 전환 (UnitPanelController CanvasGroup) 기존과 동일
- [ ] 게임 재시작(Play 재진입) 시 패널 기본 닫힘 + default 탭 정상

## 5. 위험 요소

- **드랍 영역 의미 변화**: 기존에는 패널 루트 전체가 드랍 영역이었으나 변경 후 페이지
  영역(헤더 제외)으로 줄어든다. 헤더에 드랍하면 아무 일도 일어나지 않음 — 의도된 동작으로
  간주하되 테스트에서 확인.
- **reparent 시 직렬화 참조**: 같은 씬 내 이동이므로 SkillSlotUI/UnitPanelController 등
  내부 참조는 유지되지만, UIToggleButton/UICloseButton 의 targetPanel 재연결 누락 시
  런타임 경고만 뜨고 조용히 실패한다 — wiring 후 전수 확인 필요.
- **RectTransform 레이아웃**: 두 패널의 크기/배치가 서로 달랐다면 통합 패널 크기에 맞춰
  페이지 내부 레이아웃 조정이 필요할 수 있다. UnityMCP create 의 RectTransform 미적용
  이슈가 있어 생성 후 별도 설정 필수 ([[reference_unitymcp_ui_scene_edits]]).
- **탭 라벨 폰트**: 한글 TMP 폰트가 없으므로 탭 텍스트는 영어("Inventory"/"Shop")로 표기.
- **씬 저장 타이밍**: 스크립트 컴파일(도메인 리로드) 전에 씬 저장 필수 — 미저장 GameObject
  유실 방지. 신규 스크립트 2개를 먼저 커밋/컴파일한 뒤 씬 작업을 시작한다.
- SupportInvenUI 는 DEPRECATED(#220) — 이번 작업에서 건드리지 않는다.
