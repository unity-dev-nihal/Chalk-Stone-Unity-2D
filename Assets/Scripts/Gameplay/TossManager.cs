using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TossManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private PlacementManager placementManager;
    [SerializeField] private GameStateManager gameStateManager;

    [Header("Toss UI")]
    [SerializeField] private GameObject tossPanel;
    [SerializeField] private GameObject tossState;
    [SerializeField] private GameObject resultState;

    [Header("Toss Button")]
    [SerializeField] private Button tossButton;

    [Header("Coin")]
    [SerializeField] private Image coinImage;
    [SerializeField] private Sprite coinFront;
    [SerializeField] private Sprite coinBack;
    [SerializeField] private Sprite coinEdge;

    [Header("Toss Player Icons")]
    [SerializeField] private Image tossPlayer2Icon;
    [SerializeField] private Sprite player2TossIcon;
    [SerializeField] private Sprite aiTossIcon;

    [Header("Result")]
    [SerializeField] private Image resultPlayerIcon;
    [SerializeField] private TMPro.TMP_Text resultTitleText;
    [SerializeField] private TMPro.TMP_Text resultDescriptionText;

    [Header("Player Result Icons")]
    [SerializeField] private Sprite player1ResultIcon;
    [SerializeField] private Sprite player2ResultIcon;
    [SerializeField] private Sprite aiResultIcon;

    [Header("Timing")]
    [SerializeField] private float coinFlipDuration = 1.5f;
    [SerializeField] private float resultDisplayDuration = 5f;

    private bool tossInProgress;

    private void Start()
    {
        FindReferences();

        if (tossPanel != null)
            tossPanel.SetActive(true);

        if (tossState != null)
            tossState.SetActive(true);

        if (resultState != null)
            resultState.SetActive(false);

        if (coinImage != null && coinFront != null)
            coinImage.sprite = coinFront;

        UpdateTossPlayerIcon();

        if (tossButton != null)
            tossButton.onClick.AddListener(StartToss);

        if (gameStateManager != null)
            gameStateManager.SetState(GameState.Toss);
    }

    private void OnDestroy()
    {
        if (tossButton != null)
            tossButton.onClick.RemoveListener(StartToss);
    }

    private void FindReferences()
    {
        if (playerManager == null)
            playerManager = FindFirstObjectByType<PlayerManager>();

        if (placementManager == null)
            placementManager = FindFirstObjectByType<PlacementManager>();

        if (gameStateManager == null)
            gameStateManager = FindFirstObjectByType<GameStateManager>();
    }

    public void StartToss()
    {
        if (tossInProgress)
            return;

        tossInProgress = true;

        if (tossButton != null)
            tossButton.interactable = false;

        if (resultState != null)
            resultState.SetActive(false);

        if (tossState != null)
            tossState.SetActive(true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayTossFlip();

        StartCoroutine(TossRoutine());
    }

    private IEnumerator TossRoutine()
    {
        float elapsed = 0f;

        // Decide the winner before the animation.
        PlayerOwner winningPlayer =
            Random.value < 0.5f
                ? PlayerOwner.Player1
                : PlayerOwner.Player2;

        Sprite finalCoinSprite =
            winningPlayer == PlayerOwner.Player1
                ? coinFront
                : coinBack;

        int flipCount = 8;

        while (elapsed < coinFlipDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / coinFlipDuration
                );

            float spin =
                progress * flipCount;

            int phase =
                Mathf.FloorToInt(spin);

            int visualPhase =
                phase % 4;

            if (coinImage != null)
            {
                switch (visualPhase)
                {
                    case 0:

                        if (coinFront != null)
                            coinImage.sprite = coinFront;

                        break;

                    case 1:

                        if (coinEdge != null)
                            coinImage.sprite = coinEdge;

                        break;

                    case 2:

                        if (coinBack != null)
                            coinImage.sprite = coinBack;

                        break;

                    case 3:

                        if (coinEdge != null)
                            coinImage.sprite = coinEdge;

                        break;
                }
            }

            yield return null;
        }

        // Final result is always Front or Back.
        if (coinImage != null && finalCoinSprite != null)
            coinImage.sprite = finalCoinSprite;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayTossFall();

        yield return new WaitForSeconds(0.15f);

        ShowTossResult(winningPlayer);
    }

    private void ShowTossResult(PlayerOwner winningPlayer)
    {
        if (playerManager != null)
            playerManager.SetStartingPlayer(winningPlayer);

        if (tossState != null)
            tossState.SetActive(true);

        if (resultState != null)
            resultState.SetActive(true);

        UpdateResultIcon(winningPlayer);
        UpdateResultText(winningPlayer);

        Debug.Log(
            $"Toss Result: {winningPlayer} goes first."
        );

        StartCoroutine(
            FinishTossAfterDelay()
        );
    }

    private void UpdateTossPlayerIcon()
    {
        if (tossPlayer2Icon == null)
            return;

        if (IsVsAI())
        {
            if (aiTossIcon != null)
                tossPlayer2Icon.sprite = aiTossIcon;

            Debug.Log(
                "Toss UI: Player 2 icon changed to AI icon."
            );
        }
        else
        {
            if (player2TossIcon != null)
                tossPlayer2Icon.sprite = player2TossIcon;

            Debug.Log(
                "Toss UI: Player 2 icon changed to Player 2 icon."
            );
        }
    }

    private void UpdateResultIcon(PlayerOwner winningPlayer)
    {
        if (resultPlayerIcon == null)
            return;

        if (winningPlayer == PlayerOwner.Player1)
        {
            if (player1ResultIcon != null)
                resultPlayerIcon.sprite = player1ResultIcon;

            return;
        }

        if (winningPlayer == PlayerOwner.Player2)
        {
            if (IsVsAI())
            {
                if (aiResultIcon != null)
                    resultPlayerIcon.sprite = aiResultIcon;
            }
            else
            {
                if (player2ResultIcon != null)
                    resultPlayerIcon.sprite = player2ResultIcon;
            }
        }
    }

    private void UpdateResultText(PlayerOwner winningPlayer)
    {
        if (winningPlayer == PlayerOwner.Player1)
        {
            if (resultTitleText != null)
                resultTitleText.text = "PLAYER 1";

            if (resultDescriptionText != null)
                resultDescriptionText.text =
                    "Player 1 goes first.";

            return;
        }

        if (winningPlayer == PlayerOwner.Player2)
        {
            if (IsVsAI())
            {
                if (resultTitleText != null)
                    resultTitleText.text = "AI";

                if (resultDescriptionText != null)
                    resultDescriptionText.text =
                        "AI goes first.";
            }
            else
            {
                if (resultTitleText != null)
                    resultTitleText.text = "PLAYER 2";

                if (resultDescriptionText != null)
                    resultDescriptionText.text =
                        "Player 2 goes first.";
            }
        }
    }

    private bool IsVsAI()
    {
        if (GameSession.Instance == null)
        {
            Debug.LogWarning(
                "TossManager: GameSession instance not found."
            );

            return false;
        }

        return GameSession.Instance.SelectedGameMode ==
               GameMode.VsAI;
    }

    private IEnumerator FinishTossAfterDelay()
    {
        yield return new WaitForSeconds(
            resultDisplayDuration
        );

        if (resultState != null)
            resultState.SetActive(false);

        if (tossState != null)
            tossState.SetActive(false);

        if (tossPanel != null)
            tossPanel.SetActive(false);

        tossInProgress = false;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGameStart();

        if (placementManager != null)
            placementManager.BeginPlacementPhase();

        Debug.Log(
            "Toss finished. Placement phase started."
        );
    }
}