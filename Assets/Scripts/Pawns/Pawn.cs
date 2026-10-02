using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Pawn : MonoBehaviour
{
    [Header("Pawn Image")]
    [SerializeField] private Image pawnImage;

    [Header("Player Pawn Assets")]
    [SerializeField] private Sprite player1PawnSprite;
    [SerializeField] private Sprite player2PawnSprite;

    [Header("Placement Animation")]
    [SerializeField] private float placementDropHeight = 35f;
    [SerializeField] private float placementDuration = 0.25f;
    [SerializeField] private float placementBounceHeight = 6f;
    [SerializeField] private float placementBounceDuration = 0.08f;

    [Header("Movement Animation")]
    [SerializeField] private float movementDuration = 0.45f;

    private PlayerOwner owner;
    private int currentNodeIndex = -1;

    private Coroutine animationCoroutine;

    public PlayerOwner Owner => owner;

    public int CurrentNodeIndex => currentNodeIndex;

    public bool IsAnimating { get; private set; }

    private void Awake()
    {
        if (pawnImage == null)
            pawnImage = GetComponent<Image>();

        if (pawnImage != null)
            pawnImage.raycastTarget = false;
    }

    public void Initialize(PlayerOwner pawnOwner)
    {
        owner = pawnOwner;
        UpdateAppearance();
    }

    public void SetNode(int nodeIndex)
    {
        currentNodeIndex = nodeIndex;
    }

    private void UpdateAppearance()
    {
        if (pawnImage == null)
            return;

        if (owner == PlayerOwner.Player1)
        {
            if (player1PawnSprite != null)
                pawnImage.sprite = player1PawnSprite;
        }
        else if (owner == PlayerOwner.Player2)
        {
            if (player2PawnSprite != null)
                pawnImage.sprite = player2PawnSprite;
        }
    }

    // ---------------------------------------------------------
    // PLACEMENT ANIMATION
    // ---------------------------------------------------------

    public void PlayPlacementAnimation(
        Vector3 targetPosition,
        Action onComplete = null)
    {
        StopCurrentAnimation();

        animationCoroutine =
            StartCoroutine(
                PlacementAnimationRoutine(
                    targetPosition,
                    onComplete
                )
            );
    }

    private IEnumerator PlacementAnimationRoutine(
        Vector3 targetPosition,
        Action onComplete)
    {
        IsAnimating = true;

        Vector3 startPosition =
            targetPosition +
            Vector3.up * placementDropHeight;

        transform.position = startPosition;

        float elapsed = 0f;

        while (elapsed < placementDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / placementDuration
                );

            float easedProgress =
                1f -
                Mathf.Pow(
                    1f - progress,
                    3f
                );

            transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    easedProgress
                );

            yield return null;
        }

        transform.position = targetPosition;

        // Small subtle landing bounce.
        Vector3 bouncePosition =
            targetPosition +
            Vector3.down * placementBounceHeight;

        elapsed = 0f;

        while (elapsed < placementBounceDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / placementBounceDuration
                );

            float easedProgress =
                Mathf.Sin(
                    progress * Mathf.PI * 0.5f
                );

            transform.position =
                Vector3.Lerp(
                    targetPosition,
                    bouncePosition,
                    easedProgress
                );

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < placementBounceDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / placementBounceDuration
                );

            float easedProgress =
                Mathf.Sin(
                    progress * Mathf.PI * 0.5f
                );

            transform.position =
                Vector3.Lerp(
                    bouncePosition,
                    targetPosition,
                    easedProgress
                );

            yield return null;
        }

        transform.position = targetPosition;

        IsAnimating = false;
        animationCoroutine = null;

        onComplete?.Invoke();
    }

    // ---------------------------------------------------------
    // MOVEMENT ANIMATION
    // ---------------------------------------------------------

    public void PlayMovementAnimation(
        Vector3 targetPosition,
        Action onComplete = null)
    {
        StopCurrentAnimation();

        animationCoroutine =
            StartCoroutine(
                MovementAnimationRoutine(
                    targetPosition,
                    onComplete
                )
            );
    }

    private IEnumerator MovementAnimationRoutine(
        Vector3 targetPosition,
        Action onComplete)
    {
        IsAnimating = true;

        Vector3 startPosition =
            transform.position;

        float elapsed = 0f;

        while (elapsed < movementDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / movementDuration
                );

            // Smooth ease-in/ease-out.
            float easedProgress =
                progress *
                progress *
                (3f - 2f * progress);

            transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    easedProgress
                );

            yield return null;
        }

        transform.position = targetPosition;

        IsAnimating = false;
        animationCoroutine = null;

        onComplete?.Invoke();
    }

    // ---------------------------------------------------------
    // STOP ANIMATION
    // ---------------------------------------------------------

    public void StopCurrentAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        IsAnimating = false;
    }
}