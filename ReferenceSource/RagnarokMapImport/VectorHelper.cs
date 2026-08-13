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
}