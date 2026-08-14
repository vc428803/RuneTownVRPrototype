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

            if (AssetDatabase.LoadAssetAtPath<RoMapData>(mapAssetPath) == null)
            {
                AssetDatabase.CreateAsset(mapData, mapAssetPath);
                AssetDatabase.SaveAssets();
            }
            else
            {
                // 修改原因：測試階段避免重複匯入同一張地圖時 CreateAsset 報錯。
                Debug.LogWarning(
                    $"[RuneTown Map Import] Map asset already exists: {mapAssetPath}"
                );
            }

            // 修改原因：RoMapChunk 需要 Atlas 與 AtlasRects 才能正確建立貼圖 UV，
            // 因此在進入 Mesh 測試前先建立地圖 Texture Atlas。
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

        var bounds = new RectInt(
            0,
            0,
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

        Debug.Log(
            "[RuneTown Map Import] Single Chunk generated successfully: " +
            "gl_chyard bounds=(0,0,16,16)"
        );
    }
}