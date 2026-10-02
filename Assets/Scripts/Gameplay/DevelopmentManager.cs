using System.Collections.Generic;
using UnityEngine;

public class DevelopmentManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerManager playerManager;

    private readonly HashSet<Pawn> player1DevelopedPawns =
        new HashSet<Pawn>();

    private readonly HashSet<Pawn> player2DevelopedPawns =
        new HashSet<Pawn>();

    public bool Player1DevelopmentComplete =>
        player1DevelopedPawns.Count >= 3;

    public bool Player2DevelopmentComplete =>
        player2DevelopedPawns.Count >= 3;

    private void Awake()
    {
        FindReferences();
        ResetDevelopment();
    }

    private void FindReferences()
    {
        if (playerManager == null)
        {
            playerManager =
                FindFirstObjectByType<PlayerManager>();
        }
    }

    public void ResetDevelopment()
    {
        player1DevelopedPawns.Clear();
        player2DevelopedPawns.Clear();

        Debug.Log(
            "DevelopmentManager: Development tracking reset."
        );
    }

    public void RegisterPawnMoved(
        PlayerOwner player,
        Pawn pawn)
    {
        if (pawn == null)
        {
            Debug.LogWarning(
                "DevelopmentManager: Cannot register null pawn."
            );

            return;
        }

        if (player == PlayerOwner.Player1)
        {
            if (player1DevelopedPawns.Add(pawn))
            {
                Debug.Log(
                    $"Player 1 Development: " +
                    $"{player1DevelopedPawns.Count}/3"
                );
            }

            return;
        }

        if (player == PlayerOwner.Player2)
        {
            if (player2DevelopedPawns.Add(pawn))
            {
                Debug.Log(
                    $"Player 2 Development: " +
                    $"{player2DevelopedPawns.Count}/3"
                );
            }
        }
    }

    public int GetDevelopedPawnCount(
        PlayerOwner player)
    {
        if (player == PlayerOwner.Player1)
        {
            return player1DevelopedPawns.Count;
        }

        if (player == PlayerOwner.Player2)
        {
            return player2DevelopedPawns.Count;
        }

        return 0;
    }

    public int GetRemainingDevelopment(
        PlayerOwner player)
    {
        return Mathf.Max(
            0,
            3 - GetDevelopedPawnCount(player)
        );
    }

    public bool HasCompletedDevelopment(
        PlayerOwner player)
    {
        if (player == PlayerOwner.Player1)
        {
            return Player1DevelopmentComplete;
        }

        if (player == PlayerOwner.Player2)
        {
            return Player2DevelopmentComplete;
        }

        return false;
    }

    public bool IsPawnDeveloped(
        Pawn pawn)
    {
        if (pawn == null)
        {
            return false;
        }

        if (player1DevelopedPawns.Contains(pawn))
        {
            return true;
        }

        if (player2DevelopedPawns.Contains(pawn))
        {
            return true;
        }

        return false;
    }

    public bool IsDevelopmentPhaseActive()
    {
        return
            !Player1DevelopmentComplete ||
            !Player2DevelopmentComplete;
    }

    public bool AreBothPlayersFullyDeveloped()
    {
        return
            Player1DevelopmentComplete &&
            Player2DevelopmentComplete;
    }

    public string GetPhaseName()
    {
        if (AreBothPlayersFullyDeveloped())
        {
            return "BATTLE PHASE";
        }

        return "DEVELOPMENT PHASE";
    }
}