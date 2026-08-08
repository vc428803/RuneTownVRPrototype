using UnityEditor;
using UnityEngine;

public class MonsterCreationWindow : EditorWindow
{
    // 修改原因：提供統一的魔物建立工具入口，
    // 後續新增魔物時不需要重複手動建立 Animator、Prefab 等設定。
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

        EditorGUILayout.HelpBox(
            "Monster creation automation will be added here.",
            MessageType.Info);
    }
}