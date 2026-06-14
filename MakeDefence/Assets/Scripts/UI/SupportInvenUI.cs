using UnityEngine;

/// <summary>
/// DEPRECATED — InvenUI 가 스킬/서포트를 통합 그리드에서 표시 (#220).
/// 씬에 남아있는 GameObject 가 깨지지 않도록 inert stub 으로 유지.
/// 호스트 GameObject 가 InventoryUI 루트인 경우 비활성화 시 통합 인벤 자체가 닫혀 버리므로,
/// gameObject 가 아닌 컴포넌트(this) 만 비활성화한다. Editor 정리 시 GameObject 와 함께 제거 권장.
/// </summary>
[System.Obsolete("Use InvenUI for unified inventory display (#220).")]
public class SupportInvenUI : MonoBehaviour
{
    private void OnEnable()
    {
        Debug.LogWarning("[SupportInvenUI] DEPRECATED — InvenUI 가 통합 그리드를 처리합니다. 컴포넌트 자체만 비활성화합니다.");
        enabled = false;
    }
}
