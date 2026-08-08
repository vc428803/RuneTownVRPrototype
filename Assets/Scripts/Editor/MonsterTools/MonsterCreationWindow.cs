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