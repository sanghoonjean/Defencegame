# Issue #172 — 서포트 옵션 중복 구매 허용

## 1. 시스템 구조

`ShopSystem.BuySupportOption`에서 `_ownedSupports.Contains(option)` 한 줄이 중복 구매를 막고 있음.
이 줄을 제거하면 동일 `SupportOptionData`를 여러 번 구매 가능.

## 2. 수정 파일

- `MakeDefence/Assets/Scripts/Systems/ShopSystem.cs`

## 3. 신규 클래스 / 파일

없음

## 4. 구현 상세

```csharp
// 제거한 줄
if (_ownedSupports.Contains(option)) return false;
```

## 5. 테스트 계획

- [ ] 동일 서포트 옵션 2회 이상 구매 가능 확인
- [ ] 구매한 중복 옵션이 인벤토리에 각각 슬롯으로 표시 확인
- [ ] 각 슬롯을 개별적으로 장착/판매 가능 확인

## 6. 위험 요소

없음 — `List<T>.Remove`는 첫 번째 일치 항목만 제거하므로 중복 보유 시에도 인스턴스 단위로 올바르게 동작함
