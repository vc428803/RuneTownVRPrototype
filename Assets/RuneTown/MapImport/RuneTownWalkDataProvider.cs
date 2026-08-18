using UnityEngine;
using Assets.Scripts.MapEditor;

public class RuneTownWalkDataProvider : MonoBehaviour
{
    [SerializeField]
    private RagnarokWalkData walkData;

    public RagnarokWalkData WalkData => walkData;

    public bool IsCellWalkable(Vector2Int cell)
    {
        // 修改原因：沿用原版 RebuildClient 的 CellType 判斷方式，
        // 由 GAT Walkable flag 決定該格是否可行走。
        if (walkData == null)
            return false;

        if (cell.x < 0 || cell.x >= walkData.Width ||
            cell.y < 0 || cell.y >= walkData.Height)
        {
            return false;
        }

        return walkData.CellWalkable(cell.x, cell.y);
    }

    public Vector2Int GetTilePositionForPoint(Vector3 point)
    {
        // 修改原因：沿用原版 RebuildClient 的 World X/Z → GAT Cell 對應方式。
        var position = transform.position;

        var x = Mathf.FloorToInt(point.x - position.x);
        var y = Mathf.FloorToInt(point.z - position.z);

        return new Vector2Int(x, y);
    }

    public float GetHeightForPosition(Vector3 position)
    {
        // 修改原因：沿用原版 RebuildClient 的 GAT 四角高度雙線性插值，
        // 計算指定 World X/Z 對應的實際 WalkData 地面高度。
        if (walkData == null)
            return position.y;

        var tile = GetTilePositionForPoint(position);

        if (!IsCellInsideMap(tile))
            return position.y;

        var localX = position.x - transform.position.x;
        var localZ = position.z - transform.position.z;

        var realX = localX - tile.x;
        var realY = localZ - tile.y;

        var heights = walkData.Cell(tile).Heights;

        var v1 = heights[0] * (1f - realX) * realY;
        var v2 = heights[1] * realX * realY;
        var v3 = heights[2] * (1f - realX) * (1f - realY);
        var v4 = heights[3] * realX * (1f - realY);

        return (v1 + v2 + v3 + v4) * RoMapData.YScale + 0.05f;
    }

    public void DebugPosition(Vector3 worldPosition)
    {
        // 修改原因：用指定 World Position 驗證 GAT Cell 對應、
        // Walkable 狀態與原版公式計算出的地面高度是否正確。
        var tile = GetTilePositionForPoint(worldPosition);

        bool walkable = IsCellWalkable(tile);
        float height = GetHeightForPosition(worldPosition);

        UnityEngine.Debug.Log(
            "[RuneTown WalkData Provider Debug] " +
            "World=" + worldPosition +
            ", Cell=" + tile +
            ", Walkable=" + walkable +
            ", CalculatedY=" + height
        );
    }

    private bool IsCellInsideMap(Vector2Int cell)
    {
        // 修改原因：集中處理 GAT 邊界檢查，避免存取 WalkData.Cells 時超出範圍。
        return
            cell.x >= 0 &&
            cell.x < walkData.Width &&
            cell.y >= 0 &&
            cell.y < walkData.Height;
    }
}