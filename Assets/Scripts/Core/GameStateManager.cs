using System;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    [Header("Current Game State")]
    [SerializeField] private GameState currentState =
        GameState.Toss;

    public GameState CurrentState => currentState;

    public event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        currentState = GameState.Toss;

        Debug.Log(
            $"GameStateManager initialized: {currentState}"
        );
    }

    public void SetState(GameState newState)
    {
        if (currentState == newState)
            return;

        GameState previousState = currentState;

        currentState = newState;

        Debug.Log(
            $"Game State Changed: " +
            $"{previousState} → {currentState}"
        );

        OnGameStateChanged?.Invoke(currentState);
    }

    public bool IsState(GameState state)
    {
        return currentState == state;
    }

    public bool IsGameplayActive()
    {
        return currentState == GameState.Placement ||
               currentState == GameState.Movement ||
               currentState == GameState.LastChance;
    }

    public bool IsGameOver()
    {
        return currentState == GameState.Victory ||
               currentState == GameState.DrawResult;
    }
}