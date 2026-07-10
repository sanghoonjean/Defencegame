using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 유닛 첫 배치 전 직업 선택 팝업.
/// Canvas 하위에 배치하고 버튼 3개(warriorButton, mageButton, archerButton)를 연결한다.
/// </summary>
public class JobSelectPopup : MonoBehaviour
{
    public static JobSelectPopup Instance { get; private set; }

    [SerializeField] private GameObject  panel;
    [SerializeField] private Button      warriorButton;
    [SerializeField] private Button      mageButton;
    [SerializeField] private Button      archerButton;

    private Action<JobClass> _onSelected;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);

        warriorButton.onClick.AddListener(() => Confirm(JobClass.Warrior));
        mageButton   .onClick.AddListener(() => Confirm(JobClass.Mage));
        archerButton .onClick.AddListener(() => Confirm(JobClass.Archer));
    }

    public void Show(Action<JobClass> onSelected)
    {
        _onSelected = onSelected;
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
        _onSelected = null;
    }

    private void Confirm(JobClass job)
    {
        Hide();
        _onSelected?.Invoke(job);
    }
}
