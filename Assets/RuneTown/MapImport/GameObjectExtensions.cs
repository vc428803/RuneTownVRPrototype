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

    // 修改原因：RSM 模型若含位置或旋轉 Keyframe，需要把該節點及其子物件改為非 Static，
    // 沿用 RebuildClient 的遞迴處理方式。
    public static void ChangeStaticRecursive(this GameObject go, bool isStatic)
    {
        go.layer = isStatic
            ? LayerMask.NameToLayer("Object")
            : LayerMask.NameToLayer("DynamicObject");

        go.isStatic = isStatic;

        foreach (Transform t in go.transform)
        {
            ChangeStaticRecursive(t.gameObject, isStatic);
        }
    }
}