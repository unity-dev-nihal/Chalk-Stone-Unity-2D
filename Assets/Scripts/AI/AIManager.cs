using UnityEngine;

public class AIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private PlacementManager placementManager;
    [SerializeField] private MovementManager movementManager;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private AIPlacement aiPlacement;
    [SerializeField] private AIMovement aiMovement;
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private DrawManager drawManager;

    [Header("AI Settings")]
    [SerializeField] private float thinkingDelay = 1.0f;

    private bool isAITurn;
    private bool thinking;
    private float thinkingTimer;

    private void Awake()
    {
        FindReferences();
    }

    private void Update()
    {
        if (!IsAIGame())
            return;

        if (playerManager == null)
            return;

        UpdateAITurnState();
    }

    private void FindReferences()
    {
        if (playerManager == null)
        {
            playerManager =
                FindFirstObjectByType<PlayerManager>();
        }

        if (placementManager == null)
        {
            placementManager =
                FindFirstObjectByType<PlacementManager>();
        }

        if (movementManager == null)
        {
            movementManager =
                FindFirstObjectByType<MovementManager>();
        }

        if (boardManager == null)
        {
            boardManager =
                FindFirstObjectByType<BoardManager>();
        }

        if (aiPlacement == null)
        {
            aiPlacement =
                FindFirstObjectByType<AIPlacement>();
        }

        if (aiMovement == null)
        {
            aiMovement =
                FindFirstObjectByType<AIMovement>();
        }

        if (gameStateManager == null)
        {
            gameStateManager =
                FindFirstObjectByType<GameStateManager>();
        }

        if (drawManager == null)
        {
            drawManager =
                FindFirstObjectByType<DrawManager>();
        }
    }

    private bool IsAIGame()
    {
        if (GameSession.Instance == null)
            return false;

        return
            GameSession.Instance.SelectedGameMode ==
            GameMode.VsAI;
    }

    private void UpdateAITurnState()
    {
        PlayerData currentPlayer =
            playerManager.GetPlayer(
                playerManager.CurrentPlayer
            );

        if (currentPlayer == null)
            return;

        /*
         * Only the actual AI player is allowed
         * to control the AI system.
         */
        if (!currentPlayer.isAI)
        {
            StopAITurn();
            return;
        }

        /*
         * -------------------------------------------------
         * GAME STATE SAFETY
         * -------------------------------------------------
         *
         * The AI must NEVER act during the Toss.
         *
         * The toss decides StartingPlayer first.
         *
         * After Toss:
         *
         * Toss Winner
         *      ↓
         * Placement first move
         *      ↓
         * Development first move
         *      ↓
         * Battle first move
         *
         * PlayerManager.StartingPlayer is responsible
         * for preserving that starting player.
         */
        if (gameStateManager != null)
        {
            GameState state =
                gameStateManager.CurrentState;

            if (state == GameState.Toss ||
                state == GameState.Paused ||
                state == GameState.Victory ||
                state == GameState.DrawResult ||
                state == GameState.DrawDetected)
            {
                StopAITurn();
                return;
            }
        }

        /*
         * -------------------------------------------------
         * ANIMATION SAFETY
         * -------------------------------------------------
         *
         * Never allow the AI to make another action while
         * a previous placement or movement animation is
         * still running.
         */
        if (IsActionAnimationRunning())
        {
            StopAITurn();
            return;
        }

        /*
         * If AI is not currently thinking, start a fresh
         * AI turn.
         */
        if (!isAITurn)
        {
            StartAITurn();
            return;
        }

        /*
         * Continue the thinking countdown.
         */
        if (thinking)
        {
            UpdateThinkingTimer();
        }
    }

    private bool IsActionAnimationRunning()
    {
        if (placementManager != null &&
            placementManager.IsPlacementAnimationInProgress)
        {
            return true;
        }

        if (movementManager != null &&
            movementManager.IsMovementAnimationInProgress)
        {
            return true;
        }

        return false;
    }

    private void StartAITurn()
    {
        /*
         * Double-check that the current player is really
         * the AI player before starting the timer.
         */
        if (playerManager == null)
            return;

        PlayerData currentPlayer =
            playerManager.GetPlayer(
                playerManager.CurrentPlayer
            );

        if (currentPlayer == null ||
            !currentPlayer.isAI)
        {
            StopAITurn();
            return;
        }

        /*
         * Never start an AI turn during Toss.
         */
        if (gameStateManager != null)
        {
            GameState state =
                gameStateManager.CurrentState;

            if (state == GameState.Toss ||
                state == GameState.Paused ||
                state == GameState.Victory ||
                state == GameState.DrawDetected ||
                state == GameState.DrawResult)
            {
                StopAITurn();
                return;
            }
        }

        /*
         * Never start while an animation is active.
         */
        if (IsActionAnimationRunning())
        {
            StopAITurn();
            return;
        }

        isAITurn = true;
        thinking = true;
        thinkingTimer = thinkingDelay;

        Debug.Log(
            "================================="
        );

        Debug.Log(
            "AI TURN DETECTED"
        );

        Debug.Log(
            $"AI Player: " +
            $"{playerManager.CurrentPlayer}"
        );

        Debug.Log(
            $"Difficulty: " +
            $"{GetCurrentDifficulty()}"
        );

        Debug.Log(
            $"Game Phase: " +
            $"{GetCurrentPhase()}"
        );

        Debug.Log(
            "AI is thinking..."
        );

        Debug.Log(
            "================================="
        );
    }

    private void StopAITurn()
    {
        isAITurn = false;
        thinking = false;
        thinkingTimer = 0f;
    }

    private void UpdateThinkingTimer()
    {
        /*
         * If something changed while the AI was thinking,
         * immediately stop this turn.
         */
        if (playerManager == null)
        {
            StopAITurn();
            return;
        }

        PlayerData currentPlayer =
            playerManager.GetPlayer(
                playerManager.CurrentPlayer
            );

        if (currentPlayer == null ||
            !currentPlayer.isAI)
        {
            StopAITurn();
            return;
        }

        if (gameStateManager != null)
        {
            GameState state =
                gameStateManager.CurrentState;

            if (state == GameState.Toss ||
                state == GameState.Paused ||
                state == GameState.Victory ||
                state == GameState.DrawDetected ||
                state == GameState.DrawResult)
            {
                StopAITurn();
                return;
            }
        }

        if (IsActionAnimationRunning())
        {
            StopAITurn();
            return;
        }

        thinkingTimer -= Time.deltaTime;

        if (thinkingTimer > 0f)
            return;

        thinking = false;

        Debug.Log(
            "AI thinking period complete."
        );

        PerformAIAction();
    }

    private void PerformAIAction()
    {
        if (playerManager == null)
            return;

        /*
         * Make sure the current player is actually an AI.
         */
        PlayerData currentPlayer =
            playerManager.GetPlayer(
                playerManager.CurrentPlayer
            );

        if (currentPlayer == null ||
            !currentPlayer.isAI)
        {
            StopAITurn();
            return;
        }

        /*
         * -------------------------------------------------
         * LAST CHANCE
         * -------------------------------------------------
         */
        if (IsLastChance())
        {
            PerformAILastChanceMovement();
            return;
        }

        /*
         * -------------------------------------------------
         * NORMAL PLACEMENT
         * -------------------------------------------------
         */
        if (placementManager != null &&
            !placementManager.PlacementComplete)
        {
            PerformAIPlacement();
            return;
        }

        /*
         * -------------------------------------------------
         * NORMAL MOVEMENT
         *
         * This covers both:
         *
         * DEVELOPMENT PHASE
         * BATTLE PHASE
         *
         * MovementManager / DevelopmentManager determine
         * which phase the game is currently in.
         * -------------------------------------------------
         */
        if (movementManager != null &&
            movementManager.MovementPhaseActive)
        {
            PerformAIMovement();
            return;
        }

        StopAITurn();
    }

    private void PerformAIPlacement()
    {
        if (aiPlacement == null)
        {
            Debug.LogError(
                "AIManager: AIPlacement reference is missing."
            );

            StopAITurn();
            return;
        }

        /*
         * Do not attempt placement if an animation is active.
         */
        if (IsActionAnimationRunning())
        {
            StopAITurn();
            return;
        }

        /*
         * The AI placement system itself currently controls
         * Player 2, so verify that the current player is
         * still the AI before requesting a placement.
         */
        PlayerData currentPlayer =
            playerManager.GetPlayer(
                playerManager.CurrentPlayer
            );

        if (currentPlayer == null ||
            !currentPlayer.isAI)
        {
            StopAITurn();
            return;
        }

        bool placementMade =
            aiPlacement.TryMakePlacement();

        if (placementMade)
        {
            Debug.Log(
                "AI placement completed."
            );

            /*
             * IMPORTANT:
             *
             * The placement animation now owns the turn
             * until it finishes.
             *
             * Do NOT restart the AI thinking timer here.
             *
             * PlacementManager will switch the turn after
             * the animation completes.
             *
             * If this placement was the final placement,
             * PlacementManager will restore StartingPlayer
             * before Development begins.
             */
            StopAITurn();
            return;
        }

        /*
         * If no placement was made, stop the current AI
         * turn. UpdateAITurnState() can safely start another
         * thinking cycle if the AI still owns the turn.
         */
        StopAITurn();
    }

    private void PerformAIMovement()
    {
        if (aiMovement == null)
        {
            Debug.LogError(
                "AIManager: AIMovement reference is missing."
            );

            StopAITurn();
            return;
        }

        /*
         * Do not attempt movement while another movement
         * animation is active.
         */
        if (IsActionAnimationRunning())
        {
            StopAITurn();
            return;
        }

        PlayerData currentPlayer =
            playerManager.GetPlayer(
                playerManager.CurrentPlayer
            );

        if (currentPlayer == null ||
            !currentPlayer.isAI)
        {
            StopAITurn();
            return;
        }

        bool moveMade =
            aiMovement.TryMakeMove();

        if (moveMade)
        {
            Debug.Log(
                "AI movement completed."
            );

            /*
             * IMPORTANT:
             *
             * MovementManager owns the turn transition.
             *
             * It will:
             *
             * 1. Finish the animation.
             * 2. Register development.
             * 3. Check victory.
             * 4. Check Battle block victory.
             * 5. Switch to the other player.
             *
             * If Development has just completed for both
             * players, MovementManager restores the Toss
             * winner before Battle begins.
             *
             * Therefore AIManager MUST NOT switch or
             * re-arm the AI timer here.
             */
            StopAITurn();
            return;
        }

        Debug.LogWarning(
            "AI could not make a movement."
        );

        /*
         * Stop this attempt.
         *
         * If the AI still owns the turn and the game is
         * still active, UpdateAITurnState() will start a
         * fresh thinking cycle.
         */
        StopAITurn();
    }

    private void PerformAILastChanceMovement()
    {
        if (aiMovement == null)
        {
            Debug.LogError(
                "AIManager: AIMovement reference is missing."
            );

            StopAITurn();
            return;
        }

        if (drawManager == null)
        {
            Debug.LogError(
                "AIManager: DrawManager reference is missing."
            );

            StopAITurn();
            return;
        }

        /*
         * Last Chance must remain active.
         */
        if (!drawManager.LastChanceActive)
        {
            Debug.Log(
                "AIManager: Last Chance is no longer active."
            );

            StopAITurn();
            return;
        }

        /*
         * Current AI is Player 2 in the current VsAI
         * configuration.
         */
        if (drawManager.Player2LastChanceMoves <= 0)
        {
            Debug.Log(
                "AIManager: Player 2 has no Last Chance moves remaining."
            );

            StopAITurn();
            return;
        }

        /*
         * Never move during an existing movement animation.
         */
        if (IsActionAnimationRunning())
        {
            StopAITurn();
            return;
        }

        Debug.Log(
            "===== AI LAST CHANCE MOVE ====="
        );

        Debug.Log(
            $"AI Last Chance moves remaining: " +
            $"{drawManager.Player2LastChanceMoves}"
        );

        bool moveMade =
            aiMovement.TryMakeMove();

        if (moveMade)
        {
            Debug.Log(
                "AI Last Chance movement completed."
            );

            /*
             * MovementManager controls what happens after
             * the animation:
             *
             * 1. Lock broken.
             * 2. Another Last Chance turn.
             * 3. Final Draw Result.
             * 4. Victory.
             *
             * Do not start another AI timer while the
             * movement animation is active.
             */
            StopAITurn();
            return;
        }

        Debug.LogWarning(
            "AI could not make a Last Chance movement."
        );

        StopAITurn();
    }

    private bool IsLastChance()
    {
        if (gameStateManager == null)
            return false;

        return
            gameStateManager.CurrentState ==
            GameState.LastChance;
    }

    private AIDifficulty GetCurrentDifficulty()
    {
        if (GameSession.Instance == null)
            return AIDifficulty.Easy;

        return
            GameSession.Instance.SelectedDifficulty;
    }

    private string GetCurrentPhase()
    {
        if (gameStateManager != null)
        {
            GameState state =
                gameStateManager.CurrentState;

            if (state == GameState.Toss)
                return "Toss";

            if (state == GameState.LastChance)
                return "Last Chance";

            if (state == GameState.Paused)
                return "Paused";

            if (state == GameState.Victory)
                return "Victory";

            if (state == GameState.DrawResult)
                return "Draw Result";

            if (state == GameState.DrawDetected)
                return "Draw Detected";
        }

        if (placementManager != null &&
            !placementManager.PlacementComplete)
        {
            return "Placement";
        }

        if (movementManager != null &&
            movementManager.MovementPhaseActive)
        {
            return "Movement";
        }

        return "Waiting";
    }

    public bool IsAIActive()
    {
        return IsAIGame();
    }

    public bool IsAITurn()
    {
        return isAITurn;
    }

    public bool IsAIThinking()
    {
        return thinking;
    }

    public AIDifficulty GetDifficulty()
    {
        return GetCurrentDifficulty();
    }

    public PlayerOwner GetAIPlayer()
    {
        if (!IsAIGame())
            return PlayerOwner.None;

        if (playerManager == null)
            return PlayerOwner.None;

        PlayerData player2 =
            playerManager.GetPlayer(
                PlayerOwner.Player2
            );

        if (player2 != null &&
            player2.isAI)
        {
            return PlayerOwner.Player2;
        }

        return PlayerOwner.None;
    }
}