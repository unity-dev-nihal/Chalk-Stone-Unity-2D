using System;
using System.Collections.Generic;
using UnityEngine;

public class AIBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private DevelopmentManager developmentManager;

    [Header("Search Settings")]
    [SerializeField] private int easySearchDepth = 6;
    [SerializeField] private int mediumSearchDepth = 12;
    [SerializeField] private int hardSearchDepth = 20;

    [Header("Hard AI")]
    [SerializeField] private bool hardUsesExactSearch = true;

    [Header("Easy AI Imperfection")]
    [Range(0f, 1f)]
    [SerializeField] private float easyMistakeChance = 0.02f;

    private const int EMPTY = 0;
    private const int PLAYER1 = 1;
    private const int PLAYER2 = 2;

    private const int WIN_SCORE = 1000000;
    private const int DRAW_SCORE = 0;

    private readonly int[][] winningLines =
    {
        new[] { 0, 1, 2 },
        new[] { 3, 4, 5 },
        new[] { 6, 7, 8 },

        new[] { 0, 3, 6 },
        new[] { 1, 4, 7 },
        new[] { 2, 5, 8 },

        new[] { 0, 4, 8 },
        new[] { 2, 4, 6 }
    };

    /*
     * Nera movement graph.
     *
     * This must remain synchronized with BoardManager.
     */
    private readonly int[][] connections =
    {
        new[] { 1, 3, 4 },
        new[] { 0, 2, 4 },
        new[] { 1, 4, 5 },

        new[] { 0, 4, 6 },

        new[] { 0, 1, 2, 3, 5, 6, 7, 8 },

        new[] { 2, 4, 8 },

        new[] { 3, 4, 7 },

        new[] { 4, 6, 8 },

        new[] { 4, 5, 7 }
    };

    private readonly Dictionary<int, ExactOutcome> exactOutcomeTable =
        new Dictionary<int, ExactOutcome>();

    private bool exactSolverBuilt;

    private void Awake()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (boardManager == null)
        {
            boardManager =
                FindFirstObjectByType<BoardManager>();
        }

        if (developmentManager == null)
        {
            developmentManager =
                FindFirstObjectByType<DevelopmentManager>();
        }
    }

    private void OnValidate()
    {
        easySearchDepth =
            Mathf.Max(
                6,
                easySearchDepth
            );

        mediumSearchDepth =
            Mathf.Max(
                12,
                mediumSearchDepth
            );

        hardSearchDepth =
            Mathf.Max(
                20,
                hardSearchDepth
            );

        easyMistakeChance =
            Mathf.Clamp01(
                Mathf.Min(
                    0.02f,
                    easyMistakeChance
                )
            );
    }

    // =========================================================
    // PLACEMENT
    // =========================================================

    public int ChoosePlacementNode(
        AIDifficulty difficulty)
    {
        if (boardManager == null)
            return -1;

        List<int> availableNodes =
            GetAvailableNodes();

        if (availableNodes.Count == 0)
            return -1;

        /*
         * Placement cannot directly win,
         * but immediate tactical positioning
         * still matters.
         */
        int criticalBlock =
            FindCriticalBlockingPlacement(
                availableNodes
            );

        if (criticalBlock != -1)
        {
            Debug.Log(
                $"AI [{difficulty}] blocked placement threat " +
                $"at Node {criticalBlock}."
            );

            return criticalBlock;
        }

        int depth;

        switch (difficulty)
        {
            case AIDifficulty.Medium:
                depth = 5;
                break;

            case AIDifficulty.Hard:
                depth = 6;
                break;

            default:
                depth = 4;
                break;
        }

        return ChoosePlacementSearch(
            availableNodes,
            depth,
            difficulty == AIDifficulty.Hard
        );
    }

    private int ChoosePlacementSearch(
        List<int> availableNodes,
        int depth,
        bool hardMode)
    {
        int[] board =
            ReadCurrentBoard();

        int bestNode = -1;
        int bestScore = int.MinValue;

        foreach (int node in availableNodes)
        {
            if (!IsPlacementAllowedByBlockRule(
                    board,
                    node,
                    PLAYER2))
            {
                continue;
            }

            int[] nextBoard =
                board.Clone() as int[];

            nextBoard[node] =
                PLAYER2;

            int score =
                EvaluatePlacementBoard(
                    nextBoard
                );

            if (depth > 1)
            {
                score +=
                    PlacementSearch(
                        nextBoard,
                        PLAYER1,
                        depth - 1,
                        int.MinValue + 1,
                        int.MaxValue - 1,
                        hardMode
                    );
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestNode = node;
            }
        }

        return bestNode;
    }

    private int PlacementSearch(
        int[] board,
        int currentPlayer,
        int depth,
        int alpha,
        int beta,
        bool hardMode)
    {
        List<int> available =
            GetAvailableNodes(
                board
            );

        if (available.Count == 0 ||
            depth <= 0)
        {
            return EvaluatePlacementBoard(
                board
            );
        }

        bool maximizing =
            currentPlayer == PLAYER2;

        int best =
            maximizing
                ? int.MinValue
                : int.MaxValue;

        bool foundLegalPlacement = false;

        foreach (int node in available)
        {
            if (!IsPlacementAllowedByBlockRule(
                    board,
                    node,
                    currentPlayer))
            {
                continue;
            }

            foundLegalPlacement = true;

            int[] next =
                board.Clone() as int[];

            next[node] =
                currentPlayer;

            int score =
                PlacementSearch(
                    next,
                    currentPlayer == PLAYER2
                        ? PLAYER1
                        : PLAYER2,
                    depth - 1,
                    alpha,
                    beta,
                    hardMode
                );

            if (maximizing)
            {
                best =
                    Mathf.Max(
                        best,
                        score
                    );

                alpha =
                    Mathf.Max(
                        alpha,
                        best
                    );
            }
            else
            {
                best =
                    Mathf.Min(
                        best,
                        score
                    );

                beta =
                    Mathf.Min(
                        beta,
                        best
                    );
            }

            if (beta <= alpha)
                break;
        }

        if (!foundLegalPlacement)
        {
            return EvaluatePlacementBoard(
                board
            );
        }

        return best;
    }

    private int EvaluatePlacementBoard(
        int[] board)
    {
        int score = 0;

        /*
         * Center.
         */
        if (board[4] == PLAYER2)
            score += 150;

        if (board[4] == PLAYER1)
            score -= 150;

        /*
         * Corners.
         */
        int[] corners =
        {
            0, 2, 6, 8
        };

        foreach (int node in corners)
        {
            if (board[node] == PLAYER2)
                score += 60;

            if (board[node] == PLAYER1)
                score -= 60;
        }

        /*
         * Potential lines.
         */
        score +=
            CountPotentialLines(
                board,
                PLAYER2
            ) * 180;

        score -=
            CountPotentialLines(
                board,
                PLAYER1
            ) * 210;

        /*
         * Mobility potential.
         */
        score +=
            CountAvailableConnections(
                board,
                PLAYER2
            ) * 8;

        score -=
            CountAvailableConnections(
                board,
                PLAYER1
            ) * 8;

        return score;
    }

    private int CountPotentialLines(
        int[] board,
        int player)
    {
        int count = 0;

        foreach (int[] line in winningLines)
        {
            bool blocked =
                false;

            int pieces = 0;

            for (int i = 0; i < 3; i++)
            {
                int value =
                    board[line[i]];

                if (value != EMPTY &&
                    value != player)
                {
                    blocked = true;
                    break;
                }

                if (value == player)
                    pieces++;
            }

            if (!blocked &&
                pieces >= 2)
            {
                count++;
            }
        }

        return count;
    }

    private int CountAvailableConnections(
        int[] board,
        int player)
    {
        int count = 0;

        for (int node = 0;
             node < 9;
             node++)
        {
            if (board[node] != player)
                continue;

            foreach (int target in connections[node])
            {
                if (board[target] == EMPTY)
                    count++;
            }
        }

        return count;
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    public AIMove ChooseMovement(
        AIDifficulty difficulty)
    {
        if (boardManager == null)
            return AIMove.Invalid();

        List<AIMove> legalMoves =
            GetLegalMoves(
                ReadCurrentBoard(),
                PLAYER2
            );

        if (legalMoves.Count == 0)
            return AIMove.Invalid();

        bool aiDeveloped =
            developmentManager != null &&
            developmentManager.HasCompletedDevelopment(
                PlayerOwner.Player2
            );

        bool humanDeveloped =
            developmentManager != null &&
            developmentManager.HasCompletedDevelopment(
                PlayerOwner.Player1
            );

        /*
         * -----------------------------------------------------
         * PRIORITY 1
         *
         * In Battle Phase, completely blocking the opponent
         * is an immediate winning move.
         * -----------------------------------------------------
         */

        bool battlePhase =
            aiDeveloped &&
            humanDeveloped;

        if (battlePhase)
        {
            AIMove blockingWin =
                FindBlockingWinMove(
                    ReadCurrentBoard(),
                    legalMoves,
                    PLAYER2
                );

            if (blockingWin.IsValid)
            {
                Debug.Log(
                    $"AI [{difficulty}] found a winning block."
                );

                return blockingWin;
            }
        }

        /*
         * -----------------------------------------------------
         * PRIORITY 2
         *
         * If the human has completed development,
         * immediately prevent an actual winning move.
         * -----------------------------------------------------
         */

        if (humanDeveloped)
        {
            AIMove block =
                FindImmediateBlockingMove(
                    ReadCurrentBoard(),
                    legalMoves
                );

            if (block.IsValid)
            {
                Debug.Log(
                    $"AI [{difficulty}] blocked immediate victory."
                );

                return block;
            }
        }

        /*
         * -----------------------------------------------------
         * PRIORITY 2
         *
         * AI development.
         * -----------------------------------------------------
         */

        if (!aiDeveloped)
        {
            AIMove developmentMove =
                ChooseDevelopmentMove(
                    difficulty
                );

            if (developmentMove.IsValid)
                return developmentMove;
        }

        int[] board =
            ReadCurrentBoard();

        /*
         * -----------------------------------------------------
         * PRIORITY 3
         *
         * Immediate AI victory.
         *
         * This is ONLY allowed after development.
         * -----------------------------------------------------
         */

        if (aiDeveloped)
        {
            AIMove winningMove =
                FindWinningMove(
                    board,
                    legalMoves,
                    PLAYER2
                );

            if (winningMove.IsValid)
            {
                Debug.Log(
                    $"AI [{difficulty}] found winning move."
                );

                return winningMove;
            }
        }

        /*
         * -----------------------------------------------------
         * PRIORITY 4
         *
         * If AI is developed but Player 1 isn't,
         * normal tactical evaluation is used.
         * -----------------------------------------------------
         */

        if (aiDeveloped &&
            !humanDeveloped)
        {
            AIMove tactical =
                ChooseRuleAwareTacticalMove(
                    board,
                    legalMoves,
                    difficulty
                );

            if (tactical.IsValid)
                return tactical;
        }

        /*
         * -----------------------------------------------------
         * PRIORITY 5
         *
         * Both developed.
         *
         * Hard receives exact game-theoretic analysis.
         * -----------------------------------------------------
         */

        if (aiDeveloped &&
            humanDeveloped)
        {
            if (difficulty == AIDifficulty.Hard &&
                hardUsesExactSearch)
            {
                EnsureExactSolver();

                AIMove exact =
                    ChooseExactHardMove(
                        board,
                        legalMoves
                    );

                if (exact.IsValid)
                    return exact;
            }
        }

        /*
         * -----------------------------------------------------
         * PRIORITY 6
         *
         * Search fallback.
         * -----------------------------------------------------
         */

        if (difficulty == AIDifficulty.Easy)
        {
            return ChooseEasyMove(
                board,
                legalMoves
            );
        }

        if (difficulty == AIDifficulty.Medium)
        {
            return ChooseSearchMove(
                board,
                legalMoves,
                mediumSearchDepth,
                true
            );
        }

        return ChooseSearchMove(
            board,
            legalMoves,
            hardSearchDepth,
            true
        );
    }

    // =========================================================
    // DEVELOPMENT
    // =========================================================

    private AIMove ChooseDevelopmentMove(
        AIDifficulty difficulty)
    {
        int[] board =
            ReadCurrentBoard();

        List<AIMove> legalMoves =
            GetLegalMoves(
                board,
                PLAYER2
            );

        List<MoveScore> scored =
            new List<MoveScore>();

        foreach (AIMove move in legalMoves)
        {
            if (!IsMoveAllowedByBlockRule(
                    board,
                    move,
                    PLAYER2))
            {
                continue;
            }

            Pawn pawn =
                FindPawnAtNode(
                    move.fromNode
                );

            if (pawn == null)
                continue;

            bool developed =
                developmentManager != null &&
                developmentManager.IsPawnDeveloped(
                    pawn
                );

            int[] nextBoard =
                ApplyMoveToArray(
                    board,
                    move,
                    PLAYER2
                );

            int score =
                EvaluateMovementBoard(
                    nextBoard,
                    difficulty == AIDifficulty.Hard,
                    false
                );

            /*
             * Development is the dominant objective.
             */
            if (!developed)
            {
                score += 100000;
            }
            else
            {
                score -= 30000;
            }

            /*
             * Strong tactical positioning still matters.
             */
            score +=
                CountThreats(
                    nextBoard,
                    PLAYER2
                ) * 700;

            score -=
                CountThreats(
                    nextBoard,
                    PLAYER1
                ) * 600;

            score +=
                CountForks(
                    nextBoard,
                    PLAYER2
                ) * 1000;

            score -=
                CountForks(
                    nextBoard,
                    PLAYER1
                ) * 1200;

            /*
             * Stronger modes perform deeper evaluation.
             */
            if (difficulty != AIDifficulty.Easy)
            {
                score +=
                    EvaluateDevelopmentFuture(
                        nextBoard,
                        difficulty
                    );
            }

            scored.Add(
                new MoveScore(
                    move,
                    score
                )
            );
        }

        if (scored.Count == 0)
            return AIMove.Invalid();

        scored.Sort(
            (a, b) =>
                b.score.CompareTo(
                    a.score
                )
        );

        /*
         * Easy normally takes the strongest
         * development move.
         *
         * Only 2% of turns allow a controlled
         * alternative from the top two.
         */
        int selectedIndex = 0;

        if (difficulty == AIDifficulty.Easy &&
            UnityEngine.Random.value <
            easyMistakeChance)
        {
            selectedIndex =
                UnityEngine.Random.Range(
                    0,
                    Mathf.Min(
                        2,
                        scored.Count
                    )
                );
        }

        AIMove result =
            scored[selectedIndex].move;

        Debug.Log(
            $"AI [{difficulty}] development move: " +
            $"{result.fromNode} -> " +
            $"{result.toNode}"
        );

        return result;
    }

    private int EvaluateDevelopmentFuture(
        int[] board,
        AIDifficulty difficulty)
    {
        int score = 0;

        List<AIMove> opponentMoves =
            GetLegalMoves(
                board,
                PLAYER1
            );

        foreach (AIMove move in opponentMoves)
        {
            int[] next =
                ApplyMoveToArray(
                    board,
                    move,
                    PLAYER1
                );

            score -=
                CountThreats(
                    next,
                    PLAYER1
                ) * 350;

            score +=
                CountThreats(
                    next,
                    PLAYER2
                ) * 250;
        }

        if (difficulty == AIDifficulty.Hard)
        {
            score +=
                CountForks(
                    board,
                    PLAYER2
                ) * 500;

            score -=
                CountForks(
                    board,
                    PLAYER1
                ) * 700;
        }

        return score;
    }

    private AIMove ChooseRuleAwareTacticalMove(
        int[] board,
        List<AIMove> legalMoves,
        AIDifficulty difficulty)
    {
        AIMove best =
            AIMove.Invalid();

        int bestScore =
            int.MinValue;

        foreach (AIMove move in OrderMoves(
                     board,
                     legalMoves,
                     PLAYER2))
        {
            int[] next =
                ApplyMoveToArray(
                    board,
                    move,
                    PLAYER2
                );

            int score =
                EvaluateMovementBoard(
                    next,
                    difficulty !=
                    AIDifficulty.Easy,
                    false
                );

            /*
             * AI is developed, therefore a line
             * is a legitimate winning threat.
             */
            if (HasWinningLine(
                    next,
                    PLAYER2))
            {
                score += WIN_SCORE;
            }

            score +=
                CountThreats(
                    next,
                    PLAYER2
                ) * 800;

            score -=
                CountThreats(
                    next,
                    PLAYER1
                ) * 700;

            score +=
                CountForks(
                    next,
                    PLAYER2
                ) * 1300;

            score -=
                CountForks(
                    next,
                    PLAYER1
                ) * 1500;

            if (score > bestScore)
            {
                bestScore = score;
                best = move;
            }
        }

        return best;
    }

    // =========================================================
    // EASY
    // =========================================================

    private AIMove ChooseEasyMove(
        int[] board,
        List<AIMove> legalMoves)
    {
        List<MoveScore> scored =
            new List<MoveScore>();

        foreach (AIMove move in legalMoves)
        {
            int[] next =
                ApplyMoveToArray(
                    board,
                    move,
                    PLAYER2
                );

            int score =
                EvaluateMovementBoard(
                    next,
                    false,
                    true
                );

            /*
             * Easy still gets real tactical look-ahead.
             */
            score +=
                ShallowOpponentPressure(
                    next,
                    easySearchDepth - 1
                );

            score +=
                CountThreats(
                    next,
                    PLAYER2
                ) * 400;

            score -=
                CountThreats(
                    next,
                    PLAYER1
                ) * 500;

            score +=
                CountForks(
                    next,
                    PLAYER2
                ) * 800;

            score -=
                CountForks(
                    next,
                    PLAYER1
                ) * 950;

            scored.Add(
                new MoveScore(
                    move,
                    score
                )
            );
        }

        scored.Sort(
            (a, b) =>
                b.score.CompareTo(
                    a.score
                )
        );

        if (scored.Count == 0)
            return AIMove.Invalid();

        /*
         * Strong Easy:
         *
         * 98% = strongest move.
         * 2% = second-best move.
         */
        int selectedIndex = 0;

        if (UnityEngine.Random.value <
            easyMistakeChance &&
            scored.Count > 1)
        {
            selectedIndex = 1;
        }

        return scored[selectedIndex].move;
    }

    private int ShallowOpponentPressure(
        int[] board,
        int depth)
    {
        if (depth <= 0)
            return 0;

        List<AIMove> opponentMoves =
            GetLegalMoves(
                board,
                PLAYER1
            );

        if (opponentMoves.Count == 0)
            return 0;

        int worstReply =
            int.MaxValue;

        foreach (AIMove move in opponentMoves)
        {
            int[] next =
                ApplyMoveToArray(
                    board,
                    move,
                    PLAYER1
                );

            int score =
                -EvaluateMovementBoard(
                    next,
                    false,
                    true
                );

            score -=
                CountThreats(
                    next,
                    PLAYER1
                ) * 300;

            worstReply =
                Mathf.Min(
                    worstReply,
                    score
                );
        }

        return worstReply;
    }

    // =========================================================
    // SEARCH
    // =========================================================

    private AIMove ChooseSearchMove(
        int[] board,
        List<AIMove> legalMoves,
        int searchDepth,
        bool hardEvaluation)
    {
        AIMove best =
            AIMove.Invalid();

        int bestScore =
            int.MinValue;

        foreach (AIMove move in OrderMoves(
                     board,
                     legalMoves,
                     PLAYER2))
        {
            int[] next =
                ApplyMoveToArray(
                    board,
                    move,
                    PLAYER2
                );

            int score;

            if (HasWinningLine(
                    next,
                    PLAYER2))
            {
                score =
                    WIN_SCORE;
            }
            else
            {
                HashSet<int> path =
                    new HashSet<int>();

                score =
                    SearchMovement(
                        next,
                        PLAYER1,
                        searchDepth - 1,
                        int.MinValue + 1,
                        int.MaxValue - 1,
                        hardEvaluation,
                        path
                    );
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = move;
            }
        }

        return best;
    }

    private int SearchMovement(
        int[] board,
        int currentPlayer,
        int depth,
        int alpha,
        int beta,
        bool hardEvaluation,
        HashSet<int> path)
    {
        /*
         * Both players are fully developed before
         * this search is allowed to use winning lines
         * as terminal states.
         */

        if (HasWinningLine(
                board,
                PLAYER2))
        {
            return WIN_SCORE + depth;
        }

        if (HasWinningLine(
                board,
                PLAYER1))
        {
            return -WIN_SCORE - depth;
        }

        List<AIMove> moves =
            GetLegalMoves(
                board,
                currentPlayer
            );

        if (moves.Count == 0)
        {
            return currentPlayer == PLAYER2
                ? -WIN_SCORE - depth
                : WIN_SCORE + depth;
        }

        if (depth <= 0)
        {
            return EvaluateMovementBoard(
                board,
                hardEvaluation,
                true
            );
        }

        int stateCode =
            EncodeBoard(
                board
            ) * 10 +
            currentPlayer;

        if (path.Contains(
                stateCode))
        {
            return DRAW_SCORE;
        }

        path.Add(
            stateCode
        );

        bool maximizing =
            currentPlayer == PLAYER2;

        int best =
            maximizing
                ? int.MinValue
                : int.MaxValue;

        foreach (AIMove move in OrderMoves(
                     board,
                     moves,
                     currentPlayer))
        {
            int[] next =
                ApplyMoveToArray(
                    board,
                    move,
                    currentPlayer
                );

            int score =
                SearchMovement(
                    next,
                    currentPlayer ==
                        PLAYER2
                        ? PLAYER1
                        : PLAYER2,
                    depth - 1,
                    alpha,
                    beta,
                    hardEvaluation,
                    path
                );

            if (maximizing)
            {
                best =
                    Mathf.Max(
                        best,
                        score
                    );

                alpha =
                    Mathf.Max(
                        alpha,
                        best
                    );
            }
            else
            {
                best =
                    Mathf.Min(
                        best,
                        score
                    );

                beta =
                    Mathf.Min(
                        beta,
                        best
                    );
            }

            if (beta <= alpha)
                break;
        }

        path.Remove(
            stateCode
        );

        return best;
    }

    // =========================================================
    // HARD EXACT SOLVER
    // =========================================================

    private void EnsureExactSolver()
    {
        if (exactSolverBuilt)
            return;

        BuildExactSolver();

        exactSolverBuilt = true;

        Debug.Log(
            "Nera Hard AI exact movement solver initialized."
        );
    }

    private void BuildExactSolver()
    {
        exactOutcomeTable.Clear();

        List<int[]> boards =
            GenerateAllMovementBoards();

        foreach (int[] board in boards)
        {
            int code =
                EncodeBoard(
                    board
                );

            for (int player = PLAYER1;
                 player <= PLAYER2;
                 player++)
            {
                int key =
                    code * 10 +
                    player;

                if (HasWinningLine(
                        board,
                        PLAYER2))
                {
                    exactOutcomeTable[key] =
                        ExactOutcome.AIWin;
                }
                else if (HasWinningLine(
                             board,
                             PLAYER1))
                {
                    exactOutcomeTable[key] =
                        ExactOutcome.AILost;
                }
                else if (GetLegalMoves(
                             board,
                             player
                         ).Count == 0)
                {
                    exactOutcomeTable[key] =
                        player == PLAYER2
                            ? ExactOutcome.AILost
                            : ExactOutcome.AIWin;
                }
                else
                {
                    exactOutcomeTable[key] =
                        ExactOutcome.Unknown;
                }
            }
        }

        bool changed = true;

        while (changed)
        {
            changed = false;

            foreach (int[] board in boards)
            {
                int code =
                    EncodeBoard(
                        board
                    );

                for (int player = PLAYER1;
                     player <= PLAYER2;
                     player++)
                {
                    int key =
                        code * 10 +
                        player;

                    if (exactOutcomeTable[key] !=
                        ExactOutcome.Unknown)
                    {
                        continue;
                    }

                    List<AIMove> moves =
                        GetLegalMoves(
                            board,
                            player
                        );

                    if (moves.Count == 0)
                    {
                        exactOutcomeTable[key] =
                            player == PLAYER2
                                ? ExactOutcome.AILost
                                : ExactOutcome.AIWin;

                        changed = true;
                        continue;
                    }

                    bool canForceWin =
                        false;

                    bool canForceDraw =
                        false;

                    bool allLose =
                        true;

                    foreach (AIMove move in moves)
                    {
                        int[] next =
                            ApplyMoveToArray(
                                board,
                                move,
                                player
                            );

                        ExactOutcome successor;

                        int nextKey =
                            EncodeBoard(
                                next
                            ) * 10 +
                            (
                                player ==
                                PLAYER2
                                    ? PLAYER1
                                    : PLAYER2
                            );

                        if (!exactOutcomeTable.TryGetValue(
                                nextKey,
                                out successor))
                        {
                            continue;
                        }

                        if (player == PLAYER2)
                        {
                            if (successor ==
                                ExactOutcome.AIWin)
                            {
                                canForceWin = true;
                                break;
                            }

                            if (successor ==
                                ExactOutcome.Draw)
                            {
                                canForceDraw = true;
                            }

                            if (successor !=
                                ExactOutcome.AILost)
                            {
                                allLose = false;
                            }
                        }
                        else
                        {
                            /*
                             * Player 1 is trying to make
                             * the AI lose.
                             */
                            if (successor ==
                                ExactOutcome.AILost)
                            {
                                canForceWin = true;
                                break;
                            }

                            if (successor ==
                                ExactOutcome.Draw)
                            {
                                canForceDraw = true;
                            }

                            if (successor !=
                                ExactOutcome.AIWin)
                            {
                                allLose = false;
                            }
                        }
                    }

                    if (canForceWin)
                    {
                        exactOutcomeTable[key] =
                            player == PLAYER2
                                ? ExactOutcome.AIWin
                                : ExactOutcome.AILost;

                        changed = true;
                    }
                    else if (allLose)
                    {
                        exactOutcomeTable[key] =
                            player == PLAYER2
                                ? ExactOutcome.AILost
                                : ExactOutcome.AIWin;

                        changed = true;
                    }
                    else if (canForceDraw)
                    {
                        /*
                         * We do not immediately classify
                         * this as a draw because another
                         * successor may still become known.
                         */
                    }
                }
            }
        }

        foreach (int[] board in boards)
        {
            int code =
                EncodeBoard(
                    board
                );

            for (int player = PLAYER1;
                 player <= PLAYER2;
                 player++)
            {
                int key =
                    code * 10 +
                    player;

                if (exactOutcomeTable[key] ==
                    ExactOutcome.Unknown)
                {
                    exactOutcomeTable[key] =
                        ExactOutcome.Draw;
                }
            }
        }
    }

    private AIMove ChooseExactHardMove(
        int[] board,
        List<AIMove> legalMoves)
    {
        AIMove best =
            AIMove.Invalid();

        int bestPriority =
            int.MinValue;

        int bestHeuristic =
            int.MinValue;

        foreach (AIMove move in OrderMoves(
                     board,
                     legalMoves,
                     PLAYER2))
        {
            int[] next =
                ApplyMoveToArray(
                    board,
                    move,
                    PLAYER2
                );

            ExactOutcome outcome;

            if (HasWinningLine(
                    next,
                    PLAYER2))
            {
                outcome =
                    ExactOutcome.AIWin;
            }
            else
            {
                int key =
                    EncodeBoard(
                        next
                    ) * 10 +
                    PLAYER1;

                if (!exactOutcomeTable.TryGetValue(
                        key,
                        out outcome))
                {
                    outcome =
                        ExactOutcome.Draw;
                }
            }

            int priority;

            switch (outcome)
            {
                case ExactOutcome.AIWin:
                    priority = 3;
                    break;

                case ExactOutcome.Draw:
                    priority = 2;
                    break;

                default:
                    priority = 1;
                    break;
            }

            int heuristic =
                EvaluateMovementBoard(
                    next,
                    true,
                    true
                );

            if (priority > bestPriority ||
                (
                    priority == bestPriority &&
                    heuristic > bestHeuristic
                ))
            {
                bestPriority =
                    priority;

                bestHeuristic =
                    heuristic;

                best =
                    move;
            }
        }

        return best;
    }

    private List<int[]> GenerateAllMovementBoards()
    {
        List<int[]> boards =
            new List<int[]>();

        for (int a = 0; a < 9; a++)
        {
            for (int b = a + 1; b < 9; b++)
            {
                for (int c = b + 1; c < 9; c++)
                {
                    for (int d = 0; d < 9; d++)
                    {
                        if (d == a ||
                            d == b ||
                            d == c)
                        {
                            continue;
                        }

                        for (int e = d + 1; e < 9; e++)
                        {
                            if (e == a ||
                                e == b ||
                                e == c)
                            {
                                continue;
                            }

                            for (
                                int f = e + 1;
                                f < 9;
                                f++)
                            {
                                if (f == a ||
                                    f == b ||
                                    f == c)
                                {
                                    continue;
                                }

                                int[] board =
                                    new int[9];

                                board[a] =
                                    PLAYER1;

                                board[b] =
                                    PLAYER1;

                                board[c] =
                                    PLAYER1;

                                board[d] =
                                    PLAYER2;

                                board[e] =
                                    PLAYER2;

                                board[f] =
                                    PLAYER2;

                                boards.Add(
                                    board
                                );
                            }
                        }
                    }
                }
            }
        }

        return boards;
    }

    // =========================================================
    // TACTICS
    // =========================================================

    private AIMove FindWinningMove(
        int[] board,
        List<AIMove> legalMoves,
        int player)
    {
        foreach (AIMove move in legalMoves)
        {
            int[] next =
                ApplyMoveToArray(
                    board,
                    move,
                    player
                );

            if (HasWinningLine(
                    next,
                    player
                ))
            {
                return move;
            }
        }

        return AIMove.Invalid();
    }

    private AIMove FindBlockingWinMove(
        int[] board,
        List<AIMove> legalMoves,
        int player)
    {
        foreach (AIMove move in legalMoves)
        {
            int[] next =
                ApplyMoveToArray(
                    board,
                    move,
                    player
                );

            int opponent =
                player == PLAYER2
                    ? PLAYER1
                    : PLAYER2;

            if (!HasAnyLegalMove(
                    next,
                    opponent))
            {
                return move;
            }
        }

        return AIMove.Invalid();
    }

    private AIMove FindImmediateBlockingMove(
        int[] board,
        List<AIMove> legalMoves)
    {
        List<AIMove> opponentWins =
            GetWinningMoves(
                board,
                PLAYER1
            );

        if (opponentWins.Count == 0)
            return AIMove.Invalid();

        AIMove best =
            AIMove.Invalid();

        int bestScore =
            int.MinValue;

        foreach (AIMove move in legalMoves)
        {
            int[] next =
                ApplyMoveToArray(
                    board,
                    move,
                    PLAYER2
                );

            int remaining =
                GetWinningMoves(
                    next,
                    PLAYER1
                ).Count;

            if (remaining > 0)
                continue;

            int score =
                EvaluateMovementBoard(
                    next,
                    true,
                    true
                );

            score +=
                CountThreats(
                    next,
                    PLAYER2
                ) * 1000;

            score +=
                CountForks(
                    next,
                    PLAYER2
                ) * 1500;

            if (score > bestScore)
            {
                bestScore = score;
                best = move;
            }
        }

        return best;
    }

    private List<AIMove> GetWinningMoves(
        int[] board,
        int player)
    {
        List<AIMove> legal =
            GetLegalMoves(
                board,
                player
            );

        List<AIMove> wins =
            new List<AIMove>();

        foreach (AIMove move in legal)
        {
            int[] next =
                ApplyMoveToArray(
                    board,
                    move,
                    player
                );

            if (HasWinningLine(
                    next,
                    player
                ))
            {
                wins.Add(move);
            }
        }

        return wins;
    }

    // =========================================================
    // EVALUATION
    // =========================================================

    private int EvaluateMovementBoard(
        int[] board,
        bool hardMode = false)
    {
        return EvaluateMovementBoard(
            board,
            hardMode,
            true
        );
    }

    private int EvaluateMovementBoard(
        int[] board,
        bool hardMode,
        bool includeVictory)
    {
        if (includeVictory &&
            HasWinningLine(
                board,
                PLAYER2
            ))
        {
            return WIN_SCORE;
        }

        if (includeVictory &&
            HasWinningLine(
                board,
                PLAYER1
            ))
        {
            return -WIN_SCORE;
        }

        int score = 0;

        int aiThreats =
            CountThreats(
                board,
                PLAYER2
            );

        int humanThreats =
            CountThreats(
                board,
                PLAYER1
            );

        int aiForks =
            CountForks(
                board,
                PLAYER2
            );

        int humanForks =
            CountForks(
                board,
                PLAYER1
            );

        int aiMobility =
            GetLegalMoves(
                board,
                PLAYER2
            ).Count;

        int humanMobility =
            GetLegalMoves(
                board,
                PLAYER1
            ).Count;

        /*
         * Threats.
         */
        score +=
            aiThreats *
            (
                hardMode
                    ? 600
                    : 450
            );

        score -=
            humanThreats *
            (
                hardMode
                    ? 750
                    : 550
            );

        /*
         * Forks.
         */
        score +=
            aiForks *
            (
                hardMode
                    ? 1500
                    : 1000
            );

        score -=
            humanForks *
            (
                hardMode
                    ? 1800
                    : 1200
            );

        /*
         * Mobility.
         */
        score +=
            (
                aiMobility -
                humanMobility
            ) *
            (
                hardMode
                    ? 75
                    : 55
            );

        /*
         * Center.
         */
        if (board[4] == PLAYER2)
            score += 100;

        if (board[4] == PLAYER1)
            score -= 100;

        /*
         * Corners.
         */
        int[] corners =
        {
            0, 2, 6, 8
        };

        foreach (int corner in corners)
        {
            if (board[corner] == PLAYER2)
                score += 30;

            if (board[corner] == PLAYER1)
                score -= 30;
        }

        /*
         * Connected pieces.
         */
        score +=
            CountConnectedPieces(
                board,
                PLAYER2
            ) * 30;

        score -=
            CountConnectedPieces(
                board,
                PLAYER1
            ) * 30;

        /*
         * Open winning lines.
         */
        score +=
            CountPotentialLines(
                board,
                PLAYER2
            ) * 120;

        score -=
            CountPotentialLines(
                board,
                PLAYER1
            ) * 150;

        return score;
    }

    private int CountThreats(
        int[] board,
        int player)
    {
        int count = 0;

        foreach (int[] line in winningLines)
        {
            int pieces = 0;
            int empty = 0;

            for (int i = 0; i < 3; i++)
            {
                if (board[line[i]] == player)
                    pieces++;

                if (board[line[i]] == EMPTY)
                    empty++;
            }

            if (pieces == 2 &&
                empty == 1)
            {
                count++;
            }
        }

        return count;
    }

    private int CountForks(
        int[] board,
        int player)
    {
        List<AIMove> moves =
            GetLegalMoves(
                board,
                player
            );

        int forks = 0;

        foreach (AIMove move in moves)
        {
            int[] next =
                ApplyMoveToArray(
                    board,
                    move,
                    player
                );

            if (CountThreats(
                    next,
                    player
                ) >= 2)
            {
                forks++;
            }
        }

        return forks;
    }

    private int CountConnectedPieces(
        int[] board,
        int player)
    {
        int count = 0;

        foreach (int[] line in winningLines)
        {
            int pieces = 0;

            for (int i = 0; i < 3; i++)
            {
                if (board[line[i]] ==
                    player)
                {
                    pieces++;
                }
            }

            if (pieces >= 2)
                count++;
        }

        return count;
    }

    // =========================================================
    // MOVE GENERATION
    // =========================================================

    private List<AIMove> GetLegalMoves(
        int[] board,
        int player)
    {
        List<AIMove> moves =
            new List<AIMove>();

        List<AIMove> rawMoves =
            GetRawLegalMoves(
                board,
                player
            );

        foreach (AIMove move in rawMoves)
        {
            if (!IsMoveAllowedByBlockRule(
                    board,
                    move,
                    player))
            {
                continue;
            }

            moves.Add(move);
        }

        return moves;
    }

    private List<AIMove> GetRawLegalMoves(
        int[] board,
        int player)
    {
        List<AIMove> moves =
            new List<AIMove>();

        for (int from = 0;
             from < 9;
             from++)
        {
            if (board[from] != player)
                continue;

            foreach (int to in connections[from])
            {
                if (board[to] != EMPTY)
                    continue;

                moves.Add(
                    new AIMove(
                        from,
                        to
                    )
                );
            }
        }

        return moves;
    }

    private bool IsMoveAllowedByBlockRule(
        int[] board,
        AIMove move,
        int player)
    {
        if (IsBattlePhase())
            return true;

        int opponent =
            player == PLAYER2
                ? PLAYER1
                : PLAYER2;

        int[] next =
            ApplyMoveToArray(
                board,
                move,
                player
            );

        return HasAnyLegalMove(
            next,
            opponent
        );
    }

    private bool IsBattlePhase()
    {
        return
            developmentManager != null &&
            developmentManager.AreBothPlayersFullyDeveloped();
    }

    private bool HasAnyLegalMove(
        int[] board,
        int player)
    {
        return
            GetRawLegalMoves(
                board,
                player
            ).Count > 0;
    }

    private List<AIMove> OrderMoves(
        int[] board,
        List<AIMove> moves,
        int player)
    {
        List<MoveScore> scored =
            new List<MoveScore>();

        foreach (AIMove move in moves)
        {
            int[] next =
                ApplyMoveToArray(
                    board,
                    move,
                    player
                );

            int score =
                EvaluateMovementBoard(
                    next,
                    true,
                    true
                );

            if (HasWinningLine(
                    next,
                    player
                ))
            {
                score +=
                    WIN_SCORE;
            }

            score +=
                CountThreats(
                    next,
                    player
                ) * 900;

            score +=
                CountForks(
                    next,
                    player
                ) * 1400;

            scored.Add(
                new MoveScore(
                    move,
                    score
                )
            );
        }

        scored.Sort(
            (a, b) =>
                b.score.CompareTo(
                    a.score
                )
        );

        List<AIMove> ordered =
            new List<AIMove>();

        foreach (MoveScore score in scored)
        {
            ordered.Add(
                score.move
            );
        }

        return ordered;
    }

    // =========================================================
    // BOARD
    // =========================================================

    private int[] ReadCurrentBoard()
    {
        int[] board =
            new int[9];

        for (int i = 0; i < 9; i++)
        {
            PlayerOwner owner =
                boardManager.GetOwner(i);

            if (owner ==
                PlayerOwner.Player1)
            {
                board[i] = PLAYER1;
            }
            else if (owner ==
                     PlayerOwner.Player2)
            {
                board[i] = PLAYER2;
            }
            else
            {
                board[i] = EMPTY;
            }
        }

        return board;
    }

    private int[] ApplyMoveToArray(
        int[] board,
        AIMove move,
        int player)
    {
        int[] result =
            board.Clone() as int[];

        result[move.fromNode] =
            EMPTY;

        result[move.toNode] =
            player;

        return result;
    }

    private bool IsPlacementAllowedByBlockRule(
        int[] board,
        int node,
        int player)
    {
        if (board[node] != EMPTY)
            return false;

        int[] next =
            board.Clone() as int[];

        next[node] =
            player;

        int player1Count = 0;
        int player2Count = 0;

        for (int i = 0; i < next.Length; i++)
        {
            if (next[i] == PLAYER1)
                player1Count++;
            else if (next[i] == PLAYER2)
                player2Count++;
        }

        /*
         * Before both sides have all three pawns placed,
         * there is no meaningful movement block yet.
         */
        if (player1Count < 3 ||
            player2Count < 3)
        {
            return true;
        }

        int opponent =
            player == PLAYER2
                ? PLAYER1
                : PLAYER2;

        return HasAnyLegalMove(
            next,
            opponent
        );
    }

    private List<int> GetAvailableNodes()
    {
        return GetAvailableNodes(
            ReadCurrentBoard()
        );
    }

    private List<int> GetAvailableNodes(
        int[] board)
    {
        List<int> nodes =
            new List<int>();

        for (int i = 0; i < 9; i++)
        {
            if (board[i] == EMPTY)
                nodes.Add(i);
        }

        return nodes;
    }

    private int FindCriticalBlockingPlacement(
        List<int> availableNodes)
    {
        int[] board =
            ReadCurrentBoard();

        foreach (int node in availableNodes)
        {
            int[] test =
                board.Clone() as int[];

            test[node] =
                PLAYER1;

            /*
             * Placement cannot produce a victory,
             * but preventing two-piece threats is valuable.
             */
            if (CountThreats(
                    test,
                    PLAYER1
                ) >= 2)
            {
                continue;
            }

            if (CountPotentialLines(
                    test,
                    PLAYER1
                ) > 0 &&
                IsPlacementAllowedByBlockRule(
                    board,
                    node,
                    PLAYER2
                ))
            {
                return node;
            }
        }

        return -1;
    }

    // =========================================================
    // WINNING
    // =========================================================

    private bool HasWinningLine(
        int[] board,
        int player)
    {
        foreach (int[] line in winningLines)
        {
            if (board[line[0]] == player &&
                board[line[1]] == player &&
                board[line[2]] == player)
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // PAWN
    // =========================================================

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

    // =========================================================
    // STATE
    // =========================================================

    private int EncodeBoard(
        int[] board)
    {
        int code = 0;
        int multiplier = 1;

        for (int i = 0; i < 9; i++)
        {
            code +=
                board[i] *
                multiplier;

            multiplier *= 3;
        }

        return code;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private string GetCurrentDifficultyName()
    {
        if (GameSession.Instance == null)
            return "AI";

        return
            GameSession.Instance
                .SelectedDifficulty
                .ToString();
    }

    // =========================================================
    // DATA
    // =========================================================

    private struct MoveScore
    {
        public AIMove move;
        public int score;

        public MoveScore(
            AIMove move,
            int score)
        {
            this.move = move;
            this.score = score;
        }
    }

    private enum ExactOutcome
    {
        Unknown,
        AIWin,
        Draw,
        AILost
    }

    // =========================================================
    // PUBLIC MOVE
    // =========================================================

    public struct AIMove
    {
        public int fromNode;
        public int toNode;

        public bool IsValid =>
            fromNode >= 0 &&
            toNode >= 0;

        public AIMove(
            int fromNode,
            int toNode)
        {
            this.fromNode = fromNode;
            this.toNode = toNode;
        }

        public static AIMove Invalid()
        {
            return new AIMove(
                -1,
                -1
            );
        }
    }
}