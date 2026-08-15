using System.IO;
using Assets.Scripts.MapEditor;
using Assets.Scripts.MapEditor.Editor;
using UnityEditor;
using UnityEngine;

public static class RuneTownMapImportTestWindow
{
    [MenuItem("RuneTown/Map Import/Import GND Test")]
    public static void ImportGndTest()
    {
        // 修改原因：建立 RuneTown 第一版 Ragnarok GND 匯入測試入口，
        // 先驗證可以選取 .gnd 並成功呼叫 RagnarokMapLoader.ImportMap()。
        var gndPath = EditorUtility.OpenFilePanel(
            "Select Ragnarok GND",
            "",
            "gnd"
        );

        if (string.IsNullOrWhiteSpace(gndPath))
            return;

        // 修改原因：第一版先將測試輸出集中在固定資料夾，
        // 避免匯入結果散落到專案其他位置。
        const string outputPath = "Assets/RuneTown/MapImport/Generated";

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        try
        {
            var loader = new RagnarokMapLoader();

            var mapData = loader.ImportMap(
                gndPath,
                outputPath
            );

            AssetDatabase.Refresh();

            // 修改原因：將解析完成的 RoMapData 儲存成 Unity Asset，
            // 讓後續 Atlas 建立與 Chunk Mesh 生成可以引用同一份地圖資料。
            var mapAssetPath = Path.Combine(
                outputPath,
                mapData.name + ".asset"
            ).Replace("\\", "/");

            var existingMapData =
                AssetDatabase.LoadAssetAtPath<RoMapData>(mapAssetPath);

            if (existingMapData == null)
            {
                AssetDatabase.CreateAsset(mapData, mapAssetPath);
                AssetDatabase.SaveAssets();

                // 修改原因：建立 Asset 後重新從 AssetDatabase 載入，
                // 確保後續 RebuildAtlas 可以取得有效的 Unity Asset 路徑。
                mapData =
                    AssetDatabase.LoadAssetAtPath<RoMapData>(mapAssetPath);
            }
            else
            {
                // 修改原因：重複匯入同一張 GND 時更新既有 RoMapData，
                // 保留原本 Asset GUID，避免刪除重建造成其他引用失效。
                EditorUtility.CopySerialized(
                    mapData,
                    existingMapData
                );

                Object.DestroyImmediate(mapData);

                mapData = existingMapData;

                EditorUtility.SetDirty(mapData);
                AssetDatabase.SaveAssets();

                Debug.Log(
                    $"[RuneTown Map Import] Updated existing map asset: {mapAssetPath}"
                );
            }

            // 修改原因：RoMapChunk 需要 Atlas 與 AtlasRects 才能正確建立貼圖 UV，
            // 並確保此時 mapData 已經是 AssetDatabase 中具有有效路徑的 RoMapData。
            mapData.RebuildAtlas();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[RuneTown Map Import] GND import success: {mapData.name}, " +
                $"Size={mapData.Width}x{mapData.Height}, Asset={mapAssetPath}"
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError(
                $"[RuneTown Map Import] GND import failed:\n{ex}"
            );
        }
    }

    [MenuItem("RuneTown/Map Import/Generate Single Chunk Test")]
    public static void GenerateSingleChunkTest()
    {
        // 修改原因：先用已匯入完成的 gl_chyard RoMapData 建立單一 16x16 Chunk，
        // 驗證 RuneTown 的 RoMapData → RoMapChunk → MeshBuilder 地形生成流程。
        const string mapAssetPath =
            "Assets/RuneTown/MapImport/Generated/gl_chyard.asset";

        var mapData = AssetDatabase.LoadAssetAtPath<RoMapData>(mapAssetPath);

        if (mapData == null)
        {
            Debug.LogError(
                $"[RuneTown Map Import] Map asset not found: {mapAssetPath}"
            );
            return;
        }

        // 修改原因：第一階段只建立一個測試 Chunk，
        // 避免一次生成整張 160x160 地圖，方便先確認 Mesh、UV 與高度資料是否正常。
        var chunkObject = new GameObject("gl_chyard_TestChunk_0_0");

        var chunk = chunkObject.AddComponent<RoMapChunk>();

        // 修改原因：建立最小測試 Material，
        // RoMapChunk 會自行將 MapData.Atlas 指派給 Material.mainTexture。
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = "gl_chyard_TestMaterial";

        // 修改原因：改用 gl_chyard 地圖中央區域測試 Chunk，
        // 避免地圖邊界的大型高度落差影響對 Mesh 是否正常的判斷。
        var bounds = new RectInt(
            64,
            64,
            16,
            16
        );

        chunk.Initialize(
            mapData,
            material,
            material,
            bounds,
            2f,
            false
        );

        Selection.activeGameObject = chunkObject;
        SceneView.FrameLastActiveSceneView();

        // 修改原因：測試 Chunk 位置會調整，改為輸出實際 Bounds，
        // 避免 Console 顯示舊的固定座標造成誤判。
        Debug.Log(
            $"[RuneTown Map Import] Single Chunk generated successfully: " +
            $"gl_chyard bounds=({bounds.x},{bounds.y},{bounds.width},{bounds.height})"
        );
    }

    [MenuItem("RuneTown/Map Import/Generate Full GND Terrain Test")]
    public static void GenerateFullGndTerrainTest()
    {
        // 修改原因：建立完整 gl_chyard GND 地形測試，
        // 將 160x160 Cell 依 16x16 分割成 10x10、共 100 個 Chunk。
        const string mapAssetPath =
            "Assets/RuneTown/MapImport/Generated/gl_chyard.asset";

        var mapData =
            AssetDatabase.LoadAssetAtPath<RoMapData>(mapAssetPath);

        if (mapData == null)
        {
            Debug.LogError(
                $"[RuneTown Map Import] Map asset not found: {mapAssetPath}"
            );
            return;
        }

        const int chunkSize = 16;
        const float tileSize = 2f;

        int chunkCountX =
            mapData.Width / chunkSize;

        int chunkCountY =
            mapData.Height / chunkSize;

        // 修改原因：將完整 GND 地形集中在單一父物件，
        // 方便整組移動、檢查以及後續刪除測試結果。
        var parentObject =
            new GameObject("gl_chyard_FullGndTerrain");

        var material =
            new Material(
                Shader.Find("Universal Render Pipeline/Lit")
            );

        material.name = "gl_chyard_FullGndMaterial";

        int generatedChunkCount = 0;

        for (int chunkY = 0; chunkY < chunkCountY; chunkY++)
        {
            for (int chunkX = 0; chunkX < chunkCountX; chunkX++)
            {
                int boundsX =
                    chunkX * chunkSize;

                int boundsY =
                    chunkY * chunkSize;

                var bounds =
                    new RectInt(
                        boundsX,
                        boundsY,
                        chunkSize,
                        chunkSize
                    );

                var chunkObject =
                    new GameObject(
                        $"Chunk_{chunkX}_{chunkY}"
                    );

                chunkObject.transform.SetParent(
                    parentObject.transform
                );

                // 修改原因：沿用原 RebuildClient 的 Chunk 座標排列方式，
                // 讓 Chunk 世界位置與 GND Cell 座標維持一致。
                chunkObject.transform.localPosition =
                    new Vector3(
                        boundsX * tileSize,
                        0f,
                        boundsY * tileSize
                    );

                var chunk =
                    chunkObject.AddComponent<RoMapChunk>();

                chunk.Initialize(
                    mapData,
                    material,
                    material,
                    bounds,
                    tileSize,
                    false
                );

                generatedChunkCount++;
            }
        }

        Selection.activeGameObject = parentObject;
        SceneView.FrameLastActiveSceneView();

        Debug.Log(
            $"[RuneTown Map Import] Full GND terrain generated successfully: " +
            $"{mapData.name}, Size={mapData.Width}x{mapData.Height}, " +
            $"Chunks={chunkCountX}x{chunkCountY}, Total={generatedChunkCount}"
        );
    }

    [MenuItem("RuneTown/Map Import/Generate 2x2 Chunks Test")]
    public static void Generate2x2ChunksTest()
    {
        // 修改原因：以地圖中央區域建立 2x2、共 4 個相鄰 Chunk，
        // 驗證 Chunk 之間的高度、UV 與邊界是否可以正確銜接。
        const string mapAssetPath =
            "Assets/RuneTown/MapImport/Generated/gl_chyard.asset";

        var mapData =
            AssetDatabase.LoadAssetAtPath<RoMapData>(mapAssetPath);

        if (mapData == null)
        {
            Debug.LogError(
                $"[RuneTown Map Import] Map asset not found: {mapAssetPath}"
            );
            return;
        }

        // 修改原因：將本次 2x2 測試產生的 Chunk 集中在同一個父物件下，
        // 方便 Hierarchy 檢查與後續一次刪除。
        var parentObject =
            new GameObject("gl_chyard_TestChunks_2x2");

        var material =
            new Material(
                Shader.Find("Universal Render Pipeline/Lit")
            );

        material.name = "gl_chyard_TestMaterial_2x2";

        const int startX = 64;
        const int startY = 64;
        const int chunkSize = 16;
        const float tileSize = 2f;

        for (int chunkY = 0; chunkY < 2; chunkY++)
        {
            for (int chunkX = 0; chunkX < 2; chunkX++)
            {
                int boundsX =
                    startX + chunkX * chunkSize;

                int boundsY =
                    startY + chunkY * chunkSize;

                var bounds =
                    new RectInt(
                        boundsX,
                        boundsY,
                        chunkSize,
                        chunkSize
                    );

                var chunkObject =
                    new GameObject(
                        $"Chunk_{boundsX}_{boundsY}"
                    );

                chunkObject.transform.SetParent(
                    parentObject.transform
                );

                // 修改原因：RoMapChunk 的 Mesh 計算會依照實際地圖 Chunk 座標扣除
                // transform.position，因此位置必須與原 RebuildClient 的 Chunk 排列方式一致。
                chunkObject.transform.localPosition =
                    new Vector3(
                        boundsX * tileSize,
                        0f,
                        boundsY * tileSize
                    );

                var chunk =
                    chunkObject.AddComponent<RoMapChunk>();

                chunk.Initialize(
                    mapData,
                    material,
                    material,
                    bounds,
                    tileSize,
                    false
                );
            }
        }

        Selection.activeGameObject = parentObject;
        SceneView.FrameLastActiveSceneView();

        Debug.Log(
            "[RuneTown Map Import] 2x2 Chunk test generated successfully: " +
            "start=(64,64), chunkSize=16, count=4"
        );
    }


}