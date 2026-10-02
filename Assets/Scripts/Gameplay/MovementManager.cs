using UnityEngine;

public class MovementManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private VictoryManager victoryManager;
    [SerializeField] private DrawManager drawManager;
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private DevelopmentManager developmentManager;

    [Header("Movement State")]
    [SerializeField] private bool movementPhaseActive = false;

    [Header("Selection Visual")]
    [SerializeField] private Color selectionColor =
        new Color(1f, 0.78f, 0.22f, 0.95f);

    [SerializeField] private float selectionScale = 1.35f;
    [SerializeField] private float selectionPulseAmount = 0.06f;
    [SerializeField] private float selectionPulseSpeed = 4f;
    [SerializeField] private int selectionTextureSize = 96;
    [SerializeField] private float selectionRingThickness = 0.10f;

    [Header("Selected Pawn Lift")]
    [SerializeField] private float selectionLiftHeight = 0.12f;
    [SerializeField] private float selectedPawnScale = 1.06f;
    [SerializeField] private float selectionLiftPulseAmount = 0.012f;
    [SerializeField] private float selectionLiftPulseSpeed = 3f;

    private Pawn selectedPawn;
    private int selectedNodeIndex = -1;
    private bool movementAnimationInProgress;

    private PlayerOwner pendingVictoryPlayer =
        PlayerOwner.None;

    private GameObject selectionIndicator;
    private Sprite selectionSprite;
    private Texture2D selectionTexture;

    private Vector3 selectedPawnOriginalLocalPosition;
    private Vector3 selectedPawnOriginalLocalScale;

    private bool selectionTransformStored;

    public bool MovementPhaseActive =>
        movementPhaseActive;

    public bool IsMovementAnimationInProgress =>
        movementAnimationInProgress;

    private void Awake()
    {
        FindReferences();
        CreateSelectionSprite();
    }

    private void Update()
    {
        if (selectionIndicator == null ||
            !selectionIndicator.activeSelf ||
            selectedPawn == null)
        {
            return;
        }

        float pulse =
            1f +
            Mathf.Sin(
                Time.unscaledTime *
                selectionPulseSpeed
            ) *
            selectionPulseAmount;

        float liftPulse =
            Mathf.Sin(
                Time.unscaledTime *
                selectionLiftPulseSpeed
            ) *
            selectionLiftPulseAmount;

        if (selectionTransformStored)
        {
            selectedPawn.transform.localPosition =
                selectedPawnOriginalLocalPosition +
                Vector3.up *
                (
                    selectionLiftHeight +
                    liftPulse
                );

            selectedPawn.transform.localScale =
                selectedPawnOriginalLocalScale *
                selectedPawnScale *
                pulse;
        }

        /*
         * Keep the selection ring visually
         * at the board level while the pawn
         * appears lifted above it.
         */
        selectionIndicator.transform.localPosition =
            Vector3.down *
            selectionLiftHeight;

        selectionIndicator.transform.localScale =
            Vector3.one *
            selectionScale *
            pulse;
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

        if (victoryManager == null)
        {
            victoryManager =
                FindFirstObjectByType<VictoryManager>();
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

        if (developmentManager == null)
        {
            developmentManager =
                FindFirstObjectByType<DevelopmentManager>();
        }
    }

    public void BeginMovementPhase()
    {
        BeginMovementPhase(true);
    }

    public void BeginMovementPhase(
        bool updateGameState)
    {
        movementPhaseActive = true;
        movementAnimationInProgress = false;

        ClearSelection();

        if (updateGameState &&
            gameStateManager != null)
        {
            gameStateManager.SetState(
                GameState.Movement
            );
        }

        Debug.Log(
            "===== MOVEMENT PHASE ====="
        );

        if (playerManager != null)
        {
            Debug.Log(
                $"Current Player: " +
                $"{playerManager.CurrentPlayer}"
            );
        }
    }

    public void StopMovement()
    {
        movementPhaseActive = false;
        movementAnimationInProgress = false;

        ClearSelection();

        Debug.Log(
            "Movement stopped."
        );
    }

    public void HandleNodeClicked(
        int nodeIndex)
    {
        if (!movementPhaseActive)
            return;

        if (movementAnimationInProgress)
        {
            Debug.Log(
                "Movement animation is still running."
            );

            return;
        }

        if (boardManager == null)
        {
            Debug.LogError(
                "MovementManager: " +
                "BoardManager reference is missing."
            );

            return;
        }

        if (playerManager == null)
        {
            Debug.LogError(
                "MovementManager: " +
                "PlayerManager reference is missing."
            );

            return;
        }

        BoardNode clickedNode =
            boardManager.GetNode(
                nodeIndex
            );

        if (clickedNode == null)
            return;

        if (selectedPawn == null)
        {
            TrySelectPawn(nodeIndex);
            return;
        }

        TryMovePawn(nodeIndex);
    }

    private void TrySelectPawn(
        int nodeIndex)
    {
        BoardNode node =
            boardManager.GetNode(
                nodeIndex
            );

        if (node == null)
            return;

        if (node.IsEmpty)
        {
            Debug.Log(
                "No pawn on this node."
            );

            return;
        }

        if (node.Owner !=
            playerManager.CurrentPlayer)
        {
            Debug.Log(
                "You can only select your own pawn."
            );

            return;
        }

        Pawn pawn =
            FindPawnAtNode(
                nodeIndex
            );

        if (pawn == null)
        {
            Debug.LogWarning(
                $"No Pawn object found at " +
                $"Node {nodeIndex}."
            );

            return;
        }

        /*
         * If another pawn was selected,
         * completely restore it first.
         */
        if (selectedPawn != null &&
            selectedPawn != pawn)
        {
            ClearSelection();
        }

        selectedPawn = pawn;
        selectedNodeIndex = nodeIndex;

        StoreSelectionTransform(
            selectedPawn
        );

        ShowSelectionVisual(
            selectedPawn
        );

        Debug.Log(
            $"{playerManager.CurrentPlayer} " +
            $"selected pawn at Node {nodeIndex}"
        );
    }

    private void TryMovePawn(
        int destinationNodeIndex)
    {
        if (selectedPawn == null)
            return;

        int fromNode =
            selectedNodeIndex;

        PlayerOwner movingPlayer =
            playerManager.CurrentPlayer;

        bool success =
            ExecuteMove(
                movingPlayer,
                fromNode,
                destinationNodeIndex,
                selectedPawn
            );

        if (!success)
            return;
    }

    public bool TryMakeAIMove(
        int fromNodeIndex,
        int destinationNodeIndex)
    {
        if (!movementPhaseActive)
        {
            Debug.Log(
                "MovementManager: " +
                "Movement phase is inactive."
            );

            return false;
        }

        if (movementAnimationInProgress)
        {
            Debug.Log(
                "MovementManager: " +
                "Animation is still running."
            );

            return false;
        }

        if (playerManager == null ||
            boardManager == null)
        {
            Debug.LogError(
                "MovementManager: " +
                "Required reference is missing."
            );

            return false;
        }

        if (playerManager.CurrentPlayer !=
            PlayerOwner.Player2)
        {
            Debug.Log(
                "MovementManager: " +
                "It is not Player 2's turn."
            );

            return false;
        }

        BoardNode fromNode =
            boardManager.GetNode(
                fromNodeIndex
            );

        BoardNode destinationNode =
            boardManager.GetNode(
                destinationNodeIndex
            );

        if (fromNode == null ||
            destinationNode == null)
        {
            return false;
        }

        if (fromNode.IsEmpty)
        {
            Debug.Log(
                "AI source node is empty."
            );

            return false;
        }

        if (fromNode.Owner !=
            PlayerOwner.Player2)
        {
            Debug.Log(
                "AI can only move its own pawn."
            );

            return false;
        }

        if (!destinationNode.IsEmpty)
        {
            Debug.Log(
                "AI destination is occupied."
            );

            return false;
        }

        if (!boardManager.AreConnected(
                fromNodeIndex,
                destinationNodeIndex))
        {
            Debug.Log(
                $"AI cannot move from " +
                $"{fromNodeIndex} to " +
                $"{destinationNodeIndex}."
            );

            return false;
        }

        Pawn aiPawn =
            FindPawnAtNode(
                fromNodeIndex
            );

        if (aiPawn == null)
        {
            Debug.LogWarning(
                $"No AI Pawn found at " +
                $"Node {fromNodeIndex}."
            );

            return false;
        }

        return ExecuteMove(
            PlayerOwner.Player2,
            fromNodeIndex,
            destinationNodeIndex,
            aiPawn
        );
    }

    private bool ExecuteMove(
        PlayerOwner movingPlayer,
        int fromNodeIndex,
        int destinationNodeIndex,
        Pawn pawn)
    {
        if (movementAnimationInProgress)
            return false;

        if (pawn == null)
        {
            Debug.LogError(
                "MovementManager: " +
                "Cannot execute movement " +
                "with a null pawn."
            );

            return false;
        }

        BoardNode destinationNode =
            boardManager.GetNode(
                destinationNodeIndex
            );

        if (destinationNode == null)
            return false;

        if (destinationNodeIndex ==
            fromNodeIndex)
        {
            Debug.Log(
                "Pawn cannot move to " +
                "the same node."
            );

            ClearSelection();

            return false;
        }

        if (!destinationNode.IsEmpty)
        {
            Debug.Log(
                "Destination node is occupied."
            );

            return false;
        }

        if (!boardManager.AreConnected(
                fromNodeIndex,
                destinationNodeIndex))
        {
            Debug.Log(
                $"Node {fromNodeIndex} " +
                $"is not connected to " +
                $"Node {destinationNodeIndex}."
            );

            return false;
        }

        /*
         * Development and Battle use different
         * block rules.
         *
         * DEVELOPMENT PHASE:
         * A movement that leaves the opponent
         * with no legal movement is forbidden.
         *
         * BATTLE PHASE:
         * The same movement is a winning block.
         *
         * This phase check happens BEFORE this
         * movement is registered.
         */

        bool battlePhase =
            developmentManager != null &&
            developmentManager
                .AreBothPlayersFullyDeveloped();

        bool createsBlock =
            WouldCreateBlock(
                fromNodeIndex,
                destinationNodeIndex,
                movingPlayer
            );

        if (createsBlock &&
            !battlePhase)
        {
            Debug.Log(
                $"{movingPlayer} movement rejected: " +
                "it would completely block the opponent " +
                "before Battle Phase."
            );

            ClearSelection();

            PlayInvalidMoveFeedback();

            return false;
        }

        bool wasAlreadyWinEligible =
            battlePhase &&
            developmentManager != null &&
            developmentManager
                .HasCompletedDevelopment(
                    movingPlayer
                );

        BoardNode oldNode =
            boardManager.GetNode(
                fromNodeIndex
            );

        if (oldNode != null)
        {
            oldNode.ClearOwner();
        }

        destinationNode.SetOwner(
            movingPlayer
        );

        pawn.SetNode(
            destinationNodeIndex
        );

        /*
         * Register this pawn as developed
         * after its movement has been accepted.
         */

        if (developmentManager != null)
        {
            developmentManager.RegisterPawnMoved(
                movingPlayer,
                pawn
            );
        }
        else
        {
            Debug.LogWarning(
                "MovementManager: " +
                "DevelopmentManager is missing. " +
                "Development tracking cannot occur."
            );
        }

        /*
         * Restore the pawn before its
         * movement animation begins.
         */
        ClearSelection();

        movementAnimationInProgress = true;

        Debug.Log(
            $"{movingPlayer} moving pawn from " +
            $"Node {fromNodeIndex} to " +
            $"Node {destinationNodeIndex}"
        );

        /*
         * Victory is only evaluated if the player
         * had already completed development
         * BEFORE this movement.
         */

        bool lineVictory =
            wasAlreadyWinEligible &&
            CheckVictory(
                movingPlayer
            );

        bool blockVictory =
            battlePhase &&
            createsBlock;

        bool victory =
            lineVictory ||
            blockVictory;

        if (victory)
        {
            pendingVictoryPlayer =
                movingPlayer;

            if (lineVictory)
            {
                Debug.Log(
                    $"{movingPlayer} is eligible " +
                    "for victory and has formed " +
                    "a winning line."
                );
            }

            if (blockVictory)
            {
                Debug.Log(
                    $"{movingPlayer} wins by blocking " +
                    "the opponent from all legal movement."
                );
            }
        }
        else
        {
            pendingVictoryPlayer =
                PlayerOwner.None;
        }

        pawn.PlayMovementAnimation(
            destinationNode.transform.position,
            () =>
            {
                FinishMovementAnimation(
                    movingPlayer
                );
            }
        );

        return true;
    }

    private void FinishMovementAnimation(
        PlayerOwner movingPlayer)
    {
        movementAnimationInProgress = false;

        Debug.Log(
            $"Movement animation finished " +
            $"for {movingPlayer}."
        );

        if (pendingVictoryPlayer !=
            PlayerOwner.None)
        {
            PlayerOwner winner =
                pendingVictoryPlayer;

            pendingVictoryPlayer =
                PlayerOwner.None;

            movementPhaseActive = false;

            ClearSelection();

            if (gameStateManager != null)
            {
                gameStateManager.SetState(
                    GameState.Victory
                );
            }

            if (victoryManager != null)
            {
                victoryManager.ShowVictory(
                    winner
                );
            }
            else
            {
                Debug.LogWarning(
                    "VictoryManager reference " +
                    "is missing."
                );
            }

            return;
        }

        playerManager.SwitchTurn();

        Debug.Log(
            $"Next Player: " +
            $"{playerManager.CurrentPlayer}"
        );

        if (drawManager != null)
        {
            drawManager.EvaluateAfterMove(
                movingPlayer
            );
        }
    }

    private Pawn FindPawnAtNode(
        int nodeIndex)
    {
        Pawn[] pawns =
            FindObjectsByType<Pawn>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (Pawn pawn in pawns)
        {
            if (pawn.CurrentNodeIndex ==
                nodeIndex)
            {
                return pawn;
            }
        }

        return null;
    }

    private bool WouldCreateBlock(
        int fromNodeIndex,
        int destinationNodeIndex,
        PlayerOwner movingPlayer)
    {
        PlayerOwner opponent =
            GetOpponent(
                movingPlayer
            );

        if (opponent == PlayerOwner.None)
            return false;

        int[] simulatedBoard =
            ReadBoardOwners();

        simulatedBoard[fromNodeIndex] = 0;

        simulatedBoard[destinationNodeIndex] =
            GetPlayerCode(
                movingPlayer
            );

        return !HasAnyLegalMove(
            simulatedBoard,
            opponent
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
            AudioManager.Instance
                .PlayInvalidMove();
        }

        if (VibrationManager.Instance != null)
        {
            VibrationManager.Instance
                .VibrateInvalidMove();
        }
    }

    private bool CheckVictory(
        PlayerOwner player)
    {
        if (IsWinningLine(
                player,
                0,
                1,
                2))
        {
            return true;
        }

        if (IsWinningLine(
                player,
                3,
                4,
                5))
        {
            return true;
        }

        if (IsWinningLine(
                player,
                6,
                7,
                8))
        {
            return true;
        }

        if (IsWinningLine(
                player,
                0,
                3,
                6))
        {
            return true;
        }

        if (IsWinningLine(
                player,
                1,
                4,
                7))
        {
            return true;
        }

        if (IsWinningLine(
                player,
                2,
                5,
                8))
        {
            return true;
        }

        if (IsWinningLine(
                player,
                0,
                4,
                8))
        {
            return true;
        }

        if (IsWinningLine(
                player,
                2,
                4,
                6))
        {
            return true;
        }

        return false;
    }

    private bool IsWinningLine(
        PlayerOwner player,
        int nodeA,
        int nodeB,
        int nodeC)
    {
        bool winningLine =
            boardManager.GetOwner(nodeA) ==
                player &&
            boardManager.GetOwner(nodeB) ==
                player &&
            boardManager.GetOwner(nodeC) ==
                player;

        if (winningLine)
        {
            Debug.Log(
                $"Winning line found: " +
                $"{nodeA} - " +
                $"{nodeB} - " +
                $"{nodeC}"
            );
        }

        return winningLine;
    }

    public void ClearSelection()
    {
        ClearSelectionVisual();
        RestoreSelectionTransform();

        selectedPawn = null;
        selectedNodeIndex = -1;
    }

    private void StoreSelectionTransform(
        Pawn pawn)
    {
        if (pawn == null)
            return;

        selectedPawnOriginalLocalPosition =
            pawn.transform.localPosition;

        selectedPawnOriginalLocalScale =
            pawn.transform.localScale;

        selectionTransformStored = true;
    }

    private void RestoreSelectionTransform()
    {
        if (!selectionTransformStored ||
            selectedPawn == null)
        {
            selectionTransformStored = false;
            return;
        }

        selectedPawn.transform.localPosition =
            selectedPawnOriginalLocalPosition;

        selectedPawn.transform.localScale =
            selectedPawnOriginalLocalScale;

        selectionTransformStored = false;
    }

    private void CreateSelectionSprite()
    {
        int size =
            Mathf.Clamp(
                selectionTextureSize,
                32,
                256
            );

        float thickness =
            Mathf.Clamp01(
                selectionRingThickness
            );

        selectionTexture =
            new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false
            );

        selectionTexture.name =
            "Nera_RuntimeSelectionRing";

        selectionTexture.filterMode =
            FilterMode.Bilinear;

        selectionTexture.wrapMode =
            TextureWrapMode.Clamp;

        Vector2 center =
            new Vector2(
                (size - 1) * 0.5f,
                (size - 1) * 0.5f
            );

        float outerRadius =
            (size - 2f) * 0.5f;

        float innerRadius =
            outerRadius *
            (1f - thickness);

        for (int y = 0;
            y < size;
            y++)
        {
            for (int x = 0;
                x < size;
                x++)
            {
                float distance =
                    Vector2.Distance(
                        new Vector2(
                            x,
                            y
                        ),
                        center
                    );

                float outerFade =
                    Mathf.Clamp01(
                        (
                            outerRadius -
                            distance
                        ) *
                        2.5f
                    );

                float innerFade =
                    Mathf.Clamp01(
                        (
                            distance -
                            innerRadius
                        ) *
                        2.5f
                    );

                float alpha =
                    outerFade *
                    innerFade;

                Color pixel =
                    new Color(
                        1f,
                        1f,
                        1f,
                        alpha
                    );

                selectionTexture.SetPixel(
                    x,
                    y,
                    pixel
                );
            }
        }

        selectionTexture.Apply();

        selectionSprite =
            Sprite.Create(
                selectionTexture,
                new Rect(
                    0,
                    0,
                    size,
                    size
                ),
                new Vector2(
                    0.5f,
                    0.5f
                ),
                size
            );

        selectionSprite.name =
            "Nera_RuntimeSelectionRingSprite";
    }

    private void ShowSelectionVisual(
        Pawn pawn)
    {
        if (pawn == null ||
            selectionSprite == null)
        {
            return;
        }

        if (!selectionTransformStored)
        {
            StoreSelectionTransform(
                pawn
            );
        }

        /*
         * Raise the actual pawn above
         * its original board position.
         */
        pawn.transform.localPosition =
            selectedPawnOriginalLocalPosition +
            Vector3.up *
            selectionLiftHeight;

        /*
         * Slightly enlarge the pawn so
         * the selection feels physical.
         */
        pawn.transform.localScale =
            selectedPawnOriginalLocalScale *
            selectedPawnScale;

        if (selectionIndicator == null)
        {
            selectionIndicator =
                new GameObject(
                    "SelectionIndicator"
                );

            SpriteRenderer renderer =
                selectionIndicator
                    .AddComponent<SpriteRenderer>();

            renderer.sprite =
                selectionSprite;

            renderer.color =
                selectionColor;

            selectionIndicator.transform.SetParent(
                pawn.transform,
                false
            );

            /*
             * Move the ring downward so it
             * visually stays on the board.
             */
            selectionIndicator
                .transform.localPosition =
                Vector3.down *
                selectionLiftHeight;

            selectionIndicator
                .transform.localRotation =
                Quaternion.identity;

            selectionIndicator
                .transform.localScale =
                Vector3.one *
                selectionScale;

            SetSelectionSortingOrder(
                pawn,
                renderer
            );
        }
        else
        {
            selectionIndicator.transform.SetParent(
                pawn.transform,
                false
            );

            selectionIndicator
                .transform.localPosition =
                Vector3.down *
                selectionLiftHeight;

            selectionIndicator
                .transform.localRotation =
                Quaternion.identity;

            selectionIndicator.SetActive(
                true
            );

            SpriteRenderer renderer =
                selectionIndicator
                    .GetComponent<SpriteRenderer>();

            if (renderer != null)
            {
                renderer.color =
                    selectionColor;

                renderer.sprite =
                    selectionSprite;

                SetSelectionSortingOrder(
                    pawn,
                    renderer
                );
            }
        }
    }

    private void SetSelectionSortingOrder(
        Pawn pawn,
        SpriteRenderer selectionRenderer)
    {
        if (pawn == null ||
            selectionRenderer == null)
        {
            return;
        }

        SpriteRenderer[] pawnRenderers =
            pawn.GetComponentsInChildren<SpriteRenderer>(
                true
            );

        int highestOrder =
            int.MinValue;

        string sortingLayerName =
            selectionRenderer.sortingLayerName;

        foreach (
            SpriteRenderer pawnRenderer
            in pawnRenderers)
        {
            if (pawnRenderer ==
                selectionRenderer)
            {
                continue;
            }

            if (pawnRenderer.sortingOrder >=
                highestOrder)
            {
                highestOrder =
                    pawnRenderer.sortingOrder;

                sortingLayerName =
                    pawnRenderer.sortingLayerName;
            }
        }

        if (highestOrder ==
            int.MinValue)
        {
            highestOrder = 0;
        }

        selectionRenderer.sortingLayerName =
            sortingLayerName;

        selectionRenderer.sortingOrder =
            highestOrder - 1;
    }

    private void ClearSelectionVisual()
    {
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(
                false
            );
        }
    }

    private void OnDestroy()
    {
        if (selectionSprite != null)
        {
            Destroy(
                selectionSprite
            );
        }

        if (selectionTexture != null)
        {
            Destroy(
                selectionTexture
            );
        }
    }

    public Pawn GetSelectedPawn()
    {
        return selectedPawn;
    }

    public int GetSelectedNodeIndex()
    {
        return selectedNodeIndex;
    }
}