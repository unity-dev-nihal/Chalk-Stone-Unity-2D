using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public GameMode SelectedGameMode { get; private set; }

    public AIDifficulty SelectedDifficulty { get; private set; }

    public int Player1LastChanceMoves { get; set; }
    public int Player2LastChanceMoves { get; set; }

    public bool OpenGameModeOnMainMenu { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        ResetMatchSettings();

        OpenGameModeOnMainMenu = false;
    }

    public void SetGameMode(GameMode mode)
    {
        SelectedGameMode = mode;
    }

    public void SetDifficulty(AIDifficulty difficulty)
    {
        SelectedDifficulty = difficulty;
    }

    public void ResetMatchSettings()
    {
        Player1LastChanceMoves = 5;
        Player2LastChanceMoves = 5;
    }

    public void RequestGameModePanel()
    {
        OpenGameModeOnMainMenu = true;
    }

    public void ClearGameModePanelRequest()
    {
        OpenGameModeOnMainMenu = false;
    }
}