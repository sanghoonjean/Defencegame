using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shop 패널 내 구매 버튼 GameObject에 부착.
/// Inspector에서 SkillData 에셋을 지정하면 하위 큐브 잔량에 따라
/// 버튼 활성/비활성화를 자동 갱신하고, 클릭 시 스킬을 구매한다.
/// </summary>
[RequireComponent(typeof(Button))]
public class ShopBuyButtonUI : MonoBehaviour
{
    [SerializeField] private SkillData skillData;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        CubeSystem.OnCubeChanged += OnCubeChanged;
        Refresh();
    }

    private void OnDisable()
    {
        CubeSystem.OnCubeChanged -= OnCubeChanged;
    }

    private void OnCubeChanged(CubeType type, int _)
    {
        if (type == CubeType.Lower) Refresh();
    }

    private void Refresh()
    {
        bool canBuy = CubeSystem.Instance != null &&
                      CubeSystem.Instance.GetCount(CubeType.Lower) >= 1;
        if (_button != null) _button.interactable = canBuy;
    }

    private void OnClick()
    {
        if (ShopSystem.Instance == null || skillData == null) return;
        ShopSystem.Instance.BuySkill(skillData);
    }
}
