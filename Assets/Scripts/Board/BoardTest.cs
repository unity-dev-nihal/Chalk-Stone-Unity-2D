using UnityEngine;

public class BoardTest : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;

    private void Start()
    {
        Debug.Log("===== CHALKSTONE BOARD TEST =====");

        TestConnection(0, 1);
        TestConnection(0, 3);
        TestConnection(0, 4);
        TestConnection(0, 2);

        Debug.Log("=================================");
    }

    private void TestConnection(int from, int to)
    {
        bool connected = boardManager.AreConnected(from, to);

        Debug.Log(
            $"Node {from} → Node {to}: " +
            (connected ? "LEGAL" : "NOT LEGAL")
        );
    }
}
