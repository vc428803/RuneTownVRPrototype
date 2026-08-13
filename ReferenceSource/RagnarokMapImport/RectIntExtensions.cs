using UnityEngine;

public static class RectIntExtensions
{
    // 修改原因：RoMapSharedMeshData 在重建局部 Mesh 時需要向外擴張處理區域，
    // 避免搬入整支 HelpersAndExtensions.cs 造成不必要依賴。
    public static RectInt ExpandRect(this RectInt src, int dist)
    {
        return new RectInt(
            src.xMin - dist,
            src.yMin - dist,
            src.width + dist * 2,
            src.height + dist * 2);
    }
}