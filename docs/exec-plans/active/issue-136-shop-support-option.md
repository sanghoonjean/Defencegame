# Issue #136 — 상점에서 보조 옵션 구매 기능 추가

## 1. 시스템 구조

`ShopSkillSlotUI`가 스킬 구매 슬롯 역할을 하듯,
`ShopSupportSlotUI`가 보조 옵션 구매 슬롯 역할을 담당한다.

- `ShopSystem.BuySupportOption()` — 이미 구현됨 (Lower 큐브 1개 소모)
- `ShopSystem.availableSupports` — Inspector에서 판매 목록 설정

## 2. 수정 파일

없음.

## 3. 신규 클래스 / 파일

- `MakeDefence/Assets/Scripts/UI/ShopSupportSlotUI.cs` (신규)
  - `[SerializeField] SupportOptionData optionData` — Inspector에서 에셋 지정
  - 아이콘/이름 표시
  - `CubeType.Lower` 잔량 기준 구매 버튼 활성/비활성
  - 구매 시 `ShopSystem.BuySupportOption(optionData)` 호출
  - `ShopSkillSlotUI`와 동일한 구조

## 4. 테스트 계획

- [ ] Lower 큐브 보유 시 보조 옵션 구매 버튼 활성화 확인
- [ ] Lower 큐브 0개 시 버튼 비활성화 확인
- [ ] 구매 후 `ShopSystem.OwnedSupports`에 추가 확인
- [ ] 보유 보조 옵션 목록 UI(`OwnedSupportListUI`)에 즉시 반영 확인
- [ ] 이미 보유 중인 옵션 재구매 불가 확인 (`BuySupportOption` 중복 체크)

## 5. 위험 요소

- Unity Inspector에서 `ShopSupportSlotUI`에 `SupportOptionData` 에셋 연결 필요
- `ShopSystem.availableSupports` 목록에 해당 에셋이 포함되어 있어야 구매 가능
