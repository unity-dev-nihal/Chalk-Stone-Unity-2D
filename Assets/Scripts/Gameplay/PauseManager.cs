using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private GameObject pausePanel;

    [Header("Gameplay References")]
    [SerializeField] private MovementManager movementManager;
    [SerializeField] private PlacementManager placementManager;

    private GameState stateBeforePause;
    private bool isPaused;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        FindReferences();

        isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;
    }

    private void FindReferences()
    {
        if (gameStateManager == null)
        {
            gameStateManager =
                FindFirstObjectByType<GameStateManager>();
        }

        if (movementManager == null)
        {
            movementManager =
                FindFirstObjectByType<MovementManager>();
        }

        if (placementManager == null)
        {
            placementManager =
                FindFirstObjectByType<PlacementManager>();
        }
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        if (gameStateManager == null)
        {
            Debug.LogError(
                "PauseManager: GameStateManager reference is missing."
            );

            return;
        }

        GameState currentState =
            gameStateManager.CurrentState;

        if (!CanPause(currentState))
        {
            Debug.Log(
                $"PauseManager: Cannot pause during {currentState}."
            );

            return;
        }

        stateBeforePause = currentState;

        isPaused = true;

        gameStateManager.SetState(
            GameState.Paused
        );

        if (pausePanel != null)
            pausePanel.SetActive(true);

        Time.timeScale = 0f;

        Debug.Log(
            "===== GAME PAUSED ====="
        );

        Debug.Log(
            $"Previous State: {stateBeforePause}"
        );
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        Time.timeScale = 1f;

        isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (gameStateManager != null)
        {
            gameStateManager.SetState(
                stateBeforePause
            );
        }

        Debug.Log(
            "===== GAME RESUMED ====="
        );

        Debug.Log(
            $"Restored State: {stateBeforePause}"
        );
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        isPaused = false;

        if (GameSession.Instance != null)
        {
            GameSession.Instance.ResetMatchSettings();
        }

        Debug.Log(
            "===== GAME RESTARTED ====="
        );

        SceneManager.LoadScene(
            "GameScene"
        );
    }

    public void ChangeMode()
    {
        Time.timeScale = 1f;

        isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (GameSession.Instance == null)
        {
            Debug.LogError(
                "PauseManager: GameSession.Instance is missing."
            );

            return;
        }

        GameSession.Instance.RequestGameModePanel();

        Debug.Log(
            "===== CHANGING GAME MODE ====="
        );

        Debug.Log(
            "GameModePanel will open after Main Menu loads."
        );

        SceneManager.LoadScene(
            "Main Menu"
        );
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;

        isPaused = false;

        if (GameSession.Instance != null)
        {
            GameSession.Instance.ClearGameModePanelRequest();
        }

        Debug.Log(
            "===== RETURNING TO MAIN MENU ====="
        );

        SceneManager.LoadScene(
            "Main Menu"
        );
    }

    private bool CanPause(GameState state)
    {
        return state == GameState.Placement ||
               state == GameState.Movement ||
               state == GameState.LastChance;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}