using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public PlayerData Player1 { get; private set; }
    public PlayerData Player2 { get; private set; }

    public PlayerOwner CurrentPlayer { get; private set; }

    // The player who won the toss.
    // This remains unchanged throughout the entire game.
    public PlayerOwner StartingPlayer { get; private set; }

    public void InitializePlayers()
    {
        bool vsAI = false;

        if (GameSession.Instance != null)
        {
            vsAI =
                GameSession.Instance.SelectedGameMode ==
                GameMode.VsAI;
        }
        else
        {
            Debug.LogWarning(
                "GameSession not found. " +
                "Defaulting to Two Player mode."
            );
        }

        Player1 =
            new PlayerData(
                PlayerOwner.Player1,
                false
            );

        Player2 =
            new PlayerData(
                PlayerOwner.Player2,
                vsAI
            );

        // Temporary value until the toss decides the real starter.
        StartingPlayer =
            PlayerOwner.Player1;

        CurrentPlayer =
            PlayerOwner.Player1;

        Debug.Log(
            "===== PLAYERS INITIALIZED ====="
        );

        Debug.Log(
            $"Player 1 AI: {Player1.isAI}"
        );

        Debug.Log(
            $"Player 2 AI: {Player2.isAI}"
        );

        Debug.Log(
            $"Starting Player: {StartingPlayer}"
        );

        Debug.Log(
            $"Current Player: {CurrentPlayer}"
        );
    }

    public PlayerData GetPlayer(
        PlayerOwner owner)
    {
        if (owner == PlayerOwner.Player1)
            return Player1;

        if (owner == PlayerOwner.Player2)
            return Player2;

        Debug.LogWarning(
            $"GetPlayer called with invalid owner: {owner}"
        );

        return null;
    }

    public void SetStartingPlayer(
        PlayerOwner startingPlayer)
    {
        if (startingPlayer == PlayerOwner.None)
        {
            Debug.LogError(
                "Cannot set starting player to None."
            );

            return;
        }

        StartingPlayer =
            startingPlayer;

        CurrentPlayer =
            startingPlayer;

        Debug.Log(
            $"===== STARTING PLAYER SET =====\n" +
            $"Starting Player: {StartingPlayer}\n" +
            $"Current Player: {CurrentPlayer}"
        );
    }

    public void RestoreStartingPlayer()
    {
        if (StartingPlayer == PlayerOwner.None)
        {
            Debug.LogError(
                "Cannot restore starting player " +
                "because StartingPlayer is None."
            );

            return;
        }

        CurrentPlayer =
            StartingPlayer;

        Debug.Log(
            $"===== STARTING PLAYER RESTORED =====\n" +
            $"Starting Player: {StartingPlayer}\n" +
            $"Current Player: {CurrentPlayer}"
        );
    }

    public void SwitchTurn()
    {
        if (CurrentPlayer == PlayerOwner.Player1)
        {
            CurrentPlayer =
                PlayerOwner.Player2;
        }
        else if (CurrentPlayer == PlayerOwner.Player2)
        {
            CurrentPlayer =
                PlayerOwner.Player1;
        }
        else
        {
            Debug.LogError(
                "Cannot switch turn because " +
                "CurrentPlayer is None."
            );

            CurrentPlayer =
                StartingPlayer != PlayerOwner.None
                    ? StartingPlayer
                    : PlayerOwner.Player1;
        }

        Debug.Log(
            $"Current Player: {CurrentPlayer}"
        );
    }
}