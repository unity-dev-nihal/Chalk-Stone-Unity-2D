using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VictoryManager : MonoBehaviour
{
    [Header("Victory Panel")]
    [SerializeField] private GameObject victoryPanel;

    [Header("Victory Title")]
    [SerializeField] private Image victoryTitleImage;

    [Header("Victory Sprites")]
    [SerializeField] private Sprite player1WinsSprite;
    [SerializeField] private Sprite player2WinsSprite;
    [SerializeField] private Sprite aiWinsSprite;

    [Header("References")]
    [SerializeField] private MovementManager movementManager;
    [SerializeField] private PlayerManager playerManager;

    private PlayerOwner winner;

    private bool victorySoundPlayed = false;

    private void Awake()
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        if (movementManager == null)
            movementManager =
                FindFirstObjectByType<MovementManager>();

        if (playerManager == null)
            playerManager =
                FindFirstObjectByType<PlayerManager>();
    }

    public void ShowVictory(PlayerOwner winningPlayer)
    {
        winner = winningPlayer;

        if (movementManager != null)
            movementManager.StopMovement();

        SetVictoryTitle();

        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        // Play victory sound exactly once.
        if (!victorySoundPlayed)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayVictory();

            victorySoundPlayed = true;
        }

        Debug.Log(
            $"Victory Panel shown for {winner}"
        );
    }

    private void SetVictoryTitle()
    {
        if (victoryTitleImage == null)
        {
            Debug.LogWarning(
                "VictoryManager: Victory Title Image is missing!"
            );

            return;
        }

        // PLAYER 1 WINS
        if (winner == PlayerOwner.Player1)
        {
            if (player1WinsSprite != null)
            {
                victoryTitleImage.sprite =
                    player1WinsSprite;

                victoryTitleImage.enabled = true;
            }
            else
            {
                Debug.LogWarning(
                    "VictoryManager: " +
                    "Player 1 Wins sprite is missing!"
                );
            }

            return;
        }

        // PLAYER 2 / AI WINS
        if (winner == PlayerOwner.Player2)
        {
            bool player2IsAI = false;

            if (playerManager != null &&
                playerManager.Player2 != null)
            {
                player2IsAI =
                    playerManager.Player2.isAI;
            }

            // AI WINS
            if (player2IsAI)
            {
                if (aiWinsSprite != null)
                {
                    victoryTitleImage.sprite =
                        aiWinsSprite;

                    victoryTitleImage.enabled = true;
                }
                else
                {
                    Debug.LogWarning(
                        "VictoryManager: " +
                        "AI Wins sprite is missing!"
                    );
                }
            }
            // PLAYER 2 WINS
            else
            {
                if (player2WinsSprite != null)
                {
                    victoryTitleImage.sprite =
                        player2WinsSprite;

                    victoryTitleImage.enabled = true;
                }
                else
                {
                    Debug.LogWarning(
                        "VictoryManager: " +
                        "Player 2 Wins sprite is missing!"
                    );
                }
            }

            return;
        }

        Debug.LogWarning(
            "VictoryManager: " +
            "Victory received with no valid winner."
        );
    }

    public void PlayAgain()
    {
        Debug.Log("Playing again.");

        SceneManager.LoadScene("GameScene");
    }

    public void ChangeMode()
    {
        Debug.Log("Changing game mode.");

        SceneManager.LoadScene("Main Menu");
    }

    public void MainMenu()
    {
        Debug.Log("Returning to Main Menu.");

        SceneManager.LoadScene("Main Menu");
    }
}