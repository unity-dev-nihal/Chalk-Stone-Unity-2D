using System;

[Serializable]
public class PlayerData
{
    public PlayerOwner owner;

    public bool isAI;

    public int totalPawns = 3;

    public int placedPawns;

    public int remainingPawns => totalPawns - placedPawns;

    public PlayerData(PlayerOwner owner, bool isAI)
    {
        this.owner = owner;
        this.isAI = isAI;

        totalPawns = 3;
        placedPawns = 0;
    }

    public void Reset()
    {
        placedPawns = 0;
    }

    public bool HasPawnsToPlace()
    {
        return placedPawns < totalPawns;
    }

    public void RegisterPawnPlacement()
    {
        if (placedPawns < totalPawns)
        {
            placedPawns++;
        }
    }
}
