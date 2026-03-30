using UnityEngine;

public static class BuildingPlacer
{
    private static int GetWidthOffset(int width)
    {
        return width % 2 == 0 ? 1 - (width / 2) : -(width / 2);
    }

    public static int GetDirection(Vector3 playerForward)
    {
        if (Mathf.Abs(playerForward.x) > Mathf.Abs(playerForward.z))
        {
            return playerForward.x > 0 ? 1 : 3;
        }
        else
        {
            return playerForward.z > 0 ? 0 : 2;
        }
    }

    public static BuildingFootprint GetFootprint(BuildingDataSO data, int direction, bool swapped)
    {
        int width = swapped ? data.Depth : data.Width;
        int depth = swapped ? data.Width : data.Depth;

        Vector2Int forward = direction switch
        {
            0 => new Vector2Int(0, 1),
            1 => new Vector2Int(1, 0),
            2 => new Vector2Int(0, -1),
            3 => new Vector2Int(-1, 0),
            _ => new Vector2Int(0, 1)
        };

        return new BuildingFootprint
        {
            Width = width,
            Depth = depth,
            WidthOffset = GetWidthOffset(width),
            Direction = direction,
            Forward = forward,
            Right = new Vector2Int(forward.y, -forward.x)
        };
    }
}
