using System.Text;
using UnityEditor;
using UnityEngine;

public class MonsterCreationWindow : EditorWindow
{
    private string monsterName = string.Empty;
    private string monsterId = string.Empty;
    private string displayName = string.Empty;

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

        // 修改原因：保存遊戲畫面或 Quest 等系統顯示給玩家看的魔物名稱，
        // 並允許與程式內部使用的 Monster Name 不同。
        displayName = EditorGUILayout.TextField(
            "Display Name",
            displayName);
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