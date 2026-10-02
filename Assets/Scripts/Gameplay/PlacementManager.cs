using UnityEngine;

public class PlacementManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private MovementManager movementManager;
    [SerializeField] private DrawManager drawManager;
    [SerializeField] private GameStateManager gameStateManager;

    [Header("Pawn")]
    [SerializeField] private Pawn pawnPrefab;

    [Header("Pawn Parent")]
    [SerializeField] private Transform pawnParent;

    private bool placementComplete;
    private bool placementAnimationInProgress;

    public bool PlacementComplete =>
        placementComplete;

    public bool IsPlacementAnimationInProgress =>
        placementAnimationInProgress;

    private void Start()
    {
        placementComplete = false;
        placementAnimationInProgress = false;

        FindReferences();

        if (playerManager == null)
        {
            Debug.LogError(
                "PlacementManager: PlayerManager reference is missing!"
            );

            return;
        }

        if (boardManager == null)
        {
            Debug.LogError(
                "PlacementManager: BoardManager reference is missing!"
            );

            return;
        }

        if (movementManager == null)
        {
            Debug.LogError(
                "PlacementManager: MovementManager reference is missing!"
            );

            return;
        }

        if (pawnPrefab == null)
        {
            Debug.LogError(
                "PlacementManager: Pawn Prefab reference is missing!"
            );

            return;
        }

        if (pawnParent == null)
        {
            Debug.LogError(
                "PlacementManager: Pawn Parent reference is missing!"
            );

            return;
        }

        /*
         * Initialize the players here.
         *
         * IMPORTANT:
         * Do NOT start Placement here.
         *
         * TossManager decides the StartingPlayer
         * and then calls BeginPlacementPhase().
         */
        playerManager.InitializePlayers();

        Debug.Log(
            "PlacementManager initialized. " +
            "Waiting for toss result."
        );
    }

    private void FindReferences()
    {
        if (boardManager == null)
        {
            boardManager =
                FindFirstObjectByType<BoardManager>();
        }

        if (playerManager == null)
        {
            playerManager =
                FindFirstObjectByType<PlayerManager>();
        }

        if (movementManager == null)
        {
            movementManager =
                FindFirstObjectByType<MovementManager>();
        }

        if (drawManager == null)
        {
            drawManager =
                FindFirstObjectByType<DrawManager>();
        }

        if (gameStateManager == null)
        {
            gameStateManager =
                FindFirstObjectByType<GameStateManager>();
        }
    }

    public void BeginPlacementPhase()
    {
        if (playerManager == null)
        {
            Debug.LogError(
                "PlacementManager: " +
                "PlayerManager is missing."
            );

            return;
        }

        /*
         * Toss must already have selected a starter.
         */
        if (playerManager.StartingPlayer ==
            PlayerOwner.None)
        {
            Debug.LogError(
                "PlacementManager: Cannot begin Placement " +
                "because the toss winner has not been assigned."
            );

            return;
        }

        placementComplete = false;
        placementAnimationInProgress = false;

        /*
         * Placement is the first phase of the game,
         * so the toss winner starts Placement.
         */
        playerManager.RestoreStartingPlayer();

        Debug.Log(
            "================================="
        );

        Debug.Log(
            "===== PLACEMENT PHASE STARTED ====="
        );

        Debug.Log(
            "Toss Winner / Placement First Player: " +
            $"{playerManager.StartingPlayer}"
        );

        Debug.Log(
            "Current Player: " +
            $"{playerManager.CurrentPlayer}"
        );

        Debug.Log(
            "================================="
        );

        if (gameStateManager != null)
        {
            gameStateManager.SetState(
                GameState.Placement
            );
        }
    }

    public void TryPlacePawn(
        int nodeIndex)
    {
        if (placementComplete)
        {
            Debug.Log(
                "Placement phase is already complete."
            );

            PlayInvalidMoveFeedback();
            return;
        }

        if (placementAnimationInProgress)
        {
            Debug.Log(
                "Placement animation is still running."
            );

            PlayInvalidMoveFeedback();
            return;
        }

        if (playerManager == null)
        {
            PlayInvalidMoveFeedback();
            return;
        }

        if (boardManager == null)
        {
            PlayInvalidMoveFeedback();
            return;
        }

        /*
         * Placement input is only valid while
         * Placement state is active.
         */
        if (gameStateManager != null &&
            gameStateManager.CurrentState !=
            GameState.Placement)
        {
            Debug.Log(
                "PlacementManager: " +
                "Placement input ignored because " +
                "Placement Phase is not active."
            );

            PlayInvalidMoveFeedback();
            return;
        }

        PlayerOwner currentPlayer =
            playerManager.CurrentPlayer;

        if (currentPlayer == PlayerOwner.None)
        {
            Debug.LogError(
                "Current Player is None!"
            );

            PlayInvalidMoveFeedback();
            return;
        }

        PlayerData player =
            playerManager.GetPlayer(
                currentPlayer
            );

        if (player == null)
        {
            Debug.LogError(
                "PlayerData could not be found!"
            );

            PlayInvalidMoveFeedback();
            return;
        }

        if (!player.HasPawnsToPlace())
        {
            Debug.Log(
                $"{currentPlayer} has no pawns left to place."
            );

            PlayInvalidMoveFeedback();
            return;
        }

        BoardNode node =
            boardManager.GetNode(
                nodeIndex
            );

        if (node == null)
        {
            PlayInvalidMoveFeedback();
            return;
        }

        if (!node.IsEmpty)
        {
            Debug.Log(
                "Node is already occupied."
            );

            PlayInvalidMoveFeedback();
            return;
        }

        /*
         * Placement cannot create a complete movement block
         * before Battle Phase.
         */
        if (WouldCreatePlacementBlock(
                nodeIndex,
                currentPlayer))
        {
            Debug.Log(
                $"Placement rejected: Node {nodeIndex} " +
                "would completely block the opponent " +
                "before Battle Phase."
            );

            PlayInvalidMoveFeedback();
            return;
        }

        Pawn newPawn =
            Instantiate(
                pawnPrefab,
                node.transform.position,
                Quaternion.identity,
                pawnParent
            );

        newPawn.Initialize(
            currentPlayer
        );

        newPawn.SetNode(
            nodeIndex
        );

        node.SetOwner(
            currentPlayer
        );

        player.RegisterPawnPlacement();

        /*
         * Successful placement = sound only.
         */
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPawnPlacement();
        }

        Debug.Log(
            $"{currentPlayer} placed pawn on Node " +
            $"{nodeIndex}"
        );

        Debug.Log(
            $"{currentPlayer} remaining pawns: " +
            $"{player.remainingPawns}"
        );

        placementAnimationInProgress = true;

        newPawn.PlayPlacementAnimation(
            node.transform.position,
            () =>
            {
                FinishPlacementAnimation(
                    currentPlayer
                );
            }
        );
    }

    private void FinishPlacementAnimation(
        PlayerOwner placingPlayer)
    {
        placementAnimationInProgress = false;

        Debug.Log(
            $"Placement animation finished for " +
            $"{placingPlayer}."
        );

        PlayerData player =
            playerManager.GetPlayer(
                placingPlayer
            );

        if (player == null)
            return;

        /*
         * Check whether both players have placed
         * all three pawns.
         */
        if (!player.HasPawnsToPlace())
        {
            PlayerOwner otherPlayerOwner =
                placingPlayer ==
                PlayerOwner.Player1
                    ? PlayerOwner.Player2
                    : PlayerOwner.Player1;

            PlayerData otherPlayer =
                playerManager.GetPlayer(
                    otherPlayerOwner
                );

            if (otherPlayer != null &&
                !otherPlayer.HasPawnsToPlace())
            {
                CompletePlacementPhase();
                return;
            }
        }

        /*
         * Placement still has moves remaining.
         *
         * Every successful move switches to the
         * opposite player.
         */
        playerManager.SwitchTurn();

        Debug.Log(
            "Next Placement Player: " +
            $"{playerManager.CurrentPlayer}"
        );
    }

    private void CompletePlacementPhase()
    {
        placementComplete = true;

        Debug.Log(
            "================================="
        );

        Debug.Log(
            "===== PLACEMENT COMPLETE ====="
        );

        /*
         * IMPORTANT:
         *
         * Do NOT restore StartingPlayer here.
         *
         * The final placement already belongs to the
         * current player. The next Development move must
         * belong to the opposite player.
         *
         * This preserves the continuous sequence:
         *
         * 2 → 1 → 2 → 1 → 2 → 1
         *
         * and then:
         *
         * 2 → 1 → 2 → 1...
         */
        playerManager.SwitchTurn();

        Debug.Log(
            "Development First Player: " +
            $"{playerManager.CurrentPlayer}"
        );

        Debug.Log(
            "================================="
        );

        if (gameStateManager != null)
        {
            gameStateManager.SetState(
                GameState.Movement
            );
        }

        if (drawManager != null)
        {
            drawManager.RegisterInitialBoardState();
        }

        movementManager.BeginMovementPhase();
    }

    private bool WouldCreatePlacementBlock(
        int nodeIndex,
        PlayerOwner placingPlayer)
    {
        int[] simulatedBoard =
            ReadBoardOwners();

        if (simulatedBoard[nodeIndex] != 0)
            return false;

        simulatedBoard[nodeIndex] =
            GetPlayerCode(
                placingPlayer
            );

        int player1Count = 0;
        int player2Count = 0;

        for (int i = 0;
             i < simulatedBoard.Length;
             i++)
        {
            if (simulatedBoard[i] == 1)
            {
                player1Count++;
            }
            else if (simulatedBoard[i] == 2)
            {
                player2Count++;
            }
        }

        /*
         * A complete block can only exist after both
         * players have all three pawns.
         */
        if (player1Count < 3 ||
            player2Count < 3)
        {
            return false;
        }

        return !HasAnyLegalMove(
            simulatedBoard,
            GetOpponent(placingPlayer)
        );
    }

    private bool HasAnyLegalMove(
        int[] board,
        PlayerOwner player)
    {
        int playerCode =
            GetPlayerCode(
                player
            );

        if (playerCode == 0)
            return false;

        for (int from = 0;
             from < 9;
             from++)
        {
            if (board[from] != playerCode)
                continue;

            for (int to = 0;
                 to < 9;
                 to++)
            {
                if (board[to] != 0)
                    continue;

                if (boardManager.AreConnected(
                        from,
                        to))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private int[] ReadBoardOwners()
    {
        int[] board =
            new int[9];

        for (int i = 0;
             i < 9;
             i++)
        {
            board[i] =
                GetPlayerCode(
                    boardManager.GetOwner(i)
                );
        }

        return board;
    }

    private int GetPlayerCode(
        PlayerOwner player)
    {
        if (player ==
            PlayerOwner.Player1)
        {
            return 1;
        }

        if (player ==
            PlayerOwner.Player2)
        {
            return 2;
        }

        return 0;
    }

    private PlayerOwner GetOpponent(
        PlayerOwner player)
    {
        if (player ==
            PlayerOwner.Player1)
        {
            return PlayerOwner.Player2;
        }

        if (player ==
            PlayerOwner.Player2)
        {
            return PlayerOwner.Player1;
        }

        return PlayerOwner.None;
    }

    private void PlayInvalidMoveFeedback()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayInvalidMove();
        }

        if (VibrationManager.Instance != null)
        {
            VibrationManager.Instance.VibrateInvalidMove();
        }
    }
}