# Issue #72 — 보조 옵션 구현

## 1. 시스템 구조

```
[구매]
SHOP_UI → 보조옵션 탭
  └── ShopSupportSlotUI (각 슬롯) → ShopSystem.BuySupportOption()
                                   → ShopSystem.OwnedSupports 추가

[인벤토리]
InvenUI / 보조옵션 탭
  └── InvenSupportSlotDragHandler (드래그 소스)
        → SupportSlotUI.OnDrop (드랍 타겟)
            → InventorySystem.SetSupportOption(slot, option)
            → Tower.SetSupportOption() → RefreshStats()

[타워 패널]
Unit_Panel → Support_Panel
  └── SupportSlotUI × 3 (슬롯 0~2)
        ├── IDropHandler — 인벤토리에서 드래그 드랍 장착
        └── 표시: 잠금/빈 슬롯/장착 아이콘

[효과 적용]
Tower.RefreshStats()
  └── 보조 옵션 루프 → AccumulateSupportOption()
        ├── 단순 스탯: dmgPct, spdPct, rangePct, CritChance 등
        └── 특수 플래그: HasPiercing, HasChain, HasMultiProjectile 등
```

## 2. 수정 파일

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/Gameplay/Tower/Tower.cs` | `RefreshStats()`에 보조 옵션 효과 합산 추가, 특수 효과 플래그/프로퍼티 추가 |
| `Assets/Scripts/Systems/ShopSystem.cs` | `ReturnSupportOption()`, `RemoveOwnedSupportOption()` 추가 |
| `Assets/Scripts/Systems/InventorySystem.cs` | `UnequipSupportOption(slot)` 추가 |
| `Assets/Scripts/UI/SupportSlotUI.cs` | `IDropHandler` 구현 — 드래그 드랍으로 장착 처리 |

## 3. 신규 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/UI/ShopSupportSlotUI.cs` | Shop 패널 보조 옵션 슬롯. `ShopSkillSlotUI`와 동일 패턴. 큐브 잔량으로 구매 버튼 활성화 |
| `Assets/Scripts/UI/InvenSupportUI.cs` | 보조 옵션 인벤토리 컨테이너. `ShopSystem.OwnedSupports` 구독, 슬롯 동적 생성 |
| `Assets/Scripts/UI/InvenSupportSlotDragHandler.cs` | 인벤토리 보조 옵션 슬롯 드래그 핸들러. `InvenSlotDragHandler`와 동일 패턴 |

## 4. 보조 옵션 효과 정의

| 타입 | 효과 구분 | 적용 방식 |
|------|-----------|-----------|
| `OverloadModule` | 공격력 +% | `dmgPct` 합산 |
| `AccelChip` | 공격속도 +% | `spdPct` 합산 |
| `AoeAmplifier` | 공격 범위 +% | `rangePct` 합산 |
| `CritAmplifier` | 치명타 확률/피해 +% | `CritChance`, `CritDamage` 합산 |
| `EmpAmplifier` | 기절 확률 +% | `StunChance` 합산 |
| `CorrosiveRound` | 방어력 관통 +% | `ArmorPen` 합산 |
| `ThresholdCircuit` | 체력 임계 추가 피해 % | `Tower.ThresholdDmg` 프로퍼티 |
| `MultiProjectile` | 다중 투사체 활성 | `Tower.HasMultiProjectile` 플래그 |
| `PiercingRound` | 관통 활성 | `Tower.HasPiercing` 플래그 |
| `ChainCircuit` | 연쇄 공격 활성 | `Tower.HasChain` 플래그 |
| `IncendiaryRound` | 화염 도트 활성 | `Tower.HasIncendiary` 플래그 |
| `CoolantDevice` | 쿨다운 감소 +% | `SkillCDReduce` 합산 |
| `EnergyDrain` | 피격 시 쿨다운 감소 | `Tower.HasEnergyDrain` 플래그 |

> ※ 특수 플래그(`HasPiercing`, `HasChain` 등)의 실제 전투 효과는 별도 이슈로 분리 가능

## 5. 테스트 계획

- [ ] Shop에서 보조 옵션 구매 → OwnedSupports 증가 확인
- [ ] 인벤토리에서 보조 옵션 드래그 → 타워 슬롯에 드랍 → 장착 확인
- [ ] 잠긴 슬롯에 드랍 → 장착 안 됨 확인
- [ ] 단순 스탯 옵션(OverloadModule 등) 장착 → Tower 스탯 변경 확인
- [ ] 보조 옵션 장착 후 다른 옵션 드랍 → 기존 옵션 인벤 반환 + 새 옵션 장착

## 6. 위험 요소

- 특수 플래그 효과(관통, 연쇄, 도트 등)는 전투 로직 수정 필요 → 범위 크므로 1차 구현에서 스탯 효과만 구현하고 특수 효과는 별도 이슈로 분리 권장
- `SupportSlotUI`에 `IDropHandler` 추가 시 슬롯 잠금 상태 체크 필수
- `ShopSystem.BuySupportOption()`은 이미 중복 구매 방지 로직 있음 (`_ownedSupports.Contains(option)`)
- SKILLSPEC_defense_converted.xlsx의 정확한 수치 확인 필요 (현재 `SupportOptionData.value` 필드로 에셋에서 조정 가능)
