using UnityEngine;
using UnityEngine.UI;

public class BoardNode : MonoBehaviour
{
    [Header("Node Information")]
    [SerializeField] private int nodeIndex;

    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image nodeImage;

    private PlayerOwner owner = PlayerOwner.None;

    private PlacementManager placementManager;
    private MovementManager movementManager;

    public int NodeIndex => nodeIndex;
    public PlayerOwner Owner => owner;
    public bool IsEmpty =>
        owner == PlayerOwner.None;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (nodeImage == null)
            nodeImage = GetComponent<Image>();

        placementManager =
            FindFirstObjectByType<PlacementManager>();

        movementManager =
            FindFirstObjectByType<MovementManager>();
    }

    private void Start()
    {
        if (button != null)
            button.onClick.AddListener(
                OnNodeClicked
            );
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(
                OnNodeClicked
            );
    }

    private void OnNodeClicked()
    {
        // PLACEMENT PHASE
        if (placementManager != null &&
            !placementManager.PlacementComplete)
        {
            placementManager.TryPlacePawn(
                nodeIndex
            );

            return;
        }

        // MOVEMENT PHASE
        if (movementManager != null &&
            movementManager.MovementPhaseActive)
        {
            movementManager.HandleNodeClicked(
                nodeIndex
            );

            return;
        }

        Debug.LogWarning(
            $"Node {nodeIndex} clicked, " +
            "but no active gameplay phase."
        );
    }

    public void SetOwner(PlayerOwner newOwner)
    {
        owner = newOwner;
    }

    public void ClearOwner()
    {
        owner = PlayerOwner.None;
    }

    public void SetInteractable(bool value)
    {
        if (button != null)
            button.interactable = value;
    }
}