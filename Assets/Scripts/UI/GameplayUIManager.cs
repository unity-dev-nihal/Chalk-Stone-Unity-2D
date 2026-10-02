using UnityEngine;
using TMPro;

public class GameplayUIManager : MonoBehaviour
{
    [Header("Gameplay References")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private PlacementManager placementManager;
    [SerializeField] private MovementManager movementManager;
    [SerializeField] private DevelopmentManager developmentManager;

    [Header("UI References")]
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text player1PawnText;
    [SerializeField] private TMP_Text player2PawnText;
    [SerializeField] private TMP_Text turnText;

    private void Awake()
    {
        FindReferences();
    }

    private void Update()
    {
        UpdatePhaseText();
        UpdatePawnCounters();
        UpdateTurnText();
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

        if (developmentManager == null)
        {
            developmentManager =
                FindFirstObjectByType<DevelopmentManager>();
        }
    }

    private void UpdatePhaseText()
    {
        if (phaseText == null)
            return;

        // -----------------------------------------
        // PLACEMENT PHASE
        // -----------------------------------------

        if (placementManager != null &&
            !placementManager.PlacementComplete)
        {
            phaseText.text = "PLACEMENT PHASE";
            return;
        }

        // -----------------------------------------
        // MOVEMENT PHASE
        // -----------------------------------------

        if (movementManager != null &&
            movementManager.MovementPhaseActive)
        {
            if (developmentManager != null)
            {
                phaseText.text =
                    developmentManager.GetPhaseName();
            }
            else
            {
                phaseText.text = "DEVELOPMENT PHASE";
            }

            return;
        }

        phaseText.text = "";
    }

    private void UpdatePawnCounters()
    {
        if (playerManager == null)
            return;

        PlayerData player1 =
            playerManager.GetPlayer(
                PlayerOwner.Player1
            );

        PlayerData player2 =
            playerManager.GetPlayer(
                PlayerOwner.Player2
            );

        if (player1 != null &&
            player1PawnText != null)
        {
            player1PawnText.text =
                player1.remainingPawns.ToString();
        }

        if (player2 != null &&
            player2PawnText != null)
        {
            player2PawnText.text =
                player2.remainingPawns.ToString();
        }
    }

    private void UpdateTurnText()
    {
        if (turnText == null ||
            playerManager == null)
            return;

        PlayerOwner currentPlayer =
            playerManager.CurrentPlayer;

        // -----------------------------------------
        // PLAYER 1
        // -----------------------------------------

        if (currentPlayer == PlayerOwner.Player1)
        {
            if (IsAIGame())
            {
                turnText.text = "YOUR TURN";
            }
            else
            {
                turnText.text = "PLAYER 1 TURN";
            }

            return;
        }

        // -----------------------------------------
        // PLAYER 2
        // -----------------------------------------

        if (currentPlayer == PlayerOwner.Player2)
        {
            if (IsAIGame())
            {
                turnText.text = "AI TURN";
            }
            else
            {
                turnText.text = "PLAYER 2 TURN";
            }

            return;
        }

        turnText.text = "";
    }

    private bool IsAIGame()
    {
        if (playerManager == null)
            return false;

        PlayerData player2 =
            playerManager.GetPlayer(
                PlayerOwner.Player2
            );

        if (player2 == null)
            return false;

        return player2.isAI;
    }
}