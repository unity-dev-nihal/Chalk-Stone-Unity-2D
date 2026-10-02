using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Board Nodes")]
    [SerializeField] private BoardNode[] nodes;

    private readonly int[,] connections =
    {
        // 0
        { 0, 1 },
        { 0, 3 },
        { 0, 4 },

        // 1
        { 1, 0 },
        { 1, 2 },
        { 1, 4 },

        // 2
        { 2, 1 },
        { 2, 5 },
        { 2, 4 },

        // 3
        { 3, 0 },
        { 3, 4 },
        { 3, 6 },

        // 4
        { 4, 0 },
        { 4, 1 },
        { 4, 2 },
        { 4, 3 },
        { 4, 5 },
        { 4, 6 },
        { 4, 7 },
        { 4, 8 },

        // 5
        { 5, 2 },
        { 5, 4 },
        { 5, 8 },

        // 6
        { 6, 3 },
        { 6, 4 },
        { 6, 7 },

        // 7
        { 7, 4 },
        { 7, 6 },
        { 7, 8 },

        // 8
        { 8, 4 },
        { 8, 5 },
        { 8, 7 }
    };

    public BoardNode GetNode(int index)
    {
        if (index < 0 || index >= nodes.Length)
        {
            Debug.LogError($"Invalid node index: {index}");
            return null;
        }

        return nodes[index];
    }

    public bool AreConnected(int fromIndex, int toIndex)
    {
        for (int i = 0; i < connections.GetLength(0); i++)
        {
            if (connections[i, 0] == fromIndex &&
                connections[i, 1] == toIndex)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsOccupied(int index)
    {
        BoardNode node = GetNode(index);

        return node != null && !node.IsEmpty;
    }

    public PlayerOwner GetOwner(int index)
    {
        BoardNode node = GetNode(index);

        if (node == null)
            return PlayerOwner.None;

        return node.Owner;
    }

    public void ResetBoard()
    {
        foreach (BoardNode node in nodes)
        {
            if (node != null)
                node.ClearOwner();
        }
    }
}