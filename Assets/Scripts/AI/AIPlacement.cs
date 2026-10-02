using UnityEngine;

public class AIPlacement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private PlacementManager placementManager;
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

        if (placementManager == null)
            placementManager =
                FindFirstObjectByType<PlacementManager>();

        if (aiBrain == null)
            aiBrain =
                FindFirstObjectByType<AIBrain>();
    }

    public bool TryMakePlacement()
    {
        if (boardManager == null ||
            playerManager == null ||
            placementManager == null ||
            aiBrain == null)
        {
            Debug.LogError(
                "AIPlacement: Required reference is missing."
            );

            return false;
        }

        if (placementManager.PlacementComplete)
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

        if (!aiPlayer.HasPawnsToPlace())
            return false;

        AIDifficulty difficulty =
            GetDifficulty();

        int selectedNode =
            aiBrain.ChoosePlacementNode(
                difficulty
            );

        if (selectedNode < 0)
        {
            Debug.LogWarning(
                "AIPlacement: Brain found no placement."
            );

            return false;
        }

        Debug.Log(
            $"AI [{difficulty}] placement decision: " +
            $"Node {selectedNode}"
        );

        placementManager.TryPlacePawn(
            selectedNode
        );

        return true;
    }

    private AIDifficulty GetDifficulty()
    {
        if (GameSession.Instance == null)
            return AIDifficulty.Easy;

        return GameSession.Instance.SelectedDifficulty;
    }
}