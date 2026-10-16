using UnityEngine;

public static class BlockBlastDragMath
{
    public static Vector2Int PieceOrigin(Vector2 center, Vector2 firstCell, int width, int height, float step)
    {
        float x = center.x - (width - 1) * step * 0.5f;
        float y = center.y + (height - 1) * step * 0.5f;
        return new Vector2Int(Mathf.RoundToInt((x - firstCell.x) / step),
            Mathf.RoundToInt((firstCell.y - y) / step));
    }

    public static bool NearBoard(Vector2 center, float boardSize, float tolerance)
    {
        float limit = boardSize * 0.5f + tolerance;
        return Mathf.Abs(center.x) <= limit && Mathf.Abs(center.y) <= limit;
    }
}
