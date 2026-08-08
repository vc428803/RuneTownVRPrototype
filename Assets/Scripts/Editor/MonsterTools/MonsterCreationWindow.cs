using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class MonsterCreationWindow : EditorWindow
{
    private string monsterName = string.Empty;
    private string monsterId = string.Empty;
    private string displayName = string.Empty;

    [MenuItem("RuneTown/Monsters/Monster Creation Tool")]
    public static void ShowWindow()
    {
        GetWindow<MonsterCreationWindow>(
            "Monster Creation Tool");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "RuneTown Monster Creation Tool",
            EditorStyles.boldLabel);

        EditorGUILayout.Space();

        string newMonsterName = EditorGUILayout.TextField(
            "Monster Name",
            monsterName);

        // 修改原因：Monster Name 變更時自動產生系統使用的 Monster Id，
        // 同時先以相同名稱作為 Display Name 的預設值。
        if (newMonsterName != monsterName)
        {
            monsterName = newMonsterName;
            monsterId = ConvertToMonsterId(monsterName);
            displayName = monsterName;
        }

        monsterId = EditorGUILayout.TextField(
            "Monster Id",
            monsterId);

        displayName = EditorGUILayout.TextField(
            "Display Name",
            displayName);

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Folder Structure"))
        {
            CreateFolderStructure();
        }

        if (GUILayout.Button("Create Monster Definition"))
        {
            CreateMonsterDefinition();
        }

        if (GUILayout.Button("Apply Sprite Import Settings"))
        {
            ApplySpriteImportSettings();
        }
    }

    private void ApplySpriteImportSettings()
    {
        if (string.IsNullOrWhiteSpace(monsterName))
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Monster Name cannot be empty.",
                "OK");

            return;
        }

        string rootPath =
            $"Assets/Ragnarok/Monsters/{monsterName}";

        string[] frameFolders =
        {
        $"{rootPath}/Idle/Frames",
        $"{rootPath}/Walk/Frames",
        $"{rootPath}/Attack/Frames",
        $"{rootPath}/Hit/Frames",
        $"{rootPath}/Die/Frames"
    };

        int updatedTextureCount = 0;

        // 修改原因：統一套用魔物逐幀圖片的 Sprite 匯入設定，
        // 避免每新增一個動畫都需要手動設定相同參數。
        foreach (string folderPath in frameFolders)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                continue;
            }

            string[] textureGuids =
                AssetDatabase.FindAssets(
                    "t:Texture2D",
                    new[] { folderPath });

            foreach (string textureGuid in textureGuids)
            {
                string assetPath =
                    AssetDatabase.GUIDToAssetPath(
                        textureGuid);

                TextureImporter importer =
                    AssetImporter.GetAtPath(assetPath)
                        as TextureImporter;

                if (importer == null)
                {
                    continue;
                }

                importer.textureType =
                    TextureImporterType.Sprite;

                importer.spriteImportMode =
                    SpriteImportMode.Single;

                importer.spritePixelsPerUnit = 100f;

                // 修改原因：Unity 6 的 Sprite Mesh Type 需透過
                // TextureImporterSettings 設定，統一使用 Full Rect。
                TextureImporterSettings importerSettings =
                    new TextureImporterSettings();

                importer.ReadTextureSettings(importerSettings);

                importerSettings.spriteMeshType =
                    SpriteMeshType.FullRect;

                importer.SetTextureSettings(importerSettings);

                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;


                importer.wrapMode =
                    TextureWrapMode.Clamp;

                importer.filterMode =
                    FilterMode.Point;

                importer.maxTextureSize = 2048;

                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;

                importer.SaveAndReimport();

                updatedTextureCount++;
            }


        }

        Debug.Log(
            $"Monster Creation Tool：已更新 " +
            $"{updatedTextureCount} 張 Sprite 匯入設定。");
    }

    private void CreateMonsterDefinition()
    {
        if (string.IsNullOrWhiteSpace(monsterName))
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Monster Name cannot be empty.",
                "OK");

            return;
        }

        string dataFolderPath =
            $"Assets/Ragnarok/Monsters/{monsterName}/Data";

        if (!AssetDatabase.IsValidFolder(dataFolderPath))
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Please create the monster folder structure first.",
                "OK");

            return;
        }

        string assetPath =
            $"{dataFolderPath}/{monsterName}Definition.asset";

        if (AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
                assetPath) != null)
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Monster Definition already exists.",
                "OK");

            return;
        }

        MonsterDefinition definition =
            ScriptableObject.CreateInstance<MonsterDefinition>();

        SerializedObject serializedDefinition =
            new SerializedObject(definition);

        // 修改原因：透過 SerializedObject 寫入 private SerializeField，
        // 避免為了 Editor Tool 額外暴露 MonsterDefinition 的 setter。
        serializedDefinition.FindProperty("monsterId").stringValue =
            monsterId;

        serializedDefinition.FindProperty("displayName").stringValue =
            displayName;

        serializedDefinition.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(
            definition,
            assetPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = definition;

        Debug.Log(
            $"Monster Creation Tool：已建立 {monsterName}Definition。");
    }

    private void CreateFolderStructure()
    {
        if (string.IsNullOrWhiteSpace(monsterName))
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Monster Name cannot be empty.",
                "OK");

            return;
        }

        string rootPath =
            $"Assets/Ragnarok/Monsters/{monsterName}";

        string[] folders =
        {
            "Data",
            "Prefabs",
            "Idle/Frames",
            "Idle/Animations",
            "Walk/Frames",
            "Walk/Animations",
            "Attack/Frames",
            "Attack/Animations",
            "Hit/Frames",
            "Hit/Animations",
            "Die/Frames",
            "Die/Animations"
        };

        // 修改原因：自動建立統一的魔物素材結構，
        // 避免每次新增魔物都手動建立相同資料夾。
        foreach (string folder in folders)
        {
            Directory.CreateDirectory(
                Path.Combine(rootPath, folder));
        }

        AssetDatabase.Refresh();

        Debug.Log(
            $"Monster Creation Tool：已建立 {monsterName} 資料夾結構。");
    }

    private static string ConvertToMonsterId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];

            if (char.IsUpper(current))
            {
                if (i > 0)
                {
                    builder.Append('_');
                }

                builder.Append(
                    char.ToLowerInvariant(current));
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }
}