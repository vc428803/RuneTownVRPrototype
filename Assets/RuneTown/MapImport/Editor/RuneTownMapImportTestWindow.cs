using Assets.Scripts.MapEditor;
using Assets.Scripts.MapEditor.Editor;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
// 修改原因：RSW 10 Real Models 測試需要用 HashSet 過濾重複的 RSM FileName。
using System.Collections.Generic;

public static class RuneTownMapImportTestWindow
{
    [MenuItem("RuneTown/Map Import/Generate Full RSW Models")]
    public static void GenerateFullRswModels()
    {
        // 修改原因：10 個真實 RSM 已驗證成功，
        // 現在依照 RSW 全部 Model Placement 生成完整地圖模型。
        var rswPath = EditorUtility.OpenFilePanel(
            "Select Ragnarok RSW",
            "",
            "rsw"
        );

        if (string.IsNullOrWhiteSpace(rswPath))
            return;

        RagnarokWorld world = null;

        // 修改原因：同一個 RSM 在 RSW 中可能被放置多次，
        // 每種 RSM 只 Load / Compile 一次，之後直接 Instantiate Template。
        var modelTemplates =
            new Dictionary<string, GameObject>(
                System.StringComparer.OrdinalIgnoreCase
            );

        // 修改原因：若某個 RSM 本身載入失敗，
        // 記錄後跳過同檔案後續 Placement，避免重複報錯。
        var failedFiles =
            new HashSet<string>(
                System.StringComparer.OrdinalIgnoreCase
            );

        try
        {
            const string mapAssetPath =
                "Assets/RuneTown/MapImport/Generated/gl_chyard.asset";

            var mapData =
                AssetDatabase.LoadAssetAtPath<RoMapData>(
                    mapAssetPath
                );

            if (mapData == null)
            {
                Debug.LogError(
                    $"[RuneTown Map Import] Map asset not found: {mapAssetPath}"
                );
                return;
            }

            // 修改原因：沿用目前已驗證成功的 RSW parser，
            // 取得 gl_chyard 全部 Model Placement。
            world =
                RagnarokResourceLoader.LoadResourceFile(
                    rswPath,
                    mapData
                );

            if (world == null ||
                world.Models == null ||
                world.Models.Count == 0)
            {
                Debug.LogError(
                    "[RuneTown Map Import] RSW contains no models."
                );
                return;
            }

            var mapRoot =
                GetOrCreateGeneratedMapRoot(
                    mapData.name
                );

            const string modelsRootName =
                "Models";

            // 修改原因：正式完整 Models 生成時，
            // 只清除既有 Models，不影響 Ground 或其他地圖內容。
            var existingModelsRoot =
                mapRoot.transform.Find(
                    modelsRootName
                );

            if (existingModelsRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    existingModelsRoot.gameObject
                );
            }

            var modelsRoot =
                new GameObject(
                    modelsRootName
                );

            modelsRoot.transform.SetParent(
                mapRoot.transform,
                false
            );

            // 修改原因：沿用 RebuildClient 的 RSW Resource Root InitialSize Offset。
            modelsRoot.transform.localPosition =
                new Vector3(
                    mapData.InitialSize.x,
                    0f,
                    mapData.InitialSize.y
                );

            var dataDirectory =
                RagnarokDirectory.GetRagnarokDataDirectory;

            int generatedCount = 0;
            int missingCount = 0;
            int failedCount = 0;
            int uniqueModelCount = 0;

            for (int i = 0; i < world.Models.Count; i++)
            {
                var placement =
                    world.Models[i];

                if (placement == null ||
                    string.IsNullOrWhiteSpace(placement.FileName))
                {
                    continue;
                }

                if (failedFiles.Contains(placement.FileName))
                {
                    failedCount++;
                    continue;
                }

                GameObject template;

                // 修改原因：只有第一次遇到某個 RSM 時才真正讀取及 Compile；
                // 同一模型其他 Placement 直接使用已建立的 Template。
                if (!modelTemplates.TryGetValue(
                        placement.FileName,
                        out template))
                {
                    var modelPath =
                        Path.Combine(
                            dataDirectory,
                            "model",
                            placement.FileName
                        )
                        .Replace("\\", "/");

                    if (!File.Exists(modelPath))
                    {
                        Debug.LogWarning(
                            $"[RuneTown Map Import] Skip missing RSM: {modelPath}"
                        );

                        failedFiles.Add(
                            placement.FileName
                        );

                        missingCount++;
                        continue;
                    }

                    var savePath =
                        DirectoryHelper.GetRelativeDirectory(
                            dataDirectory,
                            Path.GetDirectoryName(modelPath)
                        );

                    RagnarokModelLoader modelLoader = null;

                    try
                    {
                        modelLoader =
                            new RagnarokModelLoader();

                        modelLoader.LoadModel(
                            modelPath,
                            savePath
                        );

                        template =
                            modelLoader.Compile();

                        if (template == null)
                        {
                            Debug.LogError(
                                $"[RuneTown Map Import] RSM Compile returned null: {placement.FileName}"
                            );

                            failedFiles.Add(
                                placement.FileName
                            );

                            failedCount++;
                            continue;
                        }

                        // 修改原因：Template 只用來複製 Placement，
                        // 不應直接成為正式 Scene 中的一個模型。
                        template.name =
                            $"RSM_Template_{Path.GetFileNameWithoutExtension(placement.FileName)}";

                        template.SetActive(
                            false
                        );

                        template.hideFlags =
                            HideFlags.HideAndDontSave;

                        modelTemplates.Add(
                            placement.FileName,
                            template
                        );

                        uniqueModelCount++;
                    }
                    catch (System.Exception modelEx)
                    {
                        Debug.LogError(
                            $"[RuneTown Map Import] Failed to load RSM: " +
                            $"{placement.FileName}\n{modelEx}"
                        );

                        failedFiles.Add(
                            placement.FileName
                        );

                        failedCount++;
                        continue;
                    }
                    finally
                    {
                        modelLoader?.Dispose();
                    }
                }

                // 修改原因：每一筆 RSW Placement 都需要獨立 GameObject，
                // 但共用同一份 Mesh / Material 資產。
                var realModel =
                    UnityEngine.Object.Instantiate(
                        template
                    );

                realModel.hideFlags =
                    HideFlags.None;

                realModel.SetActive(
                    true
                );

                realModel.name =
                    $"RSM_{i}_{Path.GetFileNameWithoutExtension(placement.FileName)}";

                realModel.transform.SetParent(
                    modelsRoot.transform,
                    false
                );

                // 修改原因：沿用已驗證成功的 RSW Position 轉換。
                realModel.transform.localPosition =
                    new Vector3(
                        placement.Position.x / 5f,
                        -placement.Position.y / 5f,
                        placement.Position.z / 5f
                    )
                    + new Vector3(
                        0f,
                        0.01f,
                        0f
                    );

                // 修改原因：沿用 RebuildClient 的 RSW Rotation 組合順序。
                var rz =
                    Matrix4x4.Rotate(
                        Quaternion.Euler(
                            0f,
                            0f,
                            -placement.Rotation.z
                        )
                    );

                var rx =
                    Matrix4x4.Rotate(
                        Quaternion.Euler(
                            -placement.Rotation.x,
                            0f,
                            0f
                        )
                    );

                var ry =
                    Matrix4x4.Rotate(
                        Quaternion.Euler(
                            0f,
                            placement.Rotation.y,
                            0f
                        )
                    );

                var finalRotation =
                    rz * rx * ry;

                realModel.transform.localRotation =
                    finalRotation.rotation;

                realModel.transform.localScale =
                    placement.Scale;

                generatedCount++;

                // 修改原因：完整地圖可能接近千筆 Placement，
                // 每 100 個輸出一次進度，避免 Console 被大量逐筆 Log 塞滿。
                if (generatedCount % 100 == 0)
                {
                    Debug.Log(
                        $"[RuneTown Map Import] Full RSW Models progress: " +
                        $"{generatedCount}/{world.Models.Count}"
                    );
                }
            }

            // 修改原因：正式 Placement 全部建立完成後，
            // Template 已不再需要，避免殘留在 Editor Scene。
            foreach (var pair in modelTemplates)
            {
                if (pair.Value != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        pair.Value
                    );
                }
            }

            modelTemplates.Clear();

            Selection.activeGameObject =
                modelsRoot;

            SceneView.FrameLastActiveSceneView();

            EditorUtility.SetDirty(
                mapRoot
            );

            Debug.Log(
                $"[RuneTown Map Import] Full RSW Models generated successfully: " +
                $"Placements={generatedCount}/{world.Models.Count}, " +
                $"UniqueRSM={uniqueModelCount}, " +
                $"Missing={missingCount}, " +
                $"Failed={failedCount}"
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError(
                $"[RuneTown Map Import] Full RSW Models generation failed:\n{ex}"
            );
        }
        finally
        {
            // 修改原因：若流程中途發生例外，也必須清除暫存 Template。
            foreach (var pair in modelTemplates)
            {
                if (pair.Value != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        pair.Value
                    );
                }
            }

            if (world != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    world
                );
            }
        }
    }

    [MenuItem("RuneTown/Map Import/Debug Object Shader")]
    public static void DebugObjectShader()
    {
        // 修改原因：確認 ObjectShader 是否已被 Unity 正確匯入，
        // 並比較 AssetDatabase 載入與 Shader.Find 的結果。
        const string shaderPath =
            "Assets/RuneTown/MapImport/Shaders/ObjectShader.shader";

        var shaderAsset =
            AssetDatabase.LoadAssetAtPath<Shader>(
                shaderPath
            );

        var shaderFind =
            Shader.Find(
                "Custom/ObjectShader"
            );

        Debug.Log(
            $"[RuneTown Map Import] ObjectShader Debug: " +
            $"Asset={(shaderAsset != null ? shaderAsset.name : "NULL")}, " +
            $"Supported={(shaderAsset != null ? shaderAsset.isSupported.ToString() : "N/A")}, " +
            $"Shader.Find={(shaderFind != null ? shaderFind.name : "NULL")}"
        );
    }

    [MenuItem("RuneTown/Map Import/Import RSW Test")]
    public static void ImportRswTest()
    {
        // 修改原因：建立 RuneTown 第一版 Ragnarok RSW 匯入測試入口，
        // 先驗證 .rsw 可以成功解析成 RagnarokWorld，
        // 暫時不建立模型、不處理 Scene Placement。
        var rswPath = EditorUtility.OpenFilePanel(
            "Select Ragnarok RSW",
            "",
            "rsw"
        );

        if (string.IsNullOrWhiteSpace(rswPath))
            return;

        try
        {
            // 修改原因：RagnarokResourceLoader 解析 Water 時會同步寫入 RoMapData.Water，
            // 因此先載入目前已完成的 gl_chyard GND MapData 作為 RSW 解析基礎。
            const string mapAssetPath =
                "Assets/RuneTown/MapImport/Generated/gl_chyard.asset";

            var mapData =
                AssetDatabase.LoadAssetAtPath<RoMapData>(
                    mapAssetPath
                );

            if (mapData == null)
            {
                Debug.LogError(
                    $"[RuneTown Map Import] Map asset not found: {mapAssetPath}"
                );
                return;
            }

            var world =
                RagnarokResourceLoader.LoadResourceFile(
                    rswPath,
                    mapData
                );

            if (world == null)
            {
                Debug.LogError(
                    "[RuneTown Map Import] RSW import failed: RagnarokWorld is null."
                );
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 修改原因：第一階段只輸出 RSW 解析結果，
            // 用來確認 Models / Lights / Sounds / Effects / Water 是否成功讀取。
            Debug.Log(
                $"[RuneTown Map Import] RSW import success: " +
                $"{world.MapName}, " +
                $"Version={world.Version}, " +
                $"Models={world.Models.Count}, " +
                $"Lights={world.Lights.Count}, " +
                $"Sounds={world.Sounds.Count}, " +
                $"Effects={world.Effects.Count}, " +
                $"Water={(world.Water != null ? "Created" : "Missing")}"
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError(
                $"[RuneTown Map Import] RSW import failed:\n{ex}"
            );
        }
    }

    [MenuItem("RuneTown/Map Import/Generate RSW 10 Model Placement Test")]
    public static void GenerateRsw10ModelPlacementTest()
    {
        // 修改原因：先以 10 個 Cube 代替真正的 RSM 模型，
        // 驗證 RSW 的 Position / Rotation / Scale 與 GND 是否可以在同一 Root 內正確對齊。
        var rswPath = EditorUtility.OpenFilePanel(
            "Select Ragnarok RSW",
            "",
            "rsw"
        );

        if (string.IsNullOrWhiteSpace(rswPath))
            return;

        try
        {
            const string mapAssetPath =
                "Assets/RuneTown/MapImport/Generated/gl_chyard.asset";

            var mapData =
                AssetDatabase.LoadAssetAtPath<RoMapData>(
                    mapAssetPath
                );

            if (mapData == null)
            {
                Debug.LogError(
                    $"[RuneTown Map Import] Map asset not found: {mapAssetPath}"
                );
                return;
            }

            // 修改原因：沿用已驗證成功的 RagnarokResourceLoader，
            // 直接取得 RSW 內實際的 Model Placement 資料。
            var world =
                RagnarokResourceLoader.LoadResourceFile(
                    rswPath,
                    mapData
                );

            if (world == null)
            {
                Debug.LogError(
                    "[RuneTown Map Import] RSW Model Placement test failed: RagnarokWorld is null."
                );
                return;
            }

            var mapRoot =
                GetOrCreateGeneratedMapRoot(
                    mapData.name
                );

            const string modelsName =
                "Models";

            // 修改原因：重複測試時只清除 Models，
            // 不影響同一 GeneratedMapRoot 下已生成的 Ground。
            var existingModels =
                mapRoot.transform.Find(
                    modelsName
                );

            if (existingModels != null)
            {
                Object.DestroyImmediate(
                    existingModels.gameObject
                );
            }

            var modelsObject =
                new GameObject(
                    modelsName
                );

            modelsObject.transform.SetParent(
                mapRoot.transform,
                false
            );

            // 修改原因：RebuildClient 會將整個 RSW Resource Root
            // 放在 GND InitialSize 對應的位置，
            // 這是 RO 地圖內部座標規則，不包含任何 Scene 測試用 Offset。
            modelsObject.transform.localPosition =
                new Vector3(
                    mapData.InitialSize.x,
                    0f,
                    mapData.InitialSize.y
                );

            int modelCount =
                Mathf.Min(
                    10,
                    world.Models.Count
                );

            for (int i = 0; i < modelCount; i++)
            {
                var model =
                    world.Models[i];

                var cube =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cube
                    );

                cube.name =
                    $"RSW_Model_{i}_{Path.GetFileNameWithoutExtension(model.FileName)}";

                cube.transform.SetParent(
                    modelsObject.transform,
                    false
                );

                // 修改原因：沿用 RebuildClient 原始 RSW Position 轉換方式，
                // RSW 座標除以 5，並將 Y 軸反向轉成 Unity 座標。
                cube.transform.localPosition =
                    new Vector3(
                        model.Position.x / 5f,
                        -model.Position.y / 5f,
                        model.Position.z / 5f
                    )
                    + new Vector3(
                        0f,
                        0.01f,
                        0f
                    );

                // 修改原因：沿用 RebuildClient 真正的 Rotation 組合順序，
                // 避免直接 Quaternion.Euler(model.Rotation) 造成方向差異。
                var rz =
                    Matrix4x4.Rotate(
                        Quaternion.Euler(
                            0f,
                            0f,
                            -model.Rotation.z
                        )
                    );

                var rx =
                    Matrix4x4.Rotate(
                        Quaternion.Euler(
                            -model.Rotation.x,
                            0f,
                            0f
                        )
                    );

                var ry =
                    Matrix4x4.Rotate(
                        Quaternion.Euler(
                            0f,
                            model.Rotation.y,
                            0f
                        )
                    );

                var finalRotation =
                    rz * rx * ry;

                cube.transform.rotation =
                    finalRotation.rotation;

                cube.transform.localScale =
                    model.Scale;

                // 修改原因：Cube 只作為 Placement 視覺標記，
                // 不需要 Collider 參與物理運算。
                var collider =
                    cube.GetComponent<Collider>();

                if (collider != null)
                {
                    Object.DestroyImmediate(
                        collider
                    );
                }

                Debug.Log(
                    $"[RuneTown Map Import] RSW Model {i}: " +
                    $"File={model.FileName}, " +
                    $"RO Pos={model.Position}, " +
                    $"Local Pos={cube.transform.localPosition}, " +
                    $"Rot={model.Rotation}, " +
                    $"Scale={model.Scale}"
                );
            }

            Selection.activeGameObject =
                mapRoot;

            SceneView.FrameLastActiveSceneView();

            Object.DestroyImmediate(
                world
            );

            Debug.Log(
                $"[RuneTown Map Import] RSW 10 Model Placement test generated successfully: " +
                $"Count={modelCount}"
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError(
                $"[RuneTown Map Import] RSW Model Placement test failed:\n{ex}"
            );
        }
    }

    [MenuItem("RuneTown/Map Import/Generate RSW 1 Real Model Test")]
    public static void GenerateRsw1RealModelTest()
    {
        // 修改原因：在已驗證 RSW Placement 座標後，
        // 先只載入第一個真實 RSM 模型，確認 RSM Mesh / Texture / Scale / Rotation 與 GND 對齊。
        var rswPath = EditorUtility.OpenFilePanel(
            "Select Ragnarok RSW",
            "",
            "rsw"
        );

        if (string.IsNullOrWhiteSpace(rswPath))
            return;

        RagnarokModelLoader modelLoader = null;
        RagnarokWorld world = null;

        try
        {
            const string mapAssetPath =
                "Assets/RuneTown/MapImport/Generated/gl_chyard.asset";

            var mapData =
                AssetDatabase.LoadAssetAtPath<RoMapData>(
                    mapAssetPath
                );

            if (mapData == null)
            {
                Debug.LogError(
                    $"[RuneTown Map Import] Map asset not found: {mapAssetPath}"
                );
                return;
            }

            // 修改原因：沿用目前已驗證成功的 RSW parser，
            // 從實際 gl_chyard.rsw 取得模型檔名及 Placement 資料。
            world =
                RagnarokResourceLoader.LoadResourceFile(
                    rswPath,
                    mapData
                );

            if (world == null ||
                world.Models == null ||
                world.Models.Count == 0)
            {
                Debug.LogError(
                    "[RuneTown Map Import] RSW contains no models."
                );
                return;
            }

            var placement =
                world.Models[0];

            var dataDirectory =
                RagnarokDirectory.GetRagnarokDataDirectory;

            var modelPath =
                Path.Combine(
                    dataDirectory,
                    "model",
                    placement.FileName
                )
                .Replace("\\", "/");

            if (!File.Exists(modelPath))
            {
                Debug.LogError(
                    $"[RuneTown Map Import] RSM file not found: {modelPath}"
                );
                return;
            }

            var savePath =
                DirectoryHelper.GetRelativeDirectory(
                    dataDirectory,
                    Path.GetDirectoryName(modelPath)
                );

            // 修改原因：第一階段只驗證單一 RSM，
            // 直接使用剛移植完成的 RagnarokModelLoader。
            modelLoader =
                new RagnarokModelLoader();

            modelLoader.LoadModel(
                modelPath,
                savePath
            );

            var realModel =
                modelLoader.Compile();

            if (realModel == null)
            {
                Debug.LogError(
                    "[RuneTown Map Import] RSM Compile returned null."
                );
                return;
            }

            var mapRoot =
                GetOrCreateGeneratedMapRoot(
                    mapData.name
                );

            const string testRootName =
                "RealModelTest";

            // 修改原因：真實模型測試獨立於現有 Models Cube 測試，
            // 避免破壞目前已驗證的 10 個 Placement 標記。
            var existingTestRoot =
                mapRoot.transform.Find(
                    testRootName
                );

            if (existingTestRoot != null)
            {
                Object.DestroyImmediate(
                    existingTestRoot.gameObject
                );
            }

            var testRoot =
                new GameObject(
                    testRootName
                );

            testRoot.transform.SetParent(
                mapRoot.transform,
                false
            );

            // 修改原因：沿用 RebuildClient 的 RSW Resource Root InitialSize Offset。
            testRoot.transform.localPosition =
                new Vector3(
                    mapData.InitialSize.x,
                    0f,
                    mapData.InitialSize.y
                );

            realModel.name =
                $"RSM_RealModel_0_{Path.GetFileNameWithoutExtension(placement.FileName)}";

            realModel.transform.SetParent(
                testRoot.transform,
                false
            );

            // 修改原因：沿用已由 Cube Placement Test 驗證的 RSW Position 轉換。
            realModel.transform.localPosition =
                new Vector3(
                    placement.Position.x / 5f,
                    -placement.Position.y / 5f,
                    placement.Position.z / 5f
                )
                + new Vector3(
                    0f,
                    0.01f,
                    0f
                );

            // 修改原因：沿用 RebuildClient 的 RSW Rotation 組合順序。
            var rz =
                Matrix4x4.Rotate(
                    Quaternion.Euler(
                        0f,
                        0f,
                        -placement.Rotation.z
                    )
                );

            var rx =
                Matrix4x4.Rotate(
                    Quaternion.Euler(
                        -placement.Rotation.x,
                        0f,
                        0f
                    )
                );

            var ry =
                Matrix4x4.Rotate(
                    Quaternion.Euler(
                        0f,
                        placement.Rotation.y,
                        0f
                    )
                );

            var finalRotation =
                rz * rx * ry;

            realModel.transform.localRotation =
                finalRotation.rotation;

            realModel.transform.localScale =
                placement.Scale;

            Selection.activeGameObject =
                realModel;

            SceneView.FrameLastActiveSceneView();

            Debug.Log(
                $"[RuneTown Map Import] RSW 1 Real Model test success: " +
                $"File={placement.FileName}, " +
                $"Path={modelPath}, " +
                $"Local Pos={realModel.transform.localPosition}, " +
                $"Rot={placement.Rotation}, " +
                $"Scale={placement.Scale}"
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError(
                $"[RuneTown Map Import] RSW 1 Real Model test failed:\n{ex}"
            );
        }
        finally
        {
            modelLoader?.Dispose();

            if (world != null)
            {
                Object.DestroyImmediate(
                    world
                );
            }
        }
    }

    [MenuItem("RuneTown/Map Import/Generate RSW 10 Real Models Test")]
    public static void GenerateRsw10RealModelsTest()
    {
        // 修改原因：單一 RSM 已驗證成功，
        // 下一階段挑選 10 個不同的 RSM，確認不同模型的 Mesh / Texture / Placement 都能正常生成。
        var rswPath = EditorUtility.OpenFilePanel(
            "Select Ragnarok RSW",
            "",
            "rsw"
        );

        if (string.IsNullOrWhiteSpace(rswPath))
            return;

        RagnarokWorld world = null;

        try
        {
            const string mapAssetPath =
                "Assets/RuneTown/MapImport/Generated/gl_chyard.asset";

            var mapData =
                AssetDatabase.LoadAssetAtPath<RoMapData>(
                    mapAssetPath
                );

            if (mapData == null)
            {
                Debug.LogError(
                    $"[RuneTown Map Import] Map asset not found: {mapAssetPath}"
                );
                return;
            }

            // 修改原因：沿用已驗證成功的 RSW parser，
            // 取得實際 Model Placement 與 RSM FileName。
            world =
                RagnarokResourceLoader.LoadResourceFile(
                    rswPath,
                    mapData
                );

            if (world == null ||
                world.Models == null ||
                world.Models.Count == 0)
            {
                Debug.LogError(
                    "[RuneTown Map Import] RSW contains no models."
                );
                return;
            }

            var mapRoot =
                GetOrCreateGeneratedMapRoot(
                    mapData.name
                );

            const string testRootName =
                "RealModels10Test";

            var existingTestRoot =
                mapRoot.transform.Find(
                    testRootName
                );

            if (existingTestRoot != null)
            {
                Object.DestroyImmediate(
                    existingTestRoot.gameObject
                );
            }

            var testRoot =
                new GameObject(
                    testRootName
                );

            testRoot.transform.SetParent(
                mapRoot.transform,
                false
            );

            // 修改原因：沿用 RebuildClient 的 RSW Resource Root InitialSize Offset。
            testRoot.transform.localPosition =
                new Vector3(
                    mapData.InitialSize.x,
                    0f,
                    mapData.InitialSize.y
                );

            var dataDirectory =
                RagnarokDirectory.GetRagnarokDataDirectory;

            var processedFiles =
                new HashSet<string>(
                    System.StringComparer.OrdinalIgnoreCase
                );

            // 修改原因：第一個 RSM 已於單一模型測試生成過，
            // 先排除它，避免目前 Loader 重複 CreateAsset 時產生資產衝突。
            if (!string.IsNullOrWhiteSpace(world.Models[0].FileName))
            {
                processedFiles.Add(
                    world.Models[0].FileName
                );
            }

            int generatedCount = 0;

            for (int i = 1;
                 i < world.Models.Count && generatedCount < 10;
                 i++)
            {
                var placement =
                    world.Models[i];

                if (placement == null ||
                    string.IsNullOrWhiteSpace(placement.FileName))
                {
                    continue;
                }

                // 修改原因：同一個 RSM 可能在 RSW 中被放置多次；
                // 這一階段先測試 10 種不同模型，而不是重複建立相同資產。
                if (!processedFiles.Add(placement.FileName))
                {
                    continue;
                }

                var modelPath =
                    Path.Combine(
                        dataDirectory,
                        "model",
                        placement.FileName
                    )
                    .Replace("\\", "/");

                if (!File.Exists(modelPath))
                {
                    Debug.LogWarning(
                        $"[RuneTown Map Import] Skip missing RSM: {modelPath}"
                    );
                    continue;
                }

                var savePath =
                    DirectoryHelper.GetRelativeDirectory(
                        dataDirectory,
                        Path.GetDirectoryName(modelPath)
                    );

                RagnarokModelLoader modelLoader = null;

                try
                {
                    // 修改原因：每個 RSM 使用獨立 Loader，
                    // 避免不同模型共用 BinaryReader / FileStream 狀態。
                    modelLoader =
                        new RagnarokModelLoader();

                    modelLoader.LoadModel(
                        modelPath,
                        savePath
                    );

                    var realModel =
                        modelLoader.Compile();

                    if (realModel == null)
                    {
                        Debug.LogError(
                            $"[RuneTown Map Import] RSM Compile returned null: {placement.FileName}"
                        );
                        return;
                    }

                    realModel.name =
                        $"RSM_RealModel_{i}_{Path.GetFileNameWithoutExtension(placement.FileName)}";

                    realModel.transform.SetParent(
                        testRoot.transform,
                        false
                    );

                    // 修改原因：沿用已驗證成功的 RSW Position 轉換。
                    realModel.transform.localPosition =
                        new Vector3(
                            placement.Position.x / 5f,
                            -placement.Position.y / 5f,
                            placement.Position.z / 5f
                        )
                        + new Vector3(
                            0f,
                            0.01f,
                            0f
                        );

                    // 修改原因：沿用 RebuildClient 的 RSW Rotation 組合順序。
                    var rz =
                        Matrix4x4.Rotate(
                            Quaternion.Euler(
                                0f,
                                0f,
                                -placement.Rotation.z
                            )
                        );

                    var rx =
                        Matrix4x4.Rotate(
                            Quaternion.Euler(
                                -placement.Rotation.x,
                                0f,
                                0f
                            )
                        );

                    var ry =
                        Matrix4x4.Rotate(
                            Quaternion.Euler(
                                0f,
                                placement.Rotation.y,
                                0f
                            )
                        );

                    var finalRotation =
                        rz * rx * ry;

                    realModel.transform.localRotation =
                        finalRotation.rotation;

                    realModel.transform.localScale =
                        placement.Scale;

                    generatedCount++;

                    Debug.Log(
                        $"[RuneTown Map Import] Real RSM {generatedCount}/10: " +
                        $"RSW Index={i}, " +
                        $"File={placement.FileName}, " +
                        $"Local Pos={realModel.transform.localPosition}, " +
                        $"Scale={placement.Scale}"
                    );
                }
                finally
                {
                    modelLoader?.Dispose();
                }
            }

            Selection.activeGameObject =
                testRoot;

            SceneView.FrameLastActiveSceneView();

            Debug.Log(
                $"[RuneTown Map Import] RSW 10 Real Models test finished: " +
                $"Generated={generatedCount}"
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError(
                $"[RuneTown Map Import] RSW 10 Real Models test failed:\n{ex}"
            );
        }
        finally
        {
            if (world != null)
            {
                Object.DestroyImmediate(
                    world
                );
            }
        }
    }

    [MenuItem("RuneTown/Map Import/Generate GAT Walk Visual Test")]
    public static void GenerateGatWalkVisualTest()
    {
        // 修改原因：將已匯入的 GAT Walk Data 轉成單一 Texture2D，
        // 以 1 Pixel = 1 GAT Cell 的方式驗證 Walkable、Water、
        // Snipable 與不可行走區域，避免建立數萬個測試 GameObject。
        const string walkDataPath =
            "Assets/RuneTown/MapImport/Generated/walkdata/gl_chyard_walkdata.asset";

        const string outputDirectory =
            "Assets/RuneTown/MapImport/Generated/visual";

        const string outputPath =
            outputDirectory + "/gl_chyard_walk_visual.png";

        var walkData =
            AssetDatabase.LoadAssetAtPath<RagnarokWalkData>(
                walkDataPath
            );

        if (walkData == null)
        {
            Debug.LogError(
                $"[RuneTown Map Import] Walk data not found: {walkDataPath}"
            );
            return;
        }

        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        // 修改原因：直接按照 GAT Cell 數量建立同尺寸 Texture，
        // 讓每個 Pixel 精確對應一個 Walk Cell。
        var texture =
            new Texture2D(
                walkData.Width,
                walkData.Height,
                TextureFormat.RGBA32,
                false
            );

        texture.name = "gl_chyard_walk_visual";
        texture.filterMode = FilterMode.Point;

        var pixels =
            new Color32[
                walkData.Width *
                walkData.Height
            ];

        int walkableCount = 0;
        int waterCount = 0;
        int snipableOnlyCount = 0;
        int blockedCount = 0;

        for (int y = 0; y < walkData.Height; y++)
        {
            for (int x = 0; x < walkData.Width; x++)
            {
                var cell =
                    walkData.Cell(x, y);

                Color32 color;

                // 修改原因：Water Cell 可能同時具有 Walkable Flag，
                // 因此必須優先判斷 Water，避免被一般 Walkable 顏色蓋掉。
                if ((cell.Type & CellType.Water) != 0)
                {
                    color = new Color32(
                        0,
                        100,
                        255,
                        255
                    );

                    waterCount++;
                }
                else if ((cell.Type & CellType.Walkable) != 0)
                {
                    color = new Color32(
                        0,
                        220,
                        0,
                        255
                    );

                    walkableCount++;
                }
                else if ((cell.Type & CellType.Snipable) != 0)
                {
                    color = new Color32(
                        255,
                        220,
                        0,
                        255
                    );

                    snipableOnlyCount++;
                }
                else
                {
                    color = new Color32(
                        220,
                        0,
                        0,
                        255
                    );

                    blockedCount++;
                }

                pixels[
                    x + y * walkData.Width
                ] = color;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        // 修改原因：將測試結果輸出成 PNG Asset，
        // 方便直接在 Unity Project 視窗查看並與 RebuildClient 結果比對。
        File.WriteAllBytes(
            outputPath,
            texture.EncodeToPNG()
        );

        Object.DestroyImmediate(texture);

        AssetDatabase.Refresh();

        // 修改原因：Walk Map 代表離散 Cell 資料，
        // 使用 Point Filter 可以避免 Unity 插值造成格子邊界模糊。
        var textureImporter =
            AssetImporter.GetAtPath(outputPath)
            as TextureImporter;

        if (textureImporter != null)
        {
            textureImporter.filterMode =
                FilterMode.Point;

            textureImporter.mipmapEnabled =
                false;

            textureImporter.textureCompression =
                TextureImporterCompression.Uncompressed;

            textureImporter.SaveAndReimport();
        }

        var generatedTexture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                outputPath
            );

        Selection.activeObject =
            generatedTexture;

        Debug.Log(
            $"[RuneTown Map Import] GAT Walk Visual generated: " +
            $"Size={walkData.Width}x{walkData.Height}, " +
            $"Walkable={walkableCount}, " +
            $"Water={waterCount}, " +
            $"SnipableOnly={snipableOnlyCount}, " +
            $"Blocked={blockedCount}, " +
            $"Output={outputPath}"
        );
    }

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

    [MenuItem("RuneTown/Map Import/Import GAT Test")]
    public static void ImportGatTest()
    {
        // 修改原因：建立 RuneTown 第一版 Ragnarok GAT 匯入測試入口，
        // 驗證 .gat 可以成功解析成 RoMapData 與 RagnarokWalkData，
        // 並正式接入 RSW 的 Water.Level 作為水位判斷依據。
        var gatPath = EditorUtility.OpenFilePanel(
            "Select Ragnarok GAT",
            "",
            "gat"
        );

        if (string.IsNullOrWhiteSpace(gatPath))
            return;

        var rswPath = EditorUtility.OpenFilePanel(
            "Select Matching Ragnarok RSW",
            Path.GetDirectoryName(gatPath),
            "rsw"
        );

        if (string.IsNullOrWhiteSpace(rswPath))
            return;

        RagnarokWorld world = null;

        try
        {
            const string mapAssetPath =
                "Assets/RuneTown/MapImport/Generated/gl_chyard.asset";

            var mapData =
                AssetDatabase.LoadAssetAtPath<RoMapData>(
                    mapAssetPath
                );

            if (mapData == null)
            {
                Debug.LogError(
                    $"[RuneTown Map Import] Map asset not found: {mapAssetPath}"
                );
                return;
            }

            // 修改原因：GAT 的 Water Cell 判斷需要使用 RSW 內實際 Water.Level，
            // 不再沿用測試階段固定 waterHeight = 0 的做法。
            world =
                RagnarokResourceLoader.LoadResourceFile(
                    rswPath,
                    mapData
                );

            if (world == null)
            {
                Debug.LogError(
                    "[RuneTown Map Import] GAT import failed: RagnarokWorld is null."
                );
                return;
            }

            // 修改原因：沿用 RebuildClient 原始 Map Import 流程計算 GAT 使用的水位，
            // 不能直接把 RSW Water.Level 傳入 LoadWalkData。
            var waterHeight =
                world.Water != null
                    ? -world.Water.Level
                      + (world.Water.WaveHeight / 5f)
                      - 0.01f
                    : 0f;

            var importer =
                new RagnarokWalkableDataImporter();

            var walkMapData =
                importer.LoadWalkData(
                    gatPath,
                    waterHeight
                );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 修改原因：輸出實際使用的 RSW Water.Level，
            // 方便確認 GAT Water Cell 是依正確水位重新判斷。
            Debug.Log(
                $"[RuneTown Map Import] GAT import success: " +
                $"{Path.GetFileNameWithoutExtension(gatPath)}, " +
                $"Size={walkMapData.Width}x{walkMapData.Height}, " +
                $"WaterHeight={waterHeight}, " +
                $"WalkData={(walkMapData.WalkCellData != null ? "Created" : "Missing")}"
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError(
                $"[RuneTown Map Import] GAT import failed:\n{ex}"
            );
        }
        finally
        {
            if (world != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    world
                );
            }
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

        var mapRoot =
            GetOrCreateGeneratedMapRoot(
                mapData.name
            );

        const string groundName =
            "Ground";

        // 修改原因：重複生成完整 GND 時只清除既有 Ground，
        // 保留同一 GeneratedMapRoot 下的 Models 等其他測試內容。
        var existingGround =
            mapRoot.transform.Find(
                groundName
            );

        if (existingGround != null)
        {
            Object.DestroyImmediate(
                existingGround.gameObject
            );
        }

        // 修改原因：Ground 掛在 GeneratedMapRoot 下並保持 localPosition 為零，
        // 之後只需移動 Root 即可整張地圖一起移動。
        var parentObject =
            new GameObject(
                groundName
            );

        parentObject.transform.SetParent(
            mapRoot.transform,
            false
        );

        var material =
            new Material(
                Shader.Find("Universal Render Pipeline/Lit")
            );

        material.name =
            "gl_chyard_FullGndMaterial";

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
                // 讓 Chunk 在 Ground 內維持原始 GND Cell 座標。
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

        Selection.activeGameObject =
            mapRoot;

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

    private static GameObject GetOrCreateGeneratedMapRoot(string mapName)
    {
        // 修改原因：統一管理同一張 RO 地圖產生的 Ground / Models / Lights / Effects 等內容，
        // 讓整張地圖可以透過單一 Root 移動，而不破壞內部 GND / RSW 相對座標。
        var rootName =
            $"{mapName}_GeneratedMapRoot";

        var existingRoot =
            GameObject.Find(rootName);

        if (existingRoot != null)
            return existingRoot;

        var root =
            new GameObject(rootName);

        // 修改原因：Root 的世界位置由開發者在 Scene 中自行調整，
        // 不將目前測試用的 350 寫死進 importer。
        root.transform.position =
            Vector3.zero;

        return root;
    }

    [MenuItem("RuneTown/Map Import/Generate Gl Chyard Scene Test")]
    public static void GenerateGlChyardSceneTest()
    {
        // 修改原因：將已驗證成功的完整 gl_chyard GND，
        // 正式產生到獨立 Unity Scene，而不是繼續依附 TownPrototype 測試。
        const string mapAssetPath =
            "Assets/RuneTown/MapImport/Generated/gl_chyard.asset";

        const string sceneFolder =
            "Assets/Scenes/RO";

        const string scenePath =
            "Assets/Scenes/RO/gl_chyard.unity";

        var mapData =
            AssetDatabase.LoadAssetAtPath<RoMapData>(
                mapAssetPath
            );

        if (mapData == null)
        {
            Debug.LogError(
                $"[RuneTown Map Import] Map asset not found: {mapAssetPath}"
            );
            return;
        }

        try
        {
            // 修改原因：正式地圖使用獨立 Scene，
            // 避免與 TownPrototype 內既有測試物件互相干擾。
            var scene =
                UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                    UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                    UnityEditor.SceneManagement.NewSceneMode.Single
                );

            if (!Directory.Exists(sceneFolder))
            {
                Directory.CreateDirectory(
                    sceneFolder
                );

                AssetDatabase.Refresh();
            }

            var mapRoot =
                new GameObject(
                    $"{mapData.name}_GeneratedMapRoot"
                );

            mapRoot.transform.position =
                Vector3.zero;

            var groundObject =
                new GameObject(
                    "Ground"
                );

            groundObject.transform.SetParent(
                mapRoot.transform,
                false
            );

            const int chunkSize = 16;
            const float tileSize = 2f;

            int chunkCountX =
                mapData.Width / chunkSize;

            int chunkCountY =
                mapData.Height / chunkSize;

            // 修改原因：第一階段沿用目前已驗證成功的 GND 測試 Material，
            // 先確認完整地形能在獨立 Scene 中建立、儲存及重新開啟。
            var material =
                new Material(
                    Shader.Find(
                        "Universal Render Pipeline/Lit"
                    )
                );

            material.name =
                "gl_chyard_FullGndMaterial";

            int generatedChunkCount = 0;

            for (int chunkY = 0;
                 chunkY < chunkCountY;
                 chunkY++)
            {
                for (int chunkX = 0;
                     chunkX < chunkCountX;
                     chunkX++)
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
                        groundObject.transform,
                        false
                    );

                    // 修改原因：沿用已驗證的 RebuildClient GND Chunk 座標規則。
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

            // 修改原因：將產生完成的古城地形正式存成獨立 Unity Scene。
            bool saved =
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                    scene,
                    scenePath
                );

            if (!saved)
            {
                Debug.LogError(
                    $"[RuneTown Map Import] Failed to save scene: {scenePath}"
                );
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject =
                mapRoot;

            SceneView.FrameLastActiveSceneView();

            Debug.Log(
                $"[RuneTown Map Import] gl_chyard scene generated successfully: " +
                $"Scene={scenePath}, " +
                $"Size={mapData.Width}x{mapData.Height}, " +
                $"Chunks={chunkCountX}x{chunkCountY}, " +
                $"Total={generatedChunkCount}"
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError(
                $"[RuneTown Map Import] Generate gl_chyard scene failed:\n{ex}"
            );
        }
    }

    [MenuItem("RuneTown/Map Import/Generate Water Test")]
    public static void GenerateWaterTest()
    {
        // 修改原因：先做最小版 Water 測試，只驗證 RSW 的 Water.Level
        // 能否正確在 gl_chyard 地圖上生成一個可見水面。
        var rswPath = EditorUtility.OpenFilePanel(
            "Select Ragnarok RSW",
            "",
            "rsw"
        );

        if (string.IsNullOrWhiteSpace(rswPath))
            return;

        RagnarokWorld world = null;

        try
        {
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

            // 修改原因：沿用已完成的 RSW parser，直接取得水面資料。
            world =
                RagnarokResourceLoader.LoadResourceFile(
                    rswPath,
                    mapData
                );

            if (world == null)
            {
                Debug.LogError(
                    "[RuneTown Map Import] Water test failed: RagnarokWorld is null."
                );
                return;
            }

            if (world.Water == null)
            {
                Debug.LogWarning(
                    "[RuneTown Map Import] Water test skipped: RSW has no water data."
                );
                return;
            }

            var mapRoot =
                GetOrCreateGeneratedMapRoot(
                    mapData.name
                );

            const string waterRootName = "Water";

            var existingWater =
                mapRoot.transform.Find(waterRootName);

            if (existingWater != null)
            {
                // 修改原因：重複測試時先移除舊水面，避免 Scene 裡疊出多層水面。
                UnityEngine.Object.DestroyImmediate(
                    existingWater.gameObject
                );
            }

            var waterObject =
                GameObject.CreatePrimitive(
                    PrimitiveType.Plane
                );

            waterObject.name = waterRootName;

            waterObject.transform.SetParent(
                mapRoot.transform,
                false
            );

            // 修改原因：Ground 目前範圍是從 (0,0) 到 (InitialSize*2)，
            // 所以水面中心要放在地圖中央。
            waterObject.transform.localPosition =
                new Vector3(
                    mapData.InitialSize.x,
                    -world.Water.Level,
                    mapData.InitialSize.y
                );

            waterObject.transform.localRotation =
                Quaternion.identity;

            // 修改原因：Unity Plane 預設大小為 10x10，
            // 這裡依 RO 地圖大小放大到覆蓋整張地圖。
            waterObject.transform.localScale =
                new Vector3(
                    mapData.InitialSize.x / 5f,
                    1f,
                    mapData.InitialSize.y / 5f
                );

            var shader =
                Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
            {
                Debug.LogError(
                    "[RuneTown Map Import] Water test failed: URP/Lit shader not found."
                );
                return;
            }

            var material =
                new Material(shader);

            material.name = "gl_chyard_WaterTestMaterial";
            material.SetColor("_BaseColor", new Color(0.2f, 0.5f, 0.9f, 0.7f));

            // 修改原因：先用簡單藍色材質把水面顯示出來，
            // 目前先驗證位置與高度，不先處理真正水波動畫。
            var renderer =
                waterObject.GetComponent<MeshRenderer>();

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // 修改原因：水面測試不需要碰撞，先移除 Collider 避免干擾。
            var collider =
                waterObject.GetComponent<Collider>();

            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            Selection.activeGameObject = waterObject;
            SceneView.FrameLastActiveSceneView();

            Debug.Log(
                $"[RuneTown Map Import] Water test generated: " +
                $"Level={world.Water.Level}, " +
                $"Type={world.Water.Type}, " +
                $"WaveHeight={world.Water.WaveHeight}, " +
                $"WaveSpeed={world.Water.WaveSpeed}, " +
                $"WavePitch={world.Water.WavePitch}, " +
                $"AnimSpeed={world.Water.AnimSpeed}"
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError(
                $"[RuneTown Map Import] Water test failed:\n{ex}"
            );
        }
        finally
        {
            if (world != null)
            {
                UnityEngine.Object.DestroyImmediate(world);
            }
        }
    }

    [MenuItem("RuneTown/Map Import/Debug GAT Water Heights")]
    public static void DebugGatWaterHeights()
    {
        // 修改原因：分析 GAT 中所有 Water Cell 的實際高度，
        // 用來核對 RSW Water.Level 轉成 Unity Y 後是否合理。
        const string walkDataPath =
            "Assets/RuneTown/MapImport/Generated/walkdata/gl_chyard_walkdata.asset";

        var walkData =
            AssetDatabase.LoadAssetAtPath<RagnarokWalkData>(
                walkDataPath
            );

        if (walkData == null)
        {
            Debug.LogError(
                $"[RuneTown Map Import] WalkData not found: {walkDataPath}"
            );
            return;
        }

        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;
        float totalHeight = 0f;
        int waterCount = 0;

        for (int i = 0; i < walkData.Cells.Length; i++)
        {
            var cell =
                walkData.Cells[i];

            if ((cell.Type & CellType.Water) != CellType.Water)
                continue;

            var height =
                cell.AverageHeights;

            minHeight =
                Mathf.Min(
                    minHeight,
                    height
                );

            maxHeight =
                Mathf.Max(
                    maxHeight,
                    height
                );

            totalHeight +=
                height;

            waterCount++;
        }

        if (waterCount == 0)
        {
            Debug.LogWarning(
                "[RuneTown Map Import] No Water cells found in GAT."
            );
            return;
        }

        var averageHeight =
            totalHeight / waterCount;

        Debug.Log(
            $"[RuneTown Map Import] GAT Water Heights: " +
            $"Count={waterCount}, " +
            $"Min={minHeight}, " +
            $"Max={maxHeight}, " +
            $"Average={averageHeight}"
        );
    }

}