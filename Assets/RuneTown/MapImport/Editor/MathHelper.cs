using UnityEngine;

public static class MathHelper
{
    // 修改原因：RoMapData 判斷相鄰地圖頂點高度是否足夠接近時需要使用，
    // 僅保留目前地圖匯入流程實際用到的 CloseEnough，避免搬入整支 HelpersAndExtensions.cs。
    public static bool CloseEnough(float value, float target)
    {
        return Mathf.Abs(value - target) < 0.001f;
    }
}