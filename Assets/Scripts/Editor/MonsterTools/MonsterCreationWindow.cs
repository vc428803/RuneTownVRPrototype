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

        if (GUILayout.Button("Create Idle Animation"))
        {
            CreateIdleAnimation();
        }

        if (GUILayout.Button("Create Walk Animation"))
        {
            CreateWalkAnimation();
        }

        if (GUILayout.Button("Create Attack Animation"))
        {
            CreateAttackAnimation();
        }
    }

    private void CreateAttackAnimation()
    {
        if (string.IsNullOrWhiteSpace(monsterName))
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Monster Name cannot be empty.",
                "OK");

            return;
        }

        string framesFolderPath =
            $"Assets/Ragnarok/Monsters/{monsterName}/Attack/Frames";

        string animationFolderPath =
            $"Assets/Ragnarok/Monsters/{monsterName}/Attack/Animations";

        if (!AssetDatabase.IsValidFolder(framesFolderPath))
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Attack Frames folder does not exist.",
                "OK");

            return;
        }

        if (!AssetDatabase.IsValidFolder(animationFolderPath))
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Attack Animations folder does not exist.",
                "OK");

            return;
        }

        string animationPath =
            $"{animationFolderPath}/{monsterName}_Attack.anim";

        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(
                animationPath) != null)
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Attack Animation already exists.",
                "OK");

            return;
        }

        string[] spriteGuids =
            AssetDatabase.FindAssets(
                "t:Sprite",
                new[] { framesFolderPath });

        if (spriteGuids.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "No Attack sprites were found.",
                "OK");

            return;
        }

        // 修改原因：依 Asset Path 排序逐幀圖片，
        // 避免 AssetDatabase 回傳順序不固定造成攻擊動畫幀順序錯亂。
        System.Array.Sort(
            spriteGuids,
            (a, b) =>
            {
                string pathA =
                    AssetDatabase.GUIDToAssetPath(a);

                string pathB =
                    AssetDatabase.GUIDToAssetPath(b);

                return string.CompareOrdinal(
                    pathA,
                    pathB);
            });

        AnimationClip clip =
            new AnimationClip();

        clip.frameRate = 12f;

        ObjectReferenceKeyframe[] keyframes =
            new ObjectReferenceKeyframe[spriteGuids.Length];

        for (int i = 0; i < spriteGuids.Length; i++)
        {
            string spritePath =
                AssetDatabase.GUIDToAssetPath(
                    spriteGuids[i]);

            Sprite sprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    spritePath);

            keyframes[i] =
                new ObjectReferenceKeyframe
                {
                    time = i / clip.frameRate,
                    value = sprite
                };
        }

        EditorCurveBinding spriteBinding =
            new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = string.Empty,
                propertyName = "m_Sprite"
            };

        AnimationUtility.SetObjectReferenceCurve(
            clip,
            spriteBinding,
            keyframes);

        // 修改原因：Attack 動畫只播放一次，
        // 不設定 Loop Time，讓 Animator 播放完成後返回 Idle。
        AnimationClipSettings clipSettings =
            AnimationUtility.GetAnimationClipSettings(
                clip);

        clipSettings.loopTime = false;

        AnimationUtility.SetAnimationClipSettings(
            clip,
            clipSettings);

        AssetDatabase.CreateAsset(
            clip,
            animationPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = clip;

        Debug.Log(
            $"Monster Creation Tool：已建立 " +
            $"{monsterName}_Attack.anim，" +
            $"共 {spriteGuids.Length} 幀。");
    }

    private void CreateWalkAnimation()
    {
        if (string.IsNullOrWhiteSpace(monsterName))
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Monster Name cannot be empty.",
                "OK");

            return;
        }

        string framesFolderPath =
            $"Assets/Ragnarok/Monsters/{monsterName}/Walk/Frames";

        string animationFolderPath =
            $"Assets/Ragnarok/Monsters/{monsterName}/Walk/Animations";

        if (!AssetDatabase.IsValidFolder(framesFolderPath))
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Walk Frames folder does not exist.",
                "OK");

            return;
        }

        if (!AssetDatabase.IsValidFolder(animationFolderPath))
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Walk Animations folder does not exist.",
                "OK");

            return;
        }

        string animationPath =
            $"{animationFolderPath}/{monsterName}_Walk.anim";

        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(
                animationPath) != null)
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Walk Animation already exists.",
                "OK");

            return;
        }

        string[] spriteGuids =
            AssetDatabase.FindAssets(
                "t:Sprite",
                new[] { framesFolderPath });

        if (spriteGuids.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "No Walk sprites were found.",
                "OK");

            return;
        }

        // 修改原因：依 Asset Path 排序逐幀圖片，
        // 避免 AssetDatabase 回傳順序不固定造成動畫幀順序錯亂。
        System.Array.Sort(
            spriteGuids,
            (a, b) =>
            {
                string pathA =
                    AssetDatabase.GUIDToAssetPath(a);

                string pathB =
                    AssetDatabase.GUIDToAssetPath(b);

                return string.CompareOrdinal(
                    pathA,
                    pathB);
            });

        AnimationClip clip =
            new AnimationClip();

        clip.frameRate = 12f;

        ObjectReferenceKeyframe[] keyframes =
            new ObjectReferenceKeyframe[spriteGuids.Length];

        for (int i = 0; i < spriteGuids.Length; i++)
        {
            string spritePath =
                AssetDatabase.GUIDToAssetPath(
                    spriteGuids[i]);

            Sprite sprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    spritePath);

            keyframes[i] =
                new ObjectReferenceKeyframe
                {
                    time = i / clip.frameRate,
                    value = sprite
                };
        }

        EditorCurveBinding spriteBinding =
            new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = string.Empty,
                propertyName = "m_Sprite"
            };

        AnimationUtility.SetObjectReferenceCurve(
            clip,
            spriteBinding,
            keyframes);

        // 修改原因：Walk 動畫需要循環播放，
        // 建立時直接設定 Loop Time。
        AnimationClipSettings clipSettings =
            AnimationUtility.GetAnimationClipSettings(
                clip);

        clipSettings.loopTime = true;

        AnimationUtility.SetAnimationClipSettings(
            clip,
            clipSettings);

        AssetDatabase.CreateAsset(
            clip,
            animationPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = clip;

        Debug.Log(
            $"Monster Creation Tool：已建立 " +
            $"{monsterName}_Walk.anim，" +
            $"共 {spriteGuids.Length} 幀。");
    }

    private void CreateIdleAnimation()
    {
        if (string.IsNullOrWhiteSpace(monsterName))
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Monster Name cannot be empty.",
                "OK");

            return;
        }

        string framesFolderPath =
            $"Assets/Ragnarok/Monsters/{monsterName}/Idle/Frames";

        string animationFolderPath =
            $"Assets/Ragnarok/Monsters/{monsterName}/Idle/Animations";

        if (!AssetDatabase.IsValidFolder(framesFolderPath))
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Idle Frames folder does not exist.",
                "OK");

            return;
        }

        if (!AssetDatabase.IsValidFolder(animationFolderPath))
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Idle Animations folder does not exist.",
                "OK");

            return;
        }

        string animationPath =
            $"{animationFolderPath}/{monsterName}_Idle.anim";

        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(
                animationPath) != null)
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Idle Animation already exists.",
                "OK");

            return;
        }

        string[] spriteGuids =
            AssetDatabase.FindAssets(
                "t:Sprite",
                new[] { framesFolderPath });

        if (spriteGuids.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "No Idle sprites were found.",
                "OK");

            return;
        }

        // 修改原因：依 Asset Path 排序逐幀圖片，
        // 避免 AssetDatabase 回傳順序不固定造成動畫幀順序錯亂。
        System.Array.Sort(
            spriteGuids,
            (a, b) =>
            {
                string pathA =
                    AssetDatabase.GUIDToAssetPath(a);

                string pathB =
                    AssetDatabase.GUIDToAssetPath(b);

                return string.CompareOrdinal(
                    pathA,
                    pathB);
            });

        AnimationClip clip =
            new AnimationClip();

        clip.frameRate = 12f;

        ObjectReferenceKeyframe[] keyframes =
            new ObjectReferenceKeyframe[spriteGuids.Length];

        for (int i = 0; i < spriteGuids.Length; i++)
        {
            string spritePath =
                AssetDatabase.GUIDToAssetPath(
                    spriteGuids[i]);

            Sprite sprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    spritePath);

            keyframes[i] =
                new ObjectReferenceKeyframe
                {
                    time = i / clip.frameRate,
                    value = sprite
                };
        }

        EditorCurveBinding spriteBinding =
            new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = string.Empty,
                propertyName = "m_Sprite"
            };

        AnimationUtility.SetObjectReferenceCurve(
            clip,
            spriteBinding,
            keyframes);

        // 修改原因：Idle 動畫需要持續循環播放，
        // 建立時直接設定 Loop Time，避免之後逐隻手動勾選。
        AnimationClipSettings clipSettings =
            AnimationUtility.GetAnimationClipSettings(
                clip);

        clipSettings.loopTime = true;

        AnimationUtility.SetAnimationClipSettings(
            clip,
            clipSettings);

        AssetDatabase.CreateAsset(
            clip,
            animationPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = clip;

        Debug.Log(
            $"Monster Creation Tool：已建立 " +
            $"{monsterName}_Idle.anim，" +
            $"共 {spriteGuids.Length} 幀。");
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