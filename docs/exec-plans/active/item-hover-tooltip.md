# 아이템 호버 툴팁 — 인벤 슬롯에 커서 올리면 아이템 상세 표시

> GitHub 이슈 미발급 상태에서 채팅 요청으로 진행. 이슈 생성 시 파일명/커밋에 번호 연결 예정.

## 1. 시스템 구조

```
[커서 호버] → ItemTooltipTrigger (슬롯별 부착, IPointerEnter/Exit)
                 │  같은 GameObject 의 InvenSlotDragHandler 에서
                 │  Skill / Support / Stone 데이터를 읽어 텍스트 구성
                 ▼
            ItemTooltipUI (정적 클래스, 런타임 생성 패널)
                 │  루트 캔버스 최상단에 Image + Text 패널 1개 캐싱
                 ▼
            [툴팁 표시 / 숨김]
```

- 씬 수정 없음 — StoneGradeBadge(#394)와 동일하게 런타임 생성 방식
- 툴팁 패널은 캔버스당 1개 싱글턴, raycastTarget 비활성으로 호버 플리커 방지
- 드래그 시작 시(`IBeginDragHandler`) 즉시 숨김, 인벤 Refresh 시에도 숨김
- 인게임 텍스트는 영어 (한글 TMP 폰트 없음)

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/UI/InvenUI.cs` — `TryRegisterSlot` 에서 `ItemTooltipTrigger` 자동 부착, `Refresh` 시 툴팁 숨김

## 3. 신규 클래스 / 파일

- `MakeDefence/Assets/Scripts/UI/ItemTooltipUI.cs` — 툴팁 패널 생성/캐싱, `Show(anchor, text)` / `Hide()`. 텍스트 크기에 맞춰 패널 리사이즈, 캔버스 밖으로 나가지 않게 클램프
- `MakeDefence/Assets/Scripts/UI/ItemTooltipTrigger.cs` — 호버 감지 + 아이템 종류별 툴팁 텍스트 빌더
  - 스킬: 이름 + Damage / Cooldown / Range / Mana
  - 서포트: 이름 + description + value(%)
  - 차원석: 이름 + 등급 + 옵션 목록 (Count 는 +N, 나머지는 +N%)

## 4. 테스트 계획

- UnityMCP `refresh_unity` + `read_console` 로 컴파일 검증
- 플레이 모드에서 인벤 슬롯 호버 → 툴팁 표시/숨김/드래그 중 숨김 확인
- 스킬 / 서포트 / 차원석 3종 각각 내용 확인

## 5. 위험 요소

- 슬롯이 화면 가장자리일 때 툴팁이 잘리지 않도록 클램프 필요
- 클릭 장착으로 아이템이 사라진 뒤 툴팁이 남는 문제 → Refresh 시 Hide 로 방어
- 장착 슬롯(SupportSlotUI 등)·상점 슬롯은 이번 범위 제외 (트리거 재사용 가능 구조로 설계)
