using System;
using UnityEngine;

public class PlayerSystem : MonoBehaviour
{
    public static PlayerSystem Instance { get; private set; }

    public static event Action<int> OnHpChanged;
    public static event Action OnPlayerDied;

    public int MaxHp { get; private set; } = 100;
    public int CurrentHp { get; private set; }

    private void Awake()
    {
        Instance = this;
        CurrentHp = MaxHp;
    }

    public void ResetHp()
    {
        CurrentHp = MaxHp;
        OnHpChanged?.Invoke(CurrentHp);
    }

    public void TakeDamage(int amount)
    {
        if (CurrentHp <= 0) return;
        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        OnHpChanged?.Invoke(CurrentHp);
        if (CurrentHp <= 0)
            OnPlayerDied?.Invoke();
    }
}
