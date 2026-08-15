using UnityEngine;

public static class VectorHelper
{
    // 修改原因：RoMapData 建立 Texture Atlas UV 時需要將 0~1 UV
    // 重新映射到 Atlas 中對應的 Rect 範圍。
    public static Vector2 RemapUV(Vector2 pos, Rect space)
    {
        var x = pos.x.Remap(0f, 1f, space.xMin, space.xMax);
        var y = pos.y.Remap(0f, 1f, space.yMin, space.yMax);

        return new Vector2(x, y);
    }

    // 修改原因：提供 RemapUV 所需的 float 範圍轉換，
    // 避免搬入 RebuildClient 整支 HelpersAndExtensions.cs 所產生的額外依賴。
    public static float Remap(this float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    // 修改原因：RagnarokWalkableDataImporter 建立 GAT Walk Cell 的 Tile 時
    // 需要預設四邊形 UV，因此補上 RebuildClient 原本使用的基本 UV 排列。
    public static Vector2[] DefaultQuadUVs()
    {
        return new[]
        {
        new Vector2(0f, 1f),
        new Vector2(1f, 1f),
        new Vector2(0f, 0f),
        new Vector2(1f, 0f)
    };
    }

    // 修改原因：RoMapSharedMeshData 建立地圖 Mesh 時需要計算四邊形平均法線，
    // 因此保留 RebuildClient 原本的四邊形法線計算邏輯。
    public static Vector3 CalcQuadNormal(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        var c1 = Vector3.Cross(v2 - v1, v3 - v1);
        var c2 = Vector3.Cross(v4 - v2, v1 - v2);
        var c3 = Vector3.Cross(v3 - v4, v2 - v4);
        var c4 = Vector3.Cross(v1 - v3, v4 - v3);

        return Vector3.Normalize((c1 + c2 + c3 + c4) / 4);
    }
}