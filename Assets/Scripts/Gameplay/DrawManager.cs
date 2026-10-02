using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DrawManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private MovementManager movementManager;
    [SerializeField] private GameStateManager gameStateManager;

    [Header("Gameplay UI")]
    [SerializeField] private GameObject gameplayUI;

    [Header("Draw UI")]
    [SerializeField] private GameObject drawPanel;
    [SerializeField] private GameObject lastChancePanel;
    [SerializeField] private GameObject drawResultPanel;

    [Header("Draw Panel UI")]
    [SerializeField] private TMP_Text drawPlayer1MovesText;
    [SerializeField] private TMP_Text drawPlayer2MovesText;

    [Header("Last Chance UI")]
    [SerializeField] private TMP_Text lastChancePlayer1MovesText;
    [SerializeField] private TMP_Text lastChancePlayer2MovesText;
    [SerializeField] private TMP_Text lastChanceStatusText;

    [Header("Last Chance Settings")]
    [SerializeField] private int defaultLastChanceMoves = 5;

    [Header("Repetition Settings")]
    [SerializeField] private int repetitionsRequired = 3;

    private readonly Dictionary<string, int> boardStateOccurrences =
        new Dictionary<string, int>();

    private int player1LastChanceMoves;
    private int player2LastChanceMoves;

    private bool lastChanceActive;
    private bool lockCycleActive;

    private readonly HashSet<string> statesBeforeLastChance =
        new HashSet<string>();

    public bool LastChanceActive => lastChanceActive;

    public int Player1LastChanceMoves =>
        player1LastChanceMoves;

    public int Player2LastChanceMoves =>
        player2LastChanceMoves;

    private void Awake()
    {
        FindReferences();

        InitializeLastChanceMoves();

        boardStateOccurrences.Clear();
        statesBeforeLastChance.Clear();

        lastChanceActive = false;
        lockCycleActive = false;
    }

    private void Start()
    {
        HideAllDrawPanels();

        SetGameplayUIActive(true);

        UpdateLastChanceUI();
        UpdateDrawPanelUI();
        UpdateLastChanceStatusText();
    }

    private void FindReferences()
    {
        if (boardManager == null)
            boardManager =
                FindFirstObjectByType<BoardManager>();

        if (playerManager == null)
            playerManager =
                FindFirstObjectByType<PlayerManager>();

        if (movementManager == null)
            movementManager =
                FindFirstObjectByType<MovementManager>();

        if (gameStateManager == null)
            gameStateManager =
                FindFirstObjectByType<GameStateManager>();

        if (gameplayUI == null)
        {
            GameplayUIManager uiManager =
                FindFirstObjectByType<GameplayUIManager>();

            if (uiManager != null)
                gameplayUI = uiManager.gameObject;
        }
    }

    private void InitializeLastChanceMoves()
    {
        if (GameSession.Instance != null)
        {
            player1LastChanceMoves =
                GameSession.Instance.Player1LastChanceMoves;

            player2LastChanceMoves =
                GameSession.Instance.Player2LastChanceMoves;
        }
        else
        {
            player1LastChanceMoves =
                defaultLastChanceMoves;

            player2LastChanceMoves =
                defaultLastChanceMoves;
        }
    }

    public void RegisterInitialBoardState()
    {
        boardStateOccurrences.Clear();
        statesBeforeLastChance.Clear();

        RegisterCurrentBoardState();

        Debug.Log(
            "Initial movement-phase board state registered."
        );
    }

    public void EvaluateAfterMove(
        PlayerOwner movingPlayer)
    {
        if (boardManager == null ||
            playerManager == null)
            return;

        if (lastChanceActive)
        {
            EvaluateLastChanceMove(
                movingPlayer
            );

            return;
        }

        string currentState =
            GetCurrentBoardState();

        RegisterBoardState(currentState);

        int occurrences =
            boardStateOccurrences[currentState];

        Debug.Log(
            $"Board state occurred " +
            $"{occurrences} time(s)."
        );

        if (occurrences >= repetitionsRequired)
        {
            BeginNewLock();
            return;
        }

        /*
         * No-legal-move states are no longer draws.
         * MovementManager resolves a Battle Phase block as
         * victory, while Placement and Development reject
         * moves that would create such a block.
         *
         * DrawManager therefore handles repetition locks here.
         */
    }

    // ---------------------------------------------------------
    // NEW DRAW / LOCK
    // ---------------------------------------------------------

    private void BeginNewLock()
    {
        if (lockCycleActive)
        {
            Debug.Log(
                "Same lock detected again. " +
                "Continuing Last Chance."
            );

            return;
        }

        lockCycleActive = true;

        if (gameStateManager != null)
        {
            gameStateManager.SetState(
                GameState.DrawDetected
            );
        }

        if (movementManager != null)
            movementManager.StopMovement();

        statesBeforeLastChance.Clear();

        foreach (
            KeyValuePair<string, int> entry
            in boardStateOccurrences)
        {
            statesBeforeLastChance.Add(
                entry.Key
            );
        }

        UpdateDrawPanelUI();

        Debug.Log(
            "===== NEW DRAW / LOCK DETECTED ====="
        );

        Debug.Log(
            $"Player 1 moves available: " +
            $"{player1LastChanceMoves}"
        );

        Debug.Log(
            $"Player 2 moves available: " +
            $"{player2LastChanceMoves}"
        );

        // Play only when a genuinely new lock
        // opens the Draw Panel.
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayDrawDetected();

        ShowDrawPanel();
    }

    // ---------------------------------------------------------
    // CONTINUE FROM DRAW
    // ---------------------------------------------------------

    public void ContinueFromDraw()
    {
        if (!lockCycleActive)
        {
            Debug.LogWarning(
                "ContinueFromDraw called without " +
                "an active lock."
            );

            return;
        }

        HideDrawPanel();

        if (BothPlayersExhausted())
        {
            ShowFinalDraw();
            return;
        }

        lastChanceActive = true;

        if (gameStateManager != null)
        {
            gameStateManager.SetState(
                GameState.LastChance
            );
        }

        Debug.Log(
            "===== LAST CHANCE BEGINS ====="
        );

        Debug.Log(
            $"Player 1: {player1LastChanceMoves}"
        );

        Debug.Log(
            $"Player 2: {player2LastChanceMoves}"
        );

        UpdateLastChanceUI();
        UpdateDrawPanelUI();

        StartLastChanceTurn();
    }

    // ---------------------------------------------------------
    // LAST CHANCE TURN
    // ---------------------------------------------------------

    private void StartLastChanceTurn()
    {
        if (!lastChanceActive ||
            playerManager == null)
            return;

        PlayerOwner currentPlayer =
            playerManager.CurrentPlayer;

        if (GetRemainingMoves(currentPlayer) <= 0)
        {
            playerManager.SwitchTurn();

            currentPlayer =
                playerManager.CurrentPlayer;
        }

        if (GetRemainingMoves(currentPlayer) <= 0)
        {
            ShowFinalDraw();
            return;
        }

        Debug.Log(
            $"Last Chance turn: {currentPlayer}"
        );

        UpdateLastChanceUI();
        UpdateDrawPanelUI();
        UpdateLastChanceStatusText();

        /*
         * Enable movement controls without
         * changing GameState.LastChance.
         */
        if (movementManager != null)
        {
            movementManager.BeginMovementPhase(
                false
            );
        }

        ShowLastChancePanel();
    }

    // ---------------------------------------------------------
    // LAST CHANCE MOVE EVALUATION
    // ---------------------------------------------------------

    private void EvaluateLastChanceMove(
        PlayerOwner movingPlayer)
    {
        ConsumeLastChanceMove(
            movingPlayer
        );

        Debug.Log(
            $"{movingPlayer} used a Last Chance move."
        );

        Debug.Log(
            $"Player 1 remaining: " +
            $"{player1LastChanceMoves}"
        );

        Debug.Log(
            $"Player 2 remaining: " +
            $"{player2LastChanceMoves}"
        );

        UpdateLastChanceUI();
        UpdateDrawPanelUI();

        string currentState =
            GetCurrentBoardState();

        bool newStateCreated =
            !statesBeforeLastChance.Contains(
                currentState
            );

        if (newStateCreated)
        {
            Debug.Log(
                "===== LOCK BROKEN ====="
            );

            ExitLastChance();
            return;
        }

        if (BothPlayersExhausted())
        {
            ShowFinalDraw();
            return;
        }

        RegisterBoardState(
            currentState
        );

        StartLastChanceTurn();
    }

    // ---------------------------------------------------------
    // EXIT LAST CHANCE
    // ---------------------------------------------------------

    private void ExitLastChance()
    {
        lastChanceActive = false;
        lockCycleActive = false;

        HideLastChancePanel();

        SetGameplayUIActive(true);

        if (gameStateManager != null)
        {
            gameStateManager.SetState(
                GameState.Movement
            );
        }

        Debug.Log(
            "Normal movement has resumed."
        );

        if (movementManager != null)
            movementManager.BeginMovementPhase();
    }

    // ---------------------------------------------------------
    // LAST CHANCE COUNTERS
    // ---------------------------------------------------------

    private void ConsumeLastChanceMove(
        PlayerOwner player)
    {
        if (player == PlayerOwner.Player1)
        {
            if (player1LastChanceMoves > 0)
                player1LastChanceMoves--;
        }
        else if (player == PlayerOwner.Player2)
        {
            if (player2LastChanceMoves > 0)
                player2LastChanceMoves--;
        }

        SaveCountersToSession();
    }

    private int GetRemainingMoves(
        PlayerOwner player)
    {
        if (player == PlayerOwner.Player1)
            return player1LastChanceMoves;

        if (player == PlayerOwner.Player2)
            return player2LastChanceMoves;

        return 0;
    }

    private bool BothPlayersExhausted()
    {
        return player1LastChanceMoves <= 0 &&
               player2LastChanceMoves <= 0;
    }

    private void SaveCountersToSession()
    {
        if (GameSession.Instance == null)
            return;

        GameSession.Instance.Player1LastChanceMoves =
            player1LastChanceMoves;

        GameSession.Instance.Player2LastChanceMoves =
            player2LastChanceMoves;
    }

    // ---------------------------------------------------------
    // UI UPDATES
    // ---------------------------------------------------------

    private void UpdateLastChanceUI()
    {
        if (lastChancePlayer1MovesText != null)
        {
            lastChancePlayer1MovesText.text =
                player1LastChanceMoves.ToString();
        }

        if (lastChancePlayer2MovesText != null)
        {
            lastChancePlayer2MovesText.text =
                player2LastChanceMoves.ToString();
        }
    }

    private void UpdateDrawPanelUI()
    {
        if (drawPlayer1MovesText != null)
        {
            drawPlayer1MovesText.text =
                player1LastChanceMoves.ToString();
        }

        if (drawPlayer2MovesText != null)
        {
            drawPlayer2MovesText.text =
                player2LastChanceMoves.ToString();
        }
    }

    private void UpdateLastChanceStatusText()
    {
        if (lastChanceStatusText == null ||
            playerManager == null)
            return;

        PlayerOwner currentPlayer =
            playerManager.CurrentPlayer;

        if (currentPlayer == PlayerOwner.Player1)
        {
            lastChanceStatusText.text =
                "PLAYER 1 — BREAK THE LOCK!";
        }
        else if (currentPlayer ==
                 PlayerOwner.Player2)
        {
            lastChanceStatusText.text =
                "PLAYER 2 — BREAK THE LOCK!";
        }
        else
        {
            lastChanceStatusText.text = "";
        }
    }

    // ---------------------------------------------------------
    // LEGAL MOVE CHECK
    // ---------------------------------------------------------

    private bool HasNoLegalMoves(
        PlayerOwner player)
    {
        if (boardManager == null)
            return true;

        for (int from = 0; from < 9; from++)
        {
            if (boardManager.GetOwner(from) != player)
                continue;

            for (int to = 0; to < 9; to++)
            {
                if (boardManager.IsOccupied(to))
                    continue;

                if (boardManager.AreConnected(
                        from,
                        to))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // ---------------------------------------------------------
    // BOARD STATE
    // ---------------------------------------------------------

    private string GetCurrentBoardState()
    {
        string state = "";

        for (int i = 0; i < 9; i++)
        {
            PlayerOwner owner =
                boardManager.GetOwner(i);

            switch (owner)
            {
                case PlayerOwner.Player1:
                    state += "1";
                    break;

                case PlayerOwner.Player2:
                    state += "2";
                    break;

                default:
                    state += "0";
                    break;
            }
        }

        state += "_";

        if (playerManager.CurrentPlayer ==
            PlayerOwner.Player1)
        {
            state += "1";
        }
        else
        {
            state += "2";
        }

        return state;
    }

    private void RegisterCurrentBoardState()
    {
        string state =
            GetCurrentBoardState();

        RegisterBoardState(state);
    }

    private void RegisterBoardState(
        string state)
    {
        if (!boardStateOccurrences.ContainsKey(state))
        {
            boardStateOccurrences[state] = 1;
        }
        else
        {
            boardStateOccurrences[state]++;
        }
    }

    // ---------------------------------------------------------
    // DRAW PANEL
    // ---------------------------------------------------------

    private void ShowDrawPanel()
    {
        UpdateDrawPanelUI();

        if (drawPanel != null)
            drawPanel.SetActive(true);

        if (lastChancePanel != null)
            lastChancePanel.SetActive(false);

        if (drawResultPanel != null)
            drawResultPanel.SetActive(false);

        SetGameplayUIActive(true);
    }

    private void HideDrawPanel()
    {
        if (drawPanel != null)
            drawPanel.SetActive(false);
    }

    // ---------------------------------------------------------
    // LAST CHANCE PANEL
    // ---------------------------------------------------------

    private void ShowLastChancePanel()
    {
        UpdateLastChanceUI();
        UpdateDrawPanelUI();
        UpdateLastChanceStatusText();

        if (drawPanel != null)
            drawPanel.SetActive(false);

        if (lastChancePanel != null)
            lastChancePanel.SetActive(true);

        if (drawResultPanel != null)
            drawResultPanel.SetActive(false);

        SetGameplayUIActive(false);
    }

    private void HideLastChancePanel()
    {
        if (lastChancePanel != null)
            lastChancePanel.SetActive(false);
    }

    // ---------------------------------------------------------
    // FINAL DRAW
    // ---------------------------------------------------------

    private void ShowFinalDraw()
    {
        lastChanceActive = false;
        lockCycleActive = false;

        if (gameStateManager != null)
        {
            gameStateManager.SetState(
                GameState.DrawResult
            );
        }

        if (movementManager != null)
            movementManager.StopMovement();

        if (drawPanel != null)
            drawPanel.SetActive(false);

        if (lastChancePanel != null)
            lastChancePanel.SetActive(false);

        if (drawResultPanel != null)
            drawResultPanel.SetActive(true);

        SetGameplayUIActive(false);

        // Play only for the final draw result.
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayDraw();

        Debug.Log(
            "===== FINAL DRAW ====="
        );
    }

    // ---------------------------------------------------------
    // PANEL RESET
    // ---------------------------------------------------------

    private void HideAllDrawPanels()
    {
        if (drawPanel != null)
            drawPanel.SetActive(false);

        if (lastChancePanel != null)
            lastChancePanel.SetActive(false);

        if (drawResultPanel != null)
            drawResultPanel.SetActive(false);
    }

    private void SetGameplayUIActive(
        bool active)
    {
        if (gameplayUI != null)
        {
            gameplayUI.SetActive(active);

            if (active)
            {
                Debug.Log(
                    "Gameplay UI: ENABLED"
                );
            }
            else
            {
                Debug.Log(
                    "Gameplay UI: DISABLED"
                );
            }
        }
    }
}