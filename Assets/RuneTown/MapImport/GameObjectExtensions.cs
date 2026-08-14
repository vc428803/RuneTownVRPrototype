using UnityEngine;

public static class GameObjectExtensions
{
    // 修改原因：RoMapChunk 初始化 MeshRenderer / MeshFilter 時需要
    // GetOrAddComponent，避免搬入整支 HelpersAndExtensions.cs。
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        var obj = go.GetComponent<T>();
        if (obj != null)
            return obj;

        return go.AddComponent<T>();
    }
}