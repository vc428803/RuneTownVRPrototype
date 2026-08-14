using System.IO;
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

            Debug.Log(
                $"[RuneTown Map Import] GND import success: {mapData.name}, " +
                $"Size={mapData.Width}x{mapData.Height}"
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError(
                $"[RuneTown Map Import] GND import failed:\n{ex}"
            );
        }
    }
}