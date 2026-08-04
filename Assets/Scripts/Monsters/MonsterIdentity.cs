using UnityEngine;

public class MonsterIdentity : MonoBehaviour
{
    [SerializeField]
    private MonsterDefinition monsterDefinition;

    // 修改原因：保存負責管理此魔物的 MonsterManager，
    // 讓魔物被銷毀時可以主動取消註冊。
    private MonsterManager monsterManager;

    public string MonsterId =>
        monsterDefinition != null
            ? monsterDefinition.MonsterId
            : string.Empty;

    public string DisplayName =>
        monsterDefinition != null
            ? monsterDefinition.DisplayName
            : string.Empty;

    public MonsterDefinition Definition =>
        monsterDefinition;

    public void Initialize(MonsterManager manager)
    {
        // 修改原因：魔物生成後由 Spawner 傳入管理器，
        // 避免每隻魔物自行搜尋場景中的 MonsterManager。
        monsterManager = manager;
    }

    private void OnDestroy()
    {
        if (monsterManager == null)
        {
            return;
        }

        // 修改原因：魔物被銷毀時同步移除管理器中的登記，
        // 避免 ActiveMonsterCount 保留已不存在的魔物。
        monsterManager.UnregisterMonster(this);
    }
}