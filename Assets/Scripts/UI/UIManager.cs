using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private Transform canvasTransform;

    [Header("Main Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameModePanel;
    [SerializeField] private GameObject aiSetupPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject howToPlayPanel;

    private void Awake()
    {
        FindPanels();
    }

    private void Start()
    {
        if (GameSession.Instance != null &&
            GameSession.Instance.OpenGameModeOnMainMenu)
        {
            GameSession.Instance.ClearGameModePanelRequest();

            ShowGameMode();

            Debug.Log(
                "UIManager: Opened Game Mode Panel from gameplay."
            );

            return;
        }

        ShowMainMenu();
    }

    // =========================================================
    // FIND PANELS
    // =========================================================

    private void FindPanels()
    {
        if (canvasTransform == null)
        {
            Canvas canvas =
                FindFirstObjectByType<Canvas>();

            if (canvas != null)
                canvasTransform = canvas.transform;
        }

        if (canvasTransform == null)
        {
            Debug.LogError(
                "UIManager: Canvas could not be found."
            );

            return;
        }

        if (mainMenuPanel == null)
        {
            mainMenuPanel =
                FindChildByName(
                    canvasTransform,
                    "MainMenuPanel"
                );
        }

        if (gameModePanel == null)
        {
            gameModePanel =
                FindChildByName(
                    canvasTransform,
                    "GameModePanel"
                );
        }

        if (aiSetupPanel == null)
        {
            aiSetupPanel =
                FindChildByName(
                    canvasTransform,
                    "AISetupPanel"
                );
        }

        if (settingsPanel == null)
        {
            settingsPanel =
                FindChildByName(
                    canvasTransform,
                    "SettingsPanel"
                );
        }

        if (howToPlayPanel == null)
        {
            howToPlayPanel =
                FindChildByName(
                    canvasTransform,
                    "HowToPlayPanel"
                );
        }

        Debug.Log(
            "===== UIManager Panel References ====="
        );

        Debug.Log(
            $"MainMenuPanel: " +
            $"{GetPanelName(mainMenuPanel)}"
        );

        Debug.Log(
            $"GameModePanel: " +
            $"{GetPanelName(gameModePanel)}"
        );

        Debug.Log(
            $"AISetupPanel: " +
            $"{GetPanelName(aiSetupPanel)}"
        );

        Debug.Log(
            $"SettingsPanel: " +
            $"{GetPanelName(settingsPanel)}"
        );

        Debug.Log(
            $"HowToPlayPanel: " +
            $"{GetPanelName(howToPlayPanel)}"
        );

        Debug.Log(
            "======================================"
        );
    }

    private GameObject FindChildByName(
        Transform parent,
        string objectName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == objectName)
                return child.gameObject;

            GameObject result =
                FindChildByName(
                    child,
                    objectName
                );

            if (result != null)
                return result;
        }

        return null;
    }

    private string GetPanelName(
        GameObject panel)
    {
        if (panel == null)
            return "NOT FOUND";

        return panel.name;
    }

    // =========================================================
    // MAIN MENU
    // =========================================================

    public void ShowMainMenu()
    {
        SetPanelState(
            mainMenuPanel,
            true
        );

        SetPanelState(
            gameModePanel,
            false
        );

        SetPanelState(
            aiSetupPanel,
            false
        );

        SetPanelState(
            settingsPanel,
            false
        );

        SetPanelState(
            howToPlayPanel,
            false
        );

        Debug.Log(
            "Main Menu Panel shown."
        );
    }

    // =========================================================
    // GAME MODE
    // =========================================================

    public void ShowGameMode()
    {
        SetPanelState(
            mainMenuPanel,
            false
        );

        SetPanelState(
            gameModePanel,
            true
        );

        SetPanelState(
            aiSetupPanel,
            false
        );

        SetPanelState(
            settingsPanel,
            false
        );

        SetPanelState(
            howToPlayPanel,
            false
        );

        Debug.Log(
            "Game Mode Panel shown."
        );
    }

    // =========================================================
    // AI SETUP
    // =========================================================

    public void ShowAISetup()
    {
        SetPanelState(
            mainMenuPanel,
            false
        );

        SetPanelState(
            gameModePanel,
            false
        );

        SetPanelState(
            aiSetupPanel,
            true
        );

        SetPanelState(
            settingsPanel,
            false
        );

        SetPanelState(
            howToPlayPanel,
            false
        );

        Debug.Log(
            "AI Setup Panel shown."
        );
    }

    // =========================================================
    // GAME MODE SELECTION
    // =========================================================

    public void SelectTwoPlayer()
    {
        if (GameSession.Instance == null)
        {
            Debug.LogError(
                "UIManager: GameSession.Instance is missing."
            );

            return;
        }

        GameSession.Instance.SetGameMode(
            GameMode.TwoPlayer
        );

        Debug.Log(
            "Game Mode: Two Player"
        );

        LoadGameplayScene();
    }

    public void SelectVsAI()
    {
        if (GameSession.Instance == null)
        {
            Debug.LogError(
                "UIManager: GameSession.Instance is missing."
            );

            return;
        }

        GameSession.Instance.SetGameMode(
            GameMode.VsAI
        );

        Debug.Log(
            "Game Mode: Vs AI"
        );

        ShowAISetup();
    }

    // =========================================================
    // DIFFICULTY
    // =========================================================

    public void SelectEasy()
    {
        if (GameSession.Instance == null)
        {
            Debug.LogError(
                "UIManager: GameSession.Instance is missing."
            );

            return;
        }

        GameSession.Instance.SetDifficulty(
            AIDifficulty.Easy
        );

        Debug.Log(
            "AI Difficulty: Easy"
        );
    }

    public void SelectMedium()
    {
        if (GameSession.Instance == null)
        {
            Debug.LogError(
                "UIManager: GameSession.Instance is missing."
            );

            return;
        }

        GameSession.Instance.SetDifficulty(
            AIDifficulty.Medium
        );

        Debug.Log(
            "AI Difficulty: Medium"
        );
    }

    public void SelectHard()
    {
        if (GameSession.Instance == null)
        {
            Debug.LogError(
                "UIManager: GameSession.Instance is missing."
            );

            return;
        }

        GameSession.Instance.SetDifficulty(
            AIDifficulty.Hard
        );

        Debug.Log(
            "AI Difficulty: Hard"
        );
    }

    // =========================================================
    // START AI GAME
    // =========================================================

    public void StartAIGame()
    {
        if (GameSession.Instance == null)
        {
            Debug.LogError(
                "UIManager: GameSession.Instance is missing."
            );

            return;
        }

        if (GameSession.Instance.SelectedGameMode !=
            GameMode.VsAI)
        {
            Debug.LogWarning(
                "Game mode is not set to Vs AI."
            );

            return;
        }

        LoadGameplayScene();
    }

    // =========================================================
    // LOAD GAMEPLAY SCENE
    // =========================================================

    public void LoadGameplayScene()
    {
        if (GameSession.Instance == null)
        {
            Debug.LogError(
                "UIManager: GameSession.Instance is missing."
            );

            return;
        }

        GameSession.Instance.ResetMatchSettings();

        Debug.Log(
            "Loading GameScene..."
        );

        SceneManager.LoadScene(
            "GameScene"
        );
    }

    // =========================================================
    // BACK TO MAIN MENU
    // =========================================================

    public void BackToMainMenu()
    {
        ShowMainMenu();
    }

    // =========================================================
    // BACK TO GAME MODE
    // =========================================================

    public void BackToGameMode()
    {
        ShowGameMode();
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    public void ShowSettings()
    {
        Debug.Log(
            "Opening Settings Panel..."
        );

        if (settingsPanel == null)
        {
            Debug.LogError(
                "UIManager: SettingsPanel was NOT found!"
            );

            return;
        }

        SetPanelState(
            mainMenuPanel,
            false
        );

        SetPanelState(
            gameModePanel,
            false
        );

        SetPanelState(
            aiSetupPanel,
            false
        );

        SetPanelState(
            howToPlayPanel,
            false
        );

        SetPanelState(
            settingsPanel,
            true
        );

        Debug.Log(
            $"SettingsPanel active: " +
            $"{settingsPanel.activeSelf}"
        );
    }

    // =========================================================
    // HOW TO PLAY
    // =========================================================

    public void ShowHowToPlay()
    {
        SetPanelState(
            mainMenuPanel,
            false
        );

        SetPanelState(
            gameModePanel,
            false
        );

        SetPanelState(
            aiSetupPanel,
            false
        );

        SetPanelState(
            settingsPanel,
            false
        );

        SetPanelState(
            howToPlayPanel,
            true
        );

        Debug.Log(
            "How To Play Panel shown."
        );
    }

    // =========================================================
    // QUIT
    // =========================================================

    public void QuitGame()
    {
        Debug.Log(
            "Quit Game"
        );

        Application.Quit();
    }

    // =========================================================
    // PANEL HELPER
    // =========================================================

    private void SetPanelState(
        GameObject panel,
        bool state)
    {
        if (panel == null)
        {
            Debug.LogWarning(
                "UIManager: Attempted to change " +
                "a panel that is not assigned."
            );

            return;
        }

        panel.SetActive(state);
    }
}