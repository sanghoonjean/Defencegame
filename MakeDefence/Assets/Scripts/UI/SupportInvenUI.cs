using UnityEngine;

/// <summary>
/// DEPRECATED — InvenUI 가 스킬/서포트를 통합 그리드에서 표시 (#220).
/// 씬에 남아있는 GameObject 가 깨지지 않도록 빈 컴포넌트로 유지. 활성화 시 자동 비활성.
/// Editor 정리 시 GameObject 와 함께 제거 권장.
/// </summary>
[System.Obsolete("Use InvenUI for unified inventory display (#220).")]
public class SupportInvenUI : MonoBehaviour
{
    private void OnEnable()
    {
        Debug.LogWarning("[SupportInvenUI] DEPRECATED — InvenUI 가 통합 그리드를 처리합니다. 이 GameObject 는 비활성화됩니다.");
        gameObject.SetActive(false);
    }
}
