using UnityEngine;
using UnityEngine.UI;

public class HPPanelController : MonoBehaviour
{
    [SerializeField] private Text hpText;

    private void OnEnable()
    {
        PlayerSystem.OnHpChanged += OnHpChanged;
        if (PlayerSystem.Instance != null)
            OnHpChanged(PlayerSystem.Instance.CurrentHp);
    }

    private void OnDisable()
    {
        PlayerSystem.OnHpChanged -= OnHpChanged;
    }

    // Awake 순서는 GameObject 간에 보장되지 않아 OnEnable 시점엔 PlayerSystem.Instance가
    // 아직 null일 수 있다. Start는 씬의 모든 Awake가 끝난 뒤 실행되는 것이 보장되므로
    // 여기서 한 번 더 동기화한다.
    private void Start()
    {
        if (PlayerSystem.Instance != null)
            OnHpChanged(PlayerSystem.Instance.CurrentHp);
    }

    private void OnHpChanged(int hp)
    {
        if (hpText == null) return;
        hpText.text = $"{hp}/{PlayerSystem.Instance.MaxHp}";
    }
}
