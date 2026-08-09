using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;

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

        if (GUILayout.Button("Create Animator Controller"))
        {
            CreateAnimatorController();
        }

        if (GUILayout.Button("Create Monster Prefab"))
        {
            CreateMonsterPrefab();
        }
    }

    private void CreateMonsterPrefab()
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

        string prefabFolderPath =
            $"{rootPath}/Prefabs";

        string prefabPath =
            $"{prefabFolderPath}/Monster_{monsterName}.prefab";

        string definitionPath =
            $"{rootPath}/Data/{monsterName}Definition.asset";

        string controllerPath =
            $"{rootPath}/Data/{monsterName}.controller";

        string idleFramesPath =
            $"{rootPath}/Idle/Frames";

        if (!AssetDatabase.IsValidFolder(prefabFolderPath))
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Prefabs folder does not exist.",
                "OK");

            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(
                prefabPath) != null)
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Monster Prefab already exists.",
                "OK");

            return;
        }

        MonsterDefinition definition =
            AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
                definitionPath);

        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(
                controllerPath);

        if (definition == null || controller == null)
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Monster Definition and Animator Controller must exist first.",
                "OK");

            return;
        }

        string[] idleSpriteGuids =
            AssetDatabase.FindAssets(
                "t:Sprite",
                new[] { idleFramesPath });

        if (idleSpriteGuids.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "No Idle sprite was found.",
                "OK");

            return;
        }

        System.Array.Sort(
            idleSpriteGuids,
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

        string firstIdleSpritePath =
            AssetDatabase.GUIDToAssetPath(
                idleSpriteGuids[0]);

        Sprite firstIdleSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(
                firstIdleSpritePath);

        GameObject monsterObject =
            new GameObject(
                $"Monster_{monsterName}");

        // 修改原因：建立魔物 Prefab 的基本視覺元件，
        // 並以第一張 Idle Sprite 作為初始顯示圖片。
        SpriteRenderer spriteRenderer =
            monsterObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite =
            firstIdleSprite;

        Animator animator =
            monsterObject.AddComponent<Animator>();

        animator.runtimeAnimatorController =
            controller;

        // 修改原因：所有標準魔物共用同一套玩家偵測、
        // 追蹤與攻擊邏輯。
        MonsterPlayerDetector detector =
            monsterObject.AddComponent<MonsterPlayerDetector>();

        // 修改原因：自動將 Prefab 自己的 Animator
        // 指定給 MonsterPlayerDetector。
        SerializedObject detectorObject =
            new SerializedObject(detector);

        detectorObject.FindProperty(
            "monsterAnimator").objectReferenceValue =
            animator;

        detectorObject.ApplyModifiedPropertiesWithoutUndo();

        MonsterIdentity identity =
            monsterObject.AddComponent<MonsterIdentity>();

        // 修改原因：自動指定對應的 MonsterDefinition，
        // 讓 MonsterManager 可依 MonsterId 辨識魔物種類。
        SerializedObject identityObject =
            new SerializedObject(identity);

        identityObject.FindProperty(
            "monsterDefinition").objectReferenceValue =
            definition;

        identityObject.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab =
            PrefabUtility.SaveAsPrefabAsset(
                monsterObject,
                prefabPath);

        DestroyImmediate(monsterObject);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = prefab;

        Debug.Log(
            $"Monster Creation Tool：已建立 Monster_{monsterName} Prefab。");
    }

    private void CreateAnimatorController()
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

        string idleAnimationPath =
            $"{rootPath}/Idle/Animations/{monsterName}_Idle.anim";

        string walkAnimationPath =
            $"{rootPath}/Walk/Animations/{monsterName}_Walk.anim";

        string attackAnimationPath =
            $"{rootPath}/Attack/Animations/{monsterName}_Attack.anim";

        string controllerPath =
            $"{rootPath}/Data/{monsterName}.controller";

        AnimationClip idleClip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(
                idleAnimationPath);

        AnimationClip walkClip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(
                walkAnimationPath);

        AnimationClip attackClip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(
                attackAnimationPath);

        if (idleClip == null ||
            walkClip == null ||
            attackClip == null)
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Idle, Walk and Attack animations must exist first.",
                "OK");

            return;
        }

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(
                controllerPath) != null)
        {
            EditorUtility.DisplayDialog(
                "Monster Creation Tool",
                "Animator Controller already exists.",
                "OK");

            return;
        }

        AnimatorController controller =
            AnimatorController.CreateAnimatorControllerAtPath(
                controllerPath);

        // 修改原因：自動建立所有共用魔物 Animator 需要的參數，
        // 避免每新增一種魔物都手動新增相同參數。
        controller.AddParameter(
            "IsWalking",
            AnimatorControllerParameterType.Bool);

        controller.AddParameter(
            "Attack",
            AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine =
            controller.layers[0].stateMachine;

        // 修改原因：自動建立標準 Idle、Walk、Attack 狀態，
        // 避免每新增一種魔物都手動拖入 Animation Clip。
        AnimatorState idleState =
            stateMachine.AddState(
                $"{monsterName}_Idle");

        AnimatorState walkState =
            stateMachine.AddState(
                $"{monsterName}_Walk");

        AnimatorState attackState =
            stateMachine.AddState(
                $"{monsterName}_Attack");

        idleState.motion = idleClip;
        walkState.motion = walkClip;
        attackState.motion = attackClip;

        // 修改原因：自動建立 Idle → Walk 切換，
        // 當 IsWalking 為 true 時立即進入 Walk。
        AnimatorStateTransition idleToWalk =
            idleState.AddTransition(walkState);

        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0f;

        idleToWalk.AddCondition(
            AnimatorConditionMode.If,
            0f,
            "IsWalking");

        // 修改原因：自動建立 Walk → Idle 切換，
        // 當 IsWalking 為 false 時立即回到 Idle。
        AnimatorStateTransition walkToIdle =
            walkState.AddTransition(idleState);

        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0f;

        walkToIdle.AddCondition(
            AnimatorConditionMode.IfNot,
            0f,
            "IsWalking");

        // 修改原因：自動建立 Idle → Attack 切換，
        // 當 Attack Trigger 被觸發時立即播放攻擊動畫。
        AnimatorStateTransition idleToAttack =
            idleState.AddTransition(attackState);

        idleToAttack.hasExitTime = false;
        idleToAttack.duration = 0f;

        idleToAttack.AddCondition(
            AnimatorConditionMode.If,
            0f,
            "Attack");

        // 修改原因：Attack 動畫播放完成後自動回到 Idle，
        // 不需要額外 Trigger 或 Bool 控制回程。
        AnimatorStateTransition attackToIdle =
            attackState.AddTransition(idleState);

        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 1f;
        attackToIdle.duration = 0f;

        // 修改原因：魔物生成後預設先進入 Idle 狀態。
        stateMachine.defaultState = idleState;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = controller;

        Debug.Log(
            $"Monster Creation Tool：已建立 {monsterName} Animator Controller。");
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