// 修改原因：
// PronteraClean 底下有大量地形 Chunk。
// 每個 Chunk 都已經有 Mesh Collider，但缺少 Teleportation Area，
// 導致 XR Ray 可以射到地面，卻無法透過 G 鍵進行瞬移。
//
// 此工具會：
// 1. 取得 Hierarchy 目前選取的 PronteraClean 根物件
// 2. 找出所有子物件
// 3. 只處理有 Collider 的 Chunk
// 4. 沒有 Teleportation Area 才新增
// 5. 已存在的自動略過
// 6. 支援 Undo，操作錯誤時可復原
// 7. 最後顯示新增與略過數量

using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public static class PronteraTeleportAreaSetup
{
    [MenuItem("Ragnarok/Setup Prontera Teleport Areas")]
    private static void SetupTeleportAreas()
    {
        GameObject selectedRoot = Selection.activeGameObject;

        if (selectedRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Prontera Teleport Area Setup",
                "請先在 Hierarchy 選取 PronteraClean 根物件。",
                "確定"
            );

            return;
        }

        Collider[] colliders =
            selectedRoot.GetComponentsInChildren<Collider>(true);

        if (colliders.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Prontera Teleport Area Setup",
                "選取物件底下找不到任何 Collider。\n\n" +
                "請確認你選取的是 PronteraClean。",
                "確定"
            );

            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Prontera Teleport Area Setup",
            $"目前選取：{selectedRoot.name}\n\n" +
            $"找到 Collider：{colliders.Length}\n\n" +
            "即將替所有有 Collider 的地形物件加入 Teleportation Area。",
            "開始設定",
            "取消"
        );

        if (!confirmed)
        {
            return;
        }

        int addedCount = 0;
        int skippedCount = 0;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        Undo.SetCurrentGroupName(
            "Setup Prontera Teleport Areas"
        );

        try
        {
            foreach (Collider collider in colliders)
            {
                if (collider == null)
                {
                    continue;
                }

                GameObject targetObject =
                    collider.gameObject;

                TeleportationArea existingArea =
                    targetObject.GetComponent<TeleportationArea>();

                if (existingArea != null)
                {
                    skippedCount++;
                    continue;
                }

                // 修改原因：
                // 使用 Undo.AddComponent 而不是一般 AddComponent，
                // 讓整次批次設定可以透過 Ctrl + Z 復原。
                TeleportationArea teleportationArea =
                    Undo.AddComponent<TeleportationArea>(
                        targetObject
                    );

                if (teleportationArea == null)
                {
                    Debug.LogWarning(
                        "[PronteraTeleportAreaSetup] " +
                        $"無法替 {targetObject.name} 加入 Teleportation Area。"
                    );

                    continue;
                }

                EditorUtility.SetDirty(targetObject);
                addedCount++;
            }

            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.SetDirty(selectedRoot);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[PronteraTeleportAreaSetup] 完成。" +
                $"新增：{addedCount}，略過：{skippedCount}"
            );

            EditorUtility.DisplayDialog(
                "Prontera Teleport Area Setup",
                "設定完成。\n\n" +
                $"新增 Teleportation Area：{addedCount}\n" +
                $"已存在而略過：{skippedCount}",
                "確定"
            );
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Prontera Teleport Area Setup 失敗",
                exception.Message,
                "確定"
            );
        }
    }

    [MenuItem(
        "Ragnarok/Setup Prontera Teleport Areas",
        true
    )]
    private static bool ValidateSetupTeleportAreas()
    {
        return Selection.activeGameObject != null;
    }

    [MenuItem("Ragnarok/Remove Prontera Teleport Areas")]
    private static void RemoveTeleportAreas()
    {
        GameObject selectedRoot =
            Selection.activeGameObject;

        if (selectedRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Remove Prontera Teleport Areas",
                "請先在 Hierarchy 選取 PronteraClean 根物件。",
                "確定"
            );

            return;
        }

        TeleportationArea[] areas =
            selectedRoot.GetComponentsInChildren<TeleportationArea>(
                true
            );

        if (areas.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Remove Prontera Teleport Areas",
                "目前選取物件底下沒有 Teleportation Area。",
                "確定"
            );

            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Remove Prontera Teleport Areas",
            $"即將移除 {areas.Length} 個 Teleportation Area。",
            "移除",
            "取消"
        );

        if (!confirmed)
        {
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        Undo.SetCurrentGroupName(
            "Remove Prontera Teleport Areas"
        );

        foreach (TeleportationArea area in areas)
        {
            if (area != null)
            {
                Undo.DestroyObjectImmediate(area);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.SetDirty(selectedRoot);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[PronteraTeleportAreaSetup] " +
            $"已移除 {areas.Length} 個 Teleportation Area。"
        );
    }

    [MenuItem(
        "Ragnarok/Remove Prontera Teleport Areas",
        true
    )]
    private static bool ValidateRemoveTeleportAreas()
    {
        return Selection.activeGameObject != null;
    }
}