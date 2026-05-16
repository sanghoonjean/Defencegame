using UnityEngine;
using UnityEngine.UI;

public class InvenUI : MonoBehaviour
{
    private struct SlotRef
    {
        public Image  image;
        public Button button;
    }

    private SlotRef[] _slots;

    private void Awake()
    {
        var list = new System.Collections.Generic.List<SlotRef>();
        foreach (Transform slot in transform)
        {
            var icon = slot.Find("ICON");
            if (icon == null) continue;
            var img = icon.GetComponent<Image>();
            if (img == null) continue;
            // 텍스트 오브젝트 숨기기
            var nameObj = slot.Find("Name");
            if (nameObj != null) nameObj.gameObject.SetActive(false);
            var textObj = slot.Find("Text (TMP)");
            if (textObj != null) textObj.gameObject.SetActive(false);

            var btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                // 슬롯 루트 배경 Image를 투명하게, ICON을 Target Graphic으로 설정
                var bgImage = slot.GetComponent<Image>();
                if (bgImage != null) bgImage.color = Color.clear;
                btn.targetGraphic = img;
            }
            list.Add(new SlotRef
            {
                image  = img,
                button = btn
            });
        }
        _slots = list.ToArray();
    }

    private void OnEnable()
    {
        ShopSystem.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        ShopSystem.OnInventoryChanged -= Refresh;
    }

    private void Refresh()
    {
        var owned = ShopSystem.Instance?.OwnedSkills;
        for (int i = 0; i < _slots.Length; i++)
        {
            bool hasSkill = owned != null && i < owned.Count;

            _slots[i].image.sprite = hasSkill ? owned[i].icon : null;
            _slots[i].image.color  = hasSkill ? Color.white : Color.clear;

            if (_slots[i].button == null) continue;
            _slots[i].button.onClick.RemoveAllListeners();
            if (hasSkill)
            {
                var skill = owned[i];
                _slots[i].button.onClick.AddListener(
                    () => InventorySystem.Instance?.EquipSkill(skill));
            }
        }
    }
}
