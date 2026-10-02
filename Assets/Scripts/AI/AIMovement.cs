using UnityEngine;

public class AIMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private MovementManager movementManager;
    [SerializeField] private AIBrain aiBrain;

    private void Awake()
    {
        FindReferences();
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

        if (aiBrain == null)
            aiBrain =
                FindFirstObjectByType<AIBrain>();
    }

    public bool TryMakeMove()
    {
        if (boardManager == null ||
            playerManager == null ||
            movementManager == null ||
            aiBrain == null)
        {
            Debug.LogError(
                "AIMovement: Required reference is missing."
            );

            return false;
        }

        if (!movementManager.MovementPhaseActive)
            return false;

        if (playerManager.CurrentPlayer !=
            PlayerOwner.Player2)
            return false;

        PlayerData aiPlayer =
            playerManager.GetPlayer(
                PlayerOwner.Player2
            );

        if (aiPlayer == null ||
            !aiPlayer.isAI)
            return false;

        AIDifficulty difficulty =
            GetDifficulty();

        AIBrain.AIMove move =
            aiBrain.ChooseMovement(
                difficulty
            );

        if (!move.IsValid)
        {
            Debug.LogWarning(
                "AIMovement: Brain found no legal move."
            );

            return false;
        }

        Debug.Log(
            $"AI [{difficulty}] movement decision: " +
            $"{move.fromNode} -> {move.toNode}"
        );

        return movementManager.TryMakeAIMove(
            move.fromNode,
            move.toNode
        );
    }

    private AIDifficulty GetDifficulty()
    {
        if (GameSession.Instance == null)
            return AIDifficulty.Easy;

        return GameSession.Instance.SelectedDifficulty;
    }
}