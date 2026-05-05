using System;
using UnityEngine;

public enum GameState { Playing, WaveResult, Victory, Defeat }

public class GameStateSystem : MonoBehaviour
{
    public static GameState Current { get; private set; } = GameState.Playing;
    public static event Action<GameState> OnStateChanged;

    public static void SetState(GameState newState)
    {
        if (Current == newState) return;
        Current = newState;
        OnStateChanged?.Invoke(newState);
    }

    public static void ResetToPlaying()
    {
        Current = GameState.Playing;
        OnStateChanged?.Invoke(Current);
    }
}
