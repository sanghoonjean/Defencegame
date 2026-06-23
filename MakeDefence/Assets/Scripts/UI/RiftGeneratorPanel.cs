using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class RiftGeneratorPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Stone Display")]
    [SerializeField] private Text stoneStatusText;   // 장착된 차원석 옵션 multi-line
    [SerializeField] private Text inventoryCountText; // 인벤토리에 남은 차원석 수

    [Header("Buttons")]
    [SerializeField] private Button loadNextStoneButton;
    [SerializeField] private Button unloadStoneButton;
    [SerializeField] private Button applyLowerButton;
    [SerializeField] private Button applyUpperButton;
    [SerializeField] private Button applyTopTierButton;
    [SerializeField] private Button applyDeleteButton;
    [SerializeField] private Button applyCloneButton;

    private RiftGenerator _current;

    private void Awake()
    {
        if (loadNextStoneButton != null) loadNextStoneButton.onClick.AddListener(LoadNextStone);
        if (unloadStoneButton   != null) unloadStoneButton.onClick.AddListener(UnloadStone);
        if (applyLowerButton    != null) applyLowerButton.onClick.AddListener(() => ApplyCube(CubeType.Lower));
        if (applyUpperButton    != null) applyUpperButton.onClick.AddListener(() => ApplyCube(CubeType.Upper));
        if (applyTopTierButton  != null) applyTopTierButton.onClick.AddListener(() => ApplyCube(CubeType.TopTier));
        if (applyDeleteButton   != null) applyDeleteButton.onClick.AddListener(() => ApplyCube(CubeType.Delete));
        if (applyCloneButton    != null) applyCloneButton.onClick.AddListener(() => ApplyCube(CubeType.Clone));
    }

    private void OnEnable()
    {
        InventorySystem.OnRiftSelected += HandleRiftSelected;
        DimensionStoneInventory.OnInventoryChanged += Refresh;
        HandleRiftSelected(InventorySystem.Instance?.SelectedRift);
    }

    private void OnDisable()
    {
        InventorySystem.OnRiftSelected -= HandleRiftSelected;
        DimensionStoneInventory.OnInventoryChanged -= Refresh;
    }

    private void HandleRiftSelected(RiftGenerator rift)
    {
        if (_current != null) _current.OnStoneChanged -= Refresh;
        _current = rift;
        if (_current != null) _current.OnStoneChanged += Refresh;
        ApplyVisibility(rift != null);
        Refresh();
    }

    private void ApplyVisibility(bool show)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha          = show ? 1f : 0f;
        canvasGroup.blocksRaycasts = show;
        canvasGroup.interactable   = show;
    }

    private void Refresh()
    {
        if (_current == null) return;

        var sb = new StringBuilder();
        if (_current.LoadedStone == null)
        {
            sb.Append("차원석 미장착");
        }
        else
        {
            sb.Append("[차원석]\n");
            foreach (var opt in _current.LoadedStone.Options)
                sb.Append($"- {opt.Type} +{opt.Value:F0}\n");
        }
        if (stoneStatusText != null) stoneStatusText.text = sb.ToString();

        int invCount = DimensionStoneInventory.Instance != null ? DimensionStoneInventory.Instance.Count : 0;
        if (inventoryCountText != null) inventoryCountText.text = $"보유 차원석: {invCount}";

        bool hasStone = _current.LoadedStone != null;
        bool hasInv   = invCount > 0;
        if (loadNextStoneButton != null) loadNextStoneButton.interactable = !hasStone && hasInv;
        if (unloadStoneButton   != null) unloadStoneButton.interactable   = hasStone;
        if (applyLowerButton    != null) applyLowerButton.interactable    = hasStone;
        if (applyUpperButton    != null) applyUpperButton.interactable    = hasStone;
        if (applyTopTierButton  != null) applyTopTierButton.interactable  = hasStone;
        if (applyDeleteButton   != null) applyDeleteButton.interactable   = hasStone;
        if (applyCloneButton    != null) applyCloneButton.interactable    = hasStone;
    }

    private void LoadNextStone()
    {
        if (_current == null || _current.LoadedStone != null) return;
        if (DimensionStoneInventory.Instance == null || DimensionStoneInventory.Instance.Count == 0) return;
        var stone = DimensionStoneInventory.Instance.Stones[0];
        DimensionStoneInventory.Instance.Remove(stone);
        _current.SetStone(stone);
        Refresh();
    }

    private void UnloadStone()
    {
        if (_current == null || _current.LoadedStone == null) return;
        DimensionStoneInventory.Instance?.Add(_current.LoadedStone);
        _current.ClearStone();
        Refresh();
    }

    private void ApplyCube(CubeType cube)
    {
        if (_current == null) return;
        _current.ApplyCube(cube);
        Refresh();
    }
}
